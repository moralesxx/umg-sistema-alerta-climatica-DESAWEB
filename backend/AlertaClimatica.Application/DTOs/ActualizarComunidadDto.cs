namespace AlertaClimatica.Application.DTOs;

// RF-ADM-09: datos editables de una comunidad.
// NO incluye Estado a propósito: activar/desactivar (RF-ADM-10) tiene su
// propio endpoint (PATCH api/comunidades/{id}/estado), igual que en Sensores.
public class ActualizarComunidadDto
{
    public string Nombre { get; set; } = string.Empty;

    public string Municipio { get; set; } = string.Empty;

    public string Departamento { get; set; } = string.Empty;

    public string Pais { get; set; } = string.Empty;

    public decimal? Latitud { get; set; }

    public decimal? Longitud { get; set; }

    public string? Descripcion { get; set; }
}
