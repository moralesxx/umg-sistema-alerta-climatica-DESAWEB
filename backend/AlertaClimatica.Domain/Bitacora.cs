using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlertaClimatica.Domain;

[Table("Bitacora")]
public class Bitacora
{
    [Key]
    [Column("BitacoraId")]
    public long BitacoraId { get; set; }

    [Column("UsuarioId")]
    public int? UsuarioId { get; set; }

    [Column("Accion")]
    public string Accion { get; set; } = string.Empty;

    [Column("Detalle")]
    public string Detalle { get; set; } = string.Empty;

    [Column("FechaHora")]
    public DateTime FechaHora { get; set; }
}