using Microsoft.EntityFrameworkCore;

namespace CommunitySvc;

public class Comunidad
{
    public int ComunidadId { get; set; }
    public string Nombre { get; set; } = "";
    public string Municipio { get; set; } = "";
    public string Departamento { get; set; } = "";
    public string Pais { get; set; } = "";
    public decimal? Latitud { get; set; }
    public decimal? Longitud { get; set; }
    public string? Descripcion { get; set; }
    public bool Estado { get; set; } = true;
}

public class ComunidadDto
{
    public int ComunidadId { get; set; }
    public string Nombre { get; set; } = "";
    public string Municipio { get; set; } = "";
    public string Departamento { get; set; } = "";
    public string Pais { get; set; } = "";
    public decimal? Latitud { get; set; }
    public decimal? Longitud { get; set; }
    public string? Descripcion { get; set; }
    public bool Estado { get; set; }
    public int CantidadSensores { get; set; }
}

public class ComunidadInput
{
    public string Nombre { get; set; } = "";
    public string Municipio { get; set; } = "";
    public string Departamento { get; set; } = "";
    public string Pais { get; set; } = "";
    public decimal? Latitud { get; set; }
    public decimal? Longitud { get; set; }
    public string? Descripcion { get; set; }
    public bool Estado { get; set; } = true;
}

public record SensorCount(int ComunidadId, int Total);

public class CommunityDb(DbContextOptions<CommunityDb> o) : DbContext(o)
{
    public DbSet<Comunidad> Comunidades => Set<Comunidad>();
    protected override void OnModelCreating(ModelBuilder m)
    {
        var e = m.Entity<Comunidad>();
        e.ToTable("comunidades").HasKey(c => c.ComunidadId);
        e.Property(c => c.Latitud).HasPrecision(9, 6);
        e.Property(c => c.Longitud).HasPrecision(9, 6);
        e.Property(c => c.Nombre).HasMaxLength(150);
        e.HasIndex(c => new { c.Nombre, c.Municipio, c.Departamento }).IsUnique();
    }
}

public static class Database
{
    public static async Task InitAsync(WebApplication app)
    {
        for (var i = 1; ; i++)
        {
            try
            {
                using var scope = app.Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<CommunityDb>();
                await db.Database.EnsureCreatedAsync();
                if (app.Configuration.GetValue("Seed:Demo", false) && !await db.Comunidades.AnyAsync())
                {
                    db.Comunidades.AddRange(
                        new Comunidad { Nombre = "Comunidad Norte", Municipio = "Cobán", Departamento = "Alta Verapaz", Pais = "Guatemala", Latitud = 15.4708m, Longitud = -90.3708m, Descripcion = "Zona alta con riesgo de tormentas." },
                        new Comunidad { Nombre = "Sector Central", Municipio = "Antigua Guatemala", Departamento = "Sacatepéquez", Pais = "Guatemala", Latitud = 14.5586m, Longitud = -90.7295m, Descripcion = "Zona central de monitoreo." },
                        new Comunidad { Nombre = "Cuenca Sur", Municipio = "Escuintla", Departamento = "Escuintla", Pais = "Guatemala", Latitud = 14.3050m, Longitud = -90.7860m, Descripcion = "Cuenca con riesgo de inundación." });
                    await db.SaveChangesAsync();
                }
                return;
            }
            catch (Exception ex) when (i < 15)
            {
                app.Logger.LogWarning("BD no disponible (intento {I}/15): {Msg}", i, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(4));
            }
        }
    }
}
