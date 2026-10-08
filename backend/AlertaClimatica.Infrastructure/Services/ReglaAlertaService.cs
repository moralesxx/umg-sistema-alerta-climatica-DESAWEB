using AlertaClimatica.Application.DTOs;
using AlertaClimatica.Domain;
using AlertaClimatica.Infrastructure.Repositories.Interfaces;
using AlertaClimatica.Infrastructure.Services.Interfaces;

namespace AlertaClimatica.Infrastructure.Services;

public class ReglaAlertaService : IReglaAlertaService
{
    private readonly IReglaAlertaRepository _repository;

    public ReglaAlertaService(IReglaAlertaRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<ReglaAlerta>> ObtenerTodasAsync()
    {
        return await _repository.ObtenerTodasAsync();
    }

    public async Task<ReglaAlerta?> ObtenerPorIdAsync(int id)
    {
        return await _repository.ObtenerPorIdAsync(id);
    }

    public async Task<ReglaAlerta> CrearAsync(
        CrearReglaAlertaDto dto)
    {
        ValidarRegla(
            dto.ValorMinimo,
            dto.ValorMaximo,
            dto.NivelPeligro);

        var regla = new ReglaAlerta
        {
            Nombre = dto.Nombre.Trim(),
            TipoSensorId = dto.TipoSensorId,
            ValorMinimo = dto.ValorMinimo,
            ValorMaximo = dto.ValorMaximo,
            NivelPeligro = dto.NivelPeligro.Trim(),
            TipoFenomeno = dto.TipoFenomeno.Trim(),
            Mensaje = dto.Mensaje.Trim(),
            Estado = dto.Estado
        };

        await _repository.CrearAsync(regla);

        return regla;
    }

    public async Task<ReglaAlerta?> ActualizarAsync(
        int id,
        ActualizarReglaAlertaDto dto)
    {
        ValidarRegla(
            dto.ValorMinimo,
            dto.ValorMaximo,
            dto.NivelPeligro);

        var regla = await _repository.ObtenerPorIdAsync(id);

        if (regla == null)
            return null;

        regla.Nombre = dto.Nombre.Trim();
        regla.TipoSensorId = dto.TipoSensorId;
        regla.ValorMinimo = dto.ValorMinimo;
        regla.ValorMaximo = dto.ValorMaximo;
        regla.NivelPeligro = dto.NivelPeligro.Trim();
        regla.TipoFenomeno = dto.TipoFenomeno.Trim();
        regla.Mensaje = dto.Mensaje.Trim();
        regla.Estado = dto.Estado;

        await _repository.ActualizarAsync(regla);

        return regla;
    }

    public async Task<bool> EliminarAsync(int id)
    {
        var regla = await _repository.ObtenerPorIdAsync(id);

        if (regla == null)
            return false;

        await _repository.EliminarAsync(regla);

        return true;
    }

    private static void ValidarRegla(
        decimal? valorMinimo,
        decimal? valorMaximo,
        string nivelPeligro)
    {
        if (!valorMinimo.HasValue && !valorMaximo.HasValue)
        {
            throw new ArgumentException(
                "La regla debe tener un valor mínimo o un valor máximo.");
        }

        if (valorMinimo.HasValue &&
            valorMaximo.HasValue &&
            valorMinimo.Value > valorMaximo.Value)
        {
            throw new ArgumentException(
                "El valor mínimo no puede ser mayor que el valor máximo.");
        }

        var nivelesValidos = new[]
        {
            "Verde",
            "Amarillo",
            "Naranja",
            "Rojo"
        };

        if (!nivelesValidos.Contains(
                nivelPeligro,
                StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "El nivel de peligro debe ser Verde, Amarillo, Naranja o Rojo.");
        }
    }
}