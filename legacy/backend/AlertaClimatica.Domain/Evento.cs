using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlertaClimatica.Domain;

[Table("Eventos")]
public class Evento
{
    [Key]
    [Column("EventoId")]
    public int EventoId { get; set; }

    [Column("TipoEvento")]
    public string TipoFenomeno { get; set; } = string.Empty;

    [Column("Descripcion")]
    public string Descripcion { get; set; } = string.Empty;

    [Column("FechaHora")]
    public DateTime FechaHora { get; set; }

    // =====================================================================
    // RF-ADM-45: datos adicionales del evento.
    // Todos son nullable porque ya existen eventos guardados sin esta
    // información (se crearon antes de la Fase 2); a esas filas se les
    // muestra "—" en lugar de inventarles un valor.
    // =====================================================================

    [Column("ComunidadId")]
    public int? ComunidadId { get; set; }

    [Column("SensorId")]
    public int? SensorId { get; set; }

    // Verde, Amarillo, Naranja o Rojo (RF-ADM-31).
    [Column("NivelRiesgo")]
    public string? NivelRiesgo { get; set; }

    // Valor detectado por el sensor. Queda vacío mientras no existan las
    // reglas de alerta que lo calculen (RF-ADM-32 a 34).
    [Column("Valor")]
    public decimal? Valor { get; set; }

    // Activa, Atendida o Cerrada (mismos estados que las alertas, RF-ADM-41).
    [Column("Estado")]
    public string? Estado { get; set; }

    // "Usuario responsable cuando corresponda": solo se llena cuando alguien
    // atiende o cierra el evento.
    [Column("UsuarioResponsableId")]
    public int? UsuarioResponsableId { get; set; }

    // Propiedades de navegación (permiten mostrar nombres en el historial).
    [ForeignKey("ComunidadId")]
    public Comunidad? Comunidad { get; set; }

    [ForeignKey("SensorId")]
    public Sensor? Sensor { get; set; }

    [ForeignKey("UsuarioResponsableId")]
    public Usuario? UsuarioResponsable { get; set; }
}
