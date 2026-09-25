using Microsoft.EntityFrameworkCore;
using AlertaClimatica.Domain;

namespace AlertaClimatica.Infrastructure.Services;

// Servicio de dominio para el módulo de Sensores.
// Saca la lógica de negocio del controller (SensoresController) y la centraliza
// aquí, siguiendo el mismo patrón que ya usan con BitacoraService.
public class SensorService : ISensorService
{
    private readonly ApplicationDbContext _context;

    public SensorService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Sensor>> GetSensoresAsync()
    {
        return await _context.Sensores
            .AsNoTracking()
            .OrderBy(s => s.SensorId)
            .ToListAsync();
    }

    public async Task<Sensor?> GetSensorByIdAsync(int id)
    {
        return await _context.Sensores
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SensorId == id);
    }

    public async Task<Sensor> CreateSensorAsync(Sensor sensor)
    {
        if (string.IsNullOrWhiteSpace(sensor.NombreSensor))
            throw new ArgumentException("El nombre del sensor es obligatorio.");

        if (string.IsNullOrWhiteSpace(sensor.Ubicacion))
            throw new ArgumentException("La ubicación del sensor es obligatoria.");

        var tipoExiste = await _context.TiposSensor
            .AnyAsync(t => t.TipoSensorId == sensor.TipoSensorId);

        if (!tipoExiste)
            throw new ArgumentException($"El tipo de sensor con Id {sensor.TipoSensorId} no existe.");

        // El modal "Agregar Sensor" del frontend no envía Código, y la columna
        // Codigo en la BD es NOT NULL UNIQUE. Si llega vacío, se generaba
        // string.Empty por defecto y el segundo sensor creado violaría el
        // UNIQUE. Se genera uno automáticamente cuando no viene informado.
        if (string.IsNullOrWhiteSpace(sensor.Codigo))
        {
            sensor.Codigo = $"SEN-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
        }

        sensor.SensorId = 0; // que el autoincremental de la BD lo asigne
        sensor.Estado = true; // todo sensor nuevo nace activo

        _context.Sensores.Add(sensor);
        await _context.SaveChangesAsync();

        return sensor;
    }

    public async Task<bool> UpdateSensorAsync(int id, Sensor sensor)
    {
        if (id != sensor.SensorId)
            throw new ArgumentException("El ID del sensor no coincide.");

        var existente = await _context.Sensores.FindAsync(id);
        if (existente == null)
            return false;

        var tipoExiste = await _context.TiposSensor
            .AnyAsync(t => t.TipoSensorId == sensor.TipoSensorId);

        if (!tipoExiste)
            throw new ArgumentException($"El tipo de sensor con Id {sensor.TipoSensorId} no existe.");

        // Se actualizan campo por campo (en vez de reemplazar la entidad completa
        // con _context.Entry(sensor).State = Modified) para no pisar por accidente
        // columnas que el cliente no envió, como Codigo, Latitud o Longitud.
        existente.NombreSensor = sensor.NombreSensor;
        existente.Ubicacion = sensor.Ubicacion;
        existente.TipoSensorId = sensor.TipoSensorId;
        existente.Estado = sensor.Estado;

        if (!string.IsNullOrWhiteSpace(sensor.Codigo))
            existente.Codigo = sensor.Codigo;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CambiarEstadoSensorAsync(int id, bool activo)
    {
        var sensor = await _context.Sensores.FindAsync(id);
        if (sensor == null)
            return false;

        sensor.Estado = activo;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task ReiniciarSistemaAsync()
    {
        var sensores = await _context.Sensores.ToListAsync();
        foreach (var s in sensores)
        {
            s.Estado = true;
        }
        await _context.SaveChangesAsync();
    }

    public async Task<bool> SensorExistsAsync(int id)
    {
        return await _context.Sensores.AnyAsync(s => s.SensorId == id);
    }
}