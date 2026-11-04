using Microsoft.EntityFrameworkCore;
using AlertaClimatica.Domain;
using AlertaClimatica.Infrastructure.Repositories.Interfaces;

namespace AlertaClimatica.Infrastructure.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly ApplicationDbContext _context;

    public UsuarioRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Usuario>> ObtenerTodosAsync()
    {
        return await _context.Usuarios
            .AsNoTracking()
            .OrderBy(u => u.UsuarioId)
            .ToListAsync();
    }

    public async Task<IEnumerable<Usuario>> ObtenerFiltradosAsync(string? texto, int? rolId, bool? estado)
    {
        var query = _context.Usuarios.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(texto))
        {
            var t = texto.Trim().ToLower();
            query = query.Where(u => u.Nombre.ToLower().Contains(t) || u.Correo.ToLower().Contains(t));
        }

        if (rolId.HasValue)
        {
            query = query.Where(u => u.RolId == rolId.Value);
        }

        if (estado.HasValue)
        {
            query = query.Where(u => u.Estado == estado.Value);
        }

        return await query.OrderBy(u => u.UsuarioId).ToListAsync();
    }

    public async Task<Usuario?> ObtenerPorIdAsync(int id)
    {
        return await _context.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UsuarioId == id);
    }

    // Con tracking: para Editar y Cambiar Estado, que modifican la entidad.
    public async Task<Usuario?> ObtenerParaActualizarAsync(int id)
    {
        return await _context.Usuarios.FindAsync(id);
    }

    // Sin AsNoTracking a propósito: en Login se actualiza UltimoAcceso
    // sobre esta misma instancia.
    public async Task<Usuario?> ObtenerPorCorreoAsync(string correo)
    {
        return await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Correo.ToLower() == correo.ToLower());
    }

    public async Task<bool> ExisteCorreoAsync(string correo)
    {
        return await _context.Usuarios
            .AnyAsync(u => u.Correo.ToLower() == correo.ToLower());
    }

    public async Task<bool> ExisteCorreoEnOtroUsuarioAsync(string correo, int usuarioIdExcluir)
    {
        return await _context.Usuarios
            .AnyAsync(u => u.Correo.ToLower() == correo.ToLower() && u.UsuarioId != usuarioIdExcluir);
    }

    public async Task AgregarAsync(Usuario usuario)
    {
        await _context.Usuarios.AddAsync(usuario);
    }

    public async Task GuardarCambiosAsync()
    {
        await _context.SaveChangesAsync();
    }
}