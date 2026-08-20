using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AlertaClimatica.Infrastructure;
using AlertaClimatica.Domain;

namespace AlertaClimatica.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SensoresController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    // Constructor que inyecta el contexto de la base de datos para las consultas
    public SensoresController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/sensores
    // Obtiene el listado completo de los sensores registrados en la comunidad
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Sensor>>> GetSensores()
    {
        return await _context.Sensores.ToListAsync();
    }

    // GET: api/sensores/5
    // Busca un sensor específico mediante su identificador único
    [HttpGet("{id}")]
    public async Task<ActionResult<Sensor>> GetSensor(int id)
    {
        var sensor = await _context.Sensores
            .FirstOrDefaultAsync(s => s.SensorId == id);

        if (sensor == null)
            return NotFound();

        return sensor;
    }

    // POST: api/sensores
    // Registra un nuevo sensor en el sistema de monitoreo climático
    [HttpPost]
    public async Task<ActionResult<Sensor>> CreateSensor(Sensor sensor)
    {
        _context.Sensores.Add(sensor);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetSensor), new { id = sensor.SensorId }, sensor);
    }

    // PUT: api/sensores/5
    // Edita los valores y propiedades de un sensor existente
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSensor(int id, Sensor sensor)
    {
        if (id != sensor.SensorId)
        {
            return BadRequest(new { mensaje = "El ID del sensor no coincide." });
        }

        _context.Entry(sensor).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!SensorExists(id))
            {
                return NotFound(new { mensaje = "Sensor no encontrado." });
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    // PATCH: api/sensores/5/estado
    // Permite activar o desactivar un sensor rápidamente cambiando su propiedad Estado
    [HttpPatch("{id}/estado")]
    public async Task<IActionResult> CambiarEstadoSensor(int id, [FromBody] bool activo)
    {
        var sensor = await _context.Sensores.FindAsync(id);
        if (sensor == null)
        {
            return NotFound(new { mensaje = "Sensor no encontrado." });
        }

        // Se usa la propiedad Estado de tu modelo Sensor.cs
        sensor.Estado = activo; 

        await _context.SaveChangesAsync();

        return Ok(new { mensaje = "Estado del sensor actualizado correctamente.", sensorId = id, nuevoEstado = activo });
    }

    // POST: api/sensores/reiniciar
    // Reinicia el sistema de monitoreo general
    [HttpPost("reiniciar")]
    public async Task<IActionResult> ReiniciarSistemaMonitoreo()
    {
        try
        {
            var sensores = await _context.Sensores.ToListAsync();
            foreach (var s in sensores)
            {
                s.Estado = true; // Reactiva todos por defecto al reiniciar el sistema
            }

            await _context.SaveChangesAsync();
            return Ok(new { mensaje = "Sistema de monitoreo reiniciado con éxito." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { mensaje = "Error al reiniciar el sistema", detalle = ex.Message });
        }
    }

    private bool SensorExists(int id)
    {
        return _context.Sensores.Any(e => e.SensorId == id);
    }
}