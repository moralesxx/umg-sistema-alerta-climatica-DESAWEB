using AlertaClimatica.Domain;
using AlertaClimatica.Infrastructure.Repositories.Interfaces;
using AlertaClimatica.Infrastructure.Services.Interfaces;

namespace AlertaClimatica.Infrastructure.Services;

public class LecturaService : ILecturaService
{
    private readonly ILecturaRepository _repository;
    private readonly ISensorRepository _sensorRepository;
    private readonly IEvaluadorAlertas _evaluador;

    public LecturaService(
        ILecturaRepository repository,
        ISensorRepository sensorRepository,
        IEvaluadorAlertas evaluador)
    {
        _repository = repository;
        _sensorRepository = sensorRepository;
        _evaluador = evaluador;
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
        // RF-ADM-18: solo se registran lecturas de sensores que existen y están activos.
        var sensor = await _sensorRepository.ObtenerPorIdAsync(lectura.SensorId);
        if (sensor == null)
            throw new ArgumentException($"El sensor con Id {lectura.SensorId} no existe.");
        if (!sensor.Estado)
            throw new ArgumentException("El sensor está inactivo; no se pueden registrar lecturas.");

        lectura.LecturaId = 0;
        lectura.Sensor = null;
        if (lectura.FechaHora == default)
            lectura.FechaHora = DateTime.Now;

        await _repository.CrearAsync(lectura);

        // RF-ADM-32/33: evaluación automática contra las reglas.
        await _evaluador.EvaluarAsync(lectura);
    }
}