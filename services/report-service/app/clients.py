"""Acceso a los demás microservicios SOLO mediante sus APIs (nunca a sus bases de datos).

Se reenvía el JWT del usuario: cada servicio vuelve a validar roles y permisos.
"""
import os
from typing import Any, Optional

import httpx


class UpstreamError(Exception):
    pass


def _base(name: str) -> str:
    defaults = {"COMMUNITY": "http://community-service:8080", "SENSOR": "http://sensor-service:8080",
                "ALERT": "http://alert-service:8080"}
    return os.environ.get(f"{name}_SERVICE_URL", defaults[name]).rstrip("/")


def get(service: str, path: str, token: str, params: Optional[dict] = None) -> Any:
    params = {k: v for k, v in (params or {}).items() if v not in (None, "")}
    try:
        with httpx.Client(timeout=float(os.environ.get("UPSTREAM_TIMEOUT", "30"))) as c:
            r = c.get(_base(service) + path, params=params, headers={"Authorization": f"Bearer {token}"})
    except httpx.HTTPError as exc:
        raise UpstreamError(f"No se pudo contactar el servicio {service.lower()}: {exc}") from exc
    if r.status_code >= 400:
        raise UpstreamError(f"El servicio {service.lower()} respondió {r.status_code} en {path}.")
    return r.json()


def paged_readings(token: str, params: dict, max_rows: int, page_size: int = 2000):
    """Lecturas por lotes (RNF-REP-05). Devuelve (filas, total, truncado)."""
    rows, page, total = [], 1, 0
    while len(rows) < max_rows:
        data = get("SENSOR", "/api/sensors/readings/export", token, {**params, "page": page, "size": page_size})
        total = data.get("total", 0)
        rows.extend(data.get("items", []))
        if page * page_size >= total or not data.get("items"):
            break
        page += 1
    return rows[:max_rows], total, total > max_rows
