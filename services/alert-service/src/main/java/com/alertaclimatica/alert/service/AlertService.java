package com.alertaclimatica.alert.service;

import static com.alertaclimatica.alert.service.Util.*;

import com.alertaclimatica.alert.client.Upstream;
import com.alertaclimatica.alert.config.Errors.Conflict;
import com.alertaclimatica.alert.config.Errors.NotFound;
import com.alertaclimatica.alert.domain.Alerta;
import com.alertaclimatica.alert.domain.Evento;
import com.alertaclimatica.alert.domain.ReglaAlerta;
import com.alertaclimatica.alert.domain.Repos.*;
import com.alertaclimatica.alert.service.Dtos.*;
import jakarta.persistence.criteria.Predicate;
import java.math.BigDecimal;
import java.time.Instant;
import java.util.*;
import java.util.stream.Collectors;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.data.domain.PageRequest;
import org.springframework.data.domain.Sort;
import org.springframework.data.jpa.domain.Specification;
import org.springframework.security.oauth2.jwt.Jwt;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

@Service
public class AlertService {
    private static final Logger log = LoggerFactory.getLogger(AlertService.class);
    private final AlertaRepo alertas; private final EventoRepo eventos; private final ReglaRepo reglas; private final Upstream up;

    public AlertService(AlertaRepo a, EventoRepo e, ReglaRepo r, Upstream up) { this.alertas = a; this.eventos = e; this.reglas = r; this.up = up; }

    // ------------------------------------------------------------------ evaluación (motor de alertas)
    private static boolean coincide(ReglaAlerta r, BigDecimal v) {
        if (r.valorMinimo != null && v.compareTo(r.valorMinimo) < 0) return false;
        if (r.valorMaximo != null && v.compareTo(r.valorMaximo) > 0) return false;
        return r.valorMinimo != null || r.valorMaximo != null;
    }

    /** Evalúa lecturas contra las reglas activas del tipo de sensor: nivel más alto, sin duplicar alertas abiertas. */
    @Transactional
    public int evaluar(List<Lectura> lecturas) {
        int creadas = 0;
        for (Lectura l : lecturas) {
            if (l.sensorId() == null || l.tipoSensorId() == null || l.valor() == null) continue;
            Optional<ReglaAlerta> regla = reglas.findByEstadoTrueAndTipoSensorId(l.tipoSensorId()).stream()
                .filter(r -> peso(r.nivelPeligro) > 0 && coincide(r, l.valor()))
                .max(Comparator.<ReglaAlerta>comparingInt(r -> peso(r.nivelPeligro)).thenComparing(r -> -r.reglaAlertaId));
            if (regla.isEmpty()) continue;
            ReglaAlerta r = regla.get();
            if (alertas.existsBySensorIdAndReglaAlertaIdAndEstadoNot(l.sensorId(), r.reglaAlertaId, "Cerrada")) continue;
            String nivel = NIVELES.get(peso(r.nivelPeligro));
            String unidad = l.unidad() == null ? "" : l.unidad();
            String valorTxt = (num(l.valor()) + " " + unidad).trim();
            Instant ahora = Instant.now();
            String comNombre = up.comunidadNombre(l.comunidadId());

            Evento ev = new Evento();
            ev.tipoFenomeno = r.tipoFenomeno;
            ev.descripcion = recortar(r.mensaje + " Valor detectado: " + valorTxt + " (sensor " + l.sensorCodigo() + ").", 500);
            ev.fechaHora = ahora; ev.comunidadId = l.comunidadId(); ev.comunidadNombre = comNombre;
            ev.sensorId = l.sensorId(); ev.sensorNombre = l.sensorNombre(); ev.nivelRiesgo = nivel; ev.valor = l.valor(); ev.estado = "Activa";
            ev = eventos.save(ev);

            Alerta a = new Alerta();
            a.sensorId = l.sensorId(); a.sensorCodigo = l.sensorCodigo(); a.sensorNombre = l.sensorNombre();
            a.comunidadId = l.comunidadId(); a.comunidadNombre = comNombre; a.tipoSensorId = l.tipoSensorId(); a.unidad = unidad;
            a.reglaAlertaId = r.reglaAlertaId; a.fenomeno = r.tipoFenomeno; a.nivelRiesgo = nivel;
            a.mensaje = recortar(r.mensaje + " Valor detectado: " + valorTxt + ".", 500);
            a.fechaEmision = ahora; a.valorDetectado = l.valor(); a.estado = "Activa"; a.eventoId = ev.eventoId;
            alertas.save(a);
            creadas++;
            log.info("Alerta {} generada: sensor {}, regla '{}', valor {}", nivel, l.sensorCodigo(), r.nombre, l.valor());
        }
        return creadas;
    }

    // ------------------------------------------------------------------ consultas
    private Map<Integer, ReglaAlerta> reglasDe(Collection<Alerta> list) {
        Set<Integer> ids = list.stream().map(a -> a.reglaAlertaId).filter(Objects::nonNull).collect(Collectors.toSet());
        return ids.isEmpty() ? Map.of() : reglas.findAllById(ids).stream().collect(Collectors.toMap(r -> r.reglaAlertaId, r -> r));
    }

