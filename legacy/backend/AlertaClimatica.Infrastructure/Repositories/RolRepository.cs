using Microsoft.EntityFrameworkCore;
using AlertaClimatica.Domain;
using AlertaClimatica.Infrastructure.Repositories.Interfaces;

namespace AlertaClimatica.Infrastructure.Repositories;

public class RolRepository : IRolRepository
{
    private readonly ApplicationDbContext _context;

    public RolRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Rol>> ObtenerTodosAsync()
    {
        return await _context.Roles
            .AsNoTracking()
            .OrderBy(r => r.RolId)
            .ToListAsync();
    }

    public async Task<Rol?> ObtenerPorIdAsync(int id)
    {
        return await _context.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.RolId == id);
    }

    public async Task<Rol?> ObtenerPorNombreAsync(string nombre)
    {
        return await _context.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.NombreRol == nombre);
    }

    public async Task<bool> ExisteAsync(int id)
    {
        return await _context.Roles.AnyAsync(r => r.RolId == id);
    }
}