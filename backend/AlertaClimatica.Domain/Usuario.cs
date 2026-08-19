using System.ComponentModel.DataAnnotations;

namespace AlertaClimatica.Domain;

public class Usuario
{
    [Key]
    public int IdUsuario { get; set; }
    public int IdRol { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool Estado { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.Now;

    // Propiedad de navegación
    public Rol? Rol { get; set; }
}