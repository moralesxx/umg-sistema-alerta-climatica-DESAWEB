using AlertaClimatica.Domain;

namespace AlertaClimatica.Infrastructure.Repositories.Interfaces;

public interface IEventoRepository
{
    // RF-ADM-47: cada filtro solo aplica si viene informado.
    // hastaExclusivo: el service le suma un día a fechaFin para incluir todo ese día.
    Task<IEnumerable<Evento>> ObtenerFiltradosAsync(
        DateTime? desde, DateTime? hastaExclusivo, int? comunidadId, string? fenomeno, string? nivel);

    Task<Evento?> ObtenerPorIdAsync(int id);
    Task<bool> ExisteComunidadAsync(int id);
    Task<bool> ExisteSensorAsync(int id);
    Task<int?> ObtenerComunidadIdDeSensorAsync(int sensorId);
    Task AgregarAsync(Evento evento);
    Task GuardarCambiosAsync();
}