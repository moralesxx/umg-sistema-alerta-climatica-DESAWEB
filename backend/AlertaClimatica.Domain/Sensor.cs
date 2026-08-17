using System.ComponentModel.DataAnnotations;

namespace AlertaClimatica.Domain;

public class Sensor
{
    [Key]
    public int SensorId { get; set; }
    public string NombreSensor { get; set; } = string.Empty;
    public int TipoSensorId { get; set; }
    public string Ubicacion { get; set; } = string.Empty;
    public bool Estado { get; set; } = true;
}