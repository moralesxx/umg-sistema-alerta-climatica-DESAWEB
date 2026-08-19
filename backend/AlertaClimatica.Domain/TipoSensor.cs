using System.ComponentModel.DataAnnotations;

namespace AlertaClimatica.Domain;

public class TipoSensor
{
    [Key]
    public int IdTipoSensor { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string UnidadMedida { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
}