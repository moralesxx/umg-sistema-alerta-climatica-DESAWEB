using AlertaClimatica.Domain;
using AlertaClimatica.Infrastructure.Repositories.Interfaces;
using AlertaClimatica.Infrastructure.Services.Interfaces;

namespace AlertaClimatica.Infrastructure.Services;

public class LecturaService : ILecturaService
{
    private readonly ILecturaRepository _repository;

    public LecturaService(ILecturaRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<LecturaClimatica>> GetLecturasAsync() 
    {
        return await _repository.ObtenerTodasAsync();
    }

    public async Task<IEnumerable<LecturaClimatica>> GetLecturasPorSensorAsync(int sensorId) 
    {
        return await _repository.ObtenerPorSensorAsync(sensorId);
    }

    public async Task<IEnumerable<LecturaClimatica>> GetLecturasPorComunidadAsync(int comunidadId) 
    {
        return await _repository.ObtenerPorComunidadAsync(comunidadId);
    }

    public async Task<IEnumerable<LecturaClimatica>> GetLecturasPorFiltroAsync(int? sensorId, DateTime? fechaInicio, DateTime? fechaFin) 
    {
        return await _repository.ObtenerPorFiltroAsync(sensorId, fechaInicio, fechaFin);
    }

    public async Task CrearLecturaAsync(LecturaClimatica lectura) 
    {
        await _repository.CrearAsync(lectura);
    }
}