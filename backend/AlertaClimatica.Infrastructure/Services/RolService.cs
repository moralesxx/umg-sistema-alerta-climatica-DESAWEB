using AlertaClimatica.Application.DTOs;
using AlertaClimatica.Infrastructure.Repositories.Interfaces;

namespace AlertaClimatica.Infrastructure.Services;

// Servicio del catálogo de roles. Solo lectura: los roles los define el PDF
// (RF-ADM-06) y los siembra DbInitializer, por eso no hay crear/editar aquí.
// No registra bitácora porque consultar un catálogo no es una operación
// importante según RF-ADM-56.
public class RolService : IRolService
{
    private readonly IRolRepository _rolRepository;

    public RolService(IRolRepository rolRepository)
    {
        _rolRepository = rolRepository;
    }

    public async Task<IEnumerable<RolDto>> GetRolesAsync()
    {
        var roles = await _rolRepository.ObtenerTodosAsync();

        return roles
            .Select(r => new RolDto
            {
                RolId = r.RolId,
                NombreRol = r.NombreRol,
                Descripcion = r.Descripcion
            })
            .ToList();
    }
}
