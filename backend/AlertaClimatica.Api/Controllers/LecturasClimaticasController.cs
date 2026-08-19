using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AlertaClimatica.Infrastructure;
using AlertaClimatica.Domain;

namespace AlertaClimatica.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LecturasClimaticasController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public LecturasClimaticasController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<LecturaClimatica>>> GetLecturas()
    {
        return await _context.LecturasClimaticas
            .OrderByDescending(l => l.FechaHora)
            .Take(100)
            .ToListAsync();
    }

    [HttpGet("sensor/{sensorId}")]
    public async Task<ActionResult<IEnumerable<LecturaClimatica>>> GetLecturasPorSensor(int sensorId)
    {
        return await _context.LecturasClimaticas
            .Where(l => l.IdSensor == sensorId)
            .OrderByDescending(l => l.FechaHora)
            .ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<LecturaClimatica>> CreateLectura(LecturaClimatica lectura)
    {
        if (lectura.FechaHora == default)
        {
            lectura.FechaHora = DateTime.Now;
        }

        _context.LecturasClimaticas.Add(lectura);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetLecturas), new { id = lectura.IdLectura }, lectura);
    }
}