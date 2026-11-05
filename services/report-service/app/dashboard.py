"""Indicadores del dashboard (sección 15): se alimentan de las APIs de Community, Sensor y Alert."""
from concurrent.futures import ThreadPoolExecutor
from datetime import datetime, timezone

from . import clients


def resumen(token: str, comunidad_id, horas: int) -> dict:
    # Las 4 consultas son independientes: se hacen en paralelo (antes eran seriales y sumaban su latencia).
    with ThreadPoolExecutor(max_workers=4) as ex:
        f_com = ex.submit(clients.get, "COMMUNITY", "/api/communities", token)
        f_ss = ex.submit(clients.get, "SENSOR", "/api/sensors/summary", token, {"comunidadId": comunidad_id})
        f_rd = ex.submit(clients.get, "SENSOR", "/api/sensors/readings/dashboard", token, {"comunidadId": comunidad_id, "horas": horas})
        f_al = ex.submit(clients.get, "ALERT", "/api/alerts/summary", token, {"comunidadId": comunidad_id})
        com, ss, rd, al = f_com.result(), f_ss.result(), f_rd.result(), f_al.result()
    if comunidad_id:
        com = [c for c in com if c["comunidadId"] == comunidad_id]
    return {
        "totalComunidades": len(com), "comunidadesActivas": sum(1 for c in com if c.get("estado")),
        "sensoresActivos": ss["sensoresActivos"], "sensoresInactivos": ss["sensoresInactivos"],
        "sensoresSinComunicacion": ss["sinComunicacion"],
        "alertasActivas": al["alertasActivas"], "nivelRiesgoGlobal": al["nivelRiesgoGlobal"], "alertasPorNivel": al["alertasPorNivel"],
        "alertasPorFenomeno": al.get("alertasPorFenomeno", []),
        "alertasActivasRecientes": al["alertasActivasRecientes"], "valoresActuales": rd["valoresActuales"], "evolucion": rd["evolucion"],
        "eventosRecientes": al["eventosRecientes"], "generadoEn": datetime.now(timezone.utc).isoformat().replace("+00:00", "Z"),
    }
