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
    // Servicio privado para gestionar la lógica de negocio y consulta de los roles del sistema
    private readonly IRolService _rolService;

    // Constructor que inyecta la dependencia del servicio de roles
    public RolesController(IRolService rolService)
    {
        _rolService = rolService;
    }

    // GET: api/roles
    // Endpoint GET exclusivo para administradores que permite obtener la lista completa de roles disponibles en el sistema
    [HttpGet]
    public async Task<ActionResult<IEnumerable<RolDto>>> GetRoles()
    {
        // Llama al servicio de forma asíncrona para recuperar la colección de roles
        var roles = await _rolService.GetRolesAsync();
        // Retorna la lista de roles con un código de estado HTTP 200 OK
        return Ok(roles);
    }
}