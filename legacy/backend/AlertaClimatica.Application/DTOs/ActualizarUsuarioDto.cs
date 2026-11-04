namespace AlertaClimatica.Application.DTOs;

// RF-ADM-50 (editar) y RF-ADM-52 (asignar rol). No incluye contraseña
// a propósito: cambiar contraseña es su propio flujo, no parte de
// "editar información del usuario".
public class ActualizarUsuarioDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public int RolId { get; set; }
}