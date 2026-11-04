"""Cliente del Audit Service (API). Mejor esfuerzo: un fallo de auditoría no rompe el reporte."""
import logging
import os
import threading
import time

import httpx
import jwt

logger = logging.getLogger("audit")


def _service_token() -> str:
    now = int(time.time())
    return jwt.encode({"sub": "svc:report-service", "role": "Servicio", "iss": os.environ.get("JWT_ISSUER", "AlertaClimatica.Auth"),
                       "aud": os.environ.get("JWT_AUDIENCE", "AlertaClimatica.Client"), "iat": now, "exp": now + 120},
                      os.environ.get("JWT_KEY", ""), algorithm="HS256")


def _send(payload: dict):
    try:
        httpx.post(os.environ.get("AUDIT_SERVICE_URL", "http://audit-service:8000").rstrip("/") + "/api/audit/internal",
                   json=payload, headers={"Authorization": f"Bearer {_service_token()}"}, timeout=5)
    except Exception as exc:  # noqa: BLE001
        logger.warning("No se pudo registrar auditoría: %s", exc)


def log(user, accion: str, detalle: str, entidad: str | None = None, entidad_id: int | None = None):
    try:
        uid = int(user.sub)
    except (TypeError, ValueError):
        uid = None
    threading.Thread(target=_send, daemon=True, args=({"usuarioId": uid, "usuarioNombre": user.name, "accion": accion, "detalle": detalle,
                                                        "entidad": entidad, "entidadId": entidad_id, "servicio": "report-service"},)).start()
