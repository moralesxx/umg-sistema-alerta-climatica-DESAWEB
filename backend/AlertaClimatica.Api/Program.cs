using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using AlertaClimatica.Infrastructure;
using AlertaClimatica.Infrastructure.Services;
using AlertaClimatica.Infrastructure.Repositories;
using AlertaClimatica.Infrastructure.Repositories.Interfaces;
using AlertaClimatica.Infrastructure.Services.Interfaces;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// =========================================================
// 1. CONFIGURAR CORS
// =========================================================

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        // En producción el navegador llama a /api en el mismo origen (Nginx),
        // así que CORS solo hace falta en desarrollo (ng serve en :4200).
        // Orígenes extra: Cors__AllowedOrigins__0=https://tu-dominio (variable de entorno).
        var extraOrigins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? Array.Empty<string>();

        policy.WithOrigins(new[] { "http://localhost:4200" }.Concat(extraOrigins).ToArray())
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// =========================================================
// 2. CONFIGURAR DB CONTEXT
// =========================================================

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// =========================================================
// 3. CONFIGURAR JWT
// =========================================================

var jwtSettings = builder.Configuration.GetSection("Jwt");

var jwtKey = jwtSettings["Key"];
var jwtIssuer = jwtSettings["Issuer"];
var jwtAudience = jwtSettings["Audience"];

if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException(
        "Jwt:Key no está configurado en appsettings.json."
    );
}

if (string.IsNullOrWhiteSpace(jwtIssuer))
{
    throw new InvalidOperationException(
        "Jwt:Issuer no está configurado en appsettings.json."
    );
}

if (string.IsNullOrWhiteSpace(jwtAudience))
{
    throw new InvalidOperationException(
        "Jwt:Audience no está configurado en appsettings.json."
    );
}

// HS256 necesita una clave de al menos 256 bits; si es más corta el login
// revienta en tiempo de ejecución. Mejor fallar al arrancar con un mensaje claro.
if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException(
        "Jwt:Key debe tener al menos 32 caracteres. Defínala por variable de entorno (Jwt__Key), no en el código."
    );
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            // Validar que el token tenga un issuer correcto
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,

            // Validar que el token tenga un audience correcto
            ValidateAudience = true,
            ValidAudience = jwtAudience,

            // Validar que el token no haya expirado
            ValidateLifetime = true,

            // Validar la firma del token
            ValidateIssuerSigningKey = true,

            // Clave utilizada para verificar la firma
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)
            ),

            // Sin margen adicional después de expirar
            ClockSkew = TimeSpan.Zero
        };
    });

// =========================================================
// 4. CONFIGURAR AUTORIZACIÓN
// =========================================================

builder.Services.AddAuthorization();

// =========================================================
// 4.1 PROXY INVERSO (Nginx) Y LÍMITE DE INTENTOS DE LOGIN
// =========================================================

// Detrás de Nginx la IP y el esquema reales llegan en X-Forwarded-*.
// Se limpian KnownNetworks/KnownProxies porque la API solo es alcanzable
// desde la red interna de Docker (el puerto 5081 no se publica a Internet).
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// RNF-ADM (seguridad): frena la fuerza bruta sobre el login.
// 10 intentos por minuto por IP; el resto de la API no se limita.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        if (HttpMethods.IsPost(context.Request.Method) &&
            context.Request.Path.Equals("/api/usuarios/login", StringComparison.OrdinalIgnoreCase))
        {
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "desconocida";

            return RateLimitPartition.GetFixedWindowLimiter(
                $"login:{ip}",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                });
        }

        return RateLimitPartition.GetNoLimiter("sin-limite");
    });
});

// =========================================================
// 5. CONFIGURAR CONTROLADORES
// =========================================================

builder.Services.AddControllers();

// =========================================================
// 6. HTTP CONTEXT
// =========================================================

// Permite que BitacoraService acceda a HttpContext
// y obtenga el usuario autenticado.
builder.Services.AddHttpContextAccessor();

// =========================================================
// 7. REGISTRAR SERVICIOS
// =========================================================

// Repositorios (acceso a datos puro)
builder.Services.AddScoped<ISensorRepository, SensorRepository>();
builder.Services.AddScoped<ITipoSensorRepository, TipoSensorRepository>();
builder.Services.AddScoped<IComunidadRepository, ComunidadRepository>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IRolRepository, RolRepository>();
builder.Services.AddScoped<IBitacoraRepository, BitacoraRepository>();
builder.Services.AddScoped<ILecturaRepository, LecturaRepository>();
builder.Services.AddScoped<IEventoRepository, EventoRepository>();
builder.Services.AddScoped<IReglaAlertaRepository, ReglaAlertaRepository>();

// Services (reglas de negocio, dependen de los repositorios de arriba)
builder.Services.AddScoped<ISensorService, SensorService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IBitacoraService, BitacoraService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IComunidadService, ComunidadService>();
builder.Services.AddScoped<IRolService, RolService>();
builder.Services.AddScoped<ILecturaService, LecturaService>();
builder.Services.AddScoped<IEventoService, EventoService>();
builder.Services.AddScoped<IReglaAlertaService, ReglaAlertaService>();


// =========================================================
// 8. CONSTRUIR APLICACIÓN
// =========================================================

var app = builder.Build();

// =========================================================
// 8.1 CABECERAS DEL PROXY (debe ir primero en el pipeline)
// =========================================================

app.UseForwardedHeaders();

// =========================================================
// 9. CONFIGURACIÓN PARA DESARROLLO
// =========================================================

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// =========================================================
// 10. CORS
// =========================================================

app.UseCors("AllowAngular");

app.UseRateLimiter();

// =========================================================
// 11. AUTENTICACIÓN
// =========================================================

// IMPORTANTE:
// Debe ejecutarse antes de UseAuthorization().
app.UseAuthentication();

// =========================================================
// 12. AUTORIZACIÓN
// =========================================================

app.UseAuthorization();

// =========================================================
// 13. MAPEAR CONTROLADORES
// =========================================================

app.MapControllers();

// Endpoint público y liviano para el healthcheck de Docker / monitoreo.
// No toca la base de datos ni expone información.
app.MapGet("/health", () => Results.Ok(new { estado = "ok" })).AllowAnonymous();

// =========================================================
// 14. INICIALIZAR BASE DE DATOS Y APLICAR MIGRACIONES
// =========================================================

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var context = services.GetRequiredService<ApplicationDbContext>();

    // En Docker Compose SQL Server tarda en aceptar conexiones: se reintenta.
    // Si después de todos los intentos sigue fallando, la API NO debe quedar
    // "viva pero rota": se relanza la excepción para que el contenedor se
    // reinicie y el error se vea en `docker compose logs backend`.
    const int maxIntentos = 12;

    for (var intento = 1; ; intento++)
    {
        try
        {
            // Aplica las migraciones pendientes automáticamente
            context.Database.Migrate();

            // Siembra los datos iniciales (rol/usuario administrador)
            DbInitializer.Seed(context);

            break;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error al inicializar la base de datos (intento {Intento}/{Max}).",
                intento,
                maxIntentos
            );

            if (intento >= maxIntentos)
            {
                throw;
            }

            Thread.Sleep(TimeSpan.FromSeconds(5));
        }
    }
}

// =========================================================
// 15. EJECUTAR APLICACIÓN
// =========================================================

app.Run();