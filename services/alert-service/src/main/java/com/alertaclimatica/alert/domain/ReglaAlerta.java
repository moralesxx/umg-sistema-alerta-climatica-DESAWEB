package com.alertaclimatica.alert.domain;

import jakarta.persistence.*;
import java.math.BigDecimal;

@Entity
@Table(name = "reglas_alerta")
public class ReglaAlerta {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    public Integer reglaAlertaId;

    @Column(nullable = false)
    public String nombre;

    public int tipoSensorId;

    @Column(precision = 18, scale = 2)
    public BigDecimal valorMinimo;

    @Column(precision = 18, scale = 2)
    public BigDecimal valorMaximo;

    @Column(nullable = false)
    public String nivelPeligro;

    @Column(nullable = false)
    public String tipoFenomeno;

    @Column(nullable = false, length = 500)
    public String mensaje;

    public boolean estado = true;
}