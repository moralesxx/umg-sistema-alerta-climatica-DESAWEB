using System.ComponentModel.DataAnnotations;

namespace AlertaClimatica.Domain;

public class Evento
{
    [Key]
    public int EventoId { get; set; }
    public string TipoEvento { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public DateTime FechaHora { get; set; } = DateTime.Now;
}