using Microsoft.EntityFrameworkCore;
using AlertaClimatica.Domain;
using AlertaClimatica.Infrastructure.Repositories.Interfaces;

namespace AlertaClimatica.Infrastructure.Repositories;

public class ReglaAlertaRepository : IReglaAlertaRepository
{
    private readonly ApplicationDbContext _context;

    public ReglaAlertaRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ReglaAlerta>> ObtenerTodasAsync()
    {
        return await _context.ReglasAlerta
            .Include(r => r.TipoSensor)
            .OrderBy(r => r.Nombre)
            .ToListAsync();
    }

    public async Task<ReglaAlerta?> ObtenerPorIdAsync(int id)
    {
        return await _context.ReglasAlerta
            .Include(r => r.TipoSensor)
            .FirstOrDefaultAsync(r => r.ReglaAlertaId == id);
    }

    public async Task<IEnumerable<ReglaAlerta>> ObtenerActivasPorTipoSensorAsync(
        int tipoSensorId)
    {
        return await _context.ReglasAlerta
            .Where(r => r.Estado && r.TipoSensorId == tipoSensorId)
            .ToListAsync();
    }

    public async Task CrearAsync(ReglaAlerta regla)
    {
        _context.ReglasAlerta.Add(regla);
        await _context.SaveChangesAsync();
    }

    public async Task ActualizarAsync(ReglaAlerta regla)
    {
        _context.ReglasAlerta.Update(regla);
        await _context.SaveChangesAsync();
    }

    public async Task EliminarAsync(ReglaAlerta regla)
    {
        _context.ReglasAlerta.Remove(regla);
        await _context.SaveChangesAsync();
    }
}