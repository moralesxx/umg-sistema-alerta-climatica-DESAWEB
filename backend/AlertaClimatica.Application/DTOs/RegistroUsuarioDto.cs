namespace AlertaClimatica.Application.DTOs;

// RolId se quitó a propósito: el registro público SIEMPRE asigna el rol
// "Usuario de consulta" (ver UsuariosController.CrearUsuario). Crear
// usuarios con otros roles queda para el módulo de Administración de
// Usuarios (RF-ADM-49), protegido con [Authorize(Roles = "Administrador")].
public class RegistroUsuarioDto
{
    public string Nombre { get; set; } = string.Empty;

    public string Correo { get; set; } = string.Empty;

    public string Contrasenia { get; set; } = string.Empty;
}