using AlertaClimatica.Domain;

namespace AlertaClimatica.Infrastructure.Services;

public interface ISensorService
{
    Task<IEnumerable<Sensor>> GetSensoresAsync();
    Task<IEnumerable<Sensor>> GetSensoresFiltradosAsync(int? comunidadId, int? tipoSensorId, bool? estado, string? codigo);
    Task<Sensor?> GetSensorByIdAsync(int id);
    Task<Sensor> CreateSensorAsync(Sensor sensor);
    Task<bool> UpdateSensorAsync(int id, Sensor sensor);
    Task<bool> CambiarEstadoSensorAsync(int id, bool activo);
    Task ReiniciarSistemaAsync();
    // RF-ADM-21: fija (o quita, con null) el valor simulado de un sensor.
    Task<bool> EstablecerValorSimuladoAsync(int id, decimal? valor);
    Task<bool> SensorExistsAsync(int id);
}