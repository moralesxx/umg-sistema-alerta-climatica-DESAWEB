using Clima.Shared;
using CommunitySvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.AddClimaCommon("community-service");
builder.Services.AddDbContext<CommunityDb>(o => o.UseNpgsql(
    builder.Configuration.GetConnectionString("Default") ?? throw new InvalidOperationException("Falta ConnectionStrings__Default.")));

var app = builder.Build();
app.UseClimaCommon();

await Database.InitAsync(app);
app.MapCommunityEndpoints();
app.Run();
