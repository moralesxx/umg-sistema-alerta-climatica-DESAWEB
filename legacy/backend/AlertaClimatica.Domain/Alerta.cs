using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlertaClimatica.Domain;

[Table("Alertas")]
public class Alerta
{
    [Key]
    [Column("AlertaId")]
    public int AlertaId { get; set; }

    [Column("SensorId")]
    public int SensorId { get; set; }

    [Column("ReglaAlertaId")]
    public int? ReglaAlertaId { get; set; }

    [Column("NivelRiesgo")]
    public string NivelRiesgo { get; set; } = string.Empty;

    [Column("Mensaje")]
    public string Mensaje { get; set; } = string.Empty;

    [Column("FechaEmision")]
    public DateTime FechaEmision { get; set; }

    // RF-ADM-38: valor detectado que provocó la alerta (el umbral se obtiene
    // de la regla asociada: ValorMinimo / ValorMaximo).
    [Column("ValorDetectado")]
    public decimal? ValorDetectado { get; set; }

    // RF-ADM-41: Activa, Atendida o Cerrada.
    [Column("Estado")]
    public string Estado { get; set; } = "Activa";

    // Evento del historial que se creó junto con la alerta.
    [Column("EventoId")]
    public int? EventoId { get; set; }

    // Usuario que atendió o cerró la alerta (null mientras esté Activa).
    [Column("UsuarioResponsableId")]
    public int? UsuarioResponsableId { get; set; }

    [Column("FechaGestion")]
    public DateTime? FechaGestion { get; set; }

    [ForeignKey("EventoId")]
    public virtual Evento? Evento { get; set; }

    [ForeignKey("SensorId")]
    public virtual Sensor? Sensor { get; set; }

    [ForeignKey("ReglaAlertaId")]
    public virtual ReglaAlerta? ReglaAlerta { get; set; }
}