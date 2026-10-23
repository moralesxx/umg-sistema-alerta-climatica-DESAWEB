"""Indicadores del dashboard (sección 15): se alimentan de las APIs de Community, Sensor y Alert."""
from datetime import datetime, timezone

from . import clients


def resumen(token: str, comunidad_id, horas: int) -> dict:
    com = clients.get("COMMUNITY", "/api/communities", token)
    if comunidad_id:
        com = [c for c in com if c["comunidadId"] == comunidad_id]
    ss = clients.get("SENSOR", "/api/sensors/summary", token, {"comunidadId": comunidad_id})
    rd = clients.get("SENSOR", "/api/sensors/readings/dashboard", token, {"comunidadId": comunidad_id, "horas": horas})
    al = clients.get("ALERT", "/api/alerts/summary", token, {"comunidadId": comunidad_id})
    return {
        "totalComunidades": len(com), "comunidadesActivas": sum(1 for c in com if c.get("estado")),
        "sensoresActivos": ss["sensoresActivos"], "sensoresInactivos": ss["sensoresInactivos"],
        "sensoresSinComunicacion": ss["sinComunicacion"],
        "alertasActivas": al["alertasActivas"], "nivelRiesgoGlobal": al["nivelRiesgoGlobal"], "alertasPorNivel": al["alertasPorNivel"],
        "alertasPorFenomeno": al.get("alertasPorFenomeno", []),
        "alertasActivasRecientes": al["alertasActivasRecientes"], "valoresActuales": rd["valoresActuales"], "evolucion": rd["evolucion"],
        "eventosRecientes": al["eventosRecientes"], "generadoEn": datetime.now(timezone.utc).isoformat().replace("+00:00", "Z"),
    }
