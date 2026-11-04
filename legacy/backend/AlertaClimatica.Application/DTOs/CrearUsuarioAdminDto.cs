namespace AlertaClimatica.Application.DTOs;

// A diferencia de RegistroUsuarioDto (registro público, siempre
// "Usuario de consulta"), este DTO SÍ acepta RolId, porque solo lo
// usa un endpoint protegido con [Authorize(Roles = "Administrador")].
public class CrearUsuarioAdminDto
{
    public string Nombre { get; set; } = string.Empty;

    public string Correo { get; set; } = string.Empty;

    public string Contrasenia { get; set; } = string.Empty;

    public int RolId { get; set; }
}