using System.ComponentModel.DataAnnotations;

namespace AlertaClimatica.Domain;

public class LecturaClimatica
{
    [Key]
    public long LecturaId { get; set; }
    public int SensorId { get; set; }
    public decimal Valor { get; set; }
    public DateTime FechaHora { get; set; } = DateTime.Now;
}