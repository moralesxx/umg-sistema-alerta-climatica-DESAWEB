using AlertaClimatica.Application.DTOs;

namespace AlertaClimatica.Infrastructure.Services;

public interface IUsuarioService
{
    Task<IEnumerable<UsuarioResumenDto>> GetUsuariosAsync();

    // RF-ADM-54: búsqueda + filtros.
    Task<IEnumerable<UsuarioResumenDto>> GetUsuariosFiltradosAsync(string? texto, int? rolId, bool? estado);

    Task<UsuarioResumenDto?> GetUsuarioAsync(int id);

    // Registro público (RF-ADM-01): siempre asigna el rol de menor
    // privilegio, sin importar qué mande el cliente.
    Task<UsuarioResumenDto> RegistrarUsuarioPublicoAsync(RegistroUsuarioDto registro);

    // Creación por Administrador (RF-ADM-49): sí puede elegir el rol.
    Task<UsuarioResumenDto> CrearUsuarioComoAdminAsync(CrearUsuarioAdminDto registro);

    // RF-ADM-50/52: editar nombre, correo y rol.
    Task<UsuarioResumenDto?> ActualizarUsuarioAsync(int id, ActualizarUsuarioDto datos);

    // RF-ADM-51: activar/desactivar. usuarioQueEjecutaId es quien hace la
    // petición, para evitar que un Administrador se desactive a sí mismo.
    Task<UsuarioResumenDto?> CambiarEstadoUsuarioAsync(int id, bool activo, int usuarioQueEjecutaId);

    Task<LoginResultDto> LoginAsync(LoginDto login);
}