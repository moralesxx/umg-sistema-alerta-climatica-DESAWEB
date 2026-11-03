using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Clima.Shared;

/// <summary>
/// Revocación de sesiones: el access token lleva un id de sesión (claim "sid"). Tras validar la firma, el gateway
/// pregunta al Auth Service si esa sesión sigue vigente. Así el logout (o desactivar al usuario) invalida el token
/// al instante, aunque el JWT todavía no haya expirado.
/// </summary>
public class SessionValidator(IHttpClientFactory factory, ServiceTokenProvider tokens, IConfiguration cfg, ILogger<SessionValidator> log)
{
    private readonly ConcurrentDictionary<string, (bool Active, DateTime At)> _cache = new();
    private readonly TimeSpan _ttl = TimeSpan.FromSeconds(Math.Max(0, cfg.GetValue("Session:CacheSeconds", 2)));
    private string AuthBase => (cfg["Services:Auth"] ?? "http://auth-service:8080").TrimEnd('/');

    public async Task<bool> IsActiveAsync(string sid, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        // Una sesión revocada nunca vuelve a estar activa: se recuerda sin vencimiento. Las activas se re-verifican cada pocos segundos.
        if (_cache.TryGetValue(sid, out var c) && (!c.Active || now - c.At < _ttl)) return c.Active;
        try
        {
            using var http = factory.CreateClient();
            http.Timeout = TimeSpan.FromSeconds(3);
            using var req = new HttpRequestMessage(HttpMethod.Get, $"{AuthBase}/api/auth/internal/sessions/{Uri.EscapeDataString(sid)}");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.Get());
            using var resp = await http.SendAsync(req, ct);
            if (resp.IsSuccessStatusCode) { Store(sid, true); return true; }
            if (resp.StatusCode == HttpStatusCode.NotFound) { Store(sid, false); return false; }
            log.LogWarning("Auth Service respondió {Status} al validar la sesión.", (int)resp.StatusCode);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            log.LogWarning("No se pudo validar la sesión en Auth: {Msg}", ex.Message);
        }
        // Auth no disponible: se tolera hasta 60 s lo último que se sabía; si no hay dato, se deniega (falla cerrado).
        return _cache.TryGetValue(sid, out var last) && last.Active && now - last.At < TimeSpan.FromSeconds(60);
    }

    private void Store(string sid, bool active)
    {
        if (_cache.Count > 20000) _cache.Clear();
        _cache[sid] = (active, DateTime.UtcNow);
    }
}

public static class SessionRevocationExtensions
{
    public static WebApplicationBuilder AddSessionRevocationCheck(this WebApplicationBuilder b)
    {
        b.Services.AddSingleton<SessionValidator>();
        b.Services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, o =>
        {
            var previous = o.Events.OnTokenValidated;
            o.Events.OnTokenValidated = async ctx =>
            {
                await previous(ctx);
                var principal = ctx.Principal;
                if (principal?.FindFirst("role")?.Value == "Servicio") return;   // los tokens de servicio no tienen sesión de usuario
                var sid = principal?.FindFirst("sid")?.Value;
                var validator = ctx.HttpContext.RequestServices.GetRequiredService<SessionValidator>();
                if (string.IsNullOrEmpty(sid) || !await validator.IsActiveAsync(sid, ctx.HttpContext.RequestAborted))
                    ctx.Fail("Sesión cerrada o inválida.");
            };
        });
        return b;
    }
}
