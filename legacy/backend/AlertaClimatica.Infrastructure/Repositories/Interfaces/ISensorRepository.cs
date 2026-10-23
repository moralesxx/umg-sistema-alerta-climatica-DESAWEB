using AlertaClimatica.Domain;

namespace AlertaClimatica.Infrastructure.Repositories.Interfaces;

// Capa de acceso a datos pura: solo consultas EF Core, sin reglas de negocio.
// Las validaciones (¿existe el tipo?, ¿nombre vacío?, generar código, etc.)
// viven en SensorService, no aquí.
public interface ISensorRepository
{
    Task<IEnumerable<Sensor>> ObtenerTodosAsync();
    Task<IEnumerable<Sensor>> ObtenerFiltradosAsync(int? comunidadId, int? tipoSensorId, bool? estado, string? codigo);
    Task<Sensor?> ObtenerPorIdAsync(int id);
    Task<Sensor?> ObtenerParaActualizarAsync(int id);
    Task<IEnumerable<Sensor>> ObtenerTodosParaActualizarAsync();
    Task AgregarAsync(Sensor sensor);
    Task<bool> ExisteAsync(int id);
    Task GuardarCambiosAsync();
}