using System.ComponentModel.DataAnnotations;

namespace AlertaClimatica.Domain;

public class Usuario
{
    [Key]
    public int UsuarioId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string ContraseniaHash { get; set; } = string.Empty;
    public int RolId { get; set; }
    public bool Estado { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.Now;
}