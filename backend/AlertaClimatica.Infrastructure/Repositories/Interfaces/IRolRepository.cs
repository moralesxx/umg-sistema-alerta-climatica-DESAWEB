using AlertaClimatica.Domain;

namespace AlertaClimatica.Infrastructure.Repositories.Interfaces;

public interface IRolRepository
{
    Task<Rol?> ObtenerPorIdAsync(int id);
    Task<Rol?> ObtenerPorNombreAsync(string nombre);
    Task<bool> ExisteAsync(int id);
}