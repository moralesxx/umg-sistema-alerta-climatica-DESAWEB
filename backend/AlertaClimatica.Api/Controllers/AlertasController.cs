using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AlertaClimatica.Infrastructure;
using AlertaClimatica.Domain;

namespace AlertaClimatica.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
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
    public async Task<ActionResult<Alerta>> CreateAlerta(Alerta alerta)
    {
        if (alerta.FechaEmision == default)
        {
            alerta.FechaEmision = DateTime.Now;
        }

        _context.Alertas.Add(alerta);

        // Mapeo preciso a los 5 fenómenos climáticos oficiales de la rúbrica[cite: 2]
        string nivelStr = alerta.NivelRiesgo?.ToLower() ?? "";
        string mensajeStr = alerta.Mensaje?.ToLower() ?? "";
        string tipoFenomenoAsociado = "Tormenta";

        if (nivelStr.Contains("rojo") || mensajeStr.Contains("desbordamiento") || mensajeStr.Contains("río"))
        {
            tipoFenomenoAsociado = "Inundación";
        }
        else if (nivelStr.Contains("naranja") || mensajeStr.Contains("incendio") || mensajeStr.Contains("temperatura"))
        {
            tipoFenomenoAsociado = "Incendio forestal";
        }
        else if (nivelStr.Contains("amarillo") || mensajeStr.Contains("sequía") || mensajeStr.Contains("suelo"))
        {
            tipoFenomenoAsociado = "Sequía";
        }
        else if (nivelStr.Contains("verde") || mensajeStr.Contains("helada"))
        {
            tipoFenomenoAsociado = "Helada";
        }
        else
        {
            tipoFenomenoAsociado = "Tormenta";
        }

        var nuevoEvento = new Evento
        {
            TipoFenomeno = tipoFenomenoAsociado,
            Descripcion = alerta.Mensaje,
            FechaHora = alerta.FechaEmision
        };

        _context.Eventos.Add(nuevoEvento);

        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAlerta), new { id = alerta.AlertaId }, alerta);
    }

    // DELETE: api/alertas
    [HttpDelete]
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
            return StatusCode(500, new { mensaje = "Error al limpiar las alertas", detalle = ex.Message });
        }
    }
}