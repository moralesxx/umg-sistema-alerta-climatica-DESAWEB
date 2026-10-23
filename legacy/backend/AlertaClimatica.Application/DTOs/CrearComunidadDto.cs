namespace AlertaClimatica.Application.DTOs;

// RF-ADM-08: datos para registrar una comunidad nueva.
// Estado se incluye porque el PDF lo lista como dato mínimo; si el cliente
// no lo envía, la comunidad nace activa.
public class CrearComunidadDto
{
    public string Nombre { get; set; } = string.Empty;

    public string Municipio { get; set; } = string.Empty;

    public string Departamento { get; set; } = string.Empty;

    public string Pais { get; set; } = string.Empty;

    public decimal? Latitud { get; set; }

    public decimal? Longitud { get; set; }

    public string? Descripcion { get; set; }

    public bool Estado { get; set; } = true;
}
