using AlertaClimatica.Domain;
using AlertaClimatica.Infrastructure;
using AlertaClimatica.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlertaClimatica.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BitacoraController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly BitacoraService _bitacoraService;

    public BitacoraController(
        ApplicationDbContext context,
        BitacoraService bitacoraService)
    {
        _context = context;
        _bitacoraService = bitacoraService;
    }

    // =========================================================
    // GET: api/bitacora
    // =========================================================
    //
    // Obtiene los últimos 100 registros de la bitácora.
    //
    // Requiere:
    // Authorization: Bearer <token>
    //
    // =========================================================

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Bitacora>>> GetBitacora()
    {
        var bitacoras = await _context.Bitacora
            .AsNoTracking()
            .OrderByDescending(b => b.FechaHora)
            .Take(100)
            .ToListAsync();

        return Ok(bitacoras);
    }

    // =========================================================
    // DTO PARA REGISTRAR UNA ACCIÓN
    // =========================================================
    //
    // IMPORTANTE:
    //
    // El frontend solamente envía:
    //
    // {
    //   "accion": "SIMULAR_ALERTA",
    //   "detalle": "El usuario simuló una alerta..."
    // }
    //
    // NO se envía UsuarioId.
    //
    // El UsuarioId se obtiene del JWT mediante BitacoraService.
    //
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
    public async Task<IActionResult> RegistrarAccion(
        [FromBody] RegistrarBitacoraDto entrada)
    {
        // =====================================================
        // Validar que exista información
        // =====================================================

        if (entrada == null)
        {
            return BadRequest(new
            {
                mensaje = "Los datos de la bitácora son obligatorios."
            });
        }

        // =====================================================
        // Validar acción
        // =====================================================

        if (string.IsNullOrWhiteSpace(entrada.Accion))
        {
            return BadRequest(new
            {
                mensaje = "La acción es obligatoria."
            });
        }

        // =====================================================
        // Registrar acción
        //
        // BitacoraService se encarga de:
        //
        // 1. Verificar que exista HttpContext.
        // 2. Verificar que el usuario esté autenticado.
        // 3. Obtener UsuarioId del JWT.
        // 4. Crear la entidad Bitacora.
        // 5. Guardarla en la base de datos.
        //
        // =====================================================

        await _bitacoraService.RegistrarAsync(
            entrada.Accion.Trim(),
            entrada.Detalle?.Trim() ?? string.Empty
        );

        // =====================================================
        // Respuesta
        // =====================================================

        return Ok(new
        {
            mensaje = "Acción registrada correctamente en la bitácora."
        });
    }
}