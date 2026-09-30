using AlertaClimatica.Application.DTOs;

namespace AlertaClimatica.Infrastructure.Services;

public interface IUsuarioService
{
    Task<IEnumerable<UsuarioResumenDto>> GetUsuariosAsync();
    Task<UsuarioResumenDto?> GetUsuarioAsync(int id);

    // Registro público (RF-ADM-01): siempre asigna el rol de menor
    // privilegio, sin importar qué mande el cliente.
    Task<UsuarioResumenDto> RegistrarUsuarioPublicoAsync(RegistroUsuarioDto registro);

    // Creación por Administrador (RF-ADM-49): sí puede elegir el rol.
    Task<UsuarioResumenDto> CrearUsuarioComoAdminAsync(CrearUsuarioAdminDto registro);

    Task<LoginResultDto> LoginAsync(LoginDto login);
}