using AlertaClimatica.Application.DTOs;

namespace AlertaClimatica.Infrastructure.Services;

public interface IComunidadService
{
    // RF-ADM-11, 12 y 13: listado con filtros opcionales y cantidad de sensores.
    Task<IEnumerable<ComunidadDto>> GetComunidadesAsync(
        string? busqueda, bool? estado, string? municipio, string? departamento);

    Task<ComunidadDto?> GetComunidadAsync(int id);

    // RF-ADM-08
    Task<ComunidadDto> CrearComunidadAsync(CrearComunidadDto dto);

    // RF-ADM-09. Devuelve false si la comunidad no existe.
    Task<bool> ActualizarComunidadAsync(int id, ActualizarComunidadDto dto);

    // RF-ADM-10. Devuelve false si la comunidad no existe.
    Task<bool> CambiarEstadoComunidadAsync(int id, bool activa);
}
