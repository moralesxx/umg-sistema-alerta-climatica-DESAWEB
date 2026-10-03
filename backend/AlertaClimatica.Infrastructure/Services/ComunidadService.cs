using AlertaClimatica.Application.DTOs;
using AlertaClimatica.Domain;
using AlertaClimatica.Infrastructure.Exceptions;
using AlertaClimatica.Infrastructure.Repositories.Interfaces;

namespace AlertaClimatica.Infrastructure.Services;

// Reglas de negocio del módulo de Comunidades. Igual que SensorService, no
// conoce ApplicationDbContext: todo el acceso a datos pasa por el repositorio.
public class ComunidadService : IComunidadService
{
    private readonly IComunidadRepository _comunidadRepository;
    private readonly IBitacoraService _bitacoraService;

    public ComunidadService(
        IComunidadRepository comunidadRepository,
        IBitacoraService bitacoraService)
    {
        _comunidadRepository = comunidadRepository;
        _bitacoraService = bitacoraService;
    }

    private static ComunidadDto AResumen(Comunidad c, int cantidadSensores) => new()
    {
        ComunidadId = c.ComunidadId,
        Nombre = c.Nombre,
        Municipio = c.Municipio,
        Departamento = c.Departamento,
        Pais = c.Pais,
        Latitud = c.Latitud,
        Longitud = c.Longitud,
        Descripcion = c.Descripcion,
        Estado = c.Estado,
        CantidadSensores = cantidadSensores
    };

    public async Task<IEnumerable<ComunidadDto>> GetComunidadesAsync(
        string? busqueda, bool? estado, string? municipio, string? departamento)
    {
        var comunidades = (await _comunidadRepository
            .ObtenerFiltradosAsync(busqueda, estado, municipio, departamento))
            .ToList();

        // Una sola consulta para contar los sensores de todas las comunidades.
        var conteos = await _comunidadRepository.ContarSensoresPorComunidadAsync();

        return comunidades
            .Select(c => AResumen(c, conteos.GetValueOrDefault(c.ComunidadId)))
            .ToList();
    }

    public async Task<ComunidadDto?> GetComunidadAsync(int id)
    {
        var comunidad = await _comunidadRepository.ObtenerPorIdAsync(id);
        if (comunidad == null) return null;

        var cantidad = await _comunidadRepository.ContarSensoresAsync(id);
        return AResumen(comunidad, cantidad);
    }

    public async Task<ComunidadDto> CrearComunidadAsync(CrearComunidadDto dto)
    {
        Validar(dto.Nombre, dto.Municipio, dto.Departamento, dto.Pais,
                dto.Latitud, dto.Longitud, dto.Descripcion);

        var nombre = dto.Nombre.Trim();
        var municipio = dto.Municipio.Trim();
        var departamento = dto.Departamento.Trim();

        if (await _comunidadRepository.ExisteDuplicadoAsync(nombre, municipio, departamento, null))
            throw new ConflictException(
                "Ya existe una comunidad con ese nombre en el mismo municipio y departamento.");

        var comunidad = new Comunidad
        {
            Nombre = nombre,
            Municipio = municipio,
            Departamento = departamento,
            Pais = dto.Pais.Trim(),
            Latitud = dto.Latitud,
            Longitud = dto.Longitud,
            Descripcion = dto.Descripcion!.Trim(),
            Estado = dto.Estado
        };

        await _comunidadRepository.AgregarAsync(comunidad);
        await _comunidadRepository.GuardarCambiosAsync();

        // RF-ADM-56: la auditoría se registra aquí, en el servidor, para que
        // quede registro aunque alguien llame a la API sin pasar por Angular.
        await _bitacoraService.RegistrarAsync(
            "CREAR_COMUNIDAD",
            $"El usuario creó la comunidad #{comunidad.ComunidadId} \"{comunidad.Nombre}\" ({comunidad.Municipio}, {comunidad.Departamento})."
        );

        return AResumen(comunidad, 0);
    }

    public async Task<bool> ActualizarComunidadAsync(int id, ActualizarComunidadDto dto)
    {
        var existente = await _comunidadRepository.ObtenerParaActualizarAsync(id);
        if (existente == null)
            return false;

        Validar(dto.Nombre, dto.Municipio, dto.Departamento, dto.Pais,
                dto.Latitud, dto.Longitud, dto.Descripcion);

        var nombre = dto.Nombre.Trim();
        var municipio = dto.Municipio.Trim();
        var departamento = dto.Departamento.Trim();

        // Se excluye a la propia comunidad para poder guardarla sin cambiar el nombre.
        if (await _comunidadRepository.ExisteDuplicadoAsync(nombre, municipio, departamento, id))
            throw new ConflictException(
                "Ya existe otra comunidad con ese nombre en el mismo municipio y departamento.");

        existente.Nombre = nombre;
        existente.Municipio = municipio;
        existente.Departamento = departamento;
        existente.Pais = dto.Pais.Trim();
        existente.Latitud = dto.Latitud;
        existente.Longitud = dto.Longitud;
        existente.Descripcion = dto.Descripcion!.Trim();

        await _comunidadRepository.GuardarCambiosAsync();

        await _bitacoraService.RegistrarAsync(
            "EDITAR_COMUNIDAD",
            $"El usuario modificó la comunidad #{id} \"{nombre}\" ({municipio}, {departamento})."
        );

        return true;
    }

    public async Task<bool> CambiarEstadoComunidadAsync(int id, bool activa)
    {
        var comunidad = await _comunidadRepository.ObtenerParaActualizarAsync(id);
        if (comunidad == null)
            return false;

        // Si ya está en ese estado no hay nada que guardar ni auditar.
        if (comunidad.Estado == activa)
            return true;

        comunidad.Estado = activa;
        await _comunidadRepository.GuardarCambiosAsync();

        await _bitacoraService.RegistrarAsync(
            activa ? "ACTIVAR_COMUNIDAD" : "DESACTIVAR_COMUNIDAD",
            $"El usuario {(activa ? "activó" : "desactivó")} la comunidad #{id} \"{comunidad.Nombre}\"."
        );

        return true;
    }

    // Validaciones compartidas por crear y editar (RF-ADM-08: estos son los
    // datos mínimos que pide el PDF). Las columnas Latitud, Longitud y
    // Descripcion son nullable en la BD, pero se exigen aquí, igual que
    // FechaInstalacion y Descripcion en SensorService.
    private static void Validar(
        string nombre, string municipio, string departamento, string pais,
        decimal? latitud, decimal? longitud, string? descripcion)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre de la comunidad es obligatorio.");

        if (string.IsNullOrWhiteSpace(municipio))
            throw new ArgumentException("El municipio es obligatorio.");

        if (string.IsNullOrWhiteSpace(departamento))
            throw new ArgumentException("El departamento es obligatorio.");

        if (string.IsNullOrWhiteSpace(pais))
            throw new ArgumentException("El país es obligatorio.");

        if (latitud == null)
            throw new ArgumentException("La latitud es obligatoria.");

        if (longitud == null)
            throw new ArgumentException("La longitud es obligatoria.");

        if (latitud < -90 || latitud > 90)
            throw new ArgumentException("La latitud debe estar entre -90 y 90.");

        if (longitud < -180 || longitud > 180)
            throw new ArgumentException("La longitud debe estar entre -180 y 180.");

        if (string.IsNullOrWhiteSpace(descripcion))
            throw new ArgumentException("La descripción es obligatoria.");
    }
}
