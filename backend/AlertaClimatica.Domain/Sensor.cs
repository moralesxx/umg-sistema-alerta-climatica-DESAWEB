namespace AlertaClimatica.Domain;

public class Sensor
{
    public int SensorId { get; set; }
    public string NombreSensor { get; set; } = string.Empty;
    public int TipoSensorId { get; set; }
    public string Ubicacion { get; set; } = string.Empty;
    public bool Estado { get; set; }
    public string Codigo { get; set; } = string.Empty;

    // Nuevos campos para RF-ADM-15. Nullable en la base (por los sensores de
    // prueba ya existentes), pero obligatorios a nivel de validación en el
    // Service cuando se crea un sensor nuevo.
    public int? ComunidadId { get; set; }
    public DateTime? FechaInstalacion { get; set; }
    public string? Descripcion { get; set; }

    // Propiedades de navegación
    public TipoSensor? TipoSensor { get; set; }
    public Comunidad? Comunidad { get; set; }
}