package com.alertaclimatica.alert.web;

import com.alertaclimatica.alert.config.Errors.NotFound;
import com.alertaclimatica.alert.service.*;
import com.alertaclimatica.alert.service.Dtos.*;
import java.util.List;
import java.util.Map;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.security.access.prepost.PreAuthorize;
import org.springframework.security.core.annotation.AuthenticationPrincipal;
import org.springframework.security.oauth2.jwt.Jwt;
import org.springframework.web.bind.annotation.*;

public final class Controllers {
    private Controllers() {}
    private static final String EDITOR = "hasAnyRole('Administrador','Operador')";
    private static final String ADMIN = "hasRole('Administrador')";

    @RestController
    public static class Health {
        @GetMapping("/health") public Map<String, String> health() { return Map.of("estado", "ok"); }
    }

    /** Endpoints internos: solo para servicios (rol "Servicio"); el gateway nunca los expone. */
    @RestController @RequestMapping("/api/alerts/internal")
    public static class Internal {
        private final AlertService svc;
        public Internal(AlertService svc) { this.svc = svc; }

        @PostMapping("/evaluate") @PreAuthorize("hasRole('Servicio')")
        public Map<String, Object> evaluate(@RequestBody EvaluarRequest req) {
            return Map.of("alertasCreadas", svc.evaluar(req.lecturas() == null ? List.of() : req.lecturas()));
        }
    }

    @RestController @RequestMapping("/api/alerts")
    public static class Alertas {
        private final AlertService svc;
        public Alertas(AlertService svc) { this.svc = svc; }

        @GetMapping
        public List<AlertaDto> list(@RequestParam(required = false) String fechaDesde, @RequestParam(required = false) String fechaHasta,
                                    @RequestParam(required = false) Integer comunidadId, @RequestParam(required = false) Integer sensorId,
                                    @RequestParam(required = false) String fenomeno, @RequestParam(required = false) String nivel,
                                    @RequestParam(required = false) String estado, @RequestParam(defaultValue = "500") int limite) {
            return svc.listar(fechaDesde, fechaHasta, comunidadId, sensorId, fenomeno, nivel, estado, limite);
        }

        @GetMapping("/summary")
        public Resumen summary(@RequestParam(required = false) Integer comunidadId) { return svc.resumen(comunidadId); }

        @GetMapping("/{id:\\d+}") public AlertaDto one(@PathVariable int id) { return svc.obtener(id); }

        @PostMapping("/{id:\\d+}/atender") @PreAuthorize(EDITOR)
        public Map<String, Object> atender(@PathVariable int id, @AuthenticationPrincipal Jwt u) { return svc.cambiarEstado(id, "Atendida", u); }

        @PostMapping("/{id:\\d+}/cerrar") @PreAuthorize(EDITOR)
        public Map<String, Object> cerrar(@PathVariable int id, @AuthenticationPrincipal Jwt u) { return svc.cambiarEstado(id, "Cerrada", u); }

        @PostMapping @PreAuthorize(EDITOR)
        public ResponseEntity<AlertaDto> crear(@RequestBody CrearAlerta in) { return ResponseEntity.status(HttpStatus.CREATED).body(svc.crear(in)); }

        @DeleteMapping @PreAuthorize(EDITOR)
        public Map<String, String> borrarTodas() { svc.eliminarTodas(); return Map.of("mensaje", "Historial de alertas limpiado correctamente."); }
    }

    @RestController @RequestMapping("/api/alerts/events")
    public static class Eventos {
        private final EventService svc;
        public Eventos(EventService svc) { this.svc = svc; }

        @GetMapping
        public List<EventoDto> list(@RequestParam(required = false) String fechaInicio, @RequestParam(required = false) String fechaFin,
                                    @RequestParam(required = false) Integer comunidadId, @RequestParam(required = false) String fenomeno,
                                    @RequestParam(required = false) String nivel, @RequestParam(defaultValue = "5000") int limite) {
            return svc.listar(fechaInicio, fechaFin, comunidadId, fenomeno, nivel, limite);
        }

        @GetMapping("/estadisticas")
        public EventoStats stats(@RequestParam(required = false) String fechaInicio, @RequestParam(required = false) String fechaFin,
                                 @RequestParam(required = false) Integer comunidadId, @RequestParam(required = false) String fenomeno,
                                 @RequestParam(required = false) String nivel) {
            return svc.estadisticas(fechaInicio, fechaFin, comunidadId, fenomeno, nivel);
        }

        @GetMapping("/{id:\\d+}") public EventoDto one(@PathVariable int id) { return svc.obtener(id).orElseThrow(() -> new NotFound("Evento no encontrado.")); }

        @PostMapping @PreAuthorize(EDITOR)
        public ResponseEntity<EventoDto> crear(@RequestBody CrearEvento in) { return ResponseEntity.status(HttpStatus.CREATED).body(svc.crear(in)); }
    }

    @RestController @RequestMapping("/api/alerts/rules")
    public static class Reglas {
        private final RuleService svc;
        public Reglas(RuleService svc) { this.svc = svc; }

        @GetMapping public List<ReglaDto> all() { return svc.todas(); }
        @GetMapping("/{id:\\d+}") public ReglaDto one(@PathVariable int id) { return svc.obtener(id); }

        @PostMapping @PreAuthorize(ADMIN)
        public ResponseEntity<ReglaDto> crear(@RequestBody ReglaInput in) { return ResponseEntity.status(HttpStatus.CREATED).body(svc.crear(in)); }

        @PutMapping("/{id:\\d+}") @PreAuthorize(ADMIN) public ReglaDto upd(@PathVariable int id, @RequestBody ReglaInput in) { return svc.actualizar(id, in); }

        @PatchMapping("/{id:\\d+}/estado") @PreAuthorize(ADMIN)
        public Map<String, Object> estado(@PathVariable int id, @RequestBody Boolean estado) { return svc.cambiarEstado(id, Boolean.TRUE.equals(estado)); }

        @DeleteMapping("/{id:\\d+}") @PreAuthorize(ADMIN) public Map<String, Object> del(@PathVariable int id) { return svc.eliminar(id); }
    }
}
