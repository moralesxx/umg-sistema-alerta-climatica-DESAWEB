package com.alertaclimatica.alert.service;

import java.math.BigDecimal;
import java.time.Instant;
import java.util.List;

public final class Dtos {
    private Dtos() {}

    public record Lectura(Integer sensorId, String sensorCodigo, String sensorNombre, Integer tipoSensorId, String tipoNombre, String unidad,
                          Integer comunidadId, BigDecimal valor, Instant fechaHora) {}
    public record EvaluarRequest(List<Lectura> lecturas) {}

    public record AlertaDto(Integer alertaId, Instant fechaEmision, Integer sensorId, String sensorCodigo, String sensorNombre, Integer comunidadId,
                            String comunidadNombre, Integer reglaAlertaId, String reglaNombre, String fenomeno, String nivelRiesgo,
                            BigDecimal valorDetectado, String unidad, String umbral, String mensaje, String estado, Integer eventoId,
                            Integer usuarioResponsableId, String usuarioResponsableNombre, Instant fechaGestion) {}

    public record CrearAlerta(Integer sensorId, Integer reglaAlertaId, String nivelRiesgo, String mensaje, Instant fechaEmision, BigDecimal valorDetectado) {}

    public record EventoDto(Integer eventoId, Instant fechaHora, Integer comunidadId, String comunidadNombre, Integer sensorId, String sensorNombre,
                            String tipoFenomeno, String nivelRiesgo, BigDecimal valor, String descripcion, String estado,
                            Integer usuarioResponsableId, String usuarioResponsableNombre) {}
    public record CrearEvento(String tipoFenomeno, String descripcion, String nivelRiesgo, Integer comunidadId, Integer sensorId, BigDecimal valor,
                              String estado, Instant fechaHora) {}
    public record Conteo(String nombre, long total) {}
    public record EventoStats(long total, List<Conteo> porFenomeno, List<Conteo> porNivel, List<Conteo> porEstado, List<Conteo> porComunidad) {}

    public record TipoSensorDto(Integer tipoSensorId, String nombreTipo, String unidadMedida) {}
    public record ReglaDto(Integer reglaAlertaId, String nombre, Integer tipoSensorId, BigDecimal valorMinimo, BigDecimal valorMaximo, String nivelPeligro,
                           String tipoFenomeno, String mensaje, boolean estado, TipoSensorDto tipoSensor) {}
    public record ReglaInput(String nombre, Integer tipoSensorId, BigDecimal valorMinimo, BigDecimal valorMaximo, String nivelPeligro,
                             String tipoFenomeno, String mensaje, Boolean estado) {}

    public record NivelCount(String nivel, long total) {}
    public record ResumenAlerta(Integer alertaId, Instant fechaEmision, String nivelRiesgo, String fenomeno, String sensorNombre, String comunidadNombre,
                                BigDecimal valorDetectado, String unidad, String mensaje) {}
    public record ResumenEvento(Integer eventoId, Instant fechaHora, String fenomeno, String nivelRiesgo, String comunidadNombre, String sensorNombre,
                                BigDecimal valor, String estado) {}
    public record Resumen(long alertasActivas, String nivelRiesgoGlobal, List<NivelCount> alertasPorNivel, List<Conteo> alertasPorFenomeno,
                          List<ResumenAlerta> alertasActivasRecientes, List<ResumenEvento> eventosRecientes) {}
}
