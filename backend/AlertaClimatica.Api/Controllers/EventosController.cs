using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AlertaClimatica.Infrastructure;
using AlertaClimatica.Domain;

namespace AlertaClimatica.Api.Controllers;

// El PDF no menciona "Eventos" en el ejemplo de RF-ADM-07, pero es parte
// del mismo dominio operativo (se generan a partir de Alertas). Aplico
// la misma matriz por consistencia: GET para cualquier autenticado,
// POST restringido a Administrador/Operador.
[ApiController]
[Route("api/[controller]")]
[Authorize]
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
    [Authorize(Roles = "Administrador,Operador")]
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