using Microsoft.EntityFrameworkCore;
using AlertaClimatica.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// 1. Configurar CORS (Permite peticiones desde Angular)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// 2. Configurar DbContext con la cadena de conexión de SQL Server
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// 3. Registrar Controladores
builder.Services.AddControllers();

var app = builder.Build();

// 4. Habilitar Middleware de CORS (debe ir antes de MapControllers y UseAuthorization)
app.UseCors("AllowAngular");

app.UseAuthorization();

// 5. Mapear endpoints de los controladores
app.MapControllers();


app.Run();