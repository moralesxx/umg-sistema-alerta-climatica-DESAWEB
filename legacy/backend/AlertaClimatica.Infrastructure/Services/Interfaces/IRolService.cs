using AlertaClimatica.Application.DTOs;

namespace AlertaClimatica.Infrastructure.Services;

public interface IRolService
{
    // Catálogo de roles existentes (RF-ADM-06). Lo consume la pantalla de
    // administración de usuarios para poblar el selector de rol.
    Task<IEnumerable<RolDto>> GetRolesAsync();
}
