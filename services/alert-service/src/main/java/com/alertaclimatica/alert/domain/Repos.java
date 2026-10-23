package com.alertaclimatica.alert.domain;

import com.alertaclimatica.alert.domain.Alerta;
import com.alertaclimatica.alert.domain.Evento;
import com.alertaclimatica.alert.domain.ReglaAlerta;
import java.util.List;
import org.springframework.data.domain.Pageable;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.JpaSpecificationExecutor;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;

public final class Repos {
    private Repos() {}

    public interface ReglaRepo extends JpaRepository<ReglaAlerta, Integer> {
        List<ReglaAlerta> findByEstadoTrueAndTipoSensorId(int tipoSensorId);
    }

    public interface EventoRepo extends JpaRepository<Evento, Integer>, JpaSpecificationExecutor<Evento> {
        @Query("select e from Evento e where (:cid is null or e.comunidadId = :cid) order by e.fechaHora desc")
        List<Evento> recientes(@Param("cid") Integer comunidadId, Pageable page);
    }

    public interface AlertaRepo extends JpaRepository<Alerta, Integer>, JpaSpecificationExecutor<Alerta> {
        boolean existsBySensorIdAndReglaAlertaIdAndEstadoNot(int sensorId, int reglaId, String estado);
        boolean existsByReglaAlertaId(int reglaId);

        @Query("select a.nivelRiesgo, count(a) from Alerta a where a.estado = 'Activa' and (:cid is null or a.comunidadId = :cid) group by a.nivelRiesgo")
        List<Object[]> activasPorNivel(@Param("cid") Integer comunidadId);

        @Query("select coalesce(a.fenomeno, 'Sin dato'), count(a) from Alerta a where a.estado = 'Activa' and (:cid is null or a.comunidadId = :cid) group by a.fenomeno")
        List<Object[]> activasPorFenomeno(@Param("cid") Integer comunidadId);

        @Query("select a from Alerta a where a.estado = 'Activa' and (:cid is null or a.comunidadId = :cid) order by a.fechaEmision desc")
        List<Alerta> activasRecientes(@Param("cid") Integer comunidadId, Pageable page);
    }
}
