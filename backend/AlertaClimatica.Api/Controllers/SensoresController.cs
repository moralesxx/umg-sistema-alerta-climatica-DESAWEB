using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AlertaClimatica.Domain;
using AlertaClimatica.Infrastructure.Services;

namespace AlertaClimatica.Api.Controllers;

// RF-ADM-07: Administrador acceso completo, Operador administra sensores,
// Usuario de consulta solo puede ver (los GET no tienen restricción de rol
// adicional, solo requieren estar autenticado).
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SensoresController : ControllerBase
{
    // Servicio privado encargado de manejar la lógica de negocio y operaciones de los sensores
    private readonly ISensorService _sensorService;

    // Ahora depende diamente de ISensorService en vez de ApplicationDbContext directamente.
    // Constructor que inyecta la dependencia del servicio de sensores
    public SensoresController(ISensorService sensorService)
    {
        _sensorService = sensorService;
    }

    // GET: api/sensores
    // GET: api/sensores?comunidadId=1&tipoSensorId=2&estado=true&codigo=SEN
    // Obtiene el listado de sensores. Todos los filtros son opcionales
    // (RF-ADM-19 y RF-ADM-20); sin parámetros devuelve todos.
    // Endpoint GET que permite listar los sensores aplicando filtros opcionales de comunidad, tipo de sensor, estado y código
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Sensor>>> GetSensores(
        [FromQuery] int? comunidadId,
        [FromQuery] int? tipoSensorId,
        [FromQuery] bool? estado,
        [FromQuery] string? codigo)
    {
        // Llama al servicio para obtener los sensores filtrados de forma asíncrona
        var sensores = await _sensorService.GetSensoresFiltradosAsync(comunidadId, tipoSensorId, estado, codigo);
        // Retorna la colección de sensores con código HTTP 200 OK
        return Ok(sensores);
    }

    // GET: api/sensores/5
    // Busca un sensor específico mediante su identificador único
    // Endpoint GET para consultar la información detallada de un sensor por su ID único
    [HttpGet("{id}")]
    public async Task<ActionResult<Sensor>> GetSensor(int id)
    {
        // Consulta el sensor a través del servicio usando su ID
        var sensor = await _sensorService.GetSensorByIdAsync(id);

        // Si el sensor no existe, retorna una respuesta HTTP 404 Not Found
        if (sensor == null)
            return NotFound(new { mensaje = "Sensor no encontrado." });

        // Retorna los datos del sensor encontrado con código HTTP 200 OK
        return Ok(sensor);
    }

    // POST: api/sensores
    // Registra un nuevo sensor en el sistema de monitoreo climático
    // Endpoint POST accesible por Administradores y Operadores para registrar un nuevo sensor
    [HttpPost]
    [Authorize(Roles = "Administrador,Operador")]
    public async Task<ActionResult<Sensor>> CreateSensor(Sensor sensor)
    {
        try
        {
            // Ejecuta el servicio para crear el nuevo sensor en el sistema
            var creado = await _sensorService.CreateSensorAsync(sensor);
            // Retorna una respuesta HTTP 201 Created apuntando al método de consulta por ID y el objeto creado
            return CreatedAtAction(nameof(GetSensor), new { id = creado.SensorId }, creado);
        }
        catch (ArgumentException ex)
        {
            // Captura errores de validación de argumentos y retorna una respuesta HTTP 400 Bad Request
            return BadRequest(new { mensaje = ex.Message });
        }
    }

    // PUT: api/sensores/5
    // Edita los valores y propiedades de un sensor existente
    // Endpoint PUT exclusivo para Administradores y Operadores para actualizar la información de un sensor existente
    [HttpPut("{id}")]
    [Authorize(Roles = "Administrador,Operador")]
    public async Task<IActionResult> UpdateSensor(int id, Sensor sensor)
    {
        try
        {
            // Llama al servicio para actualizar los datos del sensor especificado
            var actualizado = await _sensorService.UpdateSensorAsync(id, sensor);

            // Si la actualización retorna falso, significa que el sensor no fue encontrado (HTTP 404)
            if (!actualizado)
                return NotFound(new { mensaje = "Sensor no encontrado." });

            // Retorna una respuesta exitosa sin contenido (HTTP 204 No Content)
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            // Captura excepciones de argumentos inválidos y retorna HTTP 400 Bad Request
            return BadRequest(new { mensaje = ex.Message });
        }
    }

    // PATCH: api/sensores/5/estado
    // Permite activar o desactivar un sensor rápidamente cambiando su propiedad Estado
    // Endpoint PATCH para cambiar el estado (activo/inactivo) de un sensor de forma directa
    [HttpPatch("{id}/estado")]
    [Authorize(Roles = "Administrador,Operador")]
    public async Task<IActionResult> CambiarEstadoSensor(int id, [FromBody] bool activo)
    {
        // Ejecuta el cambio de estado en el servicio correspondiente
        var actualizado = await _sensorService.CambiarEstadoSensorAsync(id, activo);

        // Si el sensor no existe, retorna una respuesta HTTP 404 Not Found
        if (!actualizado)
            return NotFound(new { mensaje = "Sensor no encontrado." });

        // Retorna un mensaje de éxito confirmando la actualización del estado del sensor
        return Ok(new { mensaje = "Estado del sensor actualizado correctamente.", sensorId = id, nuevoEstado = activo });
    }

    // POST: api/sensores/reiniciar
    // Reinicia el sistema de monitoreo general
    // Endpoint POST para reiniciar de forma general el sistema de monitoreo de sensores
    [HttpPost("reiniciar")]
    [Authorize(Roles = "Administrador,Operador")]
    public async Task<IActionResult> ReiniciarSistemaMonitoreo()
    {
        try
        {
            // Llama al servicio para ejecutar el reinicio del sistema de monitoreo
            await _sensorService.ReiniciarSistemaAsync();
            // Retorna una respuesta HTTP 200 OK informando el éxito de la operación
            return Ok(new { mensaje = "Sistema de monitoreo reiniciado con éxito." });
        }
        catch (Exception ex)
        {
            // Captura cualquier error inesperado y retorna una respuesta de error interno del servidor (HTTP 500)
            return StatusCode(500, new { mensaje = "No fue posible reiniciar el sistema de monitoreo." });
        }
    }
}