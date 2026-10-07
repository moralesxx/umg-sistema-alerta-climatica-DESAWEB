using AlertaClimatica.Application.DTOs;
using AlertaClimatica.Infrastructure.Exceptions;
using AlertaClimatica.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AlertaClimatica.Api.Controllers;

// Configuración general del controlador de usuarios, estableciendo la ruta base y requiriendo que cualquier usuario esté autenticado
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsuariosController : ControllerBase
{
    // Servicio privado para manejar la lógica de negocio y operaciones de los usuarios
    private readonly IUsuarioService _usuarioService;
    // Servicio privado para registrar eventos en la bitácora del sistema
    private readonly IBitacoraService _bitacoraService;

    // Constructor que inyecta las dependencias del servicio de usuarios y el servicio de bitácora
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
    // Endpoint GET exclusivo para administradores que permite consultar la lista de usuarios aplicando filtros opcionales
    [HttpGet]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult<IEnumerable<UsuarioResumenDto>>> GetUsuarios(
        [FromQuery] string? texto,
        [FromQuery] int? rolId,
        [FromQuery] bool? estado)
    {
        // Llama al servicio para obtener los usuarios filtrados de manera asíncrona
        var usuarios = await _usuarioService.GetUsuariosFiltradosAsync(texto, rolId, estado);
        // Retorna la colección de usuarios con un código HTTP 200 OK
        return Ok(usuarios);
    }

    // =========================================================
    // GET: api/usuarios/{id}
    // RESTRINGIDO A ADMINISTRADOR
    // =========================================================

    // Endpoint GET exclusivo para administradores que busca y retorna la información detallada de un usuario por su ID
    [HttpGet("{id}")]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult<UsuarioResumenDto>> GetUsuario(int id)
    {
        // Consulta el usuario en el servicio utilizando su identificador
        var usuario = await _usuarioService.GetUsuarioAsync(id);

        // Si el usuario no se encuentra, retorna una respuesta HTTP 404 Not Found
        if (usuario == null)
        {
            return NotFound(new { mensaje = "Usuario no encontrado." });
        }

        // Retorna los datos del usuario encontrado con un código HTTP 200 OK
        return Ok(usuario);
    }

    // =========================================================
    // POST: api/usuarios
    // Registro público. Siempre asigna "Usuario de consulta".
    // =========================================================

    // Endpoint POST público (anónimo) para registrar nuevos usuarios con el rol predeterminado de consulta
    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<UsuarioResumenDto>> CrearUsuario(
        [FromBody] RegistroUsuarioDto registro)
    {
        try
        {
            // Ejecuta el servicio de registro público de usuario
            var usuario = await _usuarioService.RegistrarUsuarioPublicoAsync(registro);
            // Retorna una respuesta HTTP 201 Created apuntando al endpoint de consulta por ID y el usuario creado
            return CreatedAtAction(nameof(GetUsuario), new { id = usuario.UsuarioId }, usuario);
        }
        catch (ArgumentException ex)
        {
            // Captura errores de validación y retorna HTTP 400 Bad Request
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (ConflictException ex)
        {
            // Captura conflictos (como correo duplicado) y retorna HTTP 409 Conflict
            return Conflict(new { mensaje = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            // Captura errores internos durante la operación y retorna HTTP 500 Internal Server Error
            return StatusCode(500, new { mensaje = "No fue posible completar el registro del usuario." });
        }
    }

    // =========================================================
    // POST: api/usuarios/login
    // =========================================================

    // Endpoint POST público para autenticar credenciales de usuario y generar el token de acceso
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDto login)
    {
        try
        {
            // Ejecuta el servicio de inicio de sesión con los datos proporcionados
            var resultado = await _usuarioService.LoginAsync(login);

            // Retorna una respuesta exitosa con el token JWT y los datos esenciales del usuario
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
            // Captura argumentos inválidos y retorna HTTP 400 Bad Request
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            // Captura errores de credenciales incorrectas y retorna HTTP 401 Unauthorized
            return Unauthorized(new { mensaje = ex.Message });
        }
    }

    // =========================================================
    // POST: api/usuarios/logout
    // RF-ADM-03 y RF-ADM-56
    // =========================================================

    // Endpoint POST protegido para cerrar sesión y registrar el evento respectivo en la bitácora
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        // Registra el evento de cierre de sesión en la bitácora del sistema
        await _bitacoraService.RegistrarAsync(
            "LOGOUT",
            "El usuario cerró sesión."
        );

        // Retorna una respuesta HTTP 200 OK confirmando el cierre de sesión
        return Ok(new { mensaje = "Sesión cerrada correctamente." });
    }

    // =========================================================
    // POST: api/usuarios/administracion
    // RF-ADM-49: solo Administrador, sí puede elegir el rol.
    // =========================================================

    // Endpoint POST exclusivo para administradores que permite crear un usuario asignándole un rol específico de manera manual
    [HttpPost("administracion")]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult<UsuarioResumenDto>> CrearUsuarioComoAdmin(
        [FromBody] CrearUsuarioAdminDto registro)
    {
        try
        {
            // Llama al servicio para crear el usuario con rol asignado por un administrador
            var usuario = await _usuarioService.CrearUsuarioComoAdminAsync(registro);
            // Retorna HTTP 201 Created apuntando al detalle del usuario recién creado
            return CreatedAtAction(nameof(GetUsuario), new { id = usuario.UsuarioId }, usuario);
        }
        catch (ArgumentException ex)
        {
            // Captura errores de validación y retorna HTTP 400 Bad Request
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (ConflictException ex)
        {
            // Captura conflictos de datos y retorna HTTP 409 Conflict
            return Conflict(new { mensaje = ex.Message });
        }
    }

    // =========================================================
    // PUT: api/usuarios/{id}
    // RF-ADM-50 y RF-ADM-52: editar nombre, correo y rol.
    // =========================================================

    // Endpoint PUT exclusivo para administradores que permite actualizar la información principal (nombre, correo y rol) de un usuario
    [HttpPut("{id}")]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult<UsuarioResumenDto>> ActualizarUsuario(
        int id, [FromBody] ActualizarUsuarioDto datos)
    {
        try
        {
            // Ejecuta el servicio de actualización de datos del usuario
            var usuario = await _usuarioService.ActualizarUsuarioAsync(id, datos);

            // Si no se encuentra el usuario, retorna HTTP 404 Not Found
            if (usuario == null)
            {
                return NotFound(new { mensaje = "Usuario no encontrado." });
            }

            // Retorna el usuario actualizado con código HTTP 200 OK
            return Ok(usuario);
        }
        catch (ArgumentException ex)
        {
            // Captura errores de argumentos y retorna HTTP 400 Bad Request
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (ConflictException ex)
        {
            // Captura conflictos (como correo duplicado) y retorna HTTP 409 Conflict
            return Conflict(new { mensaje = ex.Message });
        }
    }

    // =========================================================
    // PATCH: api/usuarios/{id}/estado
    // RF-ADM-51: activar/desactivar. No permite que un Administrador
    // se desactive a sí mismo.
    // =========================================================

    // Endpoint PATCH exclusivo para administradores que permite cambiar el estado (activo/inactivo) de un usuario validando restricciones de seguridad
    [HttpPatch("{id}/estado")]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult<UsuarioResumenDto>> CambiarEstadoUsuario(
        int id, [FromBody] bool activo)
    {
        // Extrae el ID del usuario que está ejecutando la petición desde los claims del token JWT
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        // Valida que el ID extraído sea un número entero válido
        if (!int.TryParse(idClaim, out var usuarioQueEjecutaId))
        {
            return Unauthorized(new { mensaje = "No se pudo identificar al usuario autenticado." });
        }

        try
        {
            // Llama al servicio para cambiar el estado del usuario, pasando el ID del usuario afectado y el del administrador que ejecuta la acción
            var usuario = await _usuarioService.CambiarEstadoUsuarioAsync(id, activo, usuarioQueEjecutaId);

            // Si el usuario objetivo no existe, retorna HTTP 404 Not Found
            if (usuario == null)
            {
                return NotFound(new { mensaje = "Usuario no encontrado." });
            }

            // Retorna el usuario con su estado modificado y un código HTTP 200 OK
            return Ok(usuario);
        }
        catch (ArgumentException ex)
        {
            // Captura reglas de negocio violadas (ej. un admin intentando desactivarse a sí mismo) y retorna HTTP 400 Bad Request
            return BadRequest(new { mensaje = ex.Message });
        }
    }
}