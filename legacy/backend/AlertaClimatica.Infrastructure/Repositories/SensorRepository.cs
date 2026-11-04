using Microsoft.EntityFrameworkCore;
using AlertaClimatica.Domain;
using AlertaClimatica.Infrastructure.Repositories.Interfaces;

namespace AlertaClimatica.Infrastructure.Repositories;

public class SensorRepository : ISensorRepository
{
    private readonly ApplicationDbContext _context;

    public SensorRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    // Para listar (GET): sin tracking, con los datos de Tipo y Comunidad ya
    // incluidos para que el frontend no tenga que hacer llamadas extra.
    public async Task<IEnumerable<Sensor>> ObtenerTodosAsync()
    {
        return await _context.Sensores
            .AsNoTracking()
            .Include(s => s.TipoSensor)
            .Include(s => s.Comunidad)
            .OrderBy(s => s.SensorId)
            .ToListAsync();
    }

    // RF-ADM-19 y RF-ADM-20: filtros opcionales. Cada filtro solo se aplica
    // si viene informado; sin ningún filtro devuelve todos los sensores.
    public async Task<IEnumerable<Sensor>> ObtenerFiltradosAsync(
        int? comunidadId, int? tipoSensorId, bool? estado, string? codigo)
    {
        var query = _context.Sensores
            .AsNoTracking()
            .Include(s => s.TipoSensor)
            .Include(s => s.Comunidad)
            .AsQueryable();

        if (comunidadId.HasValue)
            query = query.Where(s => s.ComunidadId == comunidadId.Value);

        if (tipoSensorId.HasValue)
            query = query.Where(s => s.TipoSensorId == tipoSensorId.Value);

        if (estado.HasValue)
            query = query.Where(s => s.Estado == estado.Value);

        if (!string.IsNullOrWhiteSpace(codigo))
        {
            var texto = codigo.Trim();
            query = query.Where(s => s.Codigo.Contains(texto));
        }

        return await query.OrderBy(s => s.SensorId).ToListAsync();
    }

    public async Task<Sensor?> ObtenerPorIdAsync(int id)
    {
        return await _context.Sensores
            .AsNoTracking()
            .Include(s => s.TipoSensor)
            .Include(s => s.Comunidad)
            .FirstOrDefaultAsync(s => s.SensorId == id);
    }

    // Para editar: con tracking, porque el Service va a modificar sus
    // propiedades y luego llamar a GuardarCambiosAsync().
    public async Task<Sensor?> ObtenerParaActualizarAsync(int id)
    {
        return await _context.Sensores.FindAsync(id);
    }

    // Para "Reiniciar Sistema": necesita todos los sensores con tracking
    // para poder modificar el Estado de cada uno en memoria.
    public async Task<IEnumerable<Sensor>> ObtenerTodosParaActualizarAsync()
    {
        return await _context.Sensores.ToListAsync();
    }

    public async Task AgregarAsync(Sensor sensor)
    {
        await _context.Sensores.AddAsync(sensor);
    }

    public async Task<bool> ExisteAsync(int id)
    {
        return await _context.Sensores.AnyAsync(s => s.SensorId == id);
    }

    public async Task GuardarCambiosAsync()
    {
        await _context.SaveChangesAsync();
    }
}