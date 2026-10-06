namespace AlertaClimatica.Application.DTOs;

// Respuesta de la API para un rol (catálogo). Se usa en el <select> de rol
// de la pantalla de Usuarios (RF-ADM-49, 50 y 52). Es un DTO aparte de la
// entidad Rol (Domain) para no exponer directamente el modelo de la tabla.
public class RolDto
{
    public int RolId { get; set; }

    public string NombreRol { get; set; } = string.Empty;

    public string? Descripcion { get; set; }
}
