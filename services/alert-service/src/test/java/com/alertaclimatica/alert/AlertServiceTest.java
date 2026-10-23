package com.alertaclimatica.alert;

import static org.junit.jupiter.api.Assertions.*;
import static org.mockito.ArgumentMatchers.*;
import static org.mockito.Mockito.*;

import com.alertaclimatica.alert.client.Upstream;
import com.alertaclimatica.alert.domain.Alerta;
import com.alertaclimatica.alert.domain.Evento;
import com.alertaclimatica.alert.domain.ReglaAlerta;
import com.alertaclimatica.alert.domain.Repos.*;
import com.alertaclimatica.alert.service.AlertService;
import com.alertaclimatica.alert.service.Dtos.Lectura;
import java.math.BigDecimal;
import java.time.Instant;
import java.util.List;
import org.junit.jupiter.api.Test;

/** Motor de alertas: elige el nivel más alto que coincide y no duplica alertas abiertas (misma lógica que la Fase 2). */
class AlertServiceTest {
    private ReglaAlerta regla(int id, String nivel, Integer min, Integer max) {
        ReglaAlerta r = new ReglaAlerta();
        r.reglaAlertaId = id; r.nombre = "r" + id; r.tipoSensorId = 1; r.nivelPeligro = nivel; r.tipoFenomeno = "Tormenta"; r.mensaje = "msg";
        r.valorMinimo = min == null ? null : BigDecimal.valueOf(min); r.valorMaximo = max == null ? null : BigDecimal.valueOf(max); r.estado = true;
        return r;
    }

    private Lectura lectura(double v) {
        return new Lectura(7, "SEN-07", "Sensor", 1, "Temperatura", "°C", 1, BigDecimal.valueOf(v), Instant.now());
    }

    @Test
    void generaAlertaConElNivelMasAlto() {
        AlertaRepo alertas = mock(AlertaRepo.class); EventoRepo eventos = mock(EventoRepo.class); ReglaRepo reglas = mock(ReglaRepo.class); Upstream up = mock(Upstream.class);
        when(reglas.findByEstadoTrueAndTipoSensorId(1)).thenReturn(List.of(regla(1, "Amarillo", 30, null), regla(2, "Rojo", 40, null), regla(3, "Naranja", 35, null)));
        when(eventos.save(any(Evento.class))).thenAnswer(i -> { Evento e = i.getArgument(0); e.eventoId = 9; return e; });
        when(alertas.save(any(Alerta.class))).thenAnswer(i -> i.getArgument(0));
        int n = new AlertService(alertas, eventos, reglas, up).evaluar(List.of(lectura(41)));
        assertEquals(1, n);
        verify(alertas).save(argThat((Alerta a) -> "Rojo".equals(a.nivelRiesgo) && a.reglaAlertaId == 2 && a.eventoId == 9));
    }

    @Test
    void noDuplicaSiYaHayAlertaAbierta() {
        AlertaRepo alertas = mock(AlertaRepo.class); EventoRepo eventos = mock(EventoRepo.class); ReglaRepo reglas = mock(ReglaRepo.class); Upstream up = mock(Upstream.class);
        when(reglas.findByEstadoTrueAndTipoSensorId(1)).thenReturn(List.of(regla(2, "Rojo", 40, null)));
        when(alertas.existsBySensorIdAndReglaAlertaIdAndEstadoNot(7, 2, "Cerrada")).thenReturn(true);
        assertEquals(0, new AlertService(alertas, eventos, reglas, up).evaluar(List.of(lectura(45))));
        verify(alertas, never()).save(any());
    }

    @Test
    void ignoraValoresFueraDeLaRegla() {
        AlertaRepo alertas = mock(AlertaRepo.class); EventoRepo eventos = mock(EventoRepo.class); ReglaRepo reglas = mock(ReglaRepo.class); Upstream up = mock(Upstream.class);
        when(reglas.findByEstadoTrueAndTipoSensorId(1)).thenReturn(List.of(regla(2, "Rojo", 40, null)));
        assertEquals(0, new AlertService(alertas, eventos, reglas, up).evaluar(List.of(lectura(20))));
    }
}
