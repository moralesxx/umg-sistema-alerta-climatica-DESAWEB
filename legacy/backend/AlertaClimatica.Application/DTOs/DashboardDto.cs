namespace AlertaClimatica.Application.DTOs;

// Datos del dashboard (RF-ADM-60 a 68), opcionalmente filtrados por comunidad.
public class DashboardResumenDto
{
    public int TotalComunidades { get; set; }
    public int ComunidadesActivas { get; set; }
    public int SensoresActivos { get; set; }
    public int SensoresInactivos { get; set; }
    public int AlertasActivas { get; set; }
    public string NivelRiesgoGlobal { get; set; } = "Verde";
    public List<ConteoNivelDto> AlertasPorNivel { get; set; } = new();
    public List<AlertaActivaResumenDto> AlertasActivasRecientes { get; set; } = new();
    public List<ValorActualDto> ValoresActuales { get; set; } = new();
    public List<SerieLecturasDto> Evolucion { get; set; } = new();
    public List<EventoResumenDto> EventosRecientes { get; set; } = new();
    public DateTime GeneradoEn { get; set; }
}

public class ConteoNivelDto
{
    public string Nivel { get; set; } = string.Empty;
    public int Total { get; set; }
}

public class AlertaActivaResumenDto
{
    public int AlertaId { get; set; }
    public DateTime FechaEmision { get; set; }
    public string NivelRiesgo { get; set; } = string.Empty;
    public string? Fenomeno { get; set; }
    public string? SensorNombre { get; set; }
    public string? ComunidadNombre { get; set; }
    public decimal? ValorDetectado { get; set; }
    public string? Unidad { get; set; }
    public string Mensaje { get; set; } = string.Empty;
}

// Último valor por tipo de sensor (promedio de la última lectura de cada sensor).
public class ValorActualDto
{
    public int TipoSensorId { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Unidad { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public int Sensores { get; set; }
    public DateTime Fecha { get; set; }
}

public class SerieLecturasDto
{
    public int TipoSensorId { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Unidad { get; set; } = string.Empty;
    public List<PuntoLecturaDto> Puntos { get; set; } = new();
}

public class PuntoLecturaDto
{
    public DateTime Fecha { get; set; }
    public decimal Valor { get; set; }
}

public class EventoResumenDto
{
    public int EventoId { get; set; }
    public DateTime FechaHora { get; set; }
    public string Fenomeno { get; set; } = string.Empty;
    public string? NivelRiesgo { get; set; }
    public string? ComunidadNombre { get; set; }
    public string? SensorNombre { get; set; }
    public decimal? Valor { get; set; }
    public string? Estado { get; set; }
}
