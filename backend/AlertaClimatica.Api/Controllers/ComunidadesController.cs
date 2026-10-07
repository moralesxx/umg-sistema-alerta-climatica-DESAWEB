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
    // Servicio privado para gestionar la lógica de negocio de las comunidades
    private readonly IComunidadService _comunidadService;

    // Constructor que inyecta la dependencia del servicio de comunidades
    public ComunidadesController(IComunidadService comunidadService)
    {
        _comunidadService = comunidadService;
    }

    // GET: api/comunidades
    // GET: api/comunidades?busqueda=sol&estado=true&municipio=Antigua&departamento=Sacatep
    // RF-ADM-11 y RF-ADM-12. Todos los filtros son opcionales.
    // Endpoint GET que permite listar comunidades aplicando filtros opcionales de búsqueda, estado, municipio y departamento
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ComunidadDto>>> GetComunidades(
        [FromQuery] string? busqueda,
        [FromQuery] bool? estado,
        [FromQuery] string? municipio,
        [FromQuery] string? departamento)
    {
        // Llama al servicio pasando los parámetros de filtrado opcionales
        var comunidades = await _comunidadService.GetComunidadesAsync(busqueda, estado, municipio, departamento);
        // Retorna la lista de comunidades encontradas con código HTTP 200
        return Ok(comunidades);
    }

    // GET: api/comunidades/5
    // Endpoint GET para buscar y retornar una comunidad específica mediante su ID único
    [HttpGet("{id}")]
    public async Task<ActionResult<ComunidadDto>> GetComunidad(int id)
    {
        // Consulta la comunidad por su identificador en el servicio
        var comunidad = await _comunidadService.GetComunidadAsync(id);

        // Si no se encuentra la comunidad, retorna un error HTTP 404 Not Found
        if (comunidad == null)
            return NotFound(new { mensaje = "Comunidad no encontrada." });

        // Retorna los datos de la comunidad encontrada
        return Ok(comunidad);
    }

    // POST: api/comunidades
    // RF-ADM-08
    // Endpoint POST exclusivo para Administradores que permite registrar una nueva comunidad en el sistema
    [HttpPost]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult<ComunidadDto>> CreateComunidad([FromBody] CrearComunidadDto dto)
    {
        try
        {
            // Ejecuta el servicio de creación de la comunidad
            var creada = await _comunidadService.CrearComunidadAsync(dto);
            // Retorna una respuesta HTTP 201 Created apuntando al endpoint de consulta por ID
            return CreatedAtAction(nameof(GetComunidad), new { id = creada.ComunidadId }, creada);
        }
        catch (ArgumentException ex)
        {
            // Captura errores de validación de argumentos y retorna HTTP 400 Bad Request
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (ConflictException ex)
        {
            // Captura errores de conflicto (ej. duplicados) y retorna HTTP 409 Conflict
            return Conflict(new { mensaje = ex.Message });
        }
    }

    // PUT: api/comunidades/5
    // RF-ADM-09
    // Endpoint PUT exclusivo para Administradores que actualiza la información de una comunidad existente
    [HttpPut("{id}")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> UpdateComunidad(int id, [FromBody] ActualizarComunidadDto dto)
    {
        try
        {
            // Llama al servicio para actualizar los datos de la comunidad
            var actualizada = await _comunidadService.ActualizarComunidadAsync(id, dto);

            // Si la actualización retorna falso, significa que la comunidad no existe (HTTP 404)
            if (!actualizada)
                return NotFound(new { mensaje = "Comunidad no encontrada." });

            // Retorna una respuesta exitosa sin contenido (HTTP 204 No Content)
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            // Captura excepciones de argumentos inválidos y retorna HTTP 400
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (ConflictException ex)
        {
            // Captura conflictos de datos y retorna HTTP 409
            return Conflict(new { mensaje = ex.Message });
        }
    }

    // PATCH: api/comunidades/5/estado
    // RF-ADM-10: activar o desactivar. El cuerpo es solo true o false.
    // Endpoint PATCH exclusivo para Administradores para cambiar rápidamente el estado (activo/inactivo) de una comunidad
    [HttpPatch("{id}/estado")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> CambiarEstadoComunidad(int id, [FromBody] bool activa)
    {
        // Ejecuta el cambio de estado en el servicio correspondiente
        var actualizada = await _comunidadService.CambiarEstadoComunidadAsync(id, activa);

        // Si no se encuentra la comunidad, retorna HTTP 404 Not Found
        if (!actualizada)
            return NotFound(new { mensaje = "Comunidad no encontrada." });

        // Retorna un mensaje de éxito confirmando el nuevo estado de la comunidad
        return Ok(new { mensaje = "Estado de la comunidad actualizado correctamente.", comunidadId = id, nuevoEstado = activa });
    }
}