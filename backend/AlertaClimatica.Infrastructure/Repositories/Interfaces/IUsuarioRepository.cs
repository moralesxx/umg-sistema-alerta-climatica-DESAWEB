using AlertaClimatica.Domain;

namespace AlertaClimatica.Infrastructure.Repositories.Interfaces;

public interface IUsuarioRepository
{
    Task<IEnumerable<Usuario>> ObtenerTodosAsync();
    Task<Usuario?> ObtenerPorIdAsync(int id);
    Task<Usuario?> ObtenerPorCorreoAsync(string correo);
    Task<bool> ExisteCorreoAsync(string correo);
    Task AgregarAsync(Usuario usuario);
    Task GuardarCambiosAsync();
}