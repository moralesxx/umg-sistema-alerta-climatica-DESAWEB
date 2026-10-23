package com.alertaclimatica.alert.service;

import static com.alertaclimatica.alert.service.Util.*;

import com.alertaclimatica.alert.client.Upstream;
import com.alertaclimatica.alert.config.Errors.Conflict;
import com.alertaclimatica.alert.config.Errors.NotFound;
import com.alertaclimatica.alert.domain.ReglaAlerta;
import com.alertaclimatica.alert.domain.Repos.*;
import com.alertaclimatica.alert.service.Dtos.*;
import java.math.BigDecimal;
import java.util.List;
import java.util.Map;
import org.springframework.stereotype.Service;

@Service
public class RuleService {
    private final ReglaRepo repo; private final AlertaRepo alertas; private final Upstream up;
    public RuleService(ReglaRepo repo, AlertaRepo alertas, Upstream up) { this.repo = repo; this.alertas = alertas; this.up = up; }

    private ReglaDto dto(ReglaAlerta r, Map<Integer, Upstream.Tipo> tipos) {
        Upstream.Tipo t = tipos.get(r.tipoSensorId);
        return new ReglaDto(r.reglaAlertaId, r.nombre, r.tipoSensorId, r.valorMinimo, r.valorMaximo, r.nivelPeligro, r.tipoFenomeno, r.mensaje, r.estado,
            t == null ? null : new TipoSensorDto(t.tipoSensorId(), t.nombreTipo(), t.unidadMedida()));
    }

    public List<ReglaDto> todas() { var t = up.tipos(); return repo.findAll().stream().map(r -> dto(r, t)).toList(); }
    public ReglaDto obtener(int id) { return dto(repo.findById(id).orElseThrow(() -> new NotFound("Regla de alerta no encontrada.")), up.tipos()); }

    private void aplicar(ReglaAlerta r, ReglaInput in) {
        if (in.nombre() == null || in.nombre().isBlank()) throw new IllegalArgumentException("El nombre de la regla es obligatorio.");
        if (in.tipoSensorId() == null || in.tipoSensorId() <= 0) throw new IllegalArgumentException("Debe seleccionar un tipo de sensor.");
        if (in.tipoFenomeno() == null || in.tipoFenomeno().isBlank()) throw new IllegalArgumentException("El tipo de fenómeno es obligatorio.");
        String fen = canon(FENOMENOS, in.tipoFenomeno());
        if (fen == null) throw new IllegalArgumentException("El tipo de fenómeno debe ser Inundación, Sequía, Tormenta, Helada o Incendio forestal.");
        if (in.mensaje() == null || in.mensaje().isBlank()) throw new IllegalArgumentException("El mensaje de la alerta es obligatorio.");
        String niv = canon(NIVELES, in.nivelPeligro());
        if (niv == null) throw new IllegalArgumentException("El nivel de peligro debe ser Verde, Amarillo, Naranja o Rojo.");
        BigDecimal min = in.valorMinimo(), max = in.valorMaximo();
        if (min == null && max == null) throw new IllegalArgumentException("La regla debe tener un valor mínimo o un valor máximo.");
        if (min != null && max != null && min.compareTo(max) > 0) throw new IllegalArgumentException("El valor mínimo no puede ser mayor que el valor máximo.");
        var tipos = up.tipos();
        if (!tipos.isEmpty() && !tipos.containsKey(in.tipoSensorId())) throw new IllegalArgumentException("El tipo de sensor seleccionado no existe.");
        r.nombre = in.nombre().trim(); r.tipoSensorId = in.tipoSensorId(); r.valorMinimo = min; r.valorMaximo = max;
        r.nivelPeligro = niv; r.tipoFenomeno = fen; r.mensaje = recortar(in.mensaje().trim(), 500); r.estado = in.estado() == null || in.estado();
    }

    public ReglaDto crear(ReglaInput in) {
        ReglaAlerta r = new ReglaAlerta(); aplicar(r, in); r = repo.save(r);
        up.audit("CREAR_REGLA", "Se creó la regla #" + r.reglaAlertaId + " \"" + r.nombre + "\".", "ReglaAlerta", r.reglaAlertaId);
        return dto(r, up.tipos());
    }

    public ReglaDto actualizar(int id, ReglaInput in) {
        ReglaAlerta r = repo.findById(id).orElseThrow(() -> new NotFound("Regla de alerta no encontrada."));
        aplicar(r, in); r = repo.save(r);
        up.audit("EDITAR_REGLA", "Se modificó la regla #" + id + " \"" + r.nombre + "\".", "ReglaAlerta", id);
        return dto(r, up.tipos());
    }

    public Map<String, Object> cambiarEstado(int id, boolean estado) {
        ReglaAlerta r = repo.findById(id).orElseThrow(() -> new NotFound("Regla de alerta no encontrada."));
        r.estado = estado; repo.save(r);
        up.audit(estado ? "ACTIVAR_REGLA" : "DESACTIVAR_REGLA", "La regla #" + id + " \"" + r.nombre + "\" fue " + (estado ? "activada" : "desactivada") + ".", "ReglaAlerta", id);
        return Map.of("mensaje", "Estado de la regla actualizado correctamente.", "reglaAlertaId", id, "nuevoEstado", estado);
    }

    public Map<String, Object> eliminar(int id) {
        ReglaAlerta r = repo.findById(id).orElseThrow(() -> new NotFound("Regla de alerta no encontrada."));
        if (alertas.existsByReglaAlertaId(id)) throw new Conflict("No se puede eliminar la regla porque tiene alertas asociadas. Desactívela en su lugar.");
        repo.delete(r);
        up.audit("ELIMINAR_REGLA", "Se eliminó la regla #" + id + " \"" + r.nombre + "\".", "ReglaAlerta", id);
        return Map.of("mensaje", "Regla eliminada correctamente.");
    }
}
