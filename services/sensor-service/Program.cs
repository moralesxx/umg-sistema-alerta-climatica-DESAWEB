using Clima.Shared;
using Microsoft.EntityFrameworkCore;
using SensorSvc;

var builder = WebApplication.CreateBuilder(args);
builder.AddClimaCommon("sensor-service");
builder.Services.AddDbContext<SensorDb>(o => o.UseSqlServer(
    builder.Configuration.GetConnectionString("Default") ?? throw new InvalidOperationException("Falta ConnectionStrings__Default.")));
builder.Services.AddSingleton<CommunityDirectory>();
builder.Services.AddHostedService<SimuladorLecturasService>();

var app = builder.Build();
app.UseClimaCommon();

await Database.InitAsync(app);
app.MapSensorEndpoints();
app.Run();
