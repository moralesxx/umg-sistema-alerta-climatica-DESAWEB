using AlertaClimatica.Domain;
using AlertaClimatica.Infrastructure.Repositories.Interfaces;

namespace AlertaClimatica.Infrastructure.Services;

// Reglas de negocio del módulo de Sensores. Ya NO conoce ApplicationDbContext
// ni EntityFrameworkCore directamente — todo el acceso a datos pasa por los
// repositorios inyectados. Esto es lo que permite, por ejemplo, probar esta
// clase con repositorios simulados (mocks) sin necesitar una base real.
public class SensorService : ISensorService
{
    private readonly ISensorRepository _sensorRepository;
    private readonly ITipoSensorRepository _tipoSensorRepository;
    private readonly IComunidadRepository _comunidadRepository;

    public SensorService(
        ISensorRepository sensorRepository,
        ITipoSensorRepository tipoSensorRepository,
        IComunidadRepository comunidadRepository)
    {
        _sensorRepository = sensorRepository;
        _tipoSensorRepository = tipoSensorRepository;
        _comunidadRepository = comunidadRepository;
    }

    public async Task<IEnumerable<Sensor>> GetSensoresAsync()
    {
        return await _sensorRepository.ObtenerTodosAsync();
    }

    public async Task<IEnumerable<Sensor>> GetSensoresFiltradosAsync(
        int? comunidadId, int? tipoSensorId, bool? estado, string? codigo)
    {
        return await _sensorRepository.ObtenerFiltradosAsync(comunidadId, tipoSensorId, estado, codigo);
    }

    public async Task<Sensor?> GetSensorByIdAsync(int id)
    {
        return await _sensorRepository.ObtenerPorIdAsync(id);
    }

    public async Task<Sensor> CreateSensorAsync(Sensor sensor)
    {
        if (string.IsNullOrWhiteSpace(sensor.NombreSensor))
            throw new ArgumentException("El nombre del sensor es obligatorio.");

        if (string.IsNullOrWhiteSpace(sensor.Ubicacion))
            throw new ArgumentException("La ubicación del sensor es obligatoria.");

        // RF-ADM-15: fecha de instalación y descripción son obligatorias
        // para sensores NUEVOS (los viejos de prueba se quedan con NULL).
        if (sensor.FechaInstalacion == null)
            throw new ArgumentException("La fecha de instalación es obligatoria.");

        if (string.IsNullOrWhiteSpace(sensor.Descripcion))
            throw new ArgumentException("La descripción del sensor es obligatoria.");

        if (!await _tipoSensorRepository.ExisteAsync(sensor.TipoSensorId))
            throw new ArgumentException($"El tipo de sensor con Id {sensor.TipoSensorId} no existe.");

        // ComunidadId es opcional mientras no exista el módulo de Comunidades
        // (todavía no hay ninguna fila que elegir). Si viene informado, sí
        // se valida que exista de verdad.
        if (sensor.ComunidadId.HasValue &&
            !await _comunidadRepository.ExisteAsync(sensor.ComunidadId.Value))
        {
            throw new ArgumentException($"La comunidad con Id {sensor.ComunidadId} no existe.");
        }

        // El modal "Agregar Sensor" no siempre envía Código, y la columna
        // Codigo en la BD es NOT NULL UNIQUE. Si llega vacío, se genera uno.
        if (string.IsNullOrWhiteSpace(sensor.Codigo))
        {
            sensor.Codigo = $"SEN-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
        }

        sensor.SensorId = 0; // que el autoincremental de la BD lo asigne
        sensor.Estado = true; // todo sensor nuevo nace activo

        await _sensorRepository.AgregarAsync(sensor);
        await _sensorRepository.GuardarCambiosAsync();

        return sensor;
    }

    public async Task<bool> UpdateSensorAsync(int id, Sensor sensor)
    {
        if (id != sensor.SensorId)
            throw new ArgumentException("El ID del sensor no coincide.");

        var existente = await _sensorRepository.ObtenerParaActualizarAsync(id);
        if (existente == null)
            return false;

        if (!await _tipoSensorRepository.ExisteAsync(sensor.TipoSensorId))
            throw new ArgumentException($"El tipo de sensor con Id {sensor.TipoSensorId} no existe.");

        if (sensor.ComunidadId.HasValue &&
            !await _comunidadRepository.ExisteAsync(sensor.ComunidadId.Value))
        {
            throw new ArgumentException($"La comunidad con Id {sensor.ComunidadId} no existe.");
        }

        // Se actualiza campo por campo para no pisar por accidente columnas
        // que el cliente no envió.
        existente.NombreSensor = sensor.NombreSensor;
        existente.Ubicacion = sensor.Ubicacion;
        existente.TipoSensorId = sensor.TipoSensorId;
        existente.ComunidadId = sensor.ComunidadId;
        existente.Estado = sensor.Estado;
        existente.FechaInstalacion = sensor.FechaInstalacion;
        existente.Descripcion = sensor.Descripcion;

        if (!string.IsNullOrWhiteSpace(sensor.Codigo))
            existente.Codigo = sensor.Codigo;

        await _sensorRepository.GuardarCambiosAsync();
        return true;
    }

    public async Task<bool> CambiarEstadoSensorAsync(int id, bool activo)
    {
        var sensor = await _sensorRepository.ObtenerParaActualizarAsync(id);
        if (sensor == null)
            return false;

        sensor.Estado = activo;
        await _sensorRepository.GuardarCambiosAsync();
        return true;
    }

    public async Task ReiniciarSistemaAsync()
    {
        var sensores = await _sensorRepository.ObtenerTodosParaActualizarAsync();
        foreach (var s in sensores)
        {
            s.Estado = true;
        }
        await _sensorRepository.GuardarCambiosAsync();
    }

    public async Task<bool> SensorExistsAsync(int id)
    {
        return await _sensorRepository.ExisteAsync(id);
    }
}