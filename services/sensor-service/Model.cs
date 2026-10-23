using Microsoft.EntityFrameworkCore;

namespace SensorSvc;

public class TipoSensor
{
    public int TipoSensorId { get; set; }
    public string NombreTipo { get; set; } = "";
    public string UnidadMedida { get; set; } = "";
}

public class Sensor
{
    public int SensorId { get; set; }
    public string NombreSensor { get; set; } = "";
    public int TipoSensorId { get; set; }
    public string Ubicacion { get; set; } = "";
    public bool Estado { get; set; }
    public string Codigo { get; set; } = "";
    /// <summary>Referencia lógica a la comunidad (otro microservicio): no existe FK ni acceso a su base de datos.</summary>
    public int? ComunidadId { get; set; }
    public DateTime? FechaInstalacion { get; set; }
    public string? Descripcion { get; set; }
    public decimal? ValorSimulado { get; set; }
    public TipoSensor? TipoSensor { get; set; }
}

public class LecturaClimatica
{
    public long LecturaId { get; set; }
    public int SensorId { get; set; }
    public decimal Valor { get; set; }
    public DateTime FechaHora { get; set; }
    public Sensor? Sensor { get; set; }
}

public class SensorDb(DbContextOptions<SensorDb> o) : DbContext(o)
{
    public DbSet<TipoSensor> TiposSensor => Set<TipoSensor>();
    public DbSet<Sensor> Sensores => Set<Sensor>();
    public DbSet<LecturaClimatica> Lecturas => Set<LecturaClimatica>();

    protected override void OnModelCreating(ModelBuilder m)
    {
        m.Entity<TipoSensor>().ToTable("TiposSensor").HasKey(t => t.TipoSensorId);
        var s = m.Entity<Sensor>();
        s.ToTable("Sensores").HasKey(x => x.SensorId);
        s.Property(x => x.ValorSimulado).HasPrecision(18, 2);
        s.Property(x => x.Codigo).HasMaxLength(40);
        s.HasIndex(x => x.Codigo).IsUnique();
        s.HasIndex(x => x.ComunidadId);
        s.HasOne(x => x.TipoSensor).WithMany().HasForeignKey(x => x.TipoSensorId).OnDelete(DeleteBehavior.Restrict);
        var l = m.Entity<LecturaClimatica>();
        l.ToTable("LecturasClimaticas").HasKey(x => x.LecturaId);
        l.Property(x => x.Valor).HasPrecision(18, 2);
        l.HasIndex(x => new { x.SensorId, x.FechaHora });
        l.HasIndex(x => x.FechaHora);
        l.HasOne(x => x.Sensor).WithMany().HasForeignKey(x => x.SensorId).OnDelete(DeleteBehavior.Cascade);
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
                var db = scope.ServiceProvider.GetRequiredService<SensorDb>();
                await db.Database.EnsureCreatedAsync();
                if (!await db.TiposSensor.AnyAsync())
                {
                    db.TiposSensor.AddRange(
                        new TipoSensor { NombreTipo = "Temperatura Ambiente", UnidadMedida = "°C" },
                        new TipoSensor { NombreTipo = "Humedad Relativa", UnidadMedida = "%" },
                        new TipoSensor { NombreTipo = "Velocidad del Viento", UnidadMedida = "km/h" },
                        new TipoSensor { NombreTipo = "Nivel de Lluvia", UnidadMedida = "mm" },
                        new TipoSensor { NombreTipo = "Nivel de Río", UnidadMedida = "m" },
                        new TipoSensor { NombreTipo = "Nivel de Reservorio", UnidadMedida = "m" },
                        new TipoSensor { NombreTipo = "Humo / Incendio", UnidadMedida = "ppm" },
                        new TipoSensor { NombreTipo = "Presión Atmosférica", UnidadMedida = "hPa" });
                    await db.SaveChangesAsync();
                }
                if (app.Configuration.GetValue("Seed:Demo", false) && !await db.Sensores.AnyAsync())
                {
                    var t = await db.TiposSensor.ToDictionaryAsync(x => x.NombreTipo, x => x.TipoSensorId);
                    var hoy = DateTime.UtcNow.Date;
                    // ComunidadId 1..3 corresponden a las comunidades de demostración del Community Service.
                    db.Sensores.AddRange(
                        new Sensor { Codigo = "SEN-01", NombreSensor = "Sensor Temp - Comunidad Norte", TipoSensorId = t["Temperatura Ambiente"], Ubicacion = "Comunidad Norte", ComunidadId = 1, Estado = true, FechaInstalacion = hoy, Descripcion = "Temperatura ambiente" },
                        new Sensor { Codigo = "SEN-02", NombreSensor = "Sensor Humedad - Comunidad Norte", TipoSensorId = t["Humedad Relativa"], Ubicacion = "Comunidad Norte", ComunidadId = 1, Estado = true, FechaInstalacion = hoy, Descripcion = "Humedad relativa" },
                        new Sensor { Codigo = "SEN-03", NombreSensor = "Estación de Viento - Sector Central", TipoSensorId = t["Velocidad del Viento"], Ubicacion = "Sector Central", ComunidadId = 2, Estado = true, FechaInstalacion = hoy, Descripcion = "Anemómetro" },
                        new Sensor { Codigo = "SEN-04", NombreSensor = "Pluviómetro - Cuenca Sur", TipoSensorId = t["Nivel de Lluvia"], Ubicacion = "Cuenca Sur", ComunidadId = 3, Estado = true, FechaInstalacion = hoy, Descripcion = "Pluviómetro" },
                        new Sensor { Codigo = "SEN-05", NombreSensor = "Medidor de Río - Puente Principal", TipoSensorId = t["Nivel de Río"], Ubicacion = "Puente Principal", ComunidadId = 3, Estado = true, FechaInstalacion = hoy, Descripcion = "Nivel del río" });
                    await db.SaveChangesAsync();
                }
                return;
            }
            catch (Exception ex) when (i < 20)
            {
                app.Logger.LogWarning("SQL Server no disponible (intento {I}/20): {Msg}", i, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(5));
            }
        }
    }
}

