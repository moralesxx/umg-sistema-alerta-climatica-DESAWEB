using AlertaClimatica.Application.DTOs;
using AlertaClimatica.Domain;
using AlertaClimatica.Infrastructure.Repositories.Interfaces;

namespace AlertaClimatica.Infrastructure.Services;

public class EventoService : IEventoService
{
    // RF-ADM-47, RF-ADM-31 y RF-ADM-41
    private static readonly string[] Fenomenos = { "Inundación", "Sequía", "Tormenta", "Helada", "Incendio forestal" };
    private static readonly string[] Niveles = { "Verde", "Amarillo", "Naranja", "Rojo" };
    private static readonly string[] Estados = { "Activa", "Atendida", "Cerrada" };

    private readonly IEventoRepository _eventoRepository;
    private readonly IBitacoraService _bitacoraService;

    public EventoService(IEventoRepository eventoRepository, IBitacoraService bitacoraService)
    {
        _eventoRepository = eventoRepository;
        _bitacoraService = bitacoraService;
    }

    private static EventoDto AResumen(Evento e) => new()
    {
        EventoId = e.EventoId,
        FechaHora = e.FechaHora,
        ComunidadId = e.ComunidadId,
        ComunidadNombre = e.Comunidad?.Nombre,
        SensorId = e.SensorId,
        SensorNombre = e.Sensor?.NombreSensor,
        TipoFenomeno = e.TipoFenomeno,
        NivelRiesgo = e.NivelRiesgo,
        Valor = e.Valor,
        Descripcion = e.Descripcion,
        Estado = e.Estado,
        UsuarioResponsableId = e.UsuarioResponsableId,
        UsuarioResponsableNombre = e.UsuarioResponsable?.Nombre
    };

    public async Task<IEnumerable<EventoDto>> GetEventosAsync(
        DateTime? fechaInicio, DateTime? fechaFin, int? comunidadId, string? fenomeno, string? nivel)
    {
        var eventos = await _eventoRepository.ObtenerFiltradosAsync(
            fechaInicio?.Date, fechaFin?.Date.AddDays(1), comunidadId, fenomeno, nivel);

        return eventos.Select(AResumen).ToList();
    }

    public async Task<EventoEstadisticasDto> GetEstadisticasAsync(
        DateTime? fechaInicio, DateTime? fechaFin, int? comunidadId, string? fenomeno, string? nivel)
    {
        var eventos = (await _eventoRepository.ObtenerFiltradosAsync(
            fechaInicio?.Date, fechaFin?.Date.AddDays(1), comunidadId, fenomeno, nivel)).ToList();

        return new EventoEstadisticasDto
        {
            Total = eventos.Count,
            PorFenomeno = Contar(eventos, e => e.TipoFenomeno),
            PorNivel = Contar(eventos, e => e.NivelRiesgo),
            PorEstado = Contar(eventos, e => e.Estado),
            PorComunidad = Contar(eventos, e => e.Comunidad?.Nombre)
        };
    }

    // Los eventos anteriores a la Fase 2 no tienen nivel/estado/comunidad: se agrupan como "Sin dato".
    private static List<ConteoDto> Contar(IEnumerable<Evento> eventos, Func<Evento, string?> campo) =>
        eventos
            .GroupBy(e => string.IsNullOrWhiteSpace(campo(e)) ? "Sin dato" : campo(e)!)
            .Select(g => new ConteoDto { Nombre = g.Key, Total = g.Count() })
            .OrderByDescending(c => c.Total)
            .ToList();

    public async Task<EventoDto?> GetEventoAsync(int id)
    {
        var evento = await _eventoRepository.ObtenerPorIdAsync(id);
        return evento == null ? null : AResumen(evento);
    }

    public async Task<EventoDto> CrearEventoAsync(CrearEventoDto dto)
    {
        var fenomeno = Fenomenos.FirstOrDefault(f =>
            f.Equals(dto.TipoFenomeno?.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException(
                "El fenómeno debe ser: Inundación, Sequía, Tormenta, Helada o Incendio forestal.");

        var nivel = Niveles.FirstOrDefault(n =>
            n.Equals(dto.NivelRiesgo?.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException("El nivel debe ser: Verde, Amarillo, Naranja o Rojo.");

        var estado = string.IsNullOrWhiteSpace(dto.Estado)
            ? "Activa"
            : Estados.FirstOrDefault(s => s.Equals(dto.Estado.Trim(), StringComparison.OrdinalIgnoreCase))
              ?? throw new ArgumentException("El estado debe ser: Activa, Atendida o Cerrada.");

        if (string.IsNullOrWhiteSpace(dto.Descripcion))
            throw new ArgumentException("La descripción del evento es obligatoria.");

        if (dto.Descripcion.Trim().Length > 500)
            throw new ArgumentException("La descripción no puede superar 500 caracteres.");

        if (dto.ComunidadId.HasValue && !await _eventoRepository.ExisteComunidadAsync(dto.ComunidadId.Value))
            throw new ArgumentException("La comunidad indicada no existe.");

        int? comunidadId = dto.ComunidadId;

        if (dto.SensorId.HasValue)
        {
            if (!await _eventoRepository.ExisteSensorAsync(dto.SensorId.Value))
                throw new ArgumentException("El sensor indicado no existe.");

            // Si no mandaron comunidad, se toma la del sensor.
            comunidadId ??= await _eventoRepository.ObtenerComunidadIdDeSensorAsync(dto.SensorId.Value);
        }

        var evento = new Evento
        {
            TipoFenomeno = fenomeno,
            Descripcion = dto.Descripcion.Trim(),
            NivelRiesgo = nivel,
            Estado = estado,
            Valor = dto.Valor,
            ComunidadId = comunidadId,
            SensorId = dto.SensorId,
            // Una fecha en UTC ("Z") se pasa a hora local para no mezclar zonas
            // con el resto del sistema (bitácora, alertas), que usan DateTime.Now.
            FechaHora = dto.FechaHora is { Kind: DateTimeKind.Utc } utc
                ? utc.ToLocalTime()
                : dto.FechaHora ?? DateTime.Now
        };

        await _eventoRepository.AgregarAsync(evento);
        await _eventoRepository.GuardarCambiosAsync();

        await _bitacoraService.RegistrarAsync(
            "CREAR_EVENTO",
            $"El usuario registró el evento #{evento.EventoId} ({fenomeno}, nivel {nivel}).",
            "Evento",
            evento.EventoId
        );

        var creado = await _eventoRepository.ObtenerPorIdAsync(evento.EventoId);
        return AResumen(creado!);
    }
}