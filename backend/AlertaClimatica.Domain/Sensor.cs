namespace AlertaClimatica.Domain;

public class Sensor
{
    public int SensorId { get; set; }
    public string NombreSensor { get; set; } = string.Empty;
    public int TipoSensorId { get; set; }
    public string Ubicacion { get; set; } = string.Empty;
    public bool Estado { get; set; }
    
    // Agrega esta línea si falta:
    public string Codigo { get; set; } = string.Empty;

    // Propiedad de navegación (si la tienes)
    public TipoSensor? TipoSensor { get; set; }
}