using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Clima.Shared;

public class ConflictException(string message) : Exception(message);
public class NotFoundException(string message) : Exception(message);
public record ServiceInfo(string Name);

/// <summary>Configuración JWT común (Issuer, Audience, firma HS256, expiración, claim de roles "role").</summary>
public static class ClimaExtensions
{
    public static WebApplicationBuilder AddClimaCommon(this WebApplicationBuilder b, string serviceName)
    {
        var key = b.Configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(key) || Encoding.UTF8.GetByteCount(key) < 32)
            throw new InvalidOperationException("Jwt:Key (variable Jwt__Key / Secret) debe tener al menos 32 caracteres.");
        var issuer = b.Configuration["Jwt:Issuer"] ?? "AlertaClimatica.Auth";
        var audience = b.Configuration["Jwt:Audience"] ?? "AlertaClimatica.Client";

        b.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
        {
            o.MapInboundClaims = false; // conserva los nombres del token: sub, name, role, permissions
            o.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true, ValidIssuer = issuer,
                ValidateAudience = true, ValidAudience = audience,
                ValidateLifetime = true, ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
                ClockSkew = TimeSpan.FromSeconds(30),
                RoleClaimType = "role", NameClaimType = "name"
            };
        });
        b.Services.AddAuthorization();
        b.Services.AddHttpContextAccessor();
        b.Services.AddHttpClient();
        b.Services.AddSingleton(new ServiceInfo(serviceName));
        b.Services.AddSingleton<ServiceTokenProvider>();
        b.Services.AddSingleton<AuditClient>();
        b.Services.AddScoped<UpstreamClient>();
        b.Services.Configure<HostOptions>(o => o.ShutdownTimeout = TimeSpan.FromSeconds(30)); // graceful shutdown
        return b;
    }

    public static WebApplication UseClimaCommon(this WebApplication app)
    {
        app.UseClimaCommonPipeline();
        app.MapGet("/health", () => Results.Ok(new { estado = "ok" })).AllowAnonymous();
        return app;
    }

    /// <summary>Manejo de errores + autenticación + autorización (sin endpoints).</summary>
    public static WebApplication UseClimaCommonPipeline(this WebApplication app)
    {
        app.Use(async (ctx, next) =>
        {
            try { await next(); }
            catch (Exception ex) when (!ctx.Response.HasStarted)
            {
                var (status, msg) = ex switch
                {
                    ArgumentException => (400, ex.Message),
                    NotFoundException => (404, ex.Message),
                    ConflictException => (409, ex.Message),
                    UnauthorizedAccessException => (401, ex.Message),
                    BadHttpRequestException => (400, "Solicitud inválida."),
                    _ => (500, "Error interno del servicio.")
                };
                if (status == 500) app.Logger.LogError(ex, "Error no controlado en {Path}", ctx.Request.Path);
                ctx.Response.Clear();
                ctx.Response.StatusCode = status;
                await ctx.Response.WriteAsJsonAsync(new { mensaje = msg });
            }
        });
        app.UseAuthentication();
        app.UseAuthorization();
        return app;
    }

    public static RouteHandlerBuilder RequireRoles(this RouteHandlerBuilder b, params string[] roles) =>
        b.RequireAuthorization(p => p.RequireAuthenticatedUser().RequireRole(roles));

    public static string? UserSub(this ClaimsPrincipal u) => u.FindFirst("sub")?.Value;
    public static int? UserId(this ClaimsPrincipal u) => int.TryParse(u.UserSub(), out var id) ? id : null;
    public static string? UserName(this ClaimsPrincipal u) => u.FindFirst("name")?.Value;
}

/// <summary>Token de servicio (rol "Servicio") para llamadas internas sin usuario (simulador, evaluación).</summary>
public class ServiceTokenProvider(IConfiguration cfg, ServiceInfo info)
{
    private string? _token; private DateTime _exp;
    public string Get()
    {
        lock (this)
        {
            if (_token != null && DateTime.UtcNow < _exp.AddSeconds(-30)) return _token;
            _exp = DateTime.UtcNow.AddMinutes(5);
            var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(cfg["Jwt:Key"]!)), SecurityAlgorithms.HmacSha256);
            var t = new JwtSecurityToken(cfg["Jwt:Issuer"] ?? "AlertaClimatica.Auth", cfg["Jwt:Audience"] ?? "AlertaClimatica.Client",
                new[] { new Claim("sub", $"svc:{info.Name}"), new Claim("role", "Servicio") }, expires: _exp, signingCredentials: creds);
            return _token = new JwtSecurityTokenHandler().WriteToken(t);
        }
    }
}

