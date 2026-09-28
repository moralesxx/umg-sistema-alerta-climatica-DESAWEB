using AlertaClimatica.Domain;

namespace AlertaClimatica.Infrastructure.Repositories.Interfaces;

public interface ITipoSensorRepository
{
    Task<IEnumerable<TipoSensor>> ObtenerTodosAsync();
    Task<bool> ExisteAsync(int id);
}