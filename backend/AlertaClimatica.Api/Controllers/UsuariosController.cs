using AlertaClimatica.Application.DTOs;
using AlertaClimatica.Domain;
using AlertaClimatica.Infrastructure;
using AlertaClimatica.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AlertaClimatica.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsuariosController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly PasswordHasher<Usuario> _passwordHasher;
    private readonly IConfiguration _configuration;
    private readonly BitacoraService _bitacoraService;

    public UsuariosController(
        ApplicationDbContext context,
        IConfiguration configuration,
        BitacoraService bitacoraService)
    {
        _context = context;
        _passwordHasher = new PasswordHasher<Usuario>();
        _configuration = configuration;
        _bitacoraService = bitacoraService;
    }

    // =========================================================
    // GET: api/usuarios
    // Obtener todos los usuarios
    // REQUIERE AUTENTICACIÓN
    // =========================================================

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IEnumerable<object>>> GetUsuarios()
    {
        var usuarios = await _context.Usuarios
            .Select(u => new
            {
                u.UsuarioId,
                u.Nombre,
                u.Correo,
                u.RolId,
                u.Estado,
                u.FechaCreacion
            })
            .ToListAsync();

        await _bitacoraService.RegistrarAsync(
            "CONSULTAR_USUARIOS",
            "El usuario consultó el listado de usuarios."
        );

        return Ok(usuarios);
    }

    // =========================================================
    // GET: api/usuarios/{id}
    // Obtener un usuario específico
    // REQUIERE AUTENTICACIÓN
    // =========================================================

    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<object>> GetUsuario(int id)
    {
        var usuario = await _context.Usuarios
            .Where(u => u.UsuarioId == id)
            .Select(u => new
            {
                u.UsuarioId,
                u.Nombre,
                u.Correo,
                u.RolId,
                u.Estado,
                u.FechaCreacion
            })
            .FirstOrDefaultAsync();

        if (usuario == null)
        {
            return NotFound(new
            {
                mensaje = "Usuario no encontrado."
            });
        }

        await _bitacoraService.RegistrarAsync(
            "CONSULTAR_USUARIO",
            $"El usuario consultó la información del usuario con ID {id}."
        );

        return Ok(usuario);
    }

    // =========================================================
    // POST: api/usuarios
    // Registrar nuevo usuario
    //
    // ESTE ENDPOINT ES PÚBLICO porque se utiliza para
    // crear una cuenta desde el formulario de registro.
    //
    // No se registra en Bitácora porque todavía no existe
    // un usuario autenticado realizando esta acción.
    // =========================================================

    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<object>> CrearUsuario(
        [FromBody] RegistroUsuarioDto registro)
    {
        // -----------------------------------------------------
        // Validaciones básicas
        // -----------------------------------------------------

        if (string.IsNullOrWhiteSpace(registro.Nombre))
        {
            return BadRequest(new
            {
                mensaje = "El nombre es obligatorio."
            });
        }

        if (string.IsNullOrWhiteSpace(registro.Correo))
        {
            return BadRequest(new
            {
                mensaje = "El correo es obligatorio."
            });
        }

        if (string.IsNullOrWhiteSpace(registro.Contrasenia))
        {
            return BadRequest(new
            {
                mensaje = "La contraseña es obligatoria."
            });
        }

        if (registro.Contrasenia.Length < 6)
        {
            return BadRequest(new
            {
                mensaje = "La contraseña debe tener al menos 6 caracteres."
            });
        }

        if (registro.RolId <= 0)
        {
            return BadRequest(new
            {
                mensaje = "Debe seleccionar un rol válido."
            });
        }

        // -----------------------------------------------------
        // Verificar que el rol exista
        // -----------------------------------------------------

        var rolExiste = await _context.Roles
            .AnyAsync(r => r.RolId == registro.RolId);

        if (!rolExiste)
        {
            return BadRequest(new
            {
                mensaje = "El rol seleccionado no existe."
            });
        }

        // -----------------------------------------------------
        // Verificar correo duplicado
        // -----------------------------------------------------

        var correoExiste = await _context.Usuarios
            .AnyAsync(u =>
                u.Correo.ToLower() == registro.Correo.ToLower());

        if (correoExiste)
        {
            return Conflict(new
            {
                mensaje = "Ya existe un usuario registrado con ese correo."
            });
        }

        // -----------------------------------------------------
        // Crear usuario
        // -----------------------------------------------------

        var usuario = new Usuario
        {
            Nombre = registro.Nombre.Trim(),
            Correo = registro.Correo.Trim(),
            RolId = registro.RolId,
            Estado = true,
            FechaCreacion = DateTime.Now
        };

        // -----------------------------------------------------
        // Hashear contraseña
        // -----------------------------------------------------

        usuario.ContraseniaHash =
            _passwordHasher.HashPassword(
                usuario,
                registro.Contrasenia
            );

        // -----------------------------------------------------
        // Guardar usuario
        // -----------------------------------------------------

        _context.Usuarios.Add(usuario);

        await _context.SaveChangesAsync();

        // -----------------------------------------------------
        // Respuesta
        // Nunca devolver contraseña/hash
        // -----------------------------------------------------

        var respuesta = new
        {
            usuario.UsuarioId,
            usuario.Nombre,
            usuario.Correo,
            usuario.RolId,
            usuario.Estado,
            usuario.FechaCreacion
        };

        return CreatedAtAction(
            nameof(GetUsuario),
            new { id = usuario.UsuarioId },
            respuesta
        );
    }

    // =========================================================
    // POST: api/usuarios/login
    // Iniciar sesión
    //
    // ESTE ENDPOINT ES PÚBLICO porque todavía no existe
    // autenticación al momento de iniciar sesión.
    // =========================================================

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(
        [FromBody] LoginDto login)
    {
        // -----------------------------------------------------
        // Validar datos
        // -----------------------------------------------------

        if (string.IsNullOrWhiteSpace(login.Correo))
        {
            return BadRequest(new
            {
                mensaje = "El correo es obligatorio."
            });
        }

        if (string.IsNullOrWhiteSpace(login.Contrasenia))
        {
            return BadRequest(new
            {
                mensaje = "La contraseña es obligatoria."
            });
        }

        // -----------------------------------------------------
        // Buscar usuario
        // -----------------------------------------------------

        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(
                u => u.Correo.ToLower() == login.Correo.ToLower()
            );

        // -----------------------------------------------------
        // Usuario no encontrado
        // -----------------------------------------------------

        if (usuario == null)
        {
            return Unauthorized(new
            {
                mensaje = "Correo o contraseña incorrectos."
            });
        }

        // -----------------------------------------------------
        // Verificar estado
        // -----------------------------------------------------

        if (!usuario.Estado)
        {
            return Unauthorized(new
            {
                mensaje = "El usuario se encuentra desactivado."
            });
        }

        // -----------------------------------------------------
        // Verificar contraseña
        // -----------------------------------------------------

        var resultado =
            _passwordHasher.VerifyHashedPassword(
                usuario,
                usuario.ContraseniaHash,
                login.Contrasenia
            );

        if (resultado == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new
            {
                mensaje = "Correo o contraseña incorrectos."
            });
        }

        // -----------------------------------------------------
        // Buscar rol
        // -----------------------------------------------------

        var rol = await _context.Roles
            .FirstOrDefaultAsync(
                r => r.RolId == usuario.RolId
            );

        if (rol == null)
        {
            return Unauthorized(new
            {
                mensaje = "El usuario no tiene un rol válido."
            });
        }

        // -----------------------------------------------------
        // Generar JWT
        // -----------------------------------------------------

        var token = GenerarToken(
            usuario,
            rol.NombreRol
        );

        // -----------------------------------------------------
        // Login exitoso
        // -----------------------------------------------------

        return Ok(new
        {
            mensaje = "Inicio de sesión exitoso.",

            token = token,

            usuario = new
            {
                usuario.UsuarioId,
                usuario.Nombre,
                usuario.Correo,
                usuario.RolId,
                usuario.Estado,
                rol = rol.NombreRol
            }
        });
    }

    // =========================================================
    // GET: api/usuarios/probar-bitacora
    //
    // Endpoint temporal para comprobar que:
    // 1. El JWT funciona.
    // 2. El usuario está autenticado.
    // 3. Se puede obtener el UsuarioId del JWT.
    // 4. Se registra la acción en Bitácora.
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

    // =========================================================
    // GENERAR JWT
    // =========================================================

    private string GenerarToken(
        Usuario usuario,
        string nombreRol)
    {
        var jwtSettings =
            _configuration.GetSection("Jwt");

        var key = jwtSettings["Key"];
        var issuer = jwtSettings["Issuer"];
        var audience = jwtSettings["Audience"];

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException(
                "La configuración Jwt:Key no está definida."
            );
        }

        if (string.IsNullOrWhiteSpace(issuer))
        {
            throw new InvalidOperationException(
                "La configuración Jwt:Issuer no está definida."
            );
        }

        if (string.IsNullOrWhiteSpace(audience))
        {
            throw new InvalidOperationException(
                "La configuración Jwt:Audience no está definida."
            );
        }

        var expirationMinutes = 60;

        if (int.TryParse(
            jwtSettings["ExpirationMinutes"],
            out var configuredExpiration))
        {
            expirationMinutes = configuredExpiration;
        }

        // -----------------------------------------------------
        // Claims
        // -----------------------------------------------------

        var claims = new List<Claim>
        {
            new Claim(
                JwtRegisteredClaimNames.Sub,
                usuario.UsuarioId.ToString()
            ),

            new Claim(
                JwtRegisteredClaimNames.Email,
                usuario.Correo
            ),

            new Claim(
                ClaimTypes.Name,
                usuario.Nombre
            ),

            new Claim(
                ClaimTypes.Role,
                nombreRol
            ),

            new Claim(
                "rolId",
                usuario.RolId.ToString()
            )
        };

        // -----------------------------------------------------
        // Clave de seguridad
        // -----------------------------------------------------

        var securityKey =
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(key)
            );

        var credentials =
            new SigningCredentials(
                securityKey,
                SecurityAlgorithms.HmacSha256
            );

        // -----------------------------------------------------
        // Crear token
        // -----------------------------------------------------

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(
                expirationMinutes
            ),
            signingCredentials: credentials
        );

        // -----------------------------------------------------
        // Convertir JWT a string
        // -----------------------------------------------------

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }
}