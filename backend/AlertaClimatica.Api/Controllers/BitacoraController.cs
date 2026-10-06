using AlertaClimatica.Domain;
using AlertaClimatica.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlertaClimatica.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BitacoraController : ControllerBase
{
    private readonly IBitacoraService _bitacoraService;

    public BitacoraController(IBitacoraService bitacoraService)
    {
        _bitacoraService = bitacoraService;
    }

    // =========================================================
    // GET: api/bitacora
    // Últimos 100 registros. Requiere estar autenticado.
    // =========================================================

    [HttpGet]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult<IEnumerable<Bitacora>>> GetBitacora()
    {
        var bitacoras = await _bitacoraService.ObtenerUltimosAsync(100);
        return Ok(bitacoras);
    }

    // =========================================================
    // DTO PARA REGISTRAR UNA ACCIÓN
    //
    // El frontend solamente envía { "accion": "...", "detalle": "..." }.
    // El UsuarioId se obtiene del JWT mediante BitacoraService.
    // =========================================================

    public class RegistrarBitacoraDto
    {
        public string Accion { get; set; } = string.Empty;
        public string Detalle { get; set; } = string.Empty;
    }

    // =========================================================
    // POST: api/bitacora
    // =========================================================

    [HttpPost]
    [Authorize(Roles = "Administrador,Operador")]
    public async Task<IActionResult> RegistrarAccion(
        [FromBody] RegistrarBitacoraDto entrada)
    {
        if (entrada == null)
        {
            return BadRequest(new { mensaje = "Los datos de la bitácora son obligatorios." });
        }

        if (string.IsNullOrWhiteSpace(entrada.Accion))
        {
            return BadRequest(new { mensaje = "La acción es obligatoria." });
        }

        await _bitacoraService.RegistrarAsync(
            entrada.Accion.Trim(),
            entrada.Detalle?.Trim() ?? string.Empty
        );

        return Ok(new { mensaje = "Acción registrada correctamente en la bitácora." });
    }
}