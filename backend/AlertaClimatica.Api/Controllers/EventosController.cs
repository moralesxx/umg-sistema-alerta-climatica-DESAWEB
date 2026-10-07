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
    // Contexto de base de datos inyectado para realizar consultas y operaciones directas con Entity Framework
    private readonly ApplicationDbContext _context;

    // Constructor que inicializa el controlador recibiendo el contexto de la base de datos
    public EventosController(ApplicationDbContext context)
    {
        _context = context;
    }

    // Endpoint GET que permite consultar la lista completa de eventos ordenada de forma descendente por su fecha y hora
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Evento>>> GetEventos()
    {
        // Consulta los eventos en la base de datos ordenándolos del más reciente al más antiguo y los retorna como lista
        return await _context.Eventos
            .OrderByDescending(e => e.FechaHora)
            .ToListAsync();
    }

    // Endpoint POST accesible por Administradores y Operadores para registrar un nuevo evento en el sistema
    [HttpPost]
    [Authorize(Roles = "Administrador,Operador")]
    public async Task<ActionResult<Evento>> CreateEvento(Evento evento)
    {
        // Validación: Si la fecha y hora no fueron asignadas, se establece automáticamente la fecha y hora actual
        if (evento.FechaHora == default)
        {
            evento.FechaHora = DateTime.Now;
        }

        // Agrega el nuevo evento al contexto y guarda los cambios en la base de datos de manera asíncrona
        _context.Eventos.Add(evento);
        await _context.SaveChangesAsync();

        // Retorna una respuesta HTTP 201 Created apuntando al método de consulta general de eventos
        return CreatedAtAction(nameof(GetEventos), new { id = evento.EventoId }, evento);
    }
}