/// <summary>Cliente HTTP hacia otros microservicios (únicamente por API). Reenvía el JWT del usuario si existe.</summary>
public class UpstreamClient(IHttpClientFactory factory, IHttpContextAccessor accessor, ServiceTokenProvider tokens, IConfiguration cfg)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private string Base(string service) => (cfg[$"Services:{service}"] ?? service switch
    {
        "Community" => "http://community-service:8080",
        "Sensor" => "http://sensor-service:8080",
        "Alert" => "http://alert-service:8080",
        _ => throw new ArgumentException($"Servicio desconocido: {service}")
    }).TrimEnd('/');

    private string AuthHeader(bool asService)
    {
        var inbound = accessor.HttpContext?.Request.Headers.Authorization.ToString();
        return !asService && !string.IsNullOrEmpty(inbound) ? inbound : $"Bearer {tokens.Get()}";
    }

    private HttpRequestMessage Req(HttpMethod m, string service, string path, bool asService)
    {
        var r = new HttpRequestMessage(m, Base(service) + path);
        r.Headers.TryAddWithoutValidation("Authorization", AuthHeader(asService));
        return r;
    }

    /// <summary>GET tolerante a fallos: devuelve default si el servicio no responde (degradación elegante).</summary>
    public async Task<T?> TryGetAsync<T>(string service, string path, bool asService = false, CancellationToken ct = default)
    {
        try
        {
            using var http = factory.CreateClient(); http.Timeout = TimeSpan.FromSeconds(8);
            using var resp = await http.SendAsync(Req(HttpMethod.Get, service, path, asService), ct);
            if (!resp.IsSuccessStatusCode) return default;
            return await resp.Content.ReadFromJsonAsync<T>(Json, ct);
        }
        catch (Exception) when (!ct.IsCancellationRequested) { return default; }
    }

    /// <summary>GET que devuelve el código HTTP (para validar existencia).</summary>
    public async Task<bool?> ExistsAsync(string service, string path, bool asService = false, CancellationToken ct = default)
    {
        try
        {
            using var http = factory.CreateClient(); http.Timeout = TimeSpan.FromSeconds(8);
            using var resp = await http.SendAsync(Req(HttpMethod.Get, service, path, asService), ct);
            if (resp.StatusCode == System.Net.HttpStatusCode.NotFound) return false;
            return resp.IsSuccessStatusCode ? true : null; // null = no se pudo determinar
        }
        catch (Exception) when (!ct.IsCancellationRequested) { return null; }
    }

    public async Task<bool> PostAsync(string service, string path, object body, bool asService = true, CancellationToken ct = default)
    {
        try
        {
            using var http = factory.CreateClient(); http.Timeout = TimeSpan.FromSeconds(15);
            using var r = Req(HttpMethod.Post, service, path, asService);
            r.Content = JsonContent.Create(body, options: Json);
            using var resp = await http.SendAsync(r, ct);
            return resp.IsSuccessStatusCode;
        }
        catch (Exception) when (!ct.IsCancellationRequested) { return false; }
    }
}

/// <summary>Registra en el Audit Service (por API). Mejor esfuerzo: nunca rompe la operación de negocio.</summary>
public class AuditClient(IHttpClientFactory factory, IHttpContextAccessor accessor, ServiceTokenProvider tokens, IConfiguration cfg,
    ServiceInfo info, ILogger<AuditClient> log)
{
    public void Registrar(string accion, string detalle, string? entidad = null, int? entidadId = null, int? usuarioId = null, string? usuarioNombre = null)
    {
        var u = accessor.HttpContext?.User;
        var payload = new
        {
            usuarioId = usuarioId ?? u?.UserId(), usuarioNombre = usuarioNombre ?? u?.UserName(),
            accion, detalle, entidad, entidadId, servicio = info.Name
        };
        var url = (cfg["Services:Audit"] ?? "http://audit-service:8000").TrimEnd('/') + "/api/audit/internal";
        var token = tokens.Get();
        _ = Task.Run(async () =>
        {
            try
            {
                using var http = factory.CreateClient(); http.Timeout = TimeSpan.FromSeconds(5);
                using var r = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(payload) };
                r.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                using var resp = await http.SendAsync(r);
                if (!resp.IsSuccessStatusCode) log.LogWarning("Audit Service respondió {Status}", resp.StatusCode);
            }
            catch (Exception ex) { log.LogWarning("No se pudo registrar auditoría: {Msg}", ex.Message); }
        });
    }
}

/// <summary>
/// Fechas de filtros: "yyyy-MM-dd" se interpreta en la zona de la aplicación (APP_TZ); un instante con hora/offset se respeta.
/// Todo se almacena y compara en UTC. El límite superior devuelto es EXCLUSIVO.
/// </summary>
public static class FechaUtil
{
    private static readonly TimeZoneInfo Tz = Resolve(Environment.GetEnvironmentVariable("APP_TZ") ?? "America/Guatemala");
    private static TimeZoneInfo Resolve(string id) { try { return TimeZoneInfo.FindSystemTimeZoneById(id); } catch { return TimeZoneInfo.Utc; } }

    public static DateTime? Desde(string? s) => Parse(s, false);
    public static DateTime? Hasta(string? s) => Parse(s, true);

    private static DateTime? Parse(string? s, bool upper)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        s = s.Trim();
        if (s.Length == 10 && DateTime.TryParseExact(s, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var d))
        {
            var start = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(d, DateTimeKind.Unspecified), Tz);
            return upper ? start.AddDays(1) : start;
        }
        DateTime utc;
        if (System.Text.RegularExpressions.Regex.IsMatch(s, @"(Z|[+-]\d{2}:?\d{2})$"))
        {
            if (!DateTimeOffset.TryParse(s, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var dto))
                throw new ArgumentException($"Fecha no válida: {s}");
            utc = dto.UtcDateTime;
        }
        else
        {   // fecha-hora sin offset (p. ej. <input type="datetime-local">): hora local de la aplicación
            if (!DateTime.TryParse(s, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var local))
                throw new ArgumentException($"Fecha no válida: {s}");
            utc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), Tz);
        }
        return upper ? utc.AddMilliseconds(1) : utc;
    }
}
