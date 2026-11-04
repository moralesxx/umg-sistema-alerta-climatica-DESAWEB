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

    // RF-ADM-12: cada filtro solo se aplica si viene informado; sin ningún
    // filtro devuelve todas las comunidades.
    public async Task<IEnumerable<Comunidad>> ObtenerFiltradosAsync(
        string? busqueda, bool? estado, string? municipio, string? departamento)
    {
        var query = _context.Comunidades
            .AsNoTracking()
            .AsQueryable();

        // Búsqueda general: coincide con nombre, municipio o departamento.
        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var texto = busqueda.Trim();
            query = query.Where(c =>
                c.Nombre.Contains(texto) ||
                c.Municipio.Contains(texto) ||
                c.Departamento.Contains(texto));
        }

        if (estado.HasValue)
            query = query.Where(c => c.Estado == estado.Value);

        if (!string.IsNullOrWhiteSpace(municipio))
        {
            var texto = municipio.Trim();
            query = query.Where(c => c.Municipio.Contains(texto));
        }

        if (!string.IsNullOrWhiteSpace(departamento))
        {
            var texto = departamento.Trim();
            query = query.Where(c => c.Departamento.Contains(texto));
        }

        return await query.OrderBy(c => c.Nombre).ToListAsync();
    }

    public async Task<Comunidad?> ObtenerPorIdAsync(int id)
    {
        return await _context.Comunidades
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ComunidadId == id);
    }

    public async Task<Comunidad?> ObtenerParaActualizarAsync(int id)
    {
        return await _context.Comunidades.FindAsync(id);
    }

    public async Task<bool> ExisteAsync(int id)
    {
        return await _context.Comunidades.AnyAsync(c => c.ComunidadId == id);
    }

    public async Task<bool> ExisteDuplicadoAsync(
        string nombre, string municipio, string departamento, int? excluirId)
    {
        var n = nombre.Trim().ToLower();
        var m = municipio.Trim().ToLower();
        var d = departamento.Trim().ToLower();

        return await _context.Comunidades.AnyAsync(c =>
            c.Nombre.ToLower() == n &&
            c.Municipio.ToLower() == m &&
            c.Departamento.ToLower() == d &&
            (excluirId == null || c.ComunidadId != excluirId));
    }

    public async Task<int> ContarSensoresAsync(int comunidadId)
    {
        return await _context.Sensores
            .CountAsync(s => s.ComunidadId == comunidadId);
    }

    public async Task<Dictionary<int, int>> ContarSensoresPorComunidadAsync()
    {
        return await _context.Sensores
            .Where(s => s.ComunidadId.HasValue)
            .GroupBy(s => s.ComunidadId!.Value)
            .Select(g => new { ComunidadId = g.Key, Total = g.Count() })
            .ToDictionaryAsync(x => x.ComunidadId, x => x.Total);
    }

    public async Task AgregarAsync(Comunidad comunidad)
    {
        await _context.Comunidades.AddAsync(comunidad);
    }

    public async Task GuardarCambiosAsync()
    {
        await _context.SaveChangesAsync();
    }
}
