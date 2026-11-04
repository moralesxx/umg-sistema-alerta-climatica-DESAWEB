namespace AlertaClimatica.Domain;

// RF-ADM-08: campos mínimos de una comunidad.
// El CRUD completo (crear/editar/activar/filtrar) es el objetivo #5 de la Fase 2,
// todavía no implementado. Por ahora esta entidad solo existe para poder
// relacionarla con Sensor (RF-ADM-15, RF-ADM-19, RF-ADM-20).
public class Comunidad
{
    public int ComunidadId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Municipio { get; set; } = string.Empty;
    public string Departamento { get; set; } = string.Empty;
    public string Pais { get; set; } = string.Empty;
    public decimal? Latitud { get; set; }
    public decimal? Longitud { get; set; }
    public string? Descripcion { get; set; }
    public bool Estado { get; set; } = true;
}