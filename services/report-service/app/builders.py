"""Un constructor por reporte (RF-REP-01..08). Cada uno obtiene datos vía API y
devuelve una estructura neutra que la plantilla Jinja2 convierte en HTML/PDF."""
import os
from datetime import datetime, time, timedelta, timezone
from zoneinfo import ZoneInfo

from . import clients

TZ = ZoneInfo(os.environ.get("REPORT_TZ", "America/Guatemala"))
MAX_ROWS = int(os.environ.get("REPORT_MAX_ROWS", "20000"))


def fmt_dt(v):
    if not v:
        return "—"
    try:
        d = datetime.fromisoformat(str(v).replace("Z", "+00:00"))
        if d.tzinfo is None:
            d = d.replace(tzinfo=timezone.utc)
        return d.astimezone(TZ).strftime("%Y-%m-%d %H:%M:%S")
    except ValueError:
        return str(v)


def fmt_date(v):
    return fmt_dt(v)[:10] if v else "—"


def fmt_time(v):
    return fmt_dt(v)[11:] if v else "—"


def num(v, d=2):
    return "—" if v is None else f"{float(v):,.{d}f}"


def _utc(date_str, end=False):
    """Fecha local (YYYY-MM-DD) -> instante UTC ISO. `end` => fin de ese día (exclusivo)."""
    if not date_str:
        return None
    d = datetime.strptime(date_str[:10], "%Y-%m-%d").date()
    if end:
        d = d + timedelta(days=1)
    return datetime.combine(d, time.min, TZ).astimezone(timezone.utc).strftime("%Y-%m-%dT%H:%M:%S.000Z")


def _range(f):
    return _utc(f.get("from")), _utc(f.get("to"), end=True)


def _estado_sensor(s):
    return "Activo" if s.get("estado") else "Inactivo"


def _filters_text(f):
    return {k: v for k, v in f.items() if v not in (None, "")}


def comunidades(f, token):
    data = clients.get("COMMUNITY", "/api/communities", token, {"estado": f.get("estado"), "municipio": f.get("municipio"),
                                                              "departamento": f.get("departamento")})
    return {
        "title": "Reporte de comunidades",
        "kpis": [("Comunidades", len(data)), ("Activas", sum(1 for c in data if c.get("estado"))),
                 ("Sensores asociados", sum(c.get("cantidadSensores") or 0 for c in data))],
        "sections": [{"title": "Comunidades", "columns": ["Nombre", "Municipio", "Departamento", "País", "Coordenadas", "Estado", "Sensores"],
                      "rows": [[c["nombre"], c["municipio"], c["departamento"], c["pais"],
                                f'{num(c.get("latitud"), 6)}, {num(c.get("longitud"), 6)}',
                                "Activa" if c.get("estado") else "Inactiva", c.get("cantidadSensores", 0)] for c in data]}],
    }


def sensores(f, token):
    data = clients.get("SENSOR", "/api/sensors", token, {"comunidadId": f.get("communityId"), "tipoSensorId": f.get("sensorTypeId"),
                                                       "estado": f.get("estado")})
    return {
        "title": "Reporte de sensores",
        "kpis": [("Sensores", len(data)), ("Activos", sum(1 for s in data if s.get("estado")))],
        "sections": [{"title": "Sensores", "columns": ["Código", "Nombre", "Tipo", "Comunidad", "Ubicación", "Unidad", "Estado", "Instalación"],
                      "rows": [[s.get("codigo"), s.get("nombreSensor"), (s.get("tipoSensor") or {}).get("nombreTipo", "—"),
                                s.get("comunidadNombre") or "—", s.get("ubicacion"), (s.get("tipoSensor") or {}).get("unidadMedida", "—"),
                                _estado_sensor(s), fmt_date(s.get("fechaInstalacion"))] for s in data]}],
    }


