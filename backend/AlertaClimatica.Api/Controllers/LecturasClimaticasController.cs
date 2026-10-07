using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AlertaClimatica.Domain;
using AlertaClimatica.Infrastructure.Services.Interfaces;

namespace AlertaClimatica.Api.Controllers;

// Configuración general del controlador para gestionar las lecturas climáticas de los sensores, protegido por autenticación
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LecturasClimaticasController : ControllerBase
{
    // Servicio privado para manejar la lógica de negocio y operaciones de las lecturas climáticas
    private readonly ILecturaService _service;

    // Constructor que inyecta la dependencia del servicio de lecturas
    public LecturasClimaticasController(ILecturaService service)
    {
        _service = service;
    }

    // Endpoint GET que obtiene la lista completa de todas las lecturas climáticas registradas
    [HttpGet]
    public async Task<ActionResult<IEnumerable<LecturaClimatica>>> GetLecturas()
    {
        // Llama al servicio para recuperar todas las lecturas climáticas de forma asíncrona
        var lecturas = await _service.GetLecturasAsync();
        // Retorna la colección con código HTTP 200 OK
        return Ok(lecturas);
    }

    // Endpoint GET que obtiene las lecturas climáticas filtradas específicamente por el ID de un sensor
    [HttpGet("sensor/{sensorId}")]
    public async Task<ActionResult<IEnumerable<LecturaClimatica>>> GetLecturasPorSensor(int sensorId)
    {
        // Solicita al servicio las lecturas asociadas al sensor indicado
        var lecturas = await _service.GetLecturasPorSensorAsync(sensorId);
        // Retorna los datos obtenidos con código HTTP 200 OK
        return Ok(lecturas);
    }

    // Endpoint GET que obtiene las lecturas climáticas filtradas por el ID de una comunidad específica
    [HttpGet("comunidad/{comunidadId}")]
    public async Task<ActionResult<IEnumerable<LecturaClimatica>>> GetLecturasPorComunidad(int comunidadId)
    {
        // Solicita al servicio las lecturas correspondientes a la comunidad especificada
        var lecturas = await _service.GetLecturasPorComunidadAsync(comunidadId);
        // Retorna la lista resultante con código HTTP 200 OK
        return Ok(lecturas);
    }

    // Endpoint GET que permite filtrar lecturas climáticas combinando opcionalmente un sensor y un rango de fechas (inicio y fin)
    [HttpGet("filtro")]
    public async Task<ActionResult<IEnumerable<LecturaClimatica>>> GetLecturasPorFiltro(
        [FromQuery] int? sensorId, 
        [FromQuery] DateTime? fechaInicio, 
        [FromQuery] DateTime? fechaFin)
    {
        // Ejecuta la consulta filtrada en el servicio con los parámetros de consulta opcionales proporcionados
        var lecturas = await _service.GetLecturasPorFiltroAsync(sensorId, fechaInicio, fechaFin);
        // Retorna el resultado con código HTTP 200 OK
        return Ok(lecturas);
    }

    // Endpoint POST exclusivo para Administradores y Operadores que permite registrar una nueva lectura climática en el sistema
    [HttpPost]
    [Authorize(Roles = "Administrador,Operador")]
    public async Task<ActionResult<LecturaClimatica>> CreateLectura(LecturaClimatica lectura)
    {
        // Envía la nueva lectura al servicio para ser almacenada
        await _service.CrearLecturaAsync(lectura);
        // Retorna una respuesta HTTP 201 Created apuntando al método de consulta general y la lectura creada
        return CreatedAtAction(nameof(GetLecturas), new { id = lectura.LecturaId }, lectura);
    }
}