/// <summary>Directorio en memoria de comunidades (id → nombre), alimentado por la API del Community Service.</summary>
public class CommunityDirectory(IServiceScopeFactory scopes, ILogger<CommunityDirectory> log)
{
    public record Com(int ComunidadId, string Nombre);
    private Dictionary<int, string> _names = new();
    private DateTime _loaded = DateTime.MinValue;
    private bool _everLoaded;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private async Task RefreshAsync(bool force)
    {
        if (!force && DateTime.UtcNow - _loaded < TimeSpan.FromSeconds(30)) return;
        await _gate.WaitAsync();
        try
        {
            if (!force && DateTime.UtcNow - _loaded < TimeSpan.FromSeconds(30)) return;
            using var scope = scopes.CreateScope();
            var up = scope.ServiceProvider.GetRequiredService<Clima.Shared.UpstreamClient>();
            var list = await up.TryGetAsync<List<Com>>("Community", "/api/communities", asService: true);
            if (list != null) { _names = list.ToDictionary(c => c.ComunidadId, c => c.Nombre); _everLoaded = true; }
            else log.LogWarning("Community Service no disponible; se usan nombres en caché.");
            _loaded = DateTime.UtcNow;
        }
        finally { _gate.Release(); }
    }

    public async Task<IReadOnlyDictionary<int, string>> NamesAsync() { await RefreshAsync(false); return _names; }

    /// <summary>true/false si se pudo determinar; null si el servicio de comunidades no está disponible.</summary>
    public async Task<bool?> ExistsAsync(int id)
    {
        await RefreshAsync(false);
        if (_names.ContainsKey(id)) return true;
        await RefreshAsync(true);
        if (_names.ContainsKey(id)) return true;
        return _everLoaded ? false : null;
    }
}
