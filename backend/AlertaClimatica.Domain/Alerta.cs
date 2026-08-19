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

    [Column("NivelRiesgo")]
    public string NivelRiesgo { get; set; } = string.Empty;

    [Column("Mensaje")]
    public string Mensaje { get; set; } = string.Empty;

    [Column("FechaEmision")]
    public DateTime FechaEmision { get; set; }
}