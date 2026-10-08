using AlertaClimatica.Application.DTOs;
using AlertaClimatica.Domain;

namespace AlertaClimatica.Infrastructure.Services.Interfaces;

public interface IReglaAlertaService
{
    Task<IEnumerable<ReglaAlerta>> ObtenerTodasAsync();

    Task<ReglaAlerta?> ObtenerPorIdAsync(int id);

    Task<ReglaAlerta> CrearAsync(CrearReglaAlertaDto dto);

    Task<ReglaAlerta?> ActualizarAsync(
        int id,
        ActualizarReglaAlertaDto dto);

    Task<bool> EliminarAsync(int id);
}