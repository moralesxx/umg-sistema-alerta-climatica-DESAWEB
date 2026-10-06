using AlertaClimatica.Domain;

namespace AlertaClimatica.Infrastructure.Services.Interfaces;

public interface ILecturaService
{
    Task<IEnumerable<LecturaClimatica>> GetLecturasAsync();
    Task<IEnumerable<LecturaClimatica>> GetLecturasPorSensorAsync(int sensorId);
    Task<IEnumerable<LecturaClimatica>> GetLecturasPorComunidadAsync(int comunidadId);
    Task<IEnumerable<LecturaClimatica>> GetLecturasPorFiltroAsync(int? sensorId, DateTime? fechaInicio, DateTime? fechaFin);
    Task CrearLecturaAsync(LecturaClimatica lectura);
}