using Microsoft.EntityFrameworkCore;
using AlertaClimatica.Domain;
using AlertaClimatica.Infrastructure.Repositories.Interfaces;

namespace AlertaClimatica.Infrastructure.Repositories;

public class LecturaRepository : ILecturaRepository
{
    private readonly ApplicationDbContext _context;

    public LecturaRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<LecturaClimatica>> ObtenerTodasAsync()
    {
        return await _context.LecturasClimaticas
            .Include(l => l.Sensor)
                .ThenInclude(s => s.TipoSensor)
            .OrderByDescending(l => l.FechaHora)
            .Take(100)
            .ToListAsync();
    }

    public async Task<IEnumerable<LecturaClimatica>> ObtenerPorSensorAsync(int sensorId)
    {
        return await _context.LecturasClimaticas
            .Include(l => l.Sensor)
                .ThenInclude(s => s.TipoSensor)
            .Where(l => l.SensorId == sensorId)
            .OrderByDescending(l => l.FechaHora)
            .ToListAsync();
    }

    public async Task<IEnumerable<LecturaClimatica>> ObtenerPorComunidadAsync(int comunidadId)
    {
        var sensorIds = await _context.Sensores
            .Where(s => s.ComunidadId == comunidadId)
            .Select(s => s.SensorId)
            .ToListAsync();

        return await _context.LecturasClimaticas
            .Include(l => l.Sensor)
                .ThenInclude(s => s.TipoSensor)
            .Where(l => sensorIds.Contains(l.SensorId))
            .OrderByDescending(l => l.FechaHora)
            .ToListAsync();
    }

    public async Task<IEnumerable<LecturaClimatica>> ObtenerPorFiltroAsync(int? sensorId, DateTime? fechaInicio, DateTime? fechaFin)
    {
        var query = _context.LecturasClimaticas
            .Include(l => l.Sensor)
                .ThenInclude(s => s.TipoSensor)
            .AsQueryable();

        if (sensorId.HasValue)
        {
            query = query.Where(l => l.SensorId == sensorId.Value);
        }
        if (fechaInicio.HasValue)
        {
            query = query.Where(l => l.FechaHora >= fechaInicio.Value);
        }
        if (fechaFin.HasValue)
        {
            query = query.Where(l => l.FechaHora <= fechaFin.Value);
        }

        return await query.OrderByDescending(l => l.FechaHora).ToListAsync();
    }

    public async Task CrearAsync(LecturaClimatica lectura)
    {
        if (lectura.FechaHora == default)
        {
            lectura.FechaHora = DateTime.Now;
        }
        _context.LecturasClimaticas.Add(lectura);
        await _context.SaveChangesAsync();
    }
}