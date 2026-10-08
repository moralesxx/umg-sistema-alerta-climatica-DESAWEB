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

    // Endpoint GET exclusivo para Administradores que recupera los últimos 100 registros de auditoría
    [HttpGet]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult<IEnumerable<Bitacora>>> GetBitacora()
    {
        // Llama al servicio para obtener la lista limitada de registros
        var bitacoras = await _bitacoraService.ObtenerUltimosAsync(100);
        // Retorna la respuesta HTTP 200 con la colección de datos
        return Ok(bitacoras);
    }

    // =========================================================
    // DTO PARA REGISTRAR UNA ACCIÓN
    //
    // El frontend solamente envía { "accion": "...", "detalle": "..." }.
    // El UsuarioId se obtiene del JWT mediante BitacoraService.
    // =========================================================

    // Objeto de transferencia de datos que estructura la información enviada desde el cliente
    public class RegistrarBitacoraDto
    {
        // Propiedad que almacena la acción realizada por el usuario
        public string Accion { get; set; } = string.Empty;
        
        // Propiedad opcional para detallar la descripción de la acción
        public string Detalle { get; set; } = string.Empty;
    }

    // =========================================================
    // POST: api/bitacora
    // =========================================================

    // Endpoint POST accesible por Administradores y Operadores para registrar un nuevo evento en la bitácora
    [HttpPost]
    [Authorize(Roles = "Administrador,Operador")]
    public async Task<IActionResult> RegistrarAccion(
        [FromBody] RegistrarBitacoraDto entrada)
    {
        // Validación: Verifica que el cuerpo de la petición no sea nulo
        if (entrada == null)
        {
            return BadRequest(new { mensaje = "Los datos de la bitácora son obligatorios." });
        }

        // Validación: Asegura que el campo de la acción no esté vacío o en blanco
        if (string.IsNullOrWhiteSpace(entrada.Accion))
        {
            return BadRequest(new { mensaje = "La acción es obligatoria." });
        }

        // Envía los datos limpios (haciendo Trim) al servicio para guardarlos en la base de datos
        await _bitacoraService.RegistrarAsync(
            entrada.Accion.Trim(),
            entrada.Detalle?.Trim() ?? string.Empty
        );

        // Retorna una respuesta exitosa confirmando el registro de la acción
        return Ok(new { mensaje = "Acción registrada correctamente en la bitácora." });
    }
}