    static String umbral(ReglaAlerta r) {
        if (r == null) return null;
        if (r.valorMinimo != null && r.valorMaximo != null) return num(r.valorMinimo) + " – " + num(r.valorMaximo);
        if (r.valorMinimo != null) return "≥ " + num(r.valorMinimo);
        if (r.valorMaximo != null) return "≤ " + num(r.valorMaximo);
        return null;
    }

    private AlertaDto dto(Alerta a, Map<Integer, ReglaAlerta> rs) {
        ReglaAlerta r = a.reglaAlertaId == null ? null : rs.get(a.reglaAlertaId);
        return new AlertaDto(a.alertaId, a.fechaEmision, a.sensorId, a.sensorCodigo, a.sensorNombre, a.comunidadId, a.comunidadNombre, a.reglaAlertaId,
            r == null ? null : r.nombre, a.fenomeno, a.nivelRiesgo, a.valorDetectado, a.unidad, umbral(r), a.mensaje, a.estado, a.eventoId,
            a.usuarioResponsableId, a.usuarioResponsableNombre, a.fechaGestion);
    }

    public List<AlertaDto> listar(String desde, String hasta, Integer comunidadId, Integer sensorId, String fenomeno, String nivel, String estado, int limite) {
        Instant d = parse(desde, false), h = parse(hasta, true);
        Specification<Alerta> spec = (root, q, cb) -> {
            List<Predicate> p = new ArrayList<>();
            if (d != null) p.add(cb.greaterThanOrEqualTo(root.get("fechaEmision"), d));
            if (h != null) p.add(cb.lessThan(root.get("fechaEmision"), h));
            if (comunidadId != null) p.add(cb.equal(root.get("comunidadId"), comunidadId));
            if (sensorId != null) p.add(cb.equal(root.get("sensorId"), sensorId));
            if (nivel != null && !nivel.isBlank()) p.add(cb.equal(root.get("nivelRiesgo"), nivel.trim()));
            if (estado != null && !estado.isBlank()) p.add(cb.equal(root.get("estado"), estado.trim()));
            if (fenomeno != null && !fenomeno.isBlank()) p.add(cb.equal(root.get("fenomeno"), fenomeno.trim()));
            return cb.and(p.toArray(new Predicate[0]));
        };
        List<Alerta> list = alertas.findAll(spec, PageRequest.of(0, Math.min(Math.max(limite, 1), 5000), Sort.by(Sort.Direction.DESC, "fechaEmision"))).getContent();
        Map<Integer, ReglaAlerta> rs = reglasDe(list);
        return list.stream().map(a -> dto(a, rs)).toList();
    }

    public AlertaDto obtener(int id) {
        Alerta a = alertas.findById(id).orElseThrow(() -> new NotFound("Alerta no encontrada."));
        return dto(a, reglasDe(List.of(a)));
    }

    // ------------------------------------------------------------------ gestión
    @Transactional
    public Map<String, Object> cambiarEstado(int id, String nuevo, Jwt user) {
        Alerta a = alertas.findById(id).orElseThrow(() -> new NotFound("Alerta no encontrada."));
        if ("Cerrada".equals(a.estado)) throw new Conflict("La alerta ya está cerrada.");
        if ("Atendida".equals(nuevo) && !"Activa".equals(a.estado)) throw new Conflict("Solo se puede atender una alerta activa.");
        final Integer uid = idDe(user);
        String anterior = a.estado; Instant ahora = Instant.now();
        a.estado = nuevo; a.usuarioResponsableId = uid; a.usuarioResponsableNombre = user.getClaimAsString("name"); a.fechaGestion = ahora;
        if (a.eventoId != null) eventos.findById(a.eventoId).ifPresent(e -> { e.estado = nuevo; e.usuarioResponsableId = uid; e.usuarioResponsableNombre = a.usuarioResponsableNombre; });
        up.audit("Atendida".equals(nuevo) ? "ATENDER_ALERTA" : "CERRAR_ALERTA", "La alerta #" + id + " pasó de " + anterior + " a " + nuevo + ".", "Alerta", id);
        return Map.of("mensaje", "Alerta " + nuevo.toLowerCase(Locale.ROOT) + ".", "alertaId", id, "estado", nuevo);
    }

    private static Integer idDe(Jwt user) {
        try { return Integer.valueOf(user.getSubject()); } catch (Exception e) { throw new IllegalArgumentException("No se pudo identificar al usuario."); }
    }

