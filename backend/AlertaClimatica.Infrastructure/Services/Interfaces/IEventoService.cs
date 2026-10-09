using AlertaClimatica.Application.DTOs;

namespace AlertaClimatica.Infrastructure.Services;

public interface IEventoService
{
    // RF-ADM-46 y 47
    Task<IEnumerable<EventoDto>> GetEventosAsync(
        DateTime? fechaInicio, DateTime? fechaFin, int? comunidadId, string? fenomeno, string? nivel);

    // RF-ADM-48: las estadísticas respetan los mismos filtros del listado.
    Task<EventoEstadisticasDto> GetEstadisticasAsync(
        DateTime? fechaInicio, DateTime? fechaFin, int? comunidadId, string? fenomeno, string? nivel);

    // Detalle de un evento (null si no existe).
    Task<EventoDto?> GetEventoAsync(int id);

    // RF-ADM-44 y 45
    Task<EventoDto> CrearEventoAsync(CrearEventoDto dto);
}