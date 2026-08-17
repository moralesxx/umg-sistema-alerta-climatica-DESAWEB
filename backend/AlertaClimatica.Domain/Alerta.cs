using System.ComponentModel.DataAnnotations;

namespace AlertaClimatica.Domain;

public class Alerta
{
    [Key]
    public int AlertaId { get; set; }
    public int SensorId { get; set; }
    public string NivelRiesgo { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    public DateTime FechaEmision { get; set; } = DateTime.Now;
}