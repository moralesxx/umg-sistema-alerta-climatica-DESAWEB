using Microsoft.EntityFrameworkCore;
using AlertaClimatica.Domain;
using AlertaClimatica.Infrastructure.Repositories.Interfaces;

namespace AlertaClimatica.Infrastructure.Repositories;

public class EventoRepository : IEventoRepository
{
    private readonly ApplicationDbContext _context;

    public EventoRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    private IQueryable<Evento> ConNombres() => _context.Eventos
        .AsNoTracking()
        .Include(e => e.Comunidad)
        .Include(e => e.Sensor)
        .Include(e => e.UsuarioResponsable);

    public async Task<IEnumerable<Evento>> ObtenerFiltradosAsync(
        DateTime? desde, DateTime? hastaExclusivo, int? comunidadId, string? fenomeno, string? nivel)
    {
        var query = ConNombres();

        if (desde.HasValue)
            query = query.Where(e => e.FechaHora >= desde.Value);

        if (hastaExclusivo.HasValue)
            query = query.Where(e => e.FechaHora < hastaExclusivo.Value);

        if (comunidadId.HasValue)
            query = query.Where(e => e.ComunidadId == comunidadId.Value);

        if (!string.IsNullOrWhiteSpace(fenomeno))
        {
            var f = fenomeno.Trim();
            query = query.Where(e => e.TipoFenomeno == f);
        }

        if (!string.IsNullOrWhiteSpace(nivel))
        {
            var n = nivel.Trim();
            query = query.Where(e => e.NivelRiesgo == n);
        }

        return await query.OrderByDescending(e => e.FechaHora).ToListAsync();
    }

    public async Task<Evento?> ObtenerPorIdAsync(int id)
    {
        return await ConNombres().FirstOrDefaultAsync(e => e.EventoId == id);
    }

    public async Task<bool> ExisteComunidadAsync(int id)
    {
        return await _context.Comunidades.AnyAsync(c => c.ComunidadId == id);
    }

    public async Task<bool> ExisteSensorAsync(int id)
    {
        return await _context.Sensores.AnyAsync(s => s.SensorId == id);
    }

    public async Task<int?> ObtenerComunidadIdDeSensorAsync(int sensorId)
    {
        return await _context.Sensores
            .Where(s => s.SensorId == sensorId)
            .Select(s => s.ComunidadId)
            .FirstOrDefaultAsync();
    }

    public async Task AgregarAsync(Evento evento)
    {
        await _context.Eventos.AddAsync(evento);
    }

    public async Task GuardarCambiosAsync()
    {
        await _context.SaveChangesAsync();
    }
}