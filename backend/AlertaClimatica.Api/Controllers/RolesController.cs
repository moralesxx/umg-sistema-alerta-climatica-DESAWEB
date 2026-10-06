using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AlertaClimatica.Application.DTOs;
using AlertaClimatica.Infrastructure.Services;

namespace AlertaClimatica.Api.Controllers;

// Solo el Administrador lo consulta: el único uso es el selector de rol de
// la pantalla de Usuarios, que también es exclusiva del Administrador
// (RF-ADM-49 a 52). Así no se expone la lista de roles a quien no la necesita.
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrador")]
public class RolesController : ControllerBase
{
    private readonly IRolService _rolService;

    public RolesController(IRolService rolService)
    {
        _rolService = rolService;
    }

    // GET: api/roles
    [HttpGet]
    public async Task<ActionResult<IEnumerable<RolDto>>> GetRoles()
    {
        var roles = await _rolService.GetRolesAsync();
        return Ok(roles);
    }
}
