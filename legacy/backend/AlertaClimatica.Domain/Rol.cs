using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlertaClimatica.Domain;

[Table("Roles")]
public class Rol
{
    [Key]
    [Column("RolId")]
    public int RolId { get; set; }

    [Column("NombreRol")]
    public string NombreRol { get; set; } = string.Empty;

    [Column("Descripcion")]
    public string? Descripcion { get; set; }
}