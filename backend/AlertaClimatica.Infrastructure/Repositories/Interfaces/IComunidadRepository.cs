using AlertaClimatica.Domain;

namespace AlertaClimatica.Infrastructure.Repositories.Interfaces;

// Solo lectura por ahora. El CRUD completo de Comunidades (RF-ADM-08 a 14)
// es el objetivo #5 de la fase, todavía no implementado.
public interface IComunidadRepository
{
    Task<IEnumerable<Comunidad>> ObtenerTodosAsync();
    Task<bool> ExisteAsync(int id);
}