def lecturas(f, token):
    desde, hasta = _range(f)
    rows, total, trunc = clients.paged_readings(token, {"sensorId": f.get("sensorId"), "comunidadId": f.get("communityId"),
                                                        "tipoSensorId": f.get("sensorTypeId"), "desde": desde, "hasta": hasta}, MAX_ROWS)
    note = (f"Se muestran las primeras {len(rows):,} de {total:,} lecturas. Refine los filtros para ver el resto."
            if trunc else None)
    return {
        "title": "Reporte de lecturas",
        "kpis": [("Lecturas totales", f"{total:,}")],
        "sections": [{"title": "Lecturas", "note": note,
                      "columns": ["Sensor", "Comunidad", "Fecha", "Hora", "Valor", "Unidad", "Estado"],
                      "rows": [[r.get("sensorNombre"), r.get("comunidadNombre") or "—", fmt_date(r["fechaHora"]), fmt_time(r["fechaHora"]),
                                num(r["valor"]), r.get("unidad") or "", r.get("estadoSensor") or ""] for r in rows]}],
    }


def alertas(f, token):
    desde, hasta = _range(f)
    data = clients.get("ALERT", "/api/alerts", token, {"fechaDesde": desde, "fechaHasta": hasta, "comunidadId": f.get("communityId"),
                                                     "sensorId": f.get("sensorId"), "fenomeno": f.get("phenomenon"),
                                                     "nivel": f.get("level"), "estado": f.get("estado"), "limite": MAX_ROWS})
    return {
        "title": "Historial de alertas",
        "kpis": [("Alertas", len(data))] + [(n, sum(1 for a in data if a["nivelRiesgo"] == n)) for n in ("Rojo", "Naranja", "Amarillo")],
        "sections": [{"title": "Alertas",
                      "columns": ["Fecha", "Hora", "Comunidad", "Sensor", "Fenómeno", "Nivel", "Valor", "Umbral", "Mensaje", "Estado"],
                      "rows": [[fmt_date(a["fechaEmision"]), fmt_time(a["fechaEmision"]), a.get("comunidadNombre") or "—", a.get("sensorNombre") or "—",
                                a.get("fenomeno") or "—", a["nivelRiesgo"], f'{num(a.get("valorDetectado"))} {a.get("unidad") or ""}'.strip(),
                                a.get("umbral") or "—", a["mensaje"], a["estado"]] for a in data]}],
        "level_col": 5,
    }


def eventos(f, token):
    desde, hasta = _range(f)
    data = clients.get("ALERT", "/api/alerts/events", token, {"fechaInicio": desde, "fechaFin": hasta, "comunidadId": f.get("communityId"),
                                                            "fenomeno": f.get("phenomenon"), "nivel": f.get("level"), "limite": MAX_ROWS})
    por = {}
    for e in data:
        por[e["tipoFenomeno"]] = por.get(e["tipoFenomeno"], 0) + 1
    return {
        "title": "Historial de eventos climáticos",
        "kpis": [("Eventos", len(data))] + [(k, v) for k, v in sorted(por.items())],
        "sections": [{"title": "Eventos",
                      "columns": ["Fecha", "Hora", "Fenómeno", "Nivel", "Comunidad", "Sensor", "Valor", "Estado", "Descripción"],
                      "rows": [[fmt_date(e["fechaHora"]), fmt_time(e["fechaHora"]), e["tipoFenomeno"], e.get("nivelRiesgo") or "—",
                                e.get("comunidadNombre") or "—", e.get("sensorNombre") or "—", num(e.get("valor")), e.get("estado") or "—",
                                e["descripcion"]] for e in data]}],
        "level_col": 3,
    }


def estado_sensores(f, token):
    data = clients.get("SENSOR", "/api/sensors/status", token, {"comunidadId": f.get("communityId")})
    cnt = {k: sum(1 for s in data if s["estadoOperativo"] == k) for k in ("Activo", "Inactivo", "Sin comunicación")}
    return {
        "title": "Estado de sensores",
        "kpis": [("Activos", cnt["Activo"]), ("Inactivos", cnt["Inactivo"]), ("Sin comunicación", cnt["Sin comunicación"])],
        "sections": [{"title": "Sensores",
                      "columns": ["Código", "Sensor", "Comunidad", "Estado", "Última lectura", "Último valor"],
                      "rows": [[s["codigo"], s["nombre"], s.get("comunidadNombre") or "—", s["estadoOperativo"], fmt_dt(s.get("ultimaLectura")),
                                f'{num(s.get("ultimoValor"))} {s.get("unidad") or ""}'.strip() if s.get("ultimoValor") is not None else "—"]
                               for s in data]}],
    }


