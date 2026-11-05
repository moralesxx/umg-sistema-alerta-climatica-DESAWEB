package com.alertaclimatica.alert.domain;

import jakarta.persistence.*;
import java.math.BigDecimal;
import java.time.Instant;

/** Entidades del Alert Service. Sensores y comunidades se referencian por id (otros microservicios), con nombres desnormalizados. */
// Al ser clases anidadas, Hibernate nombraría las entidades "Entities$Alerta", etc.; por eso cada @Entity declara su nombre
// explícito (así las consultas JPQL "from Alerta" de Repos las encuentran).
public final class Entities {
    private Entities() {}

    @Entity(name = "ReglaAlerta") @Table(name = "reglas_alerta")
    public static class ReglaAlerta {
        @Id @GeneratedValue(strategy = GenerationType.IDENTITY) public Integer reglaAlertaId;
        @Column(nullable = false) public String nombre;
        public int tipoSensorId;
        @Column(precision = 18, scale = 2) public BigDecimal valorMinimo;
        @Column(precision = 18, scale = 2) public BigDecimal valorMaximo;
        @Column(nullable = false) public String nivelPeligro;
        @Column(nullable = false) public String tipoFenomeno;
        @Column(nullable = false, length = 500) public String mensaje;
        public boolean estado = true;
    }

    @Entity(name = "Evento") @Table(name = "eventos")
    public static class Evento {
        @Id @GeneratedValue(strategy = GenerationType.IDENTITY) public Integer eventoId;
        @Column(nullable = false) public String tipoFenomeno;
        @Column(nullable = false, length = 500) public String descripcion;
        @Column(nullable = false) public Instant fechaHora;
        public Integer comunidadId;
        public String comunidadNombre;
        public Integer sensorId;
        public String sensorNombre;
        public String nivelRiesgo;
        @Column(precision = 18, scale = 2) public BigDecimal valor;
        public String estado;
        public Integer usuarioResponsableId;
        public String usuarioResponsableNombre;
    }

    @Entity(name = "Alerta") @Table(name = "alertas")
    public static class Alerta {
        @Id @GeneratedValue(strategy = GenerationType.IDENTITY) public Integer alertaId;
        public int sensorId;
        public String sensorCodigo;
        public String sensorNombre;
        public Integer comunidadId;
        public String comunidadNombre;
        public Integer tipoSensorId;
        public String unidad;
        public Integer reglaAlertaId;
        public String fenomeno;
        @Column(nullable = false) public String nivelRiesgo;
        @Column(nullable = false, length = 500) public String mensaje;
        @Column(nullable = false) public Instant fechaEmision;
        @Column(precision = 18, scale = 2) public BigDecimal valorDetectado;
        @Column(nullable = false, length = 20) public String estado = "Activa";
        public Integer eventoId;
        public Integer usuarioResponsableId;
        public String usuarioResponsableNombre;
        public Instant fechaGestion;
    }
}