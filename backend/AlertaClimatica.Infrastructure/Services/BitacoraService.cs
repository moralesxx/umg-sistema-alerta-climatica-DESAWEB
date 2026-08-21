using AlertaClimatica.Domain;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;

namespace AlertaClimatica.Infrastructure.Services;

public class BitacoraService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public BitacoraService(
        ApplicationDbContext context,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task RegistrarAsync(
        string accion,
        string detalle)
    {
        // =====================================================
        // 1. Obtener contexto HTTP
        // =====================================================

        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext == null)
        {
            throw new InvalidOperationException(
                "No existe un contexto HTTP válido."
            );
        }

        // =====================================================
        // 2. Verificar autenticación
        // =====================================================

        if (httpContext.User?.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedAccessException(
                "Solo un usuario autenticado puede generar registros en la bitácora."
            );
        }

        // =====================================================
        // 3. Obtener UsuarioId desde el JWT
        // =====================================================

        var usuarioIdClaim =
            httpContext.User.FindFirst(
                ClaimTypes.NameIdentifier
            )?.Value;

        // Si no existe NameIdentifier,
        // intentamos obtener "sub"
        if (string.IsNullOrWhiteSpace(usuarioIdClaim))
        {
            usuarioIdClaim =
                httpContext.User.FindFirst(
                    JwtRegisteredClaimNames.Sub
                )?.Value;
        }

        // También dejamos explícitamente "sub"
        // como alternativa directa.
        if (string.IsNullOrWhiteSpace(usuarioIdClaim))
        {
            usuarioIdClaim =
                httpContext.User.FindFirst("sub")?.Value;
        }

        // =====================================================
        // 4. Validar UsuarioId
        // =====================================================

        if (!int.TryParse(
            usuarioIdClaim,
            out var usuarioId))
        {
            throw new UnauthorizedAccessException(
                "No se pudo identificar al usuario autenticado."
            );
        }

        // =====================================================
        // 5. Verificar que el usuario exista
        // =====================================================

        var usuarioExiste = await _context.Usuarios
            .AnyAsync(u => u.UsuarioId == usuarioId);

        if (!usuarioExiste)
        {
            throw new UnauthorizedAccessException(
                "El usuario del token no existe en la base de datos."
            );
        }

        // =====================================================
        // 6. Validar acción
        // =====================================================

        if (string.IsNullOrWhiteSpace(accion))
        {
            throw new ArgumentException(
                "La acción de bitácora es obligatoria."
            );
        }

        // =====================================================
        // 7. Crear registro
        // =====================================================

        var bitacora = new Bitacora
        {
            UsuarioId = usuarioId,
            Accion = accion.Trim(),
            Detalle = detalle?.Trim() ?? string.Empty,
            FechaHora = DateTime.Now
        };

        // =====================================================
        // 8. Guardar
        // =====================================================

        _context.Bitacora.Add(bitacora);

        await _context.SaveChangesAsync();
    }
}