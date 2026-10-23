using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlertaClimatica.Domain;

[Table("TiposSensor")]
public class TipoSensor
{
    [Key]
    [Column("TipoSensorId")]
    public int TipoSensorId { get; set; }

    [Column("NombreTipo")]
    public string NombreTipo { get; set; } = string.Empty;

    [Column("UnidadMedida")]
    public string UnidadMedida { get; set; } = string.Empty;
}