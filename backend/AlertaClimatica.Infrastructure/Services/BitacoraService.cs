using AlertaClimatica.Domain;
using AlertaClimatica.Infrastructure.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace AlertaClimatica.Infrastructure.Services;

// Ya NO toca ApplicationDbContext directamente — todo el acceso a datos
// pasa por IBitacoraRepository, igual que hicimos con Sensores.
public class BitacoraService : IBitacoraService
{
    private readonly IBitacoraRepository _bitacoraRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public BitacoraService(
        IBitacoraRepository bitacoraRepository,
        IHttpContextAccessor httpContextAccessor)
    {
        _bitacoraRepository = bitacoraRepository;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task RegistrarAsync(
        string accion,
        string detalle)
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext == null)
        {
            throw new InvalidOperationException(
                "No existe un contexto HTTP válido."
            );
        }

        if (httpContext.User?.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedAccessException(
                "Solo un usuario autenticado puede generar registros en la bitácora."
            );
        }

        var usuarioIdClaim =
            httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(usuarioIdClaim))
        {
            usuarioIdClaim = httpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        }

        if (string.IsNullOrWhiteSpace(usuarioIdClaim))
        {
            usuarioIdClaim = httpContext.User.FindFirst("sub")?.Value;
        }

        if (!int.TryParse(usuarioIdClaim, out var usuarioId))
        {
            throw new UnauthorizedAccessException(
                "No se pudo identificar al usuario autenticado."
            );
        }

        await RegistrarConUsuarioIdAsync(usuarioId, accion, detalle);
    }

    public async Task RegistrarConUsuarioIdAsync(
        int usuarioId,
        string accion,
        string detalle)
    {
        if (string.IsNullOrWhiteSpace(accion))
        {
            throw new ArgumentException(
                "La acción de bitácora es obligatoria."
            );
        }

        var usuarioExiste = await _bitacoraRepository.ExisteUsuarioAsync(usuarioId);

        if (!usuarioExiste)
        {
            throw new UnauthorizedAccessException(
                "El usuario indicado no existe en la base de datos."
            );
        }

        var bitacora = new Bitacora
        {
            UsuarioId = usuarioId,
            Accion = accion.Trim(),
            Detalle = detalle?.Trim() ?? string.Empty,
            FechaHora = DateTime.Now
        };

        await _bitacoraRepository.AgregarAsync(bitacora);
        await _bitacoraRepository.GuardarCambiosAsync();
    }

    public async Task<IEnumerable<Bitacora>> ObtenerUltimosAsync(int cantidad = 100)
    {
        return await _bitacoraRepository.ObtenerUltimosAsync(cantidad);
    }
}