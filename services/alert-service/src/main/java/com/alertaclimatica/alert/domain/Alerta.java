package com.alertaclimatica.alert.domain;

import jakarta.persistence.*;
import java.math.BigDecimal;
import java.time.Instant;

@Entity
@Table(name = "alertas")
public class Alerta {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    public Integer alertaId;

    public int sensorId;
    public String sensorCodigo;
    public String sensorNombre;
    public Integer comunidadId;
    public String comunidadNombre;
    public Integer tipoSensorId;
    public String unidad;
    public Integer reglaAlertaId;
    public String fenomeno;

    @Column(nullable = false)
    public String nivelRiesgo;

    @Column(nullable = false, length = 500)
    public String mensaje;

    @Column(nullable = false)
    public Instant fechaEmision;

    @Column(precision = 18, scale = 2)
    public BigDecimal valorDetectado;

    @Column(nullable = false, length = 20)
    public String estado = "Activa";

    public Integer eventoId;
    public Integer usuarioResponsableId;
    public String usuarioResponsableNombre;
    public Instant fechaGestion;
}