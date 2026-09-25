using Microsoft.AspNetCore.Mvc;
using AlertaClimatica.Domain;
using AlertaClimatica.Infrastructure.Services;

namespace AlertaClimatica.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SensoresController : ControllerBase
{
    private readonly ISensorService _sensorService;

    // Ahora depende de ISensorService en vez de ApplicationDbContext directamente.
    public SensoresController(ISensorService sensorService)
    {
        _sensorService = sensorService;
    }

    // GET: api/sensores
    // Obtiene el listado completo de los sensores registrados en la comunidad
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Sensor>>> GetSensores()
    {
        var sensores = await _sensorService.GetSensoresAsync();
        return Ok(sensores);
    }

    // GET: api/sensores/5
    // Busca un sensor específico mediante su identificador único
    [HttpGet("{id}")]
    public async Task<ActionResult<Sensor>> GetSensor(int id)
    {
        var sensor = await _sensorService.GetSensorByIdAsync(id);

        if (sensor == null)
            return NotFound(new { mensaje = "Sensor no encontrado." });

        return Ok(sensor);
    }

    // POST: api/sensores
    // Registra un nuevo sensor en el sistema de monitoreo climático
    [HttpPost]
    public async Task<ActionResult<Sensor>> CreateSensor(Sensor sensor)
    {
        try
        {
            var creado = await _sensorService.CreateSensorAsync(sensor);
            return CreatedAtAction(nameof(GetSensor), new { id = creado.SensorId }, creado);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }

    // PUT: api/sensores/5
    // Edita los valores y propiedades de un sensor existente
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSensor(int id, Sensor sensor)
    {
        try
        {
            var actualizado = await _sensorService.UpdateSensorAsync(id, sensor);

            if (!actualizado)
                return NotFound(new { mensaje = "Sensor no encontrado." });

            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }

    // PATCH: api/sensores/5/estado
    // Permite activar o desactivar un sensor rápidamente cambiando su propiedad Estado
    [HttpPatch("{id}/estado")]
    public async Task<IActionResult> CambiarEstadoSensor(int id, [FromBody] bool activo)
    {
        var actualizado = await _sensorService.CambiarEstadoSensorAsync(id, activo);

        if (!actualizado)
            return NotFound(new { mensaje = "Sensor no encontrado." });

        return Ok(new { mensaje = "Estado del sensor actualizado correctamente.", sensorId = id, nuevoEstado = activo });
    }

    // POST: api/sensores/reiniciar
    // Reinicia el sistema de monitoreo general
    [HttpPost("reiniciar")]
    public async Task<IActionResult> ReiniciarSistemaMonitoreo()
    {
        try
        {
            await _sensorService.ReiniciarSistemaAsync();
            return Ok(new { mensaje = "Sistema de monitoreo reiniciado con éxito." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { mensaje = "Error al reiniciar el sistema", detalle = ex.Message });
        }
    }
}