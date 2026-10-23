using AlertaClimatica.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AlertaClimatica.Infrastructure.Services;

// RF-ADM-21 / "sensores simulados": genera lecturas periódicas para todos los
// sensores activos y las evalúa contra las reglas de alerta. Si un sensor
// tiene ValorSimulado (lo fija el administrador), se usa ese valor exacto.
//
// Configuración (appsettings.json):
//   "Simulador": { "Habilitado": true, "IntervaloSegundos": 15, "RetencionDias": 7 }
public class SimuladorLecturasService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<SimuladorLecturasService> _logger;

    private readonly Random _rnd = new();
    private readonly Dictionary<int, decimal> _ultimos = new();
    private DateTime _ultimaLimpieza = DateTime.MinValue;

    public SimuladorLecturasService(
        IServiceScopeFactory scopeFactory,
        IConfiguration config,
        ILogger<SimuladorLecturasService> logger)
    {
        _scopeFactory = scopeFactory;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_config.GetValue("Simulador:Habilitado", true))
        {
            _logger.LogInformation("Simulador de lecturas deshabilitado.");
            return;
        }

        var intervalo = Math.Max(2, _config.GetValue("Simulador:IntervaloSegundos", 15));
        _logger.LogInformation("Simulador de lecturas iniciado (cada {Seg} s).", intervalo);

        // Pequeña espera para que las migraciones terminen de aplicarse.
        try { await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); }
        catch (OperationCanceledException) { return; }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(intervalo));
        do
        {
            try
            {
                await GenerarCicloAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Un fallo (p. ej. SQL Server reiniciando) no debe tumbar la API.
                _logger.LogError(ex, "Error en el ciclo del simulador de lecturas.");
            }
        }
        while (await WaitNextAsync(timer, stoppingToken));
    }

    private static async Task<bool> WaitNextAsync(PeriodicTimer timer, CancellationToken ct)
    {
        try { return await timer.WaitForNextTickAsync(ct); }
        catch (OperationCanceledException) { return false; }
    }

    private async Task GenerarCicloAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var evaluador = scope.ServiceProvider.GetRequiredService<IEvaluadorAlertas>();

        var sensores = await db.Sensores
            .Include(s => s.TipoSensor)
            .Where(s => s.Estado)
            .ToListAsync(ct);

        if (sensores.Count == 0)
            return;

        var ahora = DateTime.Now;
        var lecturas = new List<LecturaClimatica>();

        foreach (var s in sensores)
        {
            var valor = s.ValorSimulado ?? SiguienteValor(s);
            _ultimos[s.SensorId] = valor;
            lecturas.Add(new LecturaClimatica
            {
                SensorId = s.SensorId,
                Valor = Math.Round(valor, 2),
                FechaHora = ahora
            });
        }

        db.LecturasClimaticas.AddRange(lecturas);
        await db.SaveChangesAsync(ct);

        foreach (var lectura in lecturas)
            await evaluador.EvaluarAsync(lectura, ct);

        await LimpiarAntiguasAsync(db, ct);
    }

    // Borra lecturas viejas (una vez por hora) para que la tabla no crezca sin límite.
    private async Task LimpiarAntiguasAsync(ApplicationDbContext db, CancellationToken ct)
    {
        if (DateTime.Now - _ultimaLimpieza < TimeSpan.FromHours(1))
            return;

        _ultimaLimpieza = DateTime.Now;
        var dias = Math.Max(1, _config.GetValue("Simulador:RetencionDias", 7));
        var limite = DateTime.Now.AddDays(-dias);

        var borradas = await db.LecturasClimaticas
            .Where(l => l.FechaHora < limite)
            .ExecuteDeleteAsync(ct);

        if (borradas > 0)
            _logger.LogInformation("Simulador: {N} lecturas anteriores a {Dias} días eliminadas.", borradas, dias);
    }

    // Camino aleatorio acotado según el tipo de sensor: cambia poco entre
    // lecturas y de vez en cuando produce un pico para disparar alertas.
    private decimal SiguienteValor(Sensor s)
    {
        var (min, max, normalMin, normalMax) = Rango(s.TipoSensor?.NombreTipo);
        var amplitud = normalMax - normalMin;

        if (!_ultimos.TryGetValue(s.SensorId, out var actual))
            actual = normalMin + (decimal)_rnd.NextDouble() * amplitud;

        // Deriva suave.
        actual += (decimal)((_rnd.NextDouble() - 0.5) * 0.10) * amplitud;

        // Tiende a volver a la zona normal.
        var centro = (normalMin + normalMax) / 2;
        actual += (centro - actual) * 0.05m;

        // Pico ocasional (≈3 %).
        if (_rnd.NextDouble() < 0.03)
            actual += (decimal)(_rnd.NextDouble() * 0.8 + 0.2) * amplitud * (_rnd.Next(2) == 0 ? 1 : -1);

        return Math.Clamp(actual, min, max);
    }

    // (límite inferior físico, límite superior físico, zona normal mín, zona normal máx)
    private static (decimal min, decimal max, decimal normalMin, decimal normalMax) Rango(string? tipo)
    {
        var t = (tipo ?? string.Empty).ToLowerInvariant();
        if (t.Contains("temperatura")) return (-5m, 50m, 16m, 30m);
        if (t.Contains("humedad")) return (0m, 100m, 40m, 80m);
        if (t.Contains("viento")) return (0m, 150m, 3m, 30m);
        if (t.Contains("lluvia") || t.Contains("precipitaci")) return (0m, 300m, 0m, 20m);
        if (t.Contains("río") || t.Contains("rio") || t.Contains("nivel") || t.Contains("reservorio")) return (0m, 10m, 0.5m, 3m);
        if (t.Contains("humo") || t.Contains("incendio")) return (0m, 1000m, 0m, 100m);
        return (0m, 100m, 20m, 60m);
    }
}
