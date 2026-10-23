using System.Diagnostics;
using System.Threading.RateLimiting;
using Clima.Shared;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
builder.AddClimaCommon("gateway"); // validación JWT (Issuer, Audience, firma, expiración), errores JSON y /health

// ---------------------------------------------------------------- CORS
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:4200"])
    .AllowAnyHeader().AllowAnyMethod().WithExposedHeaders("Content-Disposition", "X-Correlation-Id")));

// ---------------------------------------------------------------- Proxy inverso de borde (Nginx) e IP real del cliente
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownIPNetworks.Clear(); o.KnownProxies.Clear(); // solo alcanzable desde la red interna
});

// ---------------------------------------------------------------- Rate limiting
var loginLimit = builder.Configuration.GetValue("RateLimit:LoginPerMinute", 10);
var deviceLimit = builder.Configuration.GetValue("RateLimit:DevicePerMinute", 300);
var globalLimit = builder.Configuration.GetValue("RateLimit:GlobalPerMinute", 600);
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("login", ctx => RateLimitPartition.GetFixedWindowLimiter(ctx.Connection.RemoteIpAddress?.ToString() ?? "?",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = loginLimit, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    o.AddPolicy("device", ctx => RateLimitPartition.GetFixedWindowLimiter("d:" + (ctx.Connection.RemoteIpAddress?.ToString() ?? "?"),
        _ => new FixedWindowRateLimiterOptions { PermitLimit = deviceLimit, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx => RateLimitPartition.GetFixedWindowLimiter(
        "g:" + (ctx.Connection.RemoteIpAddress?.ToString() ?? "?"),
        _ => new FixedWindowRateLimiterOptions { PermitLimit = globalLimit, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});

builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();
app.UseForwardedHeaders();

// ---------------------------------------------------------------- Correlación + logging de acceso
app.Use(async (ctx, next) =>
{
    var cid = ctx.Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? Guid.NewGuid().ToString("N")[..16];
    ctx.Request.Headers["X-Correlation-Id"] = cid;
    ctx.Response.Headers["X-Correlation-Id"] = cid;
    var sw = Stopwatch.StartNew();
    await next();
    if (!ctx.Request.Path.StartsWithSegments("/health"))
        app.Logger.LogInformation("{Method} {Path} -> {Status} en {Ms} ms (usuario {User}, cid {Cid})", ctx.Request.Method, ctx.Request.Path,
            ctx.Response.StatusCode, sw.ElapsedMilliseconds, ctx.User.UserSub() ?? "anónimo", cid);
});

// ---------------------------------------------------------------- Ocultar infraestructura: las rutas internas nunca salen por el gateway
app.Use(async (ctx, next) =>
{
    var p = ctx.Request.Path.Value ?? "";
    if (p.Contains("/internal/", StringComparison.OrdinalIgnoreCase) || p.EndsWith("/internal", StringComparison.OrdinalIgnoreCase))
    { ctx.Response.StatusCode = 404; await ctx.Response.WriteAsJsonAsync(new { mensaje = "Recurso no encontrado." }); return; }
    await next();
});

// Respuestas de error sin cuerpo (401/403/429/502/504...) -> JSON uniforme, sin detalles de la infraestructura.
app.UseStatusCodePages(async c =>
{
    var msg = c.HttpContext.Response.StatusCode switch
    {
        401 => "Autenticación requerida o token vencido.",
        403 => "No tiene permisos para esta operación.",
        404 => "Recurso no encontrado.",
        429 => "Demasiadas solicitudes. Intente de nuevo en un momento.",
        502 or 503 => "El servicio solicitado no está disponible.",
        504 => "El servicio tardó demasiado en responder.",
        _ => "No se pudo completar la solicitud."
    };
    c.HttpContext.Response.ContentType = "application/json";
    await c.HttpContext.Response.WriteAsJsonAsync(new { mensaje = msg });
});

app.UseCors();
app.UseRateLimiter();
app.UseClimaCommonPipeline(); // errores + autenticación + autorización + /health

app.MapGet("/health", () => Results.Ok(new { estado = "ok" })).AllowAnonymous();

// Estado agregado de los servicios internos (útil para monitoreo; no requiere token y no expone direcciones).
app.MapGet("/health/services", async (IHttpClientFactory f, IConfiguration cfg) =>
{
    var result = new Dictionary<string, string>();
    foreach (var c in cfg.GetSection("ReverseProxy:Clusters").GetChildren())
    {
        var addr = c.GetSection("Destinations").GetChildren().First()["Address"]!.TrimEnd('/');
        try { using var http = f.CreateClient(); http.Timeout = TimeSpan.FromSeconds(3); result[c.Key] = (await http.GetAsync(addr + "/health")).IsSuccessStatusCode ? "ok" : "error"; }
        catch { result[c.Key] = "caído"; }
    }
    return Results.Json(result, statusCode: result.Values.All(v => v == "ok") ? 200 : 503);
}).AllowAnonymous();

app.MapReverseProxy();
app.Run();
