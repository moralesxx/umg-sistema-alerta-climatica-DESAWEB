using System.ComponentModel.DataAnnotations;

namespace AlertaClimatica.Domain;

public class TipoSensor
{
    [Key]
    public int TipoSensorId { get; set; }
    public string NombreTipo { get; set; } = string.Empty;
    public string UnidadMedida { get; set; } = string.Empty;
}