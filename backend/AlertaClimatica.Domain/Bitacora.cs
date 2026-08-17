using System.ComponentModel.DataAnnotations;

namespace AlertaClimatica.Domain;

public class Bitacora
{
    [Key]
    public long BitacoraId { get; set; }
    public int? UsuarioId { get; set; }
    public string Accion { get; set; } = string.Empty;
    public string Detalle { get; set; } = string.Empty;
    public DateTime FechaHora { get; set; } = DateTime.Now;
}