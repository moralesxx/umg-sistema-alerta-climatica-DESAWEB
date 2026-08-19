namespace AlertaClimatica.Domain;

public class Alerta
{
    public int IdAlerta { get; set; }
    public int IdSensor { get; set; }
    public int? IdEvento { get; set; }
    public string TipoAlerta { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    public string Nivel { get; set; } = string.Empty;
    public bool Atendida { get; set; } = false; 
    public DateTime FechaEmision { get; set; } = DateTime.Now;
}