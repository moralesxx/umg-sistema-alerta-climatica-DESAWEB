using System.ComponentModel.DataAnnotations;

namespace AlertaClimatica.Domain;

public class Evento
{
    [Key]
    public int IdEvento { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string NivelRiesgo { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; } = DateTime.Now;
    public DateTime? FechaFin { get; set; }
    public string Estado { get; set; } = "Activo";
}