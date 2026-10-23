using AlertaClimatica.Application.DTOs;
using AlertaClimatica.Domain;
using AlertaClimatica.Infrastructure.Exceptions;
using AlertaClimatica.Infrastructure.Repositories.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace AlertaClimatica.Infrastructure.Services;

public class UsuarioService : IUsuarioService
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IRolRepository _rolRepository;
    private readonly ITokenService _tokenService;
    private readonly IBitacoraService _bitacoraService;
    private readonly PasswordHasher<Usuario> _passwordHasher;

    public UsuarioService(
        IUsuarioRepository usuarioRepository,
        IRolRepository rolRepository,
        ITokenService tokenService,
        IBitacoraService bitacoraService)
    {
        _usuarioRepository = usuarioRepository;
        _rolRepository = rolRepository;
        _tokenService = tokenService;
        _bitacoraService = bitacoraService;
        _passwordHasher = new PasswordHasher<Usuario>();
    }

    private static UsuarioResumenDto AResumen(Usuario u) => new()
    {
        UsuarioId = u.UsuarioId,
        Nombre = u.Nombre,
        Correo = u.Correo,
        RolId = u.RolId,
        Estado = u.Estado,
        FechaCreacion = u.FechaCreacion,
        UltimoAcceso = u.UltimoAcceso
    };

    public async Task<IEnumerable<UsuarioResumenDto>> GetUsuariosAsync()
    {
        var usuarios = await _usuarioRepository.ObtenerTodosAsync();

        // Se elimina el registro automático de lectura para evitar falsos positivos
        /*
        await _bitacoraService.RegistrarAsync(
            "CONSULTAR_USUARIOS",
            "El usuario consultó el listado de usuarios."
        );
        */

        return usuarios.Select(AResumen);
    }

    public async Task<IEnumerable<UsuarioResumenDto>> GetUsuariosFiltradosAsync(
        string? texto, int? rolId, bool? estado)
    {
        var usuarios = await _usuarioRepository.ObtenerFiltradosAsync(texto, rolId, estado);

        // Se elimina el registro automático de lectura para evitar falsos positivos
        /*
        await _bitacoraService.RegistrarAsync(
            "CONSULTAR_USUARIOS",
            "El usuario consultó el listado de usuarios."
        );
        */

        return usuarios.Select(AResumen);
    }

    public async Task<UsuarioResumenDto?> GetUsuarioAsync(int id)
    {
        var usuario = await _usuarioRepository.ObtenerPorIdAsync(id);
        if (usuario == null) return null;

        // Se elimina el registro automático de lectura individual
        /*
        await _bitacoraService.RegistrarAsync(
            "CONSULTAR_USUARIO",
            $"El usuario consultó la información del usuario con ID {id}."
        );
        */

        return AResumen(usuario);
    }

    public async Task<UsuarioResumenDto> RegistrarUsuarioPublicoAsync(RegistroUsuarioDto registro)
    {
        if (string.IsNullOrWhiteSpace(registro.Nombre))
            throw new ArgumentException("El nombre es obligatorio.");

        if (string.IsNullOrWhiteSpace(registro.Correo))
            throw new ArgumentException("El correo es obligatorio.");

        if (string.IsNullOrWhiteSpace(registro.Contrasenia) || registro.Contrasenia.Length < 6)
            throw new ArgumentException("La contraseña debe tener al menos 6 caracteres.");

        var rolPorDefecto = await _rolRepository.ObtenerPorNombreAsync("Usuario de consulta");
        if (rolPorDefecto == null)
        {
            throw new InvalidOperationException(
                "No existe el rol 'Usuario de consulta' en el sistema. Contacte al administrador."
            );
        }

        if (await _usuarioRepository.ExisteCorreoAsync(registro.Correo))
            throw new ConflictException("Ya existe un usuario registrado con ese correo.");

        var usuario = new Usuario
        {
            Nombre = registro.Nombre.Trim(),
            Correo = registro.Correo.Trim(),
            RolId = rolPorDefecto.RolId,
            Estado = true,
            FechaCreacion = DateTime.Now
        };

        usuario.ContraseniaHash = _passwordHasher.HashPassword(usuario, registro.Contrasenia);

        await _usuarioRepository.AgregarAsync(usuario);
        await _usuarioRepository.GuardarCambiosAsync();

        return AResumen(usuario);
    }

    public async Task<UsuarioResumenDto> CrearUsuarioComoAdminAsync(CrearUsuarioAdminDto registro)
    {
        if (string.IsNullOrWhiteSpace(registro.Nombre))
            throw new ArgumentException("El nombre es obligatorio.");

        if (string.IsNullOrWhiteSpace(registro.Correo))
            throw new ArgumentException("El correo es obligatorio.");

        if (string.IsNullOrWhiteSpace(registro.Contrasenia) || registro.Contrasenia.Length < 6)
            throw new ArgumentException("La contraseña debe tener al menos 6 caracteres.");

        if (!await _rolRepository.ExisteAsync(registro.RolId))
            throw new ArgumentException("El rol seleccionado no existe.");

        if (await _usuarioRepository.ExisteCorreoAsync(registro.Correo))
            throw new ConflictException("Ya existe un usuario registrado con ese correo.");

        var usuario = new Usuario
        {
            Nombre = registro.Nombre.Trim(),
            Correo = registro.Correo.Trim(),
            RolId = registro.RolId,
            Estado = true,
            FechaCreacion = DateTime.Now
        };

        usuario.ContraseniaHash = _passwordHasher.HashPassword(usuario, registro.Contrasenia);

        await _usuarioRepository.AgregarAsync(usuario);
        await _usuarioRepository.GuardarCambiosAsync();

        await _bitacoraService.RegistrarAsync(
            "CREAR_USUARIO",
            $"El administrador creó el usuario \"{usuario.Nombre}\" ({usuario.Correo}) con rol Id {usuario.RolId}."
        );

        return AResumen(usuario);
    }

    public async Task<UsuarioResumenDto?> ActualizarUsuarioAsync(int id, ActualizarUsuarioDto datos)
    {
        if (string.IsNullOrWhiteSpace(datos.Nombre))
            throw new ArgumentException("El nombre es obligatorio.");

        if (string.IsNullOrWhiteSpace(datos.Correo))
            throw new ArgumentException("El correo es obligatorio.");

        if (!await _rolRepository.ExisteAsync(datos.RolId))
            throw new ArgumentException("El rol seleccionado no existe.");

        var usuario = await _usuarioRepository.ObtenerParaActualizarAsync(id);
        if (usuario == null) return null;

        if (await _usuarioRepository.ExisteCorreoEnOtroUsuarioAsync(datos.Correo, id))
            throw new ConflictException("Ya existe otro usuario registrado con ese correo.");

        usuario.Nombre = datos.Nombre.Trim();
        usuario.Correo = datos.Correo.Trim();
        usuario.RolId = datos.RolId;

        await _usuarioRepository.GuardarCambiosAsync();

        await _bitacoraService.RegistrarAsync(
            "EDITAR_USUARIO",
            $"El administrador modificó el usuario #{id}. Nuevo nombre: \"{usuario.Nombre}\", correo: \"{usuario.Correo}\", rol Id {usuario.RolId}."
        );

        return AResumen(usuario);
    }

    public async Task<UsuarioResumenDto?> CambiarEstadoUsuarioAsync(int id, bool activo, int usuarioQueEjecutaId)
    {
        if (id == usuarioQueEjecutaId && !activo)
        {
            throw new ArgumentException("No puedes desactivar tu propia cuenta.");
        }

        var usuario = await _usuarioRepository.ObtenerParaActualizarAsync(id);
        if (usuario == null) return null;

        usuario.Estado = activo;
        await _usuarioRepository.GuardarCambiosAsync();

        await _bitacoraService.RegistrarAsync(
            "CAMBIAR_ESTADO_USUARIO",
            $"El administrador cambió el estado del usuario #{id} a {(activo ? "Activo" : "Inactivo")}."
        );

        return AResumen(usuario);
    }

    public async Task<LoginResultDto> LoginAsync(LoginDto login)
    {
        if (string.IsNullOrWhiteSpace(login.Correo))
            throw new ArgumentException("El correo es obligatorio.");

        if (string.IsNullOrWhiteSpace(login.Contrasenia))
            throw new ArgumentException("La contraseña es obligatoria.");

        var usuario = await _usuarioRepository.ObtenerPorCorreoAsync(login.Correo);
        if (usuario == null)
            throw new UnauthorizedAccessException("Correo o contraseña incorrectos.");

        if (!usuario.Estado)
            throw new UnauthorizedAccessException("El usuario se encuentra desactivado.");

        var resultado = _passwordHasher.VerifyHashedPassword(usuario, usuario.ContraseniaHash, login.Contrasenia);
        if (resultado == PasswordVerificationResult.Failed)
            throw new UnauthorizedAccessException("Correo o contraseña incorrectos.");

        var rol = await _rolRepository.ObtenerPorIdAsync(usuario.RolId);
        if (rol == null)
            throw new UnauthorizedAccessException("El usuario no tiene un rol válido.");

        var token = _tokenService.GenerarToken(usuario, rol.NombreRol);

        usuario.UltimoAcceso = DateTime.Now;
        await _usuarioRepository.GuardarCambiosAsync();

        await _bitacoraService.RegistrarConUsuarioIdAsync(
            usuario.UsuarioId,
            "LOGIN",
            $"El usuario {usuario.Correo} inició sesión."
        );

        return new LoginResultDto
        {
            Token = token,
            Usuario = AResumen(usuario),
            Rol = rol.NombreRol
        };
    }
}