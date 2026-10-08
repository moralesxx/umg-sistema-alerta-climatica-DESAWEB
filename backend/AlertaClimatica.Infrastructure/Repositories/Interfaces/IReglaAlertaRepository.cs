using AlertaClimatica.Domain;

namespace AlertaClimatica.Infrastructure.Repositories.Interfaces;

public interface IReglaAlertaRepository
{
    Task<IEnumerable<ReglaAlerta>> ObtenerTodasAsync();

    Task<ReglaAlerta?> ObtenerPorIdAsync(int id);

    Task<IEnumerable<ReglaAlerta>> ObtenerActivasPorTipoSensorAsync(int tipoSensorId);

    Task CrearAsync(ReglaAlerta regla);

    Task ActualizarAsync(ReglaAlerta regla);

    Task EliminarAsync(ReglaAlerta regla);
}