namespace AlertaClimatica.Application.DTOs;

// RF-ADM-45: lo que devuelve la API (incluye nombres, no solo ids).
public class EventoDto
{
    public int EventoId { get; set; }
    public DateTime FechaHora { get; set; }
    public int? ComunidadId { get; set; }
    public string? ComunidadNombre { get; set; }
    public int? SensorId { get; set; }
    public string? SensorNombre { get; set; }
    public string TipoFenomeno { get; set; } = string.Empty;
    public string? NivelRiesgo { get; set; }
    public decimal? Valor { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public string? Estado { get; set; }
    public int? UsuarioResponsableId { get; set; }
    public string? UsuarioResponsableNombre { get; set; }
}

public class CrearEventoDto
{
    public string TipoFenomeno { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string NivelRiesgo { get; set; } = string.Empty;
    public int? ComunidadId { get; set; }
    public int? SensorId { get; set; }
    public decimal? Valor { get; set; }
    public string? Estado { get; set; }      // si no viene, "Activa"
    public DateTime? FechaHora { get; set; } // si no viene, ahora
}

public class ConteoDto
{
    public string Nombre { get; set; } = string.Empty;
    public int Total { get; set; }
}

// RF-ADM-48
public class EventoEstadisticasDto
{
    public int Total { get; set; }
    public List<ConteoDto> PorFenomeno { get; set; } = new();
    public List<ConteoDto> PorNivel { get; set; } = new();
    public List<ConteoDto> PorEstado { get; set; } = new();
    public List<ConteoDto> PorComunidad { get; set; } = new();
}