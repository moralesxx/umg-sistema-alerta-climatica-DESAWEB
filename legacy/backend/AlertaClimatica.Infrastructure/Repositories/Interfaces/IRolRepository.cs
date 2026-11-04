using AlertaClimatica.Domain;

namespace AlertaClimatica.Infrastructure.Repositories.Interfaces;

public interface IRolRepository
{
    // Catálogo completo, para el selector de rol de la pantalla de Usuarios.
    Task<IEnumerable<Rol>> ObtenerTodosAsync();

    Task<Rol?> ObtenerPorIdAsync(int id);
    Task<Rol?> ObtenerPorNombreAsync(string nombre);
    Task<bool> ExisteAsync(int id);
}