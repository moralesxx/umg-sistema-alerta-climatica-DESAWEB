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

    // GET: api/alertas
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Alerta>>> GetAlertas()
    {
        return await _context.Alertas
            .OrderByDescending(a => a.FechaEmision)
            .ToListAsync();
    }

    // GET: api/alertas/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Alerta>> GetAlerta(int id)
    {
        var alerta = await _context.Alertas.FindAsync(id);
        if (alerta == null) return NotFound();
        return alerta;
    }

    // POST: api/alertas
    [HttpPost]
    public async Task<ActionResult<Alerta>> CreateAlerta(Alerta alerta)
    {
        if (alerta.FechaEmision == default)
        {
            alerta.FechaEmision = DateTime.Now;
        }

        _context.Alertas.Add(alerta);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAlerta), new { id = alerta.AlertaId }, alerta);
    }
}