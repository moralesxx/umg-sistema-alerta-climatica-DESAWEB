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

    public async Task<Usuario?> ObtenerPorIdAsync(int id)
    {
        return await _context.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UsuarioId == id);
    }

    // Sin AsNoTracking a propósito: en Login no se modifica el usuario,
    // pero dejarlo sin tracking evita cualquier sorpresa si en el futuro
    // se necesita, por ejemplo, actualizar "último acceso" (RF-ADM-55)
    // en la misma operación.
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

    public async Task AgregarAsync(Usuario usuario)
    {
        await _context.Usuarios.AddAsync(usuario);
    }

    public async Task GuardarCambiosAsync()
    {
        await _context.SaveChangesAsync();
    }
}