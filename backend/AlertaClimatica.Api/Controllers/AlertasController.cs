using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AlertaClimatica.Infrastructure;
using AlertaClimatica.Domain;

namespace AlertaClimatica.Api.Controllers;

// RF-ADM-07: Administrador acceso completo, Operador administra alertas,
// Usuario de consulta solo puede ver.
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AlertasController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AlertasController(ApplicationDbContext context)
    {
        _context = context;
    }

    // ============================================================
    // GET: api/alertas
    // Obtener todas las alertas
    // ============================================================
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Alerta>>> GetAlertas()
    {
        return await _context.Alertas
            .Include(a => a.Sensor)
                .ThenInclude(s => s!.TipoSensor)
            .Include(a => a.ReglaAlerta)
            .OrderByDescending(a => a.FechaEmision)
            .ToListAsync();
    }

    // ============================================================
    // GET: api/alertas/{id}
    // Obtener una alerta específica
    // ============================================================
    [HttpGet("{id}")]
    public async Task<ActionResult<Alerta>> GetAlerta(int id)
    {
        var alerta = await _context.Alertas
            .Include(a => a.Sensor)
                .ThenInclude(s => s!.TipoSensor)
            .Include(a => a.ReglaAlerta)
            .FirstOrDefaultAsync(a => a.AlertaId == id);

        if (alerta == null)
            return NotFound();

        return alerta;
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
            // Si no viene fecha, usamos la fecha actual.
            if (alerta.FechaEmision == default)
            {
                alerta.FechaEmision = DateTime.Now;
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
                FechaHora = alerta.FechaEmision
            };

            _context.Eventos.Add(nuevoEvento);

            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetAlerta),
                new { id = alerta.AlertaId },
                alerta
            );
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                mensaje = "No fue posible crear la alerta.",
                detalle = ex.Message
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
            texto.Contains("río") ||
            texto.Contains("rio") ||
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
             texto.Contains("río") ||
             texto.Contains("rio")))
        {
            return "Inundación";
        }

        return "Tormenta";
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

            _context.Alertas.RemoveRange(todasLasAlertas);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensaje = "Historial de alertas limpiado correctamente."
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                mensaje = "No fue posible limpiar el historial de alertas.",
                detalle = ex.Message
            });
        }
    }
}