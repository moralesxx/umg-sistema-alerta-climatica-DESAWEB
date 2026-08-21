using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using AlertaClimatica.Infrastructure;
using AlertaClimatica.Infrastructure.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// =========================================================
// 1. CONFIGURAR CORS
// =========================================================

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
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

builder.Services.AddScoped<BitacoraService>();

// =========================================================
// 8. CONSTRUIR APLICACIÓN
// =========================================================

var app = builder.Build();

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

// =========================================================
// 14. INICIALIZAR BASE DE DATOS
// =========================================================

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    try
    {
        var context =
            services.GetRequiredService<ApplicationDbContext>();

        DbInitializer.Seed(context);
    }
    catch (Exception ex)
    {
        var logger =
            services.GetRequiredService<ILogger<Program>>();

        logger.LogError(
            ex,
            "Ocurrió un error al inicializar la base de datos."
        );
    }
}

// =========================================================
// 15. EJECUTAR APLICACIÓN
// =========================================================

app.Run();