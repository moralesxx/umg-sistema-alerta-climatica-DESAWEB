using AlertaClimatica.Domain;

namespace AlertaClimatica.Infrastructure.Repositories.Interfaces;

public interface IUsuarioRepository
{
    Task<IEnumerable<Usuario>> ObtenerTodosAsync();

    // RF-ADM-54: búsqueda (nombre/correo) + filtro por rol y estado.
    // Todos los parámetros son opcionales; sin ninguno, devuelve todos.
    Task<IEnumerable<Usuario>> ObtenerFiltradosAsync(string? texto, int? rolId, bool? estado);

    Task<Usuario?> ObtenerPorIdAsync(int id);
    Task<Usuario?> ObtenerParaActualizarAsync(int id);
    Task<Usuario?> ObtenerPorCorreoAsync(string correo);
    Task<bool> ExisteCorreoAsync(string correo);
    Task<bool> ExisteCorreoEnOtroUsuarioAsync(string correo, int usuarioIdExcluir);
    Task AgregarAsync(Usuario usuario);
    Task GuardarCambiosAsync();
}