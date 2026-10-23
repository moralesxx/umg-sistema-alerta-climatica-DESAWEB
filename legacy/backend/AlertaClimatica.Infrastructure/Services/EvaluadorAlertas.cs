using AlertaClimatica.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AlertaClimatica.Infrastructure.Services;

public class EvaluadorAlertas : IEvaluadorAlertas
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<EvaluadorAlertas> _logger;

    public EvaluadorAlertas(ApplicationDbContext db, ILogger<EvaluadorAlertas> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Alerta?> EvaluarAsync(LecturaClimatica lectura, CancellationToken ct = default)
    {
        var sensor = await _db.Sensores
            .Include(s => s.TipoSensor)
            .FirstOrDefaultAsync(s => s.SensorId == lectura.SensorId, ct);

        if (sensor == null)
            return null;

        var reglas = await _db.ReglasAlerta
            .Where(r => r.Estado && r.TipoSensorId == sensor.TipoSensorId)
            .ToListAsync(ct);

        // Una regla se activa cuando el valor cae dentro de su banda
        // [ValorMinimo, ValorMaximo]; un extremo vacío significa "sin límite".
        // Las reglas Verde (normal) no generan alerta. Si coinciden varias,
        // se toma la de mayor gravedad.
        var regla = reglas
            .Where(r => Peso(r.NivelPeligro) > 0 && Coincide(r, lectura.Valor))
            .OrderByDescending(r => Peso(r.NivelPeligro))
            .ThenBy(r => r.ReglaAlertaId)
            .FirstOrDefault();

        if (regla == null)
            return null;

        // Evita inundar el sistema: una sola alerta abierta (Activa o Atendida)
        // por sensor y regla. Al cerrarla, si la condición sigue, se genera otra.
        var yaActiva = await _db.Alertas.AnyAsync(a =>
            a.SensorId == sensor.SensorId &&
            a.ReglaAlertaId == regla.ReglaAlertaId &&
            a.Estado != "Cerrada", ct);

        if (yaActiva)
            return null;

        var nivel = NormalizarNivel(regla.NivelPeligro);
        var unidad = sensor.TipoSensor?.UnidadMedida ?? string.Empty;
        var valorTexto = $"{lectura.Valor:0.##} {unidad}".Trim();
        var ahora = DateTime.Now;

        var evento = new Evento
        {
            TipoFenomeno = regla.TipoFenomeno,
            Descripcion = Recortar($"{regla.Mensaje} Valor detectado: {valorTexto} (sensor {sensor.Codigo}).", 500),
            FechaHora = ahora,
            ComunidadId = sensor.ComunidadId,
            SensorId = sensor.SensorId,
            NivelRiesgo = nivel,
            Valor = lectura.Valor,
            Estado = "Activa"
        };

        var alerta = new Alerta
        {
            SensorId = sensor.SensorId,
            ReglaAlertaId = regla.ReglaAlertaId,
            NivelRiesgo = nivel,
            Mensaje = Recortar($"{regla.Mensaje} Valor detectado: {valorTexto}.", 500),
            FechaEmision = ahora,
            ValorDetectado = lectura.Valor,
            Estado = "Activa",
            Evento = evento
        };

        _db.Alertas.Add(alerta); // el evento se guarda junto por la navegación
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Alerta {Nivel} generada: sensor {Sensor}, regla '{Regla}', valor {Valor}",
            nivel, sensor.Codigo, regla.Nombre, lectura.Valor);

        return alerta;
    }

    private static bool Coincide(ReglaAlerta r, decimal valor)
    {
        if (r.ValorMinimo.HasValue && valor < r.ValorMinimo.Value) return false;
        if (r.ValorMaximo.HasValue && valor > r.ValorMaximo.Value) return false;
        return r.ValorMinimo.HasValue || r.ValorMaximo.HasValue;
    }

    private static int Peso(string? nivel) => (nivel ?? string.Empty).Trim().ToLowerInvariant() switch
    {
        "amarillo" => 1,
        "naranja" => 2,
        "rojo" => 3,
        _ => 0
    };

    private static string NormalizarNivel(string nivel) => Peso(nivel) switch
    {
        1 => "Amarillo",
        2 => "Naranja",
        3 => "Rojo",
        _ => "Verde"
    };

    private static string Recortar(string texto, int max) =>
        texto.Length <= max ? texto : texto[..max];
}
