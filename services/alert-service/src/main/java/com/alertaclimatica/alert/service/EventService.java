package com.alertaclimatica.alert.service;

import static com.alertaclimatica.alert.service.Util.*;

import com.alertaclimatica.alert.client.Upstream;
import com.alertaclimatica.alert.domain.Evento;
import com.alertaclimatica.alert.domain.Repos.EventoRepo;
import com.alertaclimatica.alert.service.Dtos.*;
import jakarta.persistence.criteria.Predicate;
import java.time.Instant;
import java.util.*;
import java.util.function.Function;
import java.util.stream.Collectors;
import org.springframework.data.domain.PageRequest;
import org.springframework.data.domain.Sort;
import org.springframework.data.jpa.domain.Specification;
import org.springframework.stereotype.Service;

@Service
public class EventService {
    private final EventoRepo repo; private final Upstream up;
    public EventService(EventoRepo repo, Upstream up) { this.repo = repo; this.up = up; }

    static EventoDto dto(Evento e) {
        return new EventoDto(e.eventoId, e.fechaHora, e.comunidadId, e.comunidadNombre, e.sensorId, e.sensorNombre, e.tipoFenomeno, e.nivelRiesgo,
            e.valor, e.descripcion, e.estado, e.usuarioResponsableId, e.usuarioResponsableNombre);
    }

    private List<Evento> filtrar(String ini, String fin, Integer comunidadId, String fenomeno, String nivel, int limite) {
        Instant d = parse(ini, false), h = parse(fin, true);
        Specification<Evento> spec = (root, q, cb) -> {
            List<Predicate> p = new ArrayList<>();
            if (d != null) p.add(cb.greaterThanOrEqualTo(root.get("fechaHora"), d));
            if (h != null) p.add(cb.lessThan(root.get("fechaHora"), h));
            if (comunidadId != null) p.add(cb.equal(root.get("comunidadId"), comunidadId));
            if (fenomeno != null && !fenomeno.isBlank()) p.add(cb.equal(root.get("tipoFenomeno"), fenomeno.trim()));
            if (nivel != null && !nivel.isBlank()) p.add(cb.equal(root.get("nivelRiesgo"), nivel.trim()));
            return cb.and(p.toArray(new Predicate[0]));
        };
        return repo.findAll(spec, PageRequest.of(0, Math.min(Math.max(limite, 1), 20000), Sort.by(Sort.Direction.DESC, "fechaHora"))).getContent();
    }

    public List<EventoDto> listar(String ini, String fin, Integer comunidadId, String fenomeno, String nivel, int limite) {
        return filtrar(ini, fin, comunidadId, fenomeno, nivel, limite).stream().map(EventService::dto).toList();
    }

    public EventoStats estadisticas(String ini, String fin, Integer comunidadId, String fenomeno, String nivel) {
        List<Evento> l = filtrar(ini, fin, comunidadId, fenomeno, nivel, 20000);
        return new EventoStats(l.size(), contar(l, e -> e.tipoFenomeno), contar(l, e -> e.nivelRiesgo), contar(l, e -> e.estado), contar(l, e -> e.comunidadNombre));
    }

    private static List<Conteo> contar(List<Evento> l, Function<Evento, String> f) {
        return l.stream().collect(Collectors.groupingBy(e -> { String v = f.apply(e); return v == null || v.isBlank() ? "Sin dato" : v; }, Collectors.counting()))
            .entrySet().stream().map(x -> new Conteo(x.getKey(), x.getValue())).sorted(Comparator.comparingLong(Conteo::total).reversed()).toList();
    }

    public Optional<EventoDto> obtener(int id) { return repo.findById(id).map(EventService::dto); }

    public EventoDto crear(CrearEvento in) {
        String fen = canon(FENOMENOS, in.tipoFenomeno());
        if (fen == null) throw new IllegalArgumentException("El fenómeno debe ser: Inundación, Sequía, Tormenta, Helada o Incendio forestal.");
        String niv = canon(NIVELES, in.nivelRiesgo());
        if (niv == null) throw new IllegalArgumentException("El nivel debe ser: Verde, Amarillo, Naranja o Rojo.");
        String est = in.estado() == null || in.estado().isBlank() ? "Activa" : canon(ESTADOS, in.estado());
        if (est == null) throw new IllegalArgumentException("El estado debe ser: Activa, Atendida o Cerrada.");
        if (in.descripcion() == null || in.descripcion().isBlank()) throw new IllegalArgumentException("La descripción del evento es obligatoria.");
        if (in.descripcion().trim().length() > 500) throw new IllegalArgumentException("La descripción no puede superar 500 caracteres.");
        Integer comId = in.comunidadId(); String comNombre = null; String sensorNombre = null;
        if (in.sensorId() != null) {
            Upstream.SensorInfo s = up.sensor(in.sensorId());
            if (s == null) throw new IllegalArgumentException("El sensor indicado no existe.");
            sensorNombre = s.nombreSensor();
            if (comId == null) comId = s.comunidadId();
        }
        if (comId != null) {
            comNombre = up.comunidadNombre(comId);
            if (comNombre == null) throw new IllegalArgumentException("La comunidad indicada no existe.");
        }
        Evento e = new Evento();
        e.tipoFenomeno = fen; e.descripcion = in.descripcion().trim(); e.nivelRiesgo = niv; e.estado = est; e.valor = in.valor();
        e.comunidadId = comId; e.comunidadNombre = comNombre; e.sensorId = in.sensorId(); e.sensorNombre = sensorNombre;
        e.fechaHora = in.fechaHora() != null ? in.fechaHora() : Instant.now();
        e = repo.save(e);
        up.audit("CREAR_EVENTO", "El usuario registró el evento #" + e.eventoId + " (" + fen + ", nivel " + niv + ").", "Evento", e.eventoId);
        return dto(e);
    }
}
