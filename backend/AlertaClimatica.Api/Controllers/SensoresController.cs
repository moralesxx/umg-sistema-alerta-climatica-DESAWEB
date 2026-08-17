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

    public SensoresController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/sensores
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Sensor>>> GetSensores()
    {
        return await _context.Sensores.ToListAsync();
    }

    // GET: api/sensores/5
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
    [HttpPost]
    public async Task<ActionResult<Sensor>> CreateSensor(Sensor sensor)
    {
        _context.Sensores.Add(sensor);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetSensor), new { id = sensor.SensorId }, sensor);
    }
}