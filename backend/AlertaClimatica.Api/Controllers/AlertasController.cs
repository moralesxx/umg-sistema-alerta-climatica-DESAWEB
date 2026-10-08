using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AlertaClimatica.Infrastructure;
using AlertaClimatica.Domain;

namespace AlertaClimatica.Api.Controllers;

// RF-ADM-07: Administrador acceso completo, Operador administra alertas,
// Usuario de consulta solo puede ver (GET sin restricción adicional de rol).
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

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Alerta>>> GetAlertas()
    {
        return await _context.Alertas
            .OrderByDescending(a => a.FechaEmision)
            .ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Alerta>> GetAlerta(int id)
    {
        var alerta = await _context.Alertas.FindAsync(id);
        if (alerta == null) return NotFound();
        return alerta;
    }

    [HttpPost]
    [Authorize(Roles = "Administrador,Operador")]
    public async Task<ActionResult<Alerta>> CreateAlerta(Alerta alerta)
    {
        if (alerta.FechaEmision == default)
        {
            alerta.FechaEmision = DateTime.Now;
        }

        _context.Alertas.Add(alerta);

        // El fenómeno se deduce del texto del mensaje (los 5 fenómenos oficiales, RF-ADM-47).
        // "helada" se revisa primero porque el mensaje normal también menciona "temperatura".
        string mensajeStr = alerta.Mensaje?.ToLower() ?? "";
        string tipoFenomenoAsociado;

        if (mensajeStr.Contains("helada"))
        {
            tipoFenomenoAsociado = "Helada";
        }
        else if (mensajeStr.Contains("desbordamiento") || mensajeStr.Contains("río"))
        {
            tipoFenomenoAsociado = "Inundación";
        }
        else if (mensajeStr.Contains("incendio"))
        {
            tipoFenomenoAsociado = "Incendio forestal";
        }
        else if (mensajeStr.Contains("sequía") || mensajeStr.Contains("suelo"))
        {
            tipoFenomenoAsociado = "Sequía";
        }
        else
        {
            tipoFenomenoAsociado = "Tormenta";
        }

        // RF-ADM-45: se busca el sensor para completar sensor y comunidad del evento.
        // Si el sensor no existe queda null, para no romper la FK al guardar.
        var sensor = await _context.Sensores
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SensorId == alerta.SensorId);

        var nuevoEvento = new Evento
        {
            TipoFenomeno = tipoFenomenoAsociado,
            Descripcion = alerta.Mensaje,
            FechaHora = alerta.FechaEmision,
            SensorId = sensor?.SensorId,
            ComunidadId = sensor?.ComunidadId,
            NivelRiesgo = alerta.NivelRiesgo,
            Estado = "Activa"
        };

        _context.Eventos.Add(nuevoEvento);

        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAlerta), new { id = alerta.AlertaId }, alerta);
    }

    // DELETE: api/alertas
    [HttpDelete]
    [Authorize(Roles = "Administrador,Operador")]
    public async Task<IActionResult> DeleteAllAlertas()
    {
        try
        {
            var todasLasAlertas = await _context.Alertas.ToListAsync();
            _context.Alertas.RemoveRange(todasLasAlertas);
            await _context.SaveChangesAsync();
            return Ok(new { mensaje = "Historial de alertas limpiado correctamente." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { mensaje = "No fue posible limpiar el historial de alertas." });
        }
    }
}