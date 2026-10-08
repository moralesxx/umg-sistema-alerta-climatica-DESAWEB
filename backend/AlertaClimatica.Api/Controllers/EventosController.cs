using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AlertaClimatica.Application.DTOs;
using AlertaClimatica.Infrastructure.Services;

namespace AlertaClimatica.Api.Controllers;

// RF-ADM-07: consultar (GET) cualquier usuario autenticado; registrar (POST) solo Administrador y Operador.
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EventosController : ControllerBase
{
    private readonly IEventoService _eventoService;

    public EventosController(IEventoService eventoService)
    {
        _eventoService = eventoService;
    }

    // GET: api/eventos?fechaInicio=2026-10-01&fechaFin=2026-10-07&comunidadId=1&fenomeno=Inundación&nivel=Rojo
    // RF-ADM-46 y RF-ADM-47. Todos los filtros son opcionales.
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EventoDto>>> GetEventos(
        [FromQuery] DateTime? fechaInicio,
        [FromQuery] DateTime? fechaFin,
        [FromQuery] int? comunidadId,
        [FromQuery] string? fenomeno,
        [FromQuery] string? nivel)
    {
        var eventos = await _eventoService.GetEventosAsync(fechaInicio, fechaFin, comunidadId, fenomeno, nivel);
        return Ok(eventos);
    }

    // GET: api/eventos/estadisticas (mismos filtros). RF-ADM-48
    [HttpGet("estadisticas")]
    public async Task<ActionResult<EventoEstadisticasDto>> GetEstadisticas(
        [FromQuery] DateTime? fechaInicio,
        [FromQuery] DateTime? fechaFin,
        [FromQuery] int? comunidadId,
        [FromQuery] string? fenomeno,
        [FromQuery] string? nivel)
    {
        var stats = await _eventoService.GetEstadisticasAsync(fechaInicio, fechaFin, comunidadId, fenomeno, nivel);
        return Ok(stats);
    }

    // POST: api/eventos. RF-ADM-44
    [HttpPost]
    [Authorize(Roles = "Administrador,Operador")]
    public async Task<ActionResult<EventoDto>> CreateEvento([FromBody] CrearEventoDto dto)
    {
        try
        {
            var creado = await _eventoService.CrearEventoAsync(dto);
            return CreatedAtAction(nameof(GetEventos), new { id = creado.EventoId }, creado);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }
}