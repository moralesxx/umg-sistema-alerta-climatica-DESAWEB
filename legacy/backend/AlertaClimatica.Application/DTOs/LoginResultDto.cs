namespace AlertaClimatica.Application.DTOs;

public class LoginResultDto
{
    public string Token { get; set; } = string.Empty;
    public UsuarioResumenDto Usuario { get; set; } = null!;
    public string Rol { get; set; } = string.Empty;
}