namespace AlertaClimatica.Application.DTOs;

public class RegistroUsuarioDto
{
    public string Nombre { get; set; } = string.Empty;

    public string Correo { get; set; } = string.Empty;

    public string Contrasenia { get; set; } = string.Empty;

    public int RolId { get; set; }
}