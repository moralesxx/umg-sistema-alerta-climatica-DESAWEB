using System.ComponentModel.DataAnnotations;

namespace AlertaClimatica.Domain;

public class Bitacora
{
    [Key]
    public long IdBitacora { get; set; }
    public int? IdUsuario { get; set; }
    public string Accion { get; set; } = string.Empty;
    public string? TablaAfectada { get; set; }
    public string? Detalle { get; set; }
    public string? IpOrigen { get; set; }
    public DateTime FechaHora { get; set; } = DateTime.Now;
}