using System.ComponentModel.DataAnnotations;

namespace AlertaClimatica.Domain;

public class Rol
{
    [Key]
    public int IdRol { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.Now;
}