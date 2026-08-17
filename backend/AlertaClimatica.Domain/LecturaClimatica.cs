using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlertaClimatica.Domain;

public class LecturaClimatica
{
    [Key]
    public long LecturaId { get; set; }
    public int SensorId { get; set; }

    [Column(TypeName = "decimal(18,2)")] // <-- Define tipo y precisión
    public decimal Valor { get; set; }

    public DateTime FechaHora { get; set; } = DateTime.Now;
}