def _stats_section(f, token):
    desde, hasta = _range(f)
    st = clients.get("SENSOR", "/api/sensors/readings/stats", token, {"desde": desde, "hasta": hasta, "comunidadId": f.get("communityId")})
    return {"title": "Estadísticas por variable",
            "columns": ["Variable", "Unidad", "Promedio", "Mínimo", "Máximo", "Acumulado", "Lecturas"],
            "rows": [[s["tipo"], s["unidad"], num(s["promedio"]), num(s["minimo"]), num(s["maximo"]),
                      num(s["acumulado"]) if "lluvia" in s["tipo"].lower() else "—", f'{s["total"]:,}'] for s in st]}


def estadisticas(f, token):
    desde, hasta = _range(f)
    sec = _stats_section(f, token)
    ev = clients.get("ALERT", "/api/alerts/events/estadisticas", token,
                     {"fechaInicio": desde, "fechaFin": hasta, "comunidadId": f.get("communityId")})
    alert_sec = {"title": "Alertas / eventos por nivel", "columns": ["Nivel", "Total"], "rows": [[c["nombre"], c["total"]] for c in ev["porNivel"]]}
    return {"title": "Estadísticas climáticas", "kpis": [("Eventos en el periodo", ev["total"])], "sections": [sec, alert_sec]}


def ejecutivo(f, token):
    cid = f.get("communityId")
    com = clients.get("COMMUNITY", "/api/communities", token)
    if cid:
        com = [c for c in com if str(c["comunidadId"]) == str(cid)]
    ss = clients.get("SENSOR", "/api/sensors/summary", token, {"comunidadId": cid})
    al = clients.get("ALERT", "/api/alerts/summary", token, {"comunidadId": cid})
    desde, hasta = _range(f)
    ev = clients.get("ALERT", "/api/alerts/events/estadisticas", token, {"fechaInicio": desde, "fechaFin": hasta, "comunidadId": cid})
    return {
        "title": "Reporte ejecutivo",
        "kpis": [("Nivel de riesgo global", al["nivelRiesgoGlobal"]), ("Comunidades", len(com)),
                 ("Sensores activos", ss["sensoresActivos"]), ("Sensores sin comunicación", ss["sinComunicacion"]),
                 ("Alertas activas", al["alertasActivas"]), ("Eventos (periodo)", ev["total"])],
        "sections": [
            {"title": "Alertas activas por nivel", "columns": ["Nivel", "Total"], "rows": [[c["nivel"], c["total"]] for c in al["alertasPorNivel"]]},
            {"title": "Eventos por fenómeno", "columns": ["Fenómeno", "Total"], "rows": [[c["nombre"], c["total"]] for c in ev["porFenomeno"]]},
            _stats_section(f, token),
            {"title": "Eventos recientes", "columns": ["Fecha", "Fenómeno", "Nivel", "Comunidad", "Sensor", "Estado"],
             "rows": [[fmt_dt(e["fechaHora"]), e["fenomeno"], e.get("nivelRiesgo") or "—", e.get("comunidadNombre") or "—",
                       e.get("sensorNombre") or "—", e.get("estado") or "—"] for e in al["eventosRecientes"]]},
        ],
        "level_col": None,
    }


# código -> (etiqueta, función, nombre de archivo, filtros soportados)
REPORTS = {
    "COMMUNITIES": ("Comunidades", comunidades, "reporte-comunidades.pdf", ["estado", "municipio", "departamento"]),
    "SENSORS": ("Sensores", sensores, "reporte-sensores.pdf", ["communityId", "sensorTypeId", "estado"]),
    "READINGS": ("Lecturas", lecturas, "reporte-lecturas.pdf", ["sensorId", "communityId", "sensorTypeId", "from", "to"]),
    "ALERTS": ("Alertas", alertas, "historial-alertas.pdf", ["communityId", "sensorId", "phenomenon", "level", "estado", "from", "to"]),
    "EVENT_HISTORY": ("Historial de eventos", eventos, "historial-eventos.pdf", ["communityId", "phenomenon", "level", "from", "to"]),
    "SENSOR_STATUS": ("Estado de sensores", estado_sensores, "estado-sensores.pdf", ["communityId"]),
    "CLIMATE_STATS": ("Estadísticas climáticas", estadisticas, "estadisticas-climaticas.pdf", ["communityId", "from", "to"]),
    "EXECUTIVE": ("Reporte ejecutivo", ejecutivo, "reporte-ejecutivo.pdf", ["communityId", "from", "to"]),
}
