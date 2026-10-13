using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.RegularExpressions;
using AlertaClimatica.Application.DTOs;
using AlertaClimatica.Infrastructure;
using AlertaClimatica.Infrastructure.Services;
using AlertaClimatica.Domain;

namespace AlertaClimatica.Api.Controllers;

// RF-ADM-07: Administrador acceso completo, Operador administra alertas,
// Usuario de consulta solo puede ver.
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AlertasController : ControllerBase
{
    // Mismos valores que usa el historial de eventos (RF-ADM-31).
    private static readonly string[] Niveles = { "Verde", "Amarillo", "Naranja", "Rojo" };

    private readonly ApplicationDbContext _context;
    private readonly IBitacoraService _bitacoraService;
    private readonly ILogger<AlertasController> _logger;

    public AlertasController(
        ApplicationDbContext context,
        IBitacoraService bitacoraService,
        ILogger<AlertasController> logger)
    {
        _context = context;
        _bitacoraService = bitacoraService;
        _logger = logger;
    }

    // ============================================================
    // GET: api/alertas?fechaDesde=&fechaHasta=&comunidadId=&sensorId=&fenomeno=&nivel=&estado=
    // RF-ADM-37 / 38 / 39: listado con filtros opcionales (más recientes primero).
    // ============================================================
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AlertaDto>>> GetAlertas(
        [FromQuery] DateTime? fechaDesde,
        [FromQuery] DateTime? fechaHasta,
        [FromQuery] int? comunidadId,
        [FromQuery] int? sensorId,
        [FromQuery] string? fenomeno,
        [FromQuery] string? nivel,
        [FromQuery] string? estado)
    {
        var query = ConsultaBase();

        if (fechaDesde.HasValue)
            query = query.Where(a => a.FechaEmision >= fechaDesde.Value.Date);

        if (fechaHasta.HasValue)
        {
            var finDia = fechaHasta.Value.Date.AddDays(1);
            query = query.Where(a => a.FechaEmision < finDia);
        }

        if (comunidadId.HasValue)
            query = query.Where(a => a.Sensor != null && a.Sensor.ComunidadId == comunidadId.Value);

        if (sensorId.HasValue)
            query = query.Where(a => a.SensorId == sensorId.Value);

        if (!string.IsNullOrWhiteSpace(nivel))
        {
            var n = nivel.Trim();
            query = query.Where(a => a.NivelRiesgo == n);
        }

        if (!string.IsNullOrWhiteSpace(estado))
        {
            var e = estado.Trim();
            query = query.Where(a => a.Estado == e);
        }

        if (!string.IsNullOrWhiteSpace(fenomeno))
        {
            var f = fenomeno.Trim();
            query = query.Where(a =>
                (a.Evento != null && a.Evento.TipoFenomeno == f) ||
                (a.Evento == null && a.ReglaAlerta != null && a.ReglaAlerta.TipoFenomeno == f));
        }

        var alertas = await query
            .OrderByDescending(a => a.FechaEmision)
            .Take(500)
            .ToListAsync();

        return Ok(await ConvertirAsync(alertas));
    }

    // ============================================================
    // GET: api/alertas/{id}
    // RF-ADM-40: detalle de una alerta
    // ============================================================
    [HttpGet("{id}")]
    public async Task<ActionResult<AlertaDto>> GetAlerta(int id)
    {
        var alerta = await ConsultaBase().FirstOrDefaultAsync(a => a.AlertaId == id);

        if (alerta == null)
            return NotFound(new { mensaje = "Alerta no encontrada." });

        var dto = (await ConvertirAsync(new List<Alerta> { alerta })).First();
        return Ok(dto);
    }

    // ============================================================
    // POST: api/alertas/{id}/atender
    // RF-ADM-41 / 42: Activa -> Atendida, registrando quién la atendió.
    // ============================================================
    [HttpPost("{id}/atender")]
    [Authorize(Roles = "Administrador,Operador")]
    public Task<IActionResult> Atender(int id) => CambiarEstadoAsync(id, "Atendida");

    // ============================================================
    // POST: api/alertas/{id}/cerrar
    // RF-ADM-41 / 42: Activa o Atendida -> Cerrada, registrando quién la cerró.
    // ============================================================
    [HttpPost("{id}/cerrar")]
    [Authorize(Roles = "Administrador,Operador")]
    public Task<IActionResult> Cerrar(int id) => CambiarEstadoAsync(id, "Cerrada");

    private async Task<IActionResult> CambiarEstadoAsync(int id, string nuevoEstado)
    {
        var alerta = await _context.Alertas
            .Include(a => a.Evento)
            .FirstOrDefaultAsync(a => a.AlertaId == id);

        if (alerta == null)
            return NotFound(new { mensaje = "Alerta no encontrada." });

        if (alerta.Estado == "Cerrada")
            return Conflict(new { mensaje = "La alerta ya está cerrada." });

        if (nuevoEstado == "Atendida" && alerta.Estado != "Activa")
            return Conflict(new { mensaje = "Solo se puede atender una alerta activa." });

        var usuarioIdTexto = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                             ?? User.FindFirst("sub")?.Value;
        if (!int.TryParse(usuarioIdTexto, out var usuarioId))
            return Unauthorized(new { mensaje = "No se pudo identificar al usuario." });

        var ahora = DateTime.Now;
        var anterior = alerta.Estado;

        alerta.Estado = nuevoEstado;
        alerta.UsuarioResponsableId = usuarioId;
        alerta.FechaGestion = ahora;

        // El evento del historial refleja el mismo estado y responsable.
        if (alerta.Evento != null)
        {
            alerta.Evento.Estado = nuevoEstado;
            alerta.Evento.UsuarioResponsableId = usuarioId;
        }

        await _context.SaveChangesAsync();

        var accion = nuevoEstado == "Atendida" ? "ATENDER_ALERTA" : "CERRAR_ALERTA";
        await _bitacoraService.RegistrarAsync(
            accion,
            $"La alerta #{alerta.AlertaId} pasó de {anterior} a {nuevoEstado}.",
            "Alerta",
            alerta.AlertaId);

        return Ok(new { mensaje = $"Alerta {nuevoEstado.ToLowerInvariant()}.", alertaId = alerta.AlertaId, estado = nuevoEstado });
    }

    // ------------------------------------------------------------
    // Auxiliares de consulta y conversión a AlertaDto
    // ------------------------------------------------------------
    private IQueryable<Alerta> ConsultaBase()
    {
        return _context.Alertas
            .AsNoTracking()
            .Include(a => a.Sensor).ThenInclude(s => s!.TipoSensor)
            .Include(a => a.Sensor).ThenInclude(s => s!.Comunidad)
            .Include(a => a.ReglaAlerta)
            .Include(a => a.Evento);
    }

    private async Task<List<AlertaDto>> ConvertirAsync(List<Alerta> alertas)
    {
        var ids = alertas
            .Where(a => a.UsuarioResponsableId.HasValue)
            .Select(a => a.UsuarioResponsableId!.Value)
            .Distinct()
            .ToList();

        var nombres = ids.Count == 0
            ? new Dictionary<int, string>()
            : await _context.Usuarios
                .Where(u => ids.Contains(u.UsuarioId))
                .ToDictionaryAsync(u => u.UsuarioId, u => u.Nombre);

        return alertas.Select(a => new AlertaDto
        {
            AlertaId = a.AlertaId,
            FechaEmision = a.FechaEmision,
            SensorId = a.SensorId,
            SensorCodigo = a.Sensor?.Codigo,
            SensorNombre = a.Sensor?.NombreSensor,
            ComunidadId = a.Sensor?.ComunidadId,
            ComunidadNombre = a.Sensor?.Comunidad?.Nombre,
            ReglaAlertaId = a.ReglaAlertaId,
            ReglaNombre = a.ReglaAlerta?.Nombre,
            Fenomeno = a.Evento?.TipoFenomeno ?? a.ReglaAlerta?.TipoFenomeno,
            NivelRiesgo = a.NivelRiesgo,
            ValorDetectado = a.ValorDetectado,
            Unidad = a.Sensor?.TipoSensor?.UnidadMedida,
            Umbral = FormatearUmbral(a.ReglaAlerta),
            Mensaje = a.Mensaje,
            Estado = a.Estado,
            EventoId = a.EventoId,
            UsuarioResponsableId = a.UsuarioResponsableId,
            UsuarioResponsableNombre = a.UsuarioResponsableId.HasValue
                && nombres.TryGetValue(a.UsuarioResponsableId.Value, out var nombre) ? nombre : null,
            FechaGestion = a.FechaGestion
        }).ToList();
    }

    private static string? FormatearUmbral(ReglaAlerta? r)
    {
        if (r == null) return null;
        if (r.ValorMinimo.HasValue && r.ValorMaximo.HasValue)
            return $"{r.ValorMinimo:0.##} – {r.ValorMaximo:0.##}";
        if (r.ValorMinimo.HasValue) return $"≥ {r.ValorMinimo:0.##}";
        if (r.ValorMaximo.HasValue) return $"≤ {r.ValorMaximo:0.##}";
        return null;
    }

    // ============================================================
    // POST: api/alertas
    // Crear una alerta manualmente
    // ============================================================
    [HttpPost]
    [Authorize(Roles = "Administrador,Operador")]
    public async Task<ActionResult<Alerta>> CreateAlerta(Alerta alerta)
    {
        try
        {
            // Lo que el cliente NO debe decidir: el id y los objetos relacionados.
            // (Si llegaran en el JSON, EF intentaría insertar también un Sensor/Regla.)
            alerta.AlertaId = 0;
            alerta.Sensor = null!;
            alerta.ReglaAlerta = null!;
            alerta.Evento = null;
            alerta.EventoId = null;
            alerta.Estado = "Activa";
            alerta.UsuarioResponsableId = null;
            alerta.FechaGestion = null;

            // Si no viene fecha, usamos la fecha actual. Si viene en UTC (con "Z")
            // se pasa a hora local para no mezclar zonas con el resto del sistema.
            if (alerta.FechaEmision == default)
            {
                alerta.FechaEmision = DateTime.Now;
            }
            else if (alerta.FechaEmision.Kind == DateTimeKind.Utc)
            {
                alerta.FechaEmision = alerta.FechaEmision.ToLocalTime();
            }

            // Validar que el sensor exista.
            var sensor = await _context.Sensores
                .Include(s => s.TipoSensor)
                .FirstOrDefaultAsync(s => s.SensorId == alerta.SensorId);

            if (sensor == null)
            {
                return BadRequest(new
                {
                    mensaje = "El sensor indicado no existe."
                });
            }

            // Si se indicó una regla, verificar que exista.
            ReglaAlerta? regla = null;

            if (alerta.ReglaAlertaId.HasValue)
            {
                regla = await _context.ReglasAlerta
                    .FirstOrDefaultAsync(r =>
                        r.ReglaAlertaId == alerta.ReglaAlertaId.Value);

                if (regla == null)
                {
                    return BadRequest(new
                    {
                        mensaje = "La regla de alerta indicada no existe."
                    });
                }

                // Si la alerta tiene una regla asociada,
                // utilizamos los datos oficiales de esa regla.
                alerta.NivelRiesgo = regla.NivelPeligro;
                alerta.Mensaje = regla.Mensaje;
            }

            // El nivel debe ser uno de los 4 oficiales: si no, el historial y
            // las estadísticas de eventos quedarían con valores sueltos.
            var nivelOficial = Niveles.FirstOrDefault(n =>
                n.Equals(alerta.NivelRiesgo?.Trim(), StringComparison.OrdinalIgnoreCase));

            if (nivelOficial == null)
            {
                return BadRequest(new
                {
                    mensaje = "El nivel de riesgo debe ser: Verde, Amarillo, Naranja o Rojo."
                });
            }

            alerta.NivelRiesgo = nivelOficial;

            _context.Alertas.Add(alerta);

            // ====================================================
            // Crear evento asociado
            // ====================================================

            string tipoFenomeno;

            if (regla != null)
            {
                // Para alertas asociadas a reglas,
                // utilizamos directamente el fenómeno configurado.
                tipoFenomeno = regla.TipoFenomeno;
            }
            else
            {
                // Compatibilidad con alertas manuales que no tengan
                // una ReglaAlertaId.
                tipoFenomeno = DeterminarFenomeno(
                    alerta.NivelRiesgo,
                    alerta.Mensaje
                );
            }

            var nuevoEvento = new Evento
            {
                TipoFenomeno = tipoFenomeno,
                Descripcion = alerta.Mensaje,
                FechaHora = alerta.FechaEmision,
                // Datos que necesita el Historial (filtros y estadisticas):
                SensorId = sensor.SensorId,
                ComunidadId = sensor.ComunidadId,
                NivelRiesgo = alerta.NivelRiesgo,
                Estado = "Activa"
            };

            _context.Eventos.Add(nuevoEvento);
            alerta.Evento = nuevoEvento; // enlaza alerta y evento

            await _context.SaveChangesAsync();

            // RF-ADM-56: toda alta queda en la bitácora de auditoría.
            await _bitacoraService.RegistrarAsync(
                "CREAR_ALERTA",
                $"Se creó la alerta #{alerta.AlertaId} (nivel {alerta.NivelRiesgo}) para el sensor #{sensor.SensorId}.",
                "Alerta",
                alerta.AlertaId
            );

            return CreatedAtAction(
                nameof(GetAlerta),
                new { id = alerta.AlertaId },
                alerta
            );
        }
        catch (Exception ex)
        {
            // El detalle técnico va al log del servidor, no al cliente.
            _logger.LogError(ex, "No fue posible crear la alerta.");

            return StatusCode(500, new
            {
                mensaje = "No fue posible crear la alerta."
            });
        }
    }

    // ============================================================
    // Método auxiliar para determinar el fenómeno
    // solamente para alertas manuales sin ReglaAlertaId.
    // ============================================================
    private static string DeterminarFenomeno(
        string? nivelRiesgo,
        string? mensaje)
    {
        string nivel = nivelRiesgo?.ToLowerInvariant() ?? "";
        string texto = mensaje?.ToLowerInvariant() ?? "";

        if (texto.Contains("desbordamiento") ||
            ContienePalabra(texto, "río") ||
            ContienePalabra(texto, "rio") ||
            texto.Contains("inundación") ||
            texto.Contains("inundacion"))
        {
            return "Inundación";
        }

        if (texto.Contains("incendio"))
        {
            return "Incendio forestal";
        }

        if (texto.Contains("sequía") ||
            texto.Contains("sequia"))
        {
            return "Sequía";
        }

        if (texto.Contains("helada"))
        {
            return "Helada";
        }

        if (texto.Contains("tormenta") ||
            texto.Contains("viento"))
        {
            return "Tormenta";
        }

        // Compatibilidad con el comportamiento anterior.
        if (nivel.Contains("rojo") &&
            (texto.Contains("agua") ||
             ContienePalabra(texto, "río") ||
             ContienePalabra(texto, "rio")))
        {
            return "Inundación";
        }

        return "Tormenta";
    }

    // Busca la palabra completa (no una parte de otra): "río" si, "frío" no.
    private static bool ContienePalabra(string texto, string palabra)
    {
        return Regex.IsMatch(texto, @"\b" + Regex.Escape(palabra) + @"\b");
    }

    // ============================================================
    // DELETE: api/alertas
    // Limpiar historial de alertas
    // ============================================================
    [HttpDelete]
    [Authorize(Roles = "Administrador,Operador")]
    public async Task<IActionResult> DeleteAllAlertas()
    {
        try
        {
            var todasLasAlertas = await _context.Alertas.ToListAsync();
            var cantidad = todasLasAlertas.Count;

            _context.Alertas.RemoveRange(todasLasAlertas);

            await _context.SaveChangesAsync();

            // RF-ADM-56 (eliminación): se deja constancia de quién limpió el historial.
            // Los eventos NO se borran: el historial de eventos (RF-ADM-44) se conserva.
            await _bitacoraService.RegistrarAsync(
                "ELIMINAR_ALERTAS",
                $"Se eliminaron {cantidad} alertas (limpieza del historial de alertas).",
                "Alerta"
            );

            return Ok(new
            {
                mensaje = "Historial de alertas limpiado correctamente."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No fue posible limpiar el historial de alertas.");

            return StatusCode(500, new
            {
                mensaje = "No fue posible limpiar el historial de alertas."
            });
        }
    }
}