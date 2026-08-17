namespace AlertaClimatica.Domain;

public class Sensor
{
    public int SensorId { get; set; }
    public string NombreSensor { get; set; } = string.Empty;
    public int TipoSensorId { get; set; }
    public string Ubicacion { get; set; } = string.Empty;
    public bool Estado { get; set; }

    // Propiedad de navegación necesaria para .Include()
    public TipoSensor? TipoSensor { get; set; } 
}