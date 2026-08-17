using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AlertaClimatica.Infrastructure;
using AlertaClimatica.Domain;

namespace AlertaClimatica.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BitacoraController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public BitacoraController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/bitacora
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Bitacora>>> GetBitacora()
    {
        return await _context.Bitacora
            .OrderByDescending(b => b.FechaHora)
            .Take(100)
            .ToListAsync();
    }

    // POST: api/bitacora
    [HttpPost]
    public async Task<ActionResult<Bitacora>> RegistarAccion(Bitacora entrada)
    {
        if (entrada.FechaHora == default)
        {
            entrada.FechaHora = DateTime.Now;
        }

        _context.Bitacora.Add(entrada);
        await _context.SaveChangesAsync();

        return Ok(entrada);
    }
}