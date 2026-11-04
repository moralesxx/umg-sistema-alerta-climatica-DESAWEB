using AlertaClimatica.Domain;

namespace AlertaClimatica.Infrastructure.Repositories.Interfaces;

// Capa de acceso a datos pura: solo consultas EF Core, sin reglas de negocio.
// Las validaciones (campos obligatorios, rangos de coordenadas, duplicados,
// bitácora) viven en ComunidadService, no aquí.
public interface IComunidadRepository
{
    Task<IEnumerable<Comunidad>> ObtenerTodosAsync();

    // RF-ADM-11 y RF-ADM-12: listado con búsqueda y filtros opcionales.
    Task<IEnumerable<Comunidad>> ObtenerFiltradosAsync(
        string? busqueda, bool? estado, string? municipio, string? departamento);

    // Sin tracking, para lectura.
    Task<Comunidad?> ObtenerPorIdAsync(int id);

    // Con tracking, porque el Service modifica sus propiedades y luego
    // llama a GuardarCambiosAsync().
    Task<Comunidad?> ObtenerParaActualizarAsync(int id);

    Task<bool> ExisteAsync(int id);

    // Regla de duplicados: mismo nombre + municipio + departamento.
    // excluirId permite ignorar a la propia comunidad al editarla.
    Task<bool> ExisteDuplicadoAsync(
        string nombre, string municipio, string departamento, int? excluirId);

    // RF-ADM-13: cantidad de sensores (activos e inactivos) de una comunidad.
    Task<int> ContarSensoresAsync(int comunidadId);

    // Misma cuenta pero para todas a la vez (una sola consulta, evita N+1).
    // Las comunidades sin sensores no aparecen en el diccionario.
    Task<Dictionary<int, int>> ContarSensoresPorComunidadAsync();

    Task AgregarAsync(Comunidad comunidad);

    Task GuardarCambiosAsync();
}
