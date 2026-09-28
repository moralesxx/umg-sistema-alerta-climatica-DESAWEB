using Microsoft.EntityFrameworkCore;
using AlertaClimatica.Domain;
using AlertaClimatica.Infrastructure.Repositories.Interfaces;

namespace AlertaClimatica.Infrastructure.Repositories;

public class TipoSensorRepository : ITipoSensorRepository
{
    private readonly ApplicationDbContext _context;

    public TipoSensorRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<TipoSensor>> ObtenerTodosAsync()
    {
        return await _context.TiposSensor
            .AsNoTracking()
            .OrderBy(t => t.NombreTipo)
            .ToListAsync();
    }

    public async Task<bool> ExisteAsync(int id)
    {
        return await _context.TiposSensor.AnyAsync(t => t.TipoSensorId == id);
    }
}