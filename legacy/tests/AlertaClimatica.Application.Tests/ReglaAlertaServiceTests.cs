using AlertaClimatica.Application.DTOs;
using AlertaClimatica.Domain;
using AlertaClimatica.Infrastructure.Repositories.Interfaces;
using AlertaClimatica.Infrastructure.Services;

namespace AlertaClimatica.Application.Tests;

// Pruebas de la validación de reglas de alerta (RF de reglas / umbrales).
// Usan un repositorio en memoria, sin base de datos.
public class ReglaAlertaServiceTests
{
    private sealed class RepositorioFalso : IReglaAlertaRepository
    {
        public List<ReglaAlerta> Reglas { get; } = new();

        public Task<IEnumerable<ReglaAlerta>> ObtenerTodasAsync() =>
            Task.FromResult<IEnumerable<ReglaAlerta>>(Reglas);

        public Task<ReglaAlerta?> ObtenerPorIdAsync(int id) =>
            Task.FromResult(Reglas.FirstOrDefault(r => r.ReglaAlertaId == id));

        public Task<IEnumerable<ReglaAlerta>> ObtenerActivasPorTipoSensorAsync(int tipoSensorId) =>
            Task.FromResult<IEnumerable<ReglaAlerta>>(
                Reglas.Where(r => r.Estado && r.TipoSensorId == tipoSensorId).ToList());

        public Task CrearAsync(ReglaAlerta regla)
        {
            Reglas.Add(regla);
            return Task.CompletedTask;
        }

        public Task ActualizarAsync(ReglaAlerta regla) => Task.CompletedTask;

        public Task EliminarAsync(ReglaAlerta regla)
        {
            Reglas.Remove(regla);
            return Task.CompletedTask;
        }
    }

    private static CrearReglaAlertaDto ReglaValida() => new()
    {
        Nombre = "Lluvia fuerte",
        TipoSensorId = 1,
        ValorMinimo = 50,
        ValorMaximo = 100,
        NivelPeligro = "Naranja",
        TipoFenomeno = "Inundación",
        Mensaje = "Lluvia intensa detectada.",
        Estado = true
    };

    [Fact]
    public async Task Crear_ReglaValida_SeGuarda()
    {
        var repo = new RepositorioFalso();
        var servicio = new ReglaAlertaService(repo);

        var creada = await servicio.CrearAsync(ReglaValida());

        Assert.Single(repo.Reglas);
        Assert.Equal("Lluvia fuerte", creada.Nombre);
    }

    [Fact]
    public async Task Crear_MinimoMayorQueMaximo_Falla()
    {
        var servicio = new ReglaAlertaService(new RepositorioFalso());
        var dto = ReglaValida();
        dto.ValorMinimo = 200;
        dto.ValorMaximo = 100;

        await Assert.ThrowsAsync<ArgumentException>(() => servicio.CrearAsync(dto));
    }

    [Fact]
    public async Task Crear_SinMinimoNiMaximo_Falla()
    {
        var servicio = new ReglaAlertaService(new RepositorioFalso());
        var dto = ReglaValida();
        dto.ValorMinimo = null;
        dto.ValorMaximo = null;

        await Assert.ThrowsAsync<ArgumentException>(() => servicio.CrearAsync(dto));
    }

    [Fact]
    public async Task Crear_NivelInvalido_Falla()
    {
        var servicio = new ReglaAlertaService(new RepositorioFalso());
        var dto = ReglaValida();
        dto.NivelPeligro = "Morado";

        await Assert.ThrowsAsync<ArgumentException>(() => servicio.CrearAsync(dto));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Crear_NombreVacio_Falla(string nombre)
    {
        var servicio = new ReglaAlertaService(new RepositorioFalso());
        var dto = ReglaValida();
        dto.Nombre = nombre;

        await Assert.ThrowsAsync<ArgumentException>(() => servicio.CrearAsync(dto));
    }

    [Fact]
    public async Task Crear_SinMensaje_Falla()
    {
        var servicio = new ReglaAlertaService(new RepositorioFalso());
        var dto = ReglaValida();
        dto.Mensaje = "";

        await Assert.ThrowsAsync<ArgumentException>(() => servicio.CrearAsync(dto));
    }

    [Fact]
    public async Task Crear_SinTipoSensor_Falla()
    {
        var servicio = new ReglaAlertaService(new RepositorioFalso());
        var dto = ReglaValida();
        dto.TipoSensorId = 0;

        await Assert.ThrowsAsync<ArgumentException>(() => servicio.CrearAsync(dto));
    }

    [Fact]
    public async Task Crear_FenomenoNoSoportado_Falla()
    {
        var servicio = new ReglaAlertaService(new RepositorioFalso());
        var dto = ReglaValida();
        dto.TipoFenomeno = "Ola de calor";

        await Assert.ThrowsAsync<ArgumentException>(() => servicio.CrearAsync(dto));
    }

    [Theory]
    [InlineData("Inundación")]
    [InlineData("Sequía")]
    [InlineData("Tormenta")]
    [InlineData("Helada")]
    [InlineData("Incendio forestal")]
    public async Task Crear_FenomenosSoportados_Pasan(string fenomeno)
    {
        var servicio = new ReglaAlertaService(new RepositorioFalso());
        var dto = ReglaValida();
        dto.TipoFenomeno = fenomeno;

        var creada = await servicio.CrearAsync(dto);

        Assert.Equal(fenomeno, creada.TipoFenomeno);
    }

    [Fact]
    public async Task Eliminar_ReglaInexistente_DevuelveFalse()
    {
        var servicio = new ReglaAlertaService(new RepositorioFalso());

        Assert.False(await servicio.EliminarAsync(999));
    }
}
