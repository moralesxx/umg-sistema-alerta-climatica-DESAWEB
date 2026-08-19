using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlertaClimatica.Domain;

[Table("LecturasClimaticas")]
public class LecturaClimatica
{
    [Key]
    [Column("LecturaId")]
    public long LecturaId { get; set; }

    [Column("SensorId")]
    public int SensorId { get; set; }

    [Column("Valor")]
    public decimal Valor { get; set; }

    [Column("FechaHora")]
    public DateTime FechaHora { get; set; }
}