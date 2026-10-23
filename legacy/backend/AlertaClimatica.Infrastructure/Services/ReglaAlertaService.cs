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
            dto.Nombre,
            dto.TipoSensorId,
            dto.ValorMinimo,
            dto.ValorMaximo,
            dto.NivelPeligro,
            dto.TipoFenomeno,
            dto.Mensaje);

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
            dto.Nombre,
            dto.TipoSensorId,
            dto.ValorMinimo,
            dto.ValorMaximo,
            dto.NivelPeligro,
            dto.TipoFenomeno,
            dto.Mensaje);

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

    private static readonly string[] FenomenosValidos =
        { "Inundación", "Sequía", "Tormenta", "Helada", "Incendio forestal" };

    // Validación de negocio de una regla (campos obligatorios, rango y nivel).
    private static void ValidarRegla(
        string? nombre,
        int tipoSensorId,
        decimal? valorMinimo,
        decimal? valorMaximo,
        string? nivelPeligro,
        string? tipoFenomeno,
        string? mensaje)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre de la regla es obligatorio.");

        if (tipoSensorId <= 0)
            throw new ArgumentException("Debe seleccionar un tipo de sensor.");

        if (string.IsNullOrWhiteSpace(tipoFenomeno))
            throw new ArgumentException("El tipo de fenómeno es obligatorio.");

        // RF-ADM-47: solo los fenómenos que maneja el sistema.
        if (!FenomenosValidos.Contains(tipoFenomeno.Trim(), StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException(
                "El tipo de fenómeno debe ser Inundación, Sequía, Tormenta, Helada o Incendio forestal.");

        if (string.IsNullOrWhiteSpace(mensaje))
            throw new ArgumentException("El mensaje de la alerta es obligatorio.");

        if (string.IsNullOrWhiteSpace(nivelPeligro))
            throw new ArgumentException(
                "El nivel de peligro debe ser Verde, Amarillo, Naranja o Rojo.");

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