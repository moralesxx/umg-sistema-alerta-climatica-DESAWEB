using AlertaClimatica.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlertaClimatica.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BitacorasController : ControllerBase
{
    private readonly BitacoraService _bitacoraService;

    public BitacorasController(BitacoraService bitacoraService)
    {
        _bitacoraService = bitacoraService;
    }

    // =========================================================
    // POST: api/bitacoras
    // Registrar una acción del usuario autenticado
    // =========================================================

    [HttpPost]
    public async Task<IActionResult> Registrar(
        [FromBody] RegistrarBitacoraDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Accion))
        {
            return BadRequest(new
            {
                mensaje = "La acción es obligatoria."
            });
        }

        if (string.IsNullOrWhiteSpace(dto.Detalle))
        {
            return BadRequest(new
            {
                mensaje = "El detalle es obligatorio."
            });
        }

        await _bitacoraService.RegistrarAsync(
            dto.Accion,
            dto.Detalle
        );

        return Ok(new
        {
            mensaje = "Acción registrada correctamente en la bitácora."
        });
    }
}

// =========================================================
// DTO para registrar una acción
// =========================================================

public class RegistrarBitacoraDto
{
    public string Accion { get; set; } = string.Empty;

    public string Detalle { get; set; } = string.Empty;
}