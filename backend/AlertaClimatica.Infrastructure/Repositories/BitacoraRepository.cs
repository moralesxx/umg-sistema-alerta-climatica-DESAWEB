using Microsoft.EntityFrameworkCore;
using AlertaClimatica.Domain;
using AlertaClimatica.Infrastructure.Repositories.Interfaces;

namespace AlertaClimatica.Infrastructure.Repositories;

public class BitacoraRepository : IBitacoraRepository
{
    private readonly ApplicationDbContext _context;

    public BitacoraRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Bitacora>> ObtenerUltimosAsync(int cantidad)
    {
        return await _context.Bitacora
            .AsNoTracking()
            .OrderByDescending(b => b.FechaHora)
            .Take(cantidad)
            .ToListAsync();
    }

    public async Task<bool> ExisteUsuarioAsync(int usuarioId)
    {
        return await _context.Usuarios.AnyAsync(u => u.UsuarioId == usuarioId);
    }

    public async Task AgregarAsync(Bitacora bitacora)
    {
        await _context.Bitacora.AddAsync(bitacora);
    }

    public async Task GuardarCambiosAsync()
    {
        await _context.SaveChangesAsync();
    }
}