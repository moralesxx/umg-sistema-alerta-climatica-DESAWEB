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
        // Retorna la lista de sensores mapeada desde la base de datos
        return await _context.Sensores.ToListAsync();
    }

    // GET: api/sensores/5
    // Busca un sensor específico mediante su identificador único
    [HttpGet("{id}")]
    public async Task<ActionResult<Sensor>> GetSensor(int id)
    {
        // Busca el registro utilizando la clave primaria actualizada SensorId
        var sensor = await _context.Sensores
            .FirstOrDefaultAsync(s => s.SensorId == id);

        // Si no se encuentra el sensor, devuelve un código 404 No Encontrado
        if (sensor == null)
            return NotFound();

        return sensor;
    }

    // POST: api/sensores
    // Registra un nuevo sensor en el sistema de monitoreo climático
    [HttpPost]
    public async Task<ActionResult<Sensor>> CreateSensor(Sensor sensor)
    {
        // Añade el objeto al contexto de Entity Framework
        _context.Sensores.Add(sensor);
        
        // Guarda los cambios de forma asíncrona en la base de datos SQL Server
        await _context.SaveChangesAsync();

        // Retorna una respuesta 201 Created apuntando a la ruta de consulta del nuevo sensor
        return CreatedAtAction(nameof(GetSensor), new { id = sensor.SensorId }, sensor);
    }
}