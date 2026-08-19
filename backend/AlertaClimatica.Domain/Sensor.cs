namespace AlertaClimatica.Domain;

public class Sensor
{
    public int IdSensor { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Ubicacion { get; set; } = string.Empty;
    public decimal Latitud { get; set; }
    public decimal Longitud { get; set; }
    public DateTime FechaInstalacion { get; set; }
    public bool Estado { get; set; } = true; 
    public string Codigo { get; set; } = string.Empty;
    public int IdTipoSensor { get; set; }
}