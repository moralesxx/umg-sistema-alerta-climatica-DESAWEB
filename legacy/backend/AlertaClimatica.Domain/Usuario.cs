using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlertaClimatica.Domain;

[Table("Usuarios")]
public class Usuario
{
    [Key]
    [Column("UsuarioId")]
    public int UsuarioId { get; set; }

    [Column("Nombre")]
    public string Nombre { get; set; } = string.Empty;

    [Column("Correo")]
    public string Correo { get; set; } = string.Empty;

    [Column("ContraseniaHash")]
    public string ContraseniaHash { get; set; } = string.Empty;

    [Column("RolId")]
    public int RolId { get; set; }

    [Column("Estado")]
    public bool Estado { get; set; }

    [Column("FechaCreacion")]
    public DateTime FechaCreacion { get; set; }

    // RF-ADM-55: "Último acceso". Nullable porque los usuarios ya
    // existentes (como el admin sembrado) todavía no tienen ninguno
    // registrado hasta su próximo login.
    [Column("UltimoAcceso")]
    public DateTime? UltimoAcceso { get; set; }
}