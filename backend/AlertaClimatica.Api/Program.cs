using Microsoft.EntityFrameworkCore;
using AlertaClimatica.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// 1. Obtener la cadena de conexión desde appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// 2. Registrar ApplicationDbContext con SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// 3. Registrar Controladores y OpenAPI
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// --- CONSTRUIR LA APLICACIÓN ---
var app = builder.Build();

// 4. Configurar el pipeline de solicitudes HTTP
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();