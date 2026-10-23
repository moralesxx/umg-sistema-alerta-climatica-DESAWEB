using Clima.Shared;
using Microsoft.EntityFrameworkCore;

namespace SensorSvc;

public record LecturaEval(int SensorId, string SensorCodigo, string SensorNombre, int TipoSensorId, string TipoNombre, string Unidad,
    int? ComunidadId, decimal Valor, DateTime FechaHora);

public static class Evaluacion
{
    /// <summary>Envía las lecturas al Alert Service (API interna) para que evalúe reglas y genere alertas.</summary>
    public static Task<bool> EnviarAsync(UpstreamClient up, IEnumerable<LecturaEval> lecturas, CancellationToken ct = default) =>
        up.PostAsync("Alert", "/api/alerts/internal/evaluate", new { lecturas }, asService: true, ct);

    public static LecturaEval Map(LecturaClimatica l, Sensor s) =>
        new(s.SensorId, s.Codigo, s.NombreSensor, s.TipoSensorId, s.TipoSensor?.NombreTipo ?? "", s.TipoSensor?.UnidadMedida ?? "",
            s.ComunidadId, l.Valor, l.FechaHora);
}

/// <summary>Simulador de lecturas (mismo comportamiento que la Fase 2). Solo escribe en su propia base de datos.</summary>
public class SimuladorLecturasService(IServiceScopeFactory scopeFactory, IConfiguration config, ILogger<SimuladorLecturasService> logger) : BackgroundService
{
    private readonly Random _rnd = new();
    private readonly Dictionary<int, decimal> _ultimos = new();
    private DateTime _ultimaLimpieza = DateTime.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!config.GetValue("Simulador:Habilitado", true)) { logger.LogInformation("Simulador de lecturas deshabilitado."); return; }
        var intervalo = Math.Max(2, config.GetValue("Simulador:IntervaloSegundos", 15));
        try { await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken); } catch (OperationCanceledException) { return; }
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(intervalo));
        do
        {
            try { await CicloAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "Error en el ciclo del simulador."); }
        }
        while (await Next(timer, stoppingToken));
    }

    private static async Task<bool> Next(PeriodicTimer t, CancellationToken ct)
    { try { return await t.WaitForNextTickAsync(ct); } catch (OperationCanceledException) { return false; } }

    private async Task CicloAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SensorDb>();
        var up = scope.ServiceProvider.GetRequiredService<UpstreamClient>();
        var sensores = await db.Sensores.Include(s => s.TipoSensor).Where(s => s.Estado).ToListAsync(ct);
        sensores = sensores.Where(s => !Dispositivos.TieneHardware(s.SensorId)).ToList(); // hardware real manda sobre la simulación
        if (sensores.Count == 0) return;
        var ahora = DateTime.UtcNow;
        var lecturas = new List<LecturaClimatica>();
        foreach (var s in sensores)
        {
            var v = s.ValorSimulado ?? Siguiente(s);
            _ultimos[s.SensorId] = v;
            lecturas.Add(new LecturaClimatica { SensorId = s.SensorId, Valor = Math.Round(v, 2), FechaHora = ahora });
        }
        db.Lecturas.AddRange(lecturas);
        await db.SaveChangesAsync(ct);
        var porId = sensores.ToDictionary(s => s.SensorId);
        if (!await Evaluacion.EnviarAsync(up, lecturas.Select(l => Evaluacion.Map(l, porId[l.SensorId])), ct))
            logger.LogWarning("Alert Service no aceptó las lecturas; se reintentará en el siguiente ciclo.");
        await LimpiarAsync(db, ct);
    }

    private async Task LimpiarAsync(SensorDb db, CancellationToken ct)
    {
        if (DateTime.UtcNow - _ultimaLimpieza < TimeSpan.FromHours(1)) return;
        _ultimaLimpieza = DateTime.UtcNow;
        var dias = Math.Max(1, config.GetValue("Simulador:RetencionDias", 7));
        var limite = DateTime.UtcNow.AddDays(-dias);
        var n = await db.Lecturas.Where(l => l.FechaHora < limite).ExecuteDeleteAsync(ct);
        if (n > 0) logger.LogInformation("Simulador: {N} lecturas anteriores a {D} días eliminadas.", n, dias);
    }

    private decimal Siguiente(Sensor s)
    {
        var (min, max, nMin, nMax) = Rango(s.TipoSensor?.NombreTipo);
        var amp = nMax - nMin;
        if (!_ultimos.TryGetValue(s.SensorId, out var actual)) actual = nMin + (decimal)_rnd.NextDouble() * amp;
        actual += (decimal)((_rnd.NextDouble() - 0.5) * 0.10) * amp;
        actual += ((nMin + nMax) / 2 - actual) * 0.05m;
        if (_rnd.NextDouble() < 0.03) actual += (decimal)(_rnd.NextDouble() * 0.8 + 0.2) * amp * (_rnd.Next(2) == 0 ? 1 : -1);
        return Math.Clamp(actual, min, max);
    }

    private static (decimal min, decimal max, decimal nMin, decimal nMax) Rango(string? tipo)
    {
        var t = (tipo ?? "").ToLowerInvariant();
        if (t.Contains("temperatura")) return (-5m, 50m, 16m, 30m);
        if (t.Contains("humedad")) return (0m, 100m, 40m, 80m);
        if (t.Contains("viento")) return (0m, 150m, 3m, 30m);
        if (t.Contains("lluvia") || t.Contains("precipitaci")) return (0m, 300m, 0m, 20m);
        if (t.Contains("río") || t.Contains("rio") || t.Contains("nivel") || t.Contains("reservorio")) return (0m, 10m, 0.5m, 3m);
        if (t.Contains("humo") || t.Contains("incendio")) return (0m, 1000m, 0m, 100m);
        return (0m, 100m, 20m, 60m);
    }
}
