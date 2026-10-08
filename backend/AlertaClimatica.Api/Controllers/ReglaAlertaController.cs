using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AlertaClimatica.Application.DTOs;
using AlertaClimatica.Infrastructure.Services.Interfaces;

namespace AlertaClimatica.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReglaAlertaController : ControllerBase
{
    private readonly IReglaAlertaService _service;

    public ReglaAlertaController(IReglaAlertaService service)
    {
        _service = service;
    }

    // =========================================================
    // GET: api/ReglaAlerta
    // Obtener todas las reglas
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> ObtenerTodas()
    {
        var reglas = await _service.ObtenerTodasAsync();

        return Ok(reglas);
    }

    // =========================================================
    // GET: api/ReglaAlerta/{id}
    // Obtener una regla por ID
    // =========================================================

    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        var regla = await _service.ObtenerPorIdAsync(id);

        if (regla == null)
        {
            return NotFound(new
            {
                mensaje = "La regla de alerta no existe."
            });
        }

        return Ok(regla);
    }

    // =========================================================
    // POST: api/ReglaAlerta
    // Crear regla
    // Solo Administrador
    // =========================================================

    [HttpPost]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Crear(
        [FromBody] CrearReglaAlertaDto dto)
    {
        try
        {
            var regla = await _service.CrearAsync(dto);

            return CreatedAtAction(
                nameof(ObtenerPorId),
                new { id = regla.ReglaAlertaId },
                regla);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                mensaje = ex.Message
            });
        }
    }

    // =========================================================
    // PUT: api/ReglaAlerta/{id}
    // Editar regla
    // Solo Administrador
    // =========================================================

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Actualizar(
        int id,
        [FromBody] ActualizarReglaAlertaDto dto)
    {
        try
        {
            var regla = await _service.ActualizarAsync(id, dto);

            if (regla == null)
            {
                return NotFound(new
                {
                    mensaje = "La regla de alerta no existe."
                });
            }

            return Ok(regla);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                mensaje = ex.Message
            });
        }
    }

    // =========================================================
    // PATCH: api/ReglaAlerta/{id}/estado
    // Activar / desactivar regla
    // Solo Administrador
    // =========================================================

    [HttpPatch("{id:int}/estado")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> CambiarEstado(
        int id,
        [FromBody] bool estado)
    {
        var regla = await _service.ObtenerPorIdAsync(id);

        if (regla == null)
        {
            return NotFound(new
            {
                mensaje = "La regla de alerta no existe."
            });
        }

        regla.Estado = estado;

        var dto = new ActualizarReglaAlertaDto
        {
            Nombre = regla.Nombre,
            TipoSensorId = regla.TipoSensorId,
            ValorMinimo = regla.ValorMinimo,
            ValorMaximo = regla.ValorMaximo,
            NivelPeligro = regla.NivelPeligro,
            TipoFenomeno = regla.TipoFenomeno,
            Mensaje = regla.Mensaje,
            Estado = estado
        };

        var actualizada = await _service.ActualizarAsync(id, dto);

        return Ok(new
        {
            mensaje = estado
                ? "Regla de alerta activada correctamente."
                : "Regla de alerta desactivada correctamente.",
            regla = actualizada
        });
    }

    // =========================================================
    // DELETE: api/ReglaAlerta/{id}
    // Eliminar regla
    // Solo Administrador
    // =========================================================

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Eliminar(int id)
    {
        try
        {
            var eliminado = await _service.EliminarAsync(id);

            if (!eliminado)
            {
                return NotFound(new
                {
                    mensaje = "La regla de alerta no existe."
                });
            }

            return Ok(new
            {
                mensaje = "Regla de alerta eliminada correctamente."
            });
        }
        catch (Exception ex)
        {
            return Conflict(new
            {
                mensaje = "No se puede eliminar la regla porque puede estar asociada a alertas existentes.",
                detalle = ex.Message
            });
        }
    }
}