using AlertaClimatica.Application.DTOs;
using AlertaClimatica.Infrastructure.Exceptions;
using AlertaClimatica.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AlertaClimatica.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsuariosController : ControllerBase
{
    private readonly IUsuarioService _usuarioService;
    private readonly IBitacoraService _bitacoraService;

    public UsuariosController(
        IUsuarioService usuarioService,
        IBitacoraService bitacoraService)
    {
        _usuarioService = usuarioService;
        _bitacoraService = bitacoraService;
    }

    // =========================================================
    // GET: api/usuarios
    // RESTRINGIDO A ADMINISTRADOR
    // =========================================================

    // GET: api/usuarios?texto=&rolId=&estado=
    // Todos los filtros son opcionales (RF-ADM-54); sin parámetros
    // devuelve el listado completo, igual que antes.
    [HttpGet]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult<IEnumerable<UsuarioResumenDto>>> GetUsuarios(
        [FromQuery] string? texto,
        [FromQuery] int? rolId,
        [FromQuery] bool? estado)
    {
        var usuarios = await _usuarioService.GetUsuariosFiltradosAsync(texto, rolId, estado);
        return Ok(usuarios);
    }

    // =========================================================
    // GET: api/usuarios/{id}
    // RESTRINGIDO A ADMINISTRADOR
    // =========================================================

    [HttpGet("{id}")]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult<UsuarioResumenDto>> GetUsuario(int id)
    {
        var usuario = await _usuarioService.GetUsuarioAsync(id);

        if (usuario == null)
        {
            return NotFound(new { mensaje = "Usuario no encontrado." });
        }

        return Ok(usuario);
    }

    // =========================================================
    // POST: api/usuarios
    // Registro público. Siempre asigna "Usuario de consulta".
    // =========================================================

    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<UsuarioResumenDto>> CrearUsuario(
        [FromBody] RegistroUsuarioDto registro)
    {
        try
        {
            var usuario = await _usuarioService.RegistrarUsuarioPublicoAsync(registro);
            return CreatedAtAction(nameof(GetUsuario), new { id = usuario.UsuarioId }, usuario);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (ConflictException ex)
        {
            return Conflict(new { mensaje = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(500, new { mensaje = ex.Message });
        }
    }

    // =========================================================
    // POST: api/usuarios/login
    // =========================================================

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDto login)
    {
        try
        {
            var resultado = await _usuarioService.LoginAsync(login);

            return Ok(new
            {
                mensaje = "Inicio de sesión exitoso.",
                token = resultado.Token,
                usuario = new
                {
                    resultado.Usuario.UsuarioId,
                    resultado.Usuario.Nombre,
                    resultado.Usuario.Correo,
                    resultado.Usuario.RolId,
                    resultado.Usuario.Estado,
                    rol = resultado.Rol
                }
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { mensaje = ex.Message });
        }
    }

    // =========================================================
    // POST: api/usuarios/logout
    // RF-ADM-03 y RF-ADM-56
    // =========================================================

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _bitacoraService.RegistrarAsync(
            "LOGOUT",
            "El usuario cerró sesión."
        );

        return Ok(new { mensaje = "Sesión cerrada correctamente." });
    }

    // =========================================================
    // POST: api/usuarios/administracion
    // RF-ADM-49: solo Administrador, sí puede elegir el rol.
    // =========================================================

    [HttpPost("administracion")]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult<UsuarioResumenDto>> CrearUsuarioComoAdmin(
        [FromBody] CrearUsuarioAdminDto registro)
    {
        try
        {
            var usuario = await _usuarioService.CrearUsuarioComoAdminAsync(registro);
            return CreatedAtAction(nameof(GetUsuario), new { id = usuario.UsuarioId }, usuario);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (ConflictException ex)
        {
            return Conflict(new { mensaje = ex.Message });
        }
    }

    // =========================================================
    // PUT: api/usuarios/{id}
    // RF-ADM-50 y RF-ADM-52: editar nombre, correo y rol.
    // =========================================================

    [HttpPut("{id}")]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult<UsuarioResumenDto>> ActualizarUsuario(
        int id, [FromBody] ActualizarUsuarioDto datos)
    {
        try
        {
            var usuario = await _usuarioService.ActualizarUsuarioAsync(id, datos);

            if (usuario == null)
            {
                return NotFound(new { mensaje = "Usuario no encontrado." });
            }

            return Ok(usuario);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (ConflictException ex)
        {
            return Conflict(new { mensaje = ex.Message });
        }
    }

    // =========================================================
    // PATCH: api/usuarios/{id}/estado
    // RF-ADM-51: activar/desactivar. No permite que un Administrador
    // se desactive a sí mismo.
    // =========================================================

    [HttpPatch("{id}/estado")]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult<UsuarioResumenDto>> CambiarEstadoUsuario(
        int id, [FromBody] bool activo)
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (!int.TryParse(idClaim, out var usuarioQueEjecutaId))
        {
            return Unauthorized(new { mensaje = "No se pudo identificar al usuario autenticado." });
        }

        try
        {
            var usuario = await _usuarioService.CambiarEstadoUsuarioAsync(id, activo, usuarioQueEjecutaId);

            if (usuario == null)
            {
                return NotFound(new { mensaje = "Usuario no encontrado." });
            }

            return Ok(usuario);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }

    // =========================================================
    // GET: api/usuarios/probar-bitacora
    // =========================================================

    [HttpGet("probar-bitacora")]
    [Authorize]
    public async Task<IActionResult> ProbarBitacora()
    {
        await _bitacoraService.RegistrarAsync(
            "PRUEBA_BITACORA",
            "El usuario autenticado ejecutó la prueba de bitácora."
        );

        return Ok(new
        {
            mensaje = "La acción fue registrada correctamente en la bitácora."
        });
    }
}