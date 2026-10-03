using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AlertaClimatica.Application.DTOs;
using AlertaClimatica.Infrastructure.Exceptions;
using AlertaClimatica.Infrastructure.Services;

namespace AlertaClimatica.Api.Controllers;

// RF-ADM-07: cualquier usuario autenticado puede consultar (GET), incluido
// "Usuario de consulta". Crear, editar y activar/desactivar son solo del
// Administrador (RF-ADM-08, 09 y 10); el PDF no le da comunidades al Operador.
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ComunidadesController : ControllerBase
{
    private readonly IComunidadService _comunidadService;

    public ComunidadesController(IComunidadService comunidadService)
    {
        _comunidadService = comunidadService;
    }

    // GET: api/comunidades
    // GET: api/comunidades?busqueda=sol&estado=true&municipio=Antigua&departamento=Sacatep
    // RF-ADM-11 y RF-ADM-12. Todos los filtros son opcionales.
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ComunidadDto>>> GetComunidades(
        [FromQuery] string? busqueda,
        [FromQuery] bool? estado,
        [FromQuery] string? municipio,
        [FromQuery] string? departamento)
    {
        var comunidades = await _comunidadService.GetComunidadesAsync(busqueda, estado, municipio, departamento);
        return Ok(comunidades);
    }

    // GET: api/comunidades/5
    [HttpGet("{id}")]
    public async Task<ActionResult<ComunidadDto>> GetComunidad(int id)
    {
        var comunidad = await _comunidadService.GetComunidadAsync(id);

        if (comunidad == null)
            return NotFound(new { mensaje = "Comunidad no encontrada." });

        return Ok(comunidad);
    }

    // POST: api/comunidades
    // RF-ADM-08
    [HttpPost]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult<ComunidadDto>> CreateComunidad([FromBody] CrearComunidadDto dto)
    {
        try
        {
            var creada = await _comunidadService.CrearComunidadAsync(dto);
            return CreatedAtAction(nameof(GetComunidad), new { id = creada.ComunidadId }, creada);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (ConflictException ex)
        {
            return Conflict(new { mensaje = ex.Message });
        }
    }

    // PUT: api/comunidades/5
    // RF-ADM-09
    [HttpPut("{id}")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> UpdateComunidad(int id, [FromBody] ActualizarComunidadDto dto)
    {
        try
        {
            var actualizada = await _comunidadService.ActualizarComunidadAsync(id, dto);

            if (!actualizada)
                return NotFound(new { mensaje = "Comunidad no encontrada." });

            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (ConflictException ex)
        {
            return Conflict(new { mensaje = ex.Message });
        }
    }

    // PATCH: api/comunidades/5/estado
    // RF-ADM-10: activar o desactivar. El cuerpo es solo true o false.
    [HttpPatch("{id}/estado")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> CambiarEstadoComunidad(int id, [FromBody] bool activa)
    {
        var actualizada = await _comunidadService.CambiarEstadoComunidadAsync(id, activa);

        if (!actualizada)
            return NotFound(new { mensaje = "Comunidad no encontrada." });

        return Ok(new { mensaje = "Estado de la comunidad actualizado correctamente.", comunidadId = id, nuevoEstado = activa });
    }
}
