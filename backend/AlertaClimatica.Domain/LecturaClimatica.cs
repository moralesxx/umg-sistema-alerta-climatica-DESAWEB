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

    // Propiedad de navegación hacia el sensor
    [ForeignKey("SensorId")]
    public virtual Sensor? Sensor { get; set; }

    // Propiedades calculadas para que Angular muestre la Unidad y el Estado del Sensor sin afectar la BD
    [NotMapped]
    public string? Unidad => Sensor?.TipoSensor?.UnidadMedida; // Asumiendo que TipoSensor tiene la propiedad Unidad o equivalente

    [NotMapped]
    public string? EstadoSensor => Sensor != null ? (Sensor.Estado ? "Activo" : "Inactivo") : "Activo";
}