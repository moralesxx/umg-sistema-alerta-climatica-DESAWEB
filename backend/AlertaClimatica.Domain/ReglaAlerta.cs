using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlertaClimatica.Domain;

[Table("ReglasAlerta")]
public class ReglaAlerta
{
    [Key]
    [Column("ReglaAlertaId")]
    public int ReglaAlertaId { get; set; }

    [Column("Nombre")]
    [Required]
    public string Nombre { get; set; } = string.Empty;

    [Column("TipoSensorId")]
    public int TipoSensorId { get; set; }

    [Column("ValorMinimo")]
    public decimal? ValorMinimo { get; set; }

    [Column("ValorMaximo")]
    public decimal? ValorMaximo { get; set; }

    [Column("NivelPeligro")]
    [Required]
    public string NivelPeligro { get; set; } = string.Empty;

    [Column("TipoFenomeno")]
    [Required]
    public string TipoFenomeno { get; set; } = string.Empty;

    [Column("Mensaje")]
    [Required]
    public string Mensaje { get; set; } = string.Empty;

    [Column("Estado")]
    public bool Estado { get; set; } = true;

    [ForeignKey("TipoSensorId")]
    public virtual TipoSensor? TipoSensor { get; set; }
}