namespace AlertaClimatica.Application.DTOs;

// Respuesta de la API para una comunidad. A diferencia de la entidad
// Comunidad (Domain), incluye CantidadSensores (RF-ADM-13), que no es una
// columna de la tabla sino un dato calculado.
public class ComunidadDto
{
    public int ComunidadId { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string Municipio { get; set; } = string.Empty;

    public string Departamento { get; set; } = string.Empty;

    public string Pais { get; set; } = string.Empty;

    public decimal? Latitud { get; set; }

    public decimal? Longitud { get; set; }

    public string? Descripcion { get; set; }

    public bool Estado { get; set; }

    public int CantidadSensores { get; set; }
}
