using System.ComponentModel.DataAnnotations;

namespace AlertaClimatica.Domain;

public class Rol
{
    [Key]
    public int RolId { get; set; }
    public string NombreRol { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
}