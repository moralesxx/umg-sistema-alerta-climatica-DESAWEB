package com.alertaclimatica.alert.domain;

import jakarta.persistence.*;
import java.math.BigDecimal;
import java.time.Instant;

@Entity
@Table(name = "eventos")
public class Evento {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    public Integer eventoId;

    @Column(nullable = false)
    public String tipoFenomeno;

    @Column(nullable = false, length = 500)
    public String descripcion;

    @Column(nullable = false)
    public Instant fechaHora;

    public Integer comunidadId;
    public String comunidadNombre;
    public Integer sensorId;
    public String sensorNombre;
    public String nivelRiesgo;

    @Column(precision = 18, scale = 2)
    public BigDecimal valor;

    public String estado;
    public Integer usuarioResponsableId;
    public String usuarioResponsableNombre;
}