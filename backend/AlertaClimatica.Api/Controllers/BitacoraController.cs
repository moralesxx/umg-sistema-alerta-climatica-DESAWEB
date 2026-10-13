using AlertaClimatica.Domain;
using AlertaClimatica.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlertaClimatica.Api.Controllers;

// Configuración general del controlador de API y restricciones de seguridad base
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BitacoraController : ControllerBase
{
    // Servicio privado para manejar la lógica de negocio de la bitácora
    private readonly IBitacoraService _bitacoraService;

    // Constructor que inyecta la dependencia del servicio de bitácora
    public BitacoraController(IBitacoraService bitacoraService)
    {
        _bitacoraService = bitacoraService;
    }

    // =========================================================
    // GET: api/bitacora
    // Últimos 100 registros. Requiere estar autenticado.
    // =========================================================

    // Endpoint GET exclusivo para Administradores que recupera los últimos registros de auditoría
    // (RF-ADM-58). Por defecto 500; con ?limite=N se pide otra cantidad (máximo 2000).
    [HttpGet]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult<IEnumerable<Bitacora>>> GetBitacora([FromQuery] int limite = 500)
    {
        limite = Math.Clamp(limite, 1, 2000);
        // Llama al servicio para obtener la lista limitada de registros
        var bitacoras = await _bitacoraService.ObtenerUltimosAsync(limite);
        // Retorna la respuesta HTTP 200 con la colección de datos
        return Ok(bitacoras);
    }

    // Nota: ya no existe POST api/bitacora. Los registros de auditoría los crea
    // únicamente el backend en cada operación (RF-ADM-56 / RNF-ADM-08).
}