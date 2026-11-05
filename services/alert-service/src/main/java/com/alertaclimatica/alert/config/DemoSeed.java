package com.alertaclimatica.alert.config;

import com.alertaclimatica.alert.client.Upstream;
import com.alertaclimatica.alert.domain.Entities.ReglaAlerta;
import com.alertaclimatica.alert.domain.Repos.ReglaRepo;
import java.math.BigDecimal;
import java.util.Map;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.boot.ApplicationArguments;
import org.springframework.boot.ApplicationRunner;
import org.springframework.stereotype.Component;

/** Reglas de ejemplo (solo con SEED_DEMO=true y tabla vacía) para que el simulador genere alertas desde el primer arranque. */
@Component
public class DemoSeed implements ApplicationRunner {
    private static final Logger log = LoggerFactory.getLogger(DemoSeed.class);
    private java.util.Set<String> existentes = new java.util.HashSet<>();
    private final ReglaRepo repo; private final Upstream up; private final boolean enabled;

    public DemoSeed(ReglaRepo repo, Upstream up, @Value("${app.seed-demo:false}") boolean enabled) { this.repo = repo; this.up = up; this.enabled = enabled; }

    @Override
    public void run(ApplicationArguments args) {
        if (!enabled) return;
        // Idempotente: solo agrega las reglas que no existan por nombre (así también se completan bases ya sembradas).
        existentes = new java.util.HashSet<>();
        repo.findAll().forEach(x -> existentes.add(x.nombre));
        // Se resuelven los tipos por nombre en el Sensor Service (con reintentos: puede estar arrancando).
        Map<Integer, Upstream.Tipo> tipos = Map.of();
        for (int i = 0; i < 12 && tipos.isEmpty(); i++) {
            tipos = up.tipos();
            if (tipos.isEmpty()) { try { Thread.sleep(5000); } catch (InterruptedException e) { return; } }
        }
        if (tipos.isEmpty()) { log.warn("Sin tipos de sensor disponibles; no se sembraron reglas de demostración."); return; }
        for (var t : tipos.values()) {
            String n = t.nombreTipo().toLowerCase();
            if (n.contains("temperatura")) { r(t, "Temperatura alta", 30, null, "Naranja", "Sequía", "Temperatura alta detectada"); r(t, "Temperatura extrema", 38, null, "Naranja", "Sequía", "Temperatura extrema detectada"); r(t, "Temperatura crítica", 36, null, "Rojo", "Sequía", "Temperatura crítica: riesgo de golpe de calor e incendio"); r(t, "Temperatura de helada", -50, 2, "Naranja", "Helada", "Riesgo de helada"); }
            else if (n.contains("humedad")) { r(t, "Humedad crítica", -1, 15, "Amarillo", "Sequía", "Humedad relativa críticamente baja"); r(t, "Humedad muy alta", 85, null, "Naranja", "Tormenta", "Humedad relativa muy alta: posible tormenta"); r(t, "Humedad saturada", 95, null, "Rojo", "Tormenta", "Humedad saturada: tormenta inminente"); }
            else if (n.contains("viento")) { r(t, "Viento fuerte", 60, null, "Naranja", "Tormenta", "Vientos fuertes"); r(t, "Viento extremo", 90, null, "Rojo", "Tormenta", "Vientos extremos"); }
            else if (n.contains("lluvia")) { r(t, "Lluvia intensa", 100, null, "Naranja", "Inundación", "Lluvia intensa acumulada"); r(t, "Lluvia extrema", 150, null, "Rojo", "Inundación", "Lluvia extrema: riesgo de inundación"); }
            else if (n.contains("río") || n.contains("rio")) { r(t, "Río en alerta", 3.5, null, "Amarillo", "Inundación", "Nivel del río en alerta"); r(t, "Río en nivel de peligro", 4.2, null, "Naranja", "Inundación", "Nivel del río en zona de peligro"); r(t, "Desbordamiento del río", 5, null, "Rojo", "Inundación", "Riesgo de desbordamiento del río"); }
            else if (n.contains("humo")) r(t, "Humo elevado", 300, null, "Rojo", "Incendio forestal", "Concentración de humo elevada: posible incendio");
        }
        log.info("Reglas de demostración verificadas; total: {}", repo.count());
    }

    private void r(Upstream.Tipo t, String nombre, Number min, Number max, String nivel, String fen, String msg) {
        String completo = nombre + " (" + t.nombreTipo() + ")";
        if (!existentes.add(completo)) return;
        ReglaAlerta r = new ReglaAlerta();
        r.nombre = completo; r.tipoSensorId = t.tipoSensorId();
        r.valorMinimo = (min == null || min.doubleValue() < 0) ? null : BigDecimal.valueOf(min.doubleValue()); // negativo = sin mínimo
        r.valorMaximo = max == null ? null : BigDecimal.valueOf(max.doubleValue());
        r.nivelPeligro = nivel; r.tipoFenomeno = fen; r.mensaje = msg; r.estado = true;
        repo.save(r);
    }
}
