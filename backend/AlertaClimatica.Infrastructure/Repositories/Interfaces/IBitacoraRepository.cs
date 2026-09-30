using AlertaClimatica.Domain;

namespace AlertaClimatica.Infrastructure.Repositories.Interfaces;

public interface IBitacoraRepository
{
    Task<IEnumerable<Bitacora>> ObtenerUltimosAsync(int cantidad);
    Task<bool> ExisteUsuarioAsync(int usuarioId);
    Task AgregarAsync(Bitacora bitacora);
    Task GuardarCambiosAsync();
}