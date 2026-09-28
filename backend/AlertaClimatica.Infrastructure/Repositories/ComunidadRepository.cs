using Microsoft.EntityFrameworkCore;
using AlertaClimatica.Domain;
using AlertaClimatica.Infrastructure.Repositories.Interfaces;

namespace AlertaClimatica.Infrastructure.Repositories;

public class ComunidadRepository : IComunidadRepository
{
    private readonly ApplicationDbContext _context;

    public ComunidadRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Comunidad>> ObtenerTodosAsync()
    {
        return await _context.Comunidades
            .AsNoTracking()
            .OrderBy(c => c.Nombre)
            .ToListAsync();
    }

    public async Task<bool> ExisteAsync(int id)
    {
        return await _context.Comunidades.AnyAsync(c => c.ComunidadId == id);
    }
}