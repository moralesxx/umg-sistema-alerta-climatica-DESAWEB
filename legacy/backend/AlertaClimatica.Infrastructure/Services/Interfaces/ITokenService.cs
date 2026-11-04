using AlertaClimatica.Domain;

namespace AlertaClimatica.Infrastructure.Services;

public interface ITokenService
{
    string GenerarToken(Usuario usuario, string nombreRol);
}