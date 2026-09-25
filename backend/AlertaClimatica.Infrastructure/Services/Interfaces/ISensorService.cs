using AlertaClimatica.Domain;

namespace AlertaClimatica.Infrastructure.Services;

public interface ISensorService
{
    Task<IEnumerable<Sensor>> GetSensoresAsync();
    Task<Sensor?> GetSensorByIdAsync(int id);
    Task<Sensor> CreateSensorAsync(Sensor sensor);
    Task<bool> UpdateSensorAsync(int id, Sensor sensor);
    Task<bool> CambiarEstadoSensorAsync(int id, bool activo);
    Task ReiniciarSistemaAsync();
    Task<bool> SensorExistsAsync(int id);
}