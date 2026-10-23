using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AlertaClimatica.Application.DTOs;
using AlertaClimatica.Infrastructure;

namespace AlertaClimatica.Api.Controllers;

// Datos agregados para el dashboard. Solo lectura: cualquier usuario autenticado.
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private static readonly string[] Niveles = { "Verde", "Amarillo", "Naranja", "Rojo" };

    private readonly ApplicationDbContext _db;

    public DashboardController(ApplicationDbContext db)
    {
        _db = db;
    }

    // GET: api/dashboard/resumen?comunidadId=1&horas=1
    [HttpGet("resumen")]
    public async Task<ActionResult<DashboardResumenDto>> GetResumen(
        [FromQuery] int? comunidadId,
        [FromQuery] int horas = 1)
    {
        horas = Math.Clamp(horas, 1, 24);
        var ahora = DateTime.Now;
        var desde = ahora.AddHours(-horas);

        var resultado = new DashboardResumenDto { GeneradoEn = ahora };

        // ---- Comunidades (RF-ADM-60)
        var comunidades = _db.Comunidades.AsNoTracking().AsQueryable();
        if (comunidadId.HasValue)
            comunidades = comunidades.Where(c => c.ComunidadId == comunidadId.Value);
        resultado.TotalComunidades = await comunidades.CountAsync();
        resultado.ComunidadesActivas = await comunidades.CountAsync(c => c.Estado);

        // ---- Sensores (RF-ADM-61 / 62)
        var sensores = _db.Sensores.AsNoTracking().AsQueryable();
        if (comunidadId.HasValue)
            sensores = sensores.Where(s => s.ComunidadId == comunidadId.Value);
        resultado.SensoresActivos = await sensores.CountAsync(s => s.Estado);
        resultado.SensoresInactivos = await sensores.CountAsync(s => !s.Estado);

        // ---- Alertas activas (RF-ADM-43 / 63 / 64)
        var activas = _db.Alertas.AsNoTracking().Where(a => a.Estado == "Activa");
        if (comunidadId.HasValue)
            activas = activas.Where(a => a.Sensor != null && a.Sensor.ComunidadId == comunidadId.Value);

        var conteos = await activas
            .GroupBy(a => a.NivelRiesgo)
            .Select(g => new { Nivel = g.Key, Total = g.Count() })
            .ToListAsync();

        resultado.AlertasActivas = conteos.Sum(c => c.Total);
        resultado.AlertasPorNivel = Niveles
            .Select(n => new ConteoNivelDto
            {
                Nivel = n,
                Total = conteos.Where(c => string.Equals(c.Nivel, n, StringComparison.OrdinalIgnoreCase)).Sum(c => c.Total)
            })
            .ToList();

        // Nivel global: el más grave entre las alertas activas (Verde si no hay).
        resultado.NivelRiesgoGlobal = Niveles
            .Where(n => n != "Verde")
            .Reverse()
            .FirstOrDefault(n => resultado.AlertasPorNivel.Any(c => c.Nivel == n && c.Total > 0))
            ?? "Verde";

        var recientes = await activas
            .Include(a => a.Sensor).ThenInclude(s => s!.TipoSensor)
            .Include(a => a.Sensor).ThenInclude(s => s!.Comunidad)
            .Include(a => a.ReglaAlerta)
            .Include(a => a.Evento)
            .OrderByDescending(a => a.FechaEmision)
            .Take(6)
            .ToListAsync();

        resultado.AlertasActivasRecientes = recientes.Select(a => new AlertaActivaResumenDto
        {
            AlertaId = a.AlertaId,
            FechaEmision = a.FechaEmision,
            NivelRiesgo = a.NivelRiesgo,
            Fenomeno = a.Evento?.TipoFenomeno ?? a.ReglaAlerta?.TipoFenomeno,
            SensorNombre = a.Sensor?.NombreSensor,
            ComunidadNombre = a.Sensor?.Comunidad?.Nombre,
            ValorDetectado = a.ValorDetectado,
            Unidad = a.Sensor?.TipoSensor?.UnidadMedida,
            Mensaje = a.Mensaje
        }).ToList();

        // ---- Lecturas (RF-ADM-28 / 65): últimas "horas" de sensores activos
        var tipos = await _db.TiposSensor.AsNoTracking().ToListAsync();

        var lecturasQuery = _db.LecturasClimaticas.AsNoTracking()
            .Where(l => l.FechaHora >= desde && l.Sensor != null && l.Sensor.Estado);
        if (comunidadId.HasValue)
            lecturasQuery = lecturasQuery.Where(l => l.Sensor!.ComunidadId == comunidadId.Value);

        var lecturas = await lecturasQuery
            .Select(l => new
            {
                l.SensorId,
                TipoSensorId = l.Sensor!.TipoSensorId,
                l.Valor,
                l.FechaHora
            })
            .ToListAsync();

        // Valor actual por tipo = promedio de la última lectura de cada sensor.
        var ultimasPorSensor = lecturas
            .GroupBy(l => l.SensorId)
            .Select(g => g.OrderByDescending(x => x.FechaHora).First())
            .ToList();

        resultado.ValoresActuales = ultimasPorSensor
            .GroupBy(l => l.TipoSensorId)
            .Select(g =>
            {
                var tipo = tipos.FirstOrDefault(t => t.TipoSensorId == g.Key);
                return new ValorActualDto
                {
                    TipoSensorId = g.Key,
                    Tipo = tipo?.NombreTipo ?? "Sensor",
                    Unidad = tipo?.UnidadMedida ?? string.Empty,
                    Valor = Math.Round(g.Average(x => x.Valor), 2),
                    Sensores = g.Count(),
                    Fecha = g.Max(x => x.FechaHora)
                };
            })
            .OrderBy(v => v.TipoSensorId)
            .ToList();

        // Evolución: promedio por intervalo para no enviar miles de puntos.
        var minutosPorPunto = horas <= 1 ? 1 : horas <= 6 ? 5 : 15;
        var ticksPorPunto = TimeSpan.FromMinutes(minutosPorPunto).Ticks;

        resultado.Evolucion = lecturas
            .GroupBy(l => l.TipoSensorId)
            .Select(g =>
            {
                var tipo = tipos.FirstOrDefault(t => t.TipoSensorId == g.Key);
                return new SerieLecturasDto
                {
                    TipoSensorId = g.Key,
                    Tipo = tipo?.NombreTipo ?? "Sensor",
                    Unidad = tipo?.UnidadMedida ?? string.Empty,
                    Puntos = g
                        .GroupBy(x => x.FechaHora.Ticks / ticksPorPunto)
                        .OrderBy(b => b.Key)
                        .Select(b => new PuntoLecturaDto
                        {
                            Fecha = new DateTime(b.Key * ticksPorPunto),
                            Valor = Math.Round(b.Average(x => x.Valor), 2)
                        })
                        .ToList()
                };
            })
            .OrderBy(s => s.TipoSensorId)
            .ToList();

        // ---- Eventos climáticos recientes (RF-ADM-67)
        var eventos = _db.Eventos.AsNoTracking()
            .Include(e => e.Sensor)
            .Include(e => e.Comunidad)
            .AsQueryable();
        if (comunidadId.HasValue)
            eventos = eventos.Where(e => e.ComunidadId == comunidadId.Value);

        var listaEventos = await eventos
            .OrderByDescending(e => e.FechaHora)
            .Take(8)
            .ToListAsync();

        resultado.EventosRecientes = listaEventos.Select(e => new EventoResumenDto
        {
            EventoId = e.EventoId,
            FechaHora = e.FechaHora,
            Fenomeno = e.TipoFenomeno,
            NivelRiesgo = e.NivelRiesgo,
            ComunidadNombre = e.Comunidad?.Nombre,
            SensorNombre = e.Sensor?.NombreSensor,
            Valor = e.Valor,
            Estado = e.Estado
        }).ToList();

        return Ok(resultado);
    }
}
