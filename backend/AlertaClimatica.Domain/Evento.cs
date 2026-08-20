using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlertaClimatica.Domain;

[Table("Eventos")]
public class Evento
{
    [Key]
    [Column("EventoId")]
    public int EventoId { get; set; }

    [Column("TipoEvento")]
    public string TipoFenomeno { get; set; } = string.Empty;

    [Column("Descripcion")]
    public string Descripcion { get; set; } = string.Empty;

    [Column("FechaHora")]
    public DateTime FechaHora { get; set; }
}