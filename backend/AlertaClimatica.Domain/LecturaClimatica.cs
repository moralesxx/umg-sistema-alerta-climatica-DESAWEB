using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlertaClimatica.Domain;

public class LecturaClimatica
{
    [Key]
    public long IdLectura { get; set; }
    public int IdSensor { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Valor { get; set; }
    public DateTime FechaHora { get; set; } = DateTime.Now;

    // Propiedad de navegación opcional
    public Sensor? Sensor { get; set; }
}