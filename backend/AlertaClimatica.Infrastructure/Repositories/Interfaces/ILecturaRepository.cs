using AlertaClimatica.Domain;

namespace AlertaClimatica.Infrastructure.Repositories.Interfaces;

public interface ILecturaRepository
{
    Task<IEnumerable<LecturaClimatica>> ObtenerTodasAsync();
    Task<IEnumerable<LecturaClimatica>> ObtenerPorSensorAsync(int sensorId);
    Task<IEnumerable<LecturaClimatica>> ObtenerPorComunidadAsync(int comunidadId);
    Task<IEnumerable<LecturaClimatica>> ObtenerPorFiltroAsync(int? sensorId, DateTime? fechaInicio, DateTime? fechaFin);
    Task CrearAsync(LecturaClimatica lectura);
}