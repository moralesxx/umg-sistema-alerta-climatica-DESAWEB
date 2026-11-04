using AlertaClimatica.Domain;

namespace AlertaClimatica.Infrastructure.Services;

public interface IBitacoraService
{
    // Registra una acción leyendo el UsuarioId del JWT del request actual
    // (lo que ya usa Sensores). Requiere que el usuario esté autenticado.
    // RF-ADM-57: entidad y entidadId son opcionales (Login/Logout no tienen).
    Task RegistrarAsync(string accion, string detalle, string? entidad = null, int? entidadId = null);

    // Registra una acción indicando el UsuarioId de forma explícita, sin
    // depender del HttpContext. Necesario para el Login, donde todavía
    // no existe un JWT en ese mismo request.
    Task RegistrarConUsuarioIdAsync(int usuarioId, string accion, string detalle,
                                    string? entidad = null, int? entidadId = null);

    Task<IEnumerable<Bitacora>> ObtenerUltimosAsync(int cantidad = 100);
}