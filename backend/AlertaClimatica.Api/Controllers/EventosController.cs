using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AlertaClimatica.Infrastructure;
using AlertaClimatica.Domain;

namespace AlertaClimatica.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventosController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public EventosController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Evento>>> GetEventos()
    {
        return await _context.Eventos
            .OrderByDescending(e => e.FechaHora)
            .ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<Evento>> CreateEvento(Evento evento)
    {
        if (evento.FechaHora == default)
        {
            evento.FechaHora = DateTime.Now;
        }

        _context.Eventos.Add(evento);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetEventos), new { id = evento.EventoId }, evento);
    }
}