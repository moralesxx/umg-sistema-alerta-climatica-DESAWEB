using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AlertaClimatica.Infrastructure;
using AlertaClimatica.Domain;

namespace AlertaClimatica.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TiposSensorController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public TiposSensorController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TipoSensor>>> GetTiposSensor()
    {
        return await _context.TiposSensor.ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TipoSensor>> GetTipoSensor(int id)
    {
        var tipo = await _context.TiposSensor.FindAsync(id);
        if (tipo == null) return NotFound();
        return tipo;
    }

    [HttpPost]
    public async Task<ActionResult<TipoSensor>> CreateTipoSensor(TipoSensor tipoSensor)
    {
        _context.TiposSensor.Add(tipoSensor);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetTipoSensor), new { id = tipoSensor.IdTipoSensor }, tipoSensor);
    }
}