    @Transactional
    public AlertaDto crear(CrearAlerta in) {
        if (in.sensorId() == null) throw new IllegalArgumentException("El sensor indicado no existe.");
        Upstream.SensorInfo s = up.sensor(in.sensorId());
        if (s == null) throw new IllegalArgumentException("El sensor indicado no existe.");
        ReglaAlerta regla = null; String nivel = in.nivelRiesgo(); String mensaje = in.mensaje();
        if (in.reglaAlertaId() != null) {
            regla = reglas.findById(in.reglaAlertaId()).orElseThrow(() -> new IllegalArgumentException("La regla de alerta indicada no existe."));
            nivel = regla.nivelPeligro; mensaje = regla.mensaje;
        }
        String oficial = canon(NIVELES, nivel);
        if (oficial == null) throw new IllegalArgumentException("El nivel de riesgo debe ser: Verde, Amarillo, Naranja o Rojo.");
        if (mensaje == null || mensaje.isBlank()) throw new IllegalArgumentException("El mensaje de la alerta es obligatorio.");
        String fenomeno = regla != null ? regla.tipoFenomeno : fenomenoDe(mensaje, oficial);
        Instant fecha = in.fechaEmision() != null ? in.fechaEmision() : Instant.now();
        String comNombre = up.comunidadNombre(s.comunidadId());

        Evento ev = new Evento();
        ev.tipoFenomeno = fenomeno; ev.descripcion = recortar(mensaje, 500); ev.fechaHora = fecha; ev.comunidadId = s.comunidadId(); ev.comunidadNombre = comNombre;
        ev.sensorId = s.sensorId(); ev.sensorNombre = s.nombreSensor(); ev.nivelRiesgo = oficial; ev.estado = "Activa"; ev.valor = in.valorDetectado();
        ev = eventos.save(ev);

        Alerta a = new Alerta();
        a.sensorId = s.sensorId(); a.sensorCodigo = s.codigo(); a.sensorNombre = s.nombreSensor(); a.comunidadId = s.comunidadId(); a.comunidadNombre = comNombre;
        a.tipoSensorId = s.tipoSensorId(); a.unidad = s.tipoSensor() == null ? null : s.tipoSensor().unidadMedida();
        a.reglaAlertaId = regla == null ? null : regla.reglaAlertaId; a.fenomeno = fenomeno; a.nivelRiesgo = oficial; a.mensaje = recortar(mensaje, 500);
        a.fechaEmision = fecha; a.valorDetectado = in.valorDetectado(); a.estado = "Activa"; a.eventoId = ev.eventoId;
        a = alertas.save(a);
        up.audit("CREAR_ALERTA", "Se creó la alerta #" + a.alertaId + " (nivel " + oficial + ") para el sensor #" + s.sensorId() + ".", "Alerta", a.alertaId);
        return dto(a, reglasDe(List.of(a)));
    }

    private static String fenomenoDe(String mensaje, String nivel) {
        String t = mensaje.toLowerCase(Locale.ROOT);
        if (t.contains("desbordamiento") || t.matches(".*\\b(río|rio)\\b.*") || t.contains("inundaci")) return "Inundación";
        if (t.contains("incendio")) return "Incendio forestal";
        if (t.contains("sequ")) return "Sequía";
        if (t.contains("helada")) return "Helada";
        if (t.contains("tormenta") || t.contains("viento")) return "Tormenta";
        return "Tormenta";
    }

    @Transactional
    public void eliminarTodas() {
        long n = alertas.count();
        alertas.deleteAllInBatch();
        up.audit("ELIMINAR_ALERTAS", "Se eliminaron " + n + " alertas (limpieza del historial de alertas).", "Alerta", null);
    }

    // ------------------------------------------------------------------ resumen para el dashboard
    public Resumen resumen(Integer comunidadId) {
        Map<String, Long> porNivel = new HashMap<>();
        for (Object[] row : alertas.activasPorNivel(comunidadId)) porNivel.merge(String.valueOf(row[0]).toLowerCase(Locale.ROOT), (Long) row[1], Long::sum);
        List<NivelCount> niveles = NIVELES.stream().map(n -> new NivelCount(n, porNivel.getOrDefault(n.toLowerCase(Locale.ROOT), 0L))).toList();
        long total = niveles.stream().mapToLong(NivelCount::total).sum();
        String global = "Verde";
        for (int i = NIVELES.size() - 1; i >= 1; i--) if (niveles.get(i).total() > 0) { global = NIVELES.get(i); break; }
        List<Conteo> fen = alertas.activasPorFenomeno(comunidadId).stream().map(r -> new Conteo(String.valueOf(r[0]), (Long) r[1])).toList();
        List<ResumenAlerta> rec = alertas.activasRecientes(comunidadId, PageRequest.of(0, 6)).stream()
            .map(a -> new ResumenAlerta(a.alertaId, a.fechaEmision, a.nivelRiesgo, a.fenomeno, a.sensorNombre, a.comunidadNombre, a.valorDetectado, a.unidad, a.mensaje)).toList();
        List<ResumenEvento> evs = eventos.recientes(comunidadId, PageRequest.of(0, 8)).stream()
            .map(e -> new ResumenEvento(e.eventoId, e.fechaHora, e.tipoFenomeno, e.nivelRiesgo, e.comunidadNombre, e.sensorNombre, e.valor, e.estado)).toList();
        return new Resumen(total, global, niveles, fen, rec, evs);
    }
}
