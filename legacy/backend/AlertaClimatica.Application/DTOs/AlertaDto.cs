namespace AlertaClimatica.Application.DTOs;

// Alerta lista para mostrar (RF-ADM-34 y RF-ADM-38): incluye comunidad,
// sensor, regla, fenómeno, valor detectado, umbral y responsable.
public class AlertaDto
{
    public int AlertaId { get; set; }
    public DateTime FechaEmision { get; set; }

    public int SensorId { get; set; }
    public string? SensorCodigo { get; set; }
    public string? SensorNombre { get; set; }

    public int? ComunidadId { get; set; }
    public string? ComunidadNombre { get; set; }

    public int? ReglaAlertaId { get; set; }
    public string? ReglaNombre { get; set; }

    public string? Fenomeno { get; set; }
    public string NivelRiesgo { get; set; } = string.Empty;
    public decimal? ValorDetectado { get; set; }
    public string? Unidad { get; set; }
    public string? Umbral { get; set; }
    public string Mensaje { get; set; } = string.Empty;

    public string Estado { get; set; } = "Activa";
    public int? EventoId { get; set; }
    public int? UsuarioResponsableId { get; set; }
    public string? UsuarioResponsableNombre { get; set; }
    public DateTime? FechaGestion { get; set; }
}
