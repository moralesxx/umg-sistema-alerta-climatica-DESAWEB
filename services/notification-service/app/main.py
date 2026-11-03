"""Notification Service — avisos de alertas por correo (FastAPI + MongoDB + SMTP).

* El Alert Service llama a POST /api/notifications/internal/alert (rol Servicio) cuando se crea una alerta.
* Los suscriptores (correo, nivel mínimo, comunidades) los administra el Administrador.
* Cada envío queda registrado (SENT / FAILED / SKIPPED) y hay un límite por destinatario para evitar inundar la bandeja.
"""
import logging
import os
import re
import threading
from contextlib import asynccontextmanager
from datetime import datetime, timedelta, timezone
from typing import Optional

from fastapi import Depends, FastAPI, HTTPException, Query
from fastapi.exceptions import RequestValidationError
from fastapi.responses import JSONResponse
from pydantic import BaseModel, ConfigDict, Field
from pymongo import DESCENDING, MongoClient, ReturnDocument

from . import mailer
from .security import Principal, require_roles
from .templates import render_alert

LEVELS = {"Verde": 0, "Amarillo": 1, "Naranja": 2, "Rojo": 3}
EMAIL_RE = re.compile(r"^[^\s@<>\",;]+@[^\s@<>\",;]+\.[^\s@<>\",;]+$")
MAX_PER_HOUR = int(os.environ.get("NOTIFY_MAX_PER_HOUR", "30"))
MAX_TRIES = int(os.environ.get("NOTIFY_MAX_TRIES", "5"))
RETRY_BASE = int(os.environ.get("NOTIFY_RETRY_BASE_SECONDS", "15"))
LEASE_SECONDS = 90
logger = logging.getLogger("notifications")
_state = {"db": None}


def get_db():
    if _state["db"] is None:
        client = MongoClient(os.environ.get("MONGO_URI", "mongodb://mongo:27017"), serverSelectionTimeoutMS=3000)
        db = client[os.environ.get("MONGO_DB", "notification_db")]
        db.subscribers.create_index("email", unique=True)
        db.log.create_index([("fecha", DESCENDING)])
        db.log.create_index([("alertaId", 1), ("email", 1)])
        db.outbox.create_index([("status", 1), ("nextAt", 1)])
        db.outbox.create_index([("alertaId", 1), ("email", 1)])
        _state["db"] = db
        _seed_defaults(db)
    return _state["db"]


def _seed_defaults(db):
    """NOTIFY_DEFAULT_RECIPIENTS="a@x.com,b@y.com" crea suscriptores iniciales (solo si no hay ninguno)."""
    raw = os.environ.get("NOTIFY_DEFAULT_RECIPIENTS", "")
    if db.subscribers.count_documents({}) > 0 or not raw.strip():
        return
    for email in [e.strip().lower() for e in raw.split(",") if e.strip()]:
        if EMAIL_RE.match(email):
            db.subscribers.insert_one({"subId": _next_id(db), "email": email, "nombre": email.split("@")[0], "minNivel": "Amarillo",
                                       "comunidadIds": [], "activo": True, "creado": _now()})


def _now() -> datetime:
    return datetime.now(timezone.utc).replace(tzinfo=None)


def _next_id(db) -> int:
    return db.counters.find_one_and_update({"_id": "subs"}, {"$inc": {"seq": 1}}, upsert=True, return_document=ReturnDocument.AFTER)["seq"]


_stop = threading.Event()


def _worker():
    """Procesa la cola de correos cada pocos segundos (persistente en MongoDB: sobrevive a reinicios)."""
    while not _stop.is_set():
        try:
            procesar_cola()
        except Exception as exc:  # noqa: BLE001 — la cola nunca debe detener al servicio
            logger.warning("Error procesando la cola de correos: %s", exc)
        _stop.wait(2)


@asynccontextmanager
async def lifespan(app: FastAPI):
    t = None
    if os.environ.get("NOTIFY_WORKER", "1") != "0":
        _stop.clear()
        t = threading.Thread(target=_worker, name="outbox-worker", daemon=True)
        t.start()
    yield
    _stop.set()   # apagado ordenado


app = FastAPI(title="Notification Service", docs_url=None, redoc_url=None, openapi_url=None, lifespan=lifespan)


@app.exception_handler(RequestValidationError)
async def _validation(_, exc):
    return JSONResponse(status_code=400, content={"mensaje": "Datos inválidos.", "errores": [e.get("msg") for e in exc.errors()[:5]]})


@app.get("/health")
def health():
    return {"estado": "ok"}


@app.get("/health/ready")
def ready():
    get_db().command("ping")
    return {"estado": "ok"}


def _iso(d: Optional[datetime]):
    return d.replace(tzinfo=timezone.utc).isoformat().replace("+00:00", "Z") if d else None


def _sub_out(d: dict) -> dict:
    return {"subId": d["subId"], "email": d["email"], "nombre": d.get("nombre"), "minNivel": d["minNivel"],
            "comunidadIds": d.get("comunidadIds", []), "activo": d["activo"], "creado": _iso(d.get("creado"))}


# ------------------------------------------------------------------ suscriptores (Administrador)
class SubIn(BaseModel):
    model_config = ConfigDict(extra="ignore")
    email: str = Field(min_length=5, max_length=120)
    nombre: Optional[str] = Field(default=None, max_length=80)
    minNivel: str = "Amarillo"
    comunidadIds: list[int] = Field(default_factory=list, max_length=100)
    activo: bool = True


def _validate(body: SubIn) -> SubIn:
    body.email = body.email.strip().lower()
    if not EMAIL_RE.match(body.email):
        raise HTTPException(400, {"mensaje": "El correo no tiene un formato válido."})
    if body.minNivel not in ("Amarillo", "Naranja", "Rojo"):
        raise HTTPException(400, {"mensaje": "El nivel mínimo debe ser Amarillo, Naranja o Rojo."})
    return body


@app.get("/api/notifications/subscribers")
def list_subs(_: Principal = Depends(require_roles("Administrador"))):
    return [_sub_out(d) for d in get_db().subscribers.find().sort("subId", 1)]


@app.post("/api/notifications/subscribers", status_code=201)
def add_sub(body: SubIn, user: Principal = Depends(require_roles("Administrador"))):
    body = _validate(body)
    db = get_db()
    if db.subscribers.find_one({"email": body.email}):
        raise HTTPException(409, {"mensaje": "Ese correo ya está suscrito."})
    doc = {"subId": _next_id(db), "email": body.email, "nombre": (body.nombre or body.email.split("@")[0]).strip(), "minNivel": body.minNivel,
           "comunidadIds": body.comunidadIds, "activo": body.activo, "creado": _now()}
    db.subscribers.insert_one(doc)
    return _sub_out(doc)


@app.put("/api/notifications/subscribers/{sub_id}")
def upd_sub(sub_id: int, body: SubIn, user: Principal = Depends(require_roles("Administrador"))):
    body = _validate(body)
    db = get_db()
    other = db.subscribers.find_one({"email": body.email, "subId": {"$ne": sub_id}})
    if other:
        raise HTTPException(409, {"mensaje": "Ese correo ya está suscrito."})
    r = db.subscribers.find_one_and_update({"subId": sub_id}, {"$set": {"email": body.email, "nombre": body.nombre, "minNivel": body.minNivel,
                                           "comunidadIds": body.comunidadIds, "activo": body.activo}}, return_document=ReturnDocument.AFTER)
    if not r:
        raise HTTPException(404, {"mensaje": "Suscriptor no encontrado."})
    return _sub_out(r)


@app.patch("/api/notifications/subscribers/{sub_id}/estado")
def toggle_sub(sub_id: int, activo: bool, user: Principal = Depends(require_roles("Administrador"))):
    r = get_db().subscribers.find_one_and_update({"subId": sub_id}, {"$set": {"activo": activo}}, return_document=ReturnDocument.AFTER)
    if not r:
        raise HTTPException(404, {"mensaje": "Suscriptor no encontrado."})
    return _sub_out(r)


@app.delete("/api/notifications/subscribers/{sub_id}")
def del_sub(sub_id: int, user: Principal = Depends(require_roles("Administrador"))):
    if get_db().subscribers.delete_one({"subId": sub_id}).deleted_count == 0:
        raise HTTPException(404, {"mensaje": "Suscriptor no encontrado."})
    return {"mensaje": "Suscriptor eliminado."}


# ------------------------------------------------------------------ envío
def _log(db, email: str, alerta_id, asunto: str, estado: str, error: str = ""):
    db.log.insert_one({"fecha": _now(), "email": email, "alertaId": alerta_id, "asunto": asunto, "estado": estado, "error": error})


def _enqueue(alert: dict, recipients: list[dict]) -> int:
    """Guarda un correo PENDIENTE por destinatario en MongoDB (respeta el límite por hora y no repite la misma alerta)."""
    db = get_db()
    fecha = alert.get("fechaEmision") or _iso(_now())
    encolados = 0
    for sub in recipients:
        email = sub["email"]
        hace_1h = _now() - timedelta(hours=1)
        recientes = db.outbox.count_documents({"email": email, "status": {"$in": ["SENT", "PENDING", "SENDING"]}, "createdAt": {"$gte": hace_1h}})
        if recientes >= MAX_PER_HOUR:
            _log(db, email, alert.get("alertaId"), "", "SKIPPED", f"Límite de {MAX_PER_HOUR} correos/hora alcanzado")
            continue
        if alert.get("alertaId") is not None and db.outbox.find_one({"email": email, "alertaId": alert["alertaId"], "status": {"$in": ["SENT", "PENDING", "SENDING"]}}):
            continue
        subject, html, text = render_alert(alert, sub["minNivel"], fecha)
        db.outbox.insert_one({"email": email, "alertaId": alert.get("alertaId"), "subject": subject, "html": html, "text": text,
                              "status": "PENDING", "tries": 0, "nextAt": _now(), "createdAt": _now(), "lastError": ""})
        encolados += 1
    return encolados


def procesar_cola(limite: int = 20) -> int:
    """Envía los correos pendientes. Ante un fallo reintenta con espera creciente (hasta NOTIFY_MAX_TRIES) y deja el motivo registrado."""
    db = get_db()
    ahora = _now()
    # un envío "SENDING" cuyo arrendamiento venció (servicio reiniciado a mitad) vuelve a la cola
    db.outbox.update_many({"status": "SENDING", "leaseUntil": {"$lt": ahora}}, {"$set": {"status": "PENDING"}})
    hechos = 0
    while hechos < limite:
        item = db.outbox.find_one_and_update({"status": "PENDING", "nextAt": {"$lte": _now()}},
                                             {"$set": {"status": "SENDING", "leaseUntil": _now() + timedelta(seconds=LEASE_SECONDS)}},
                                             return_document=ReturnDocument.AFTER)
        if not item:
            break
        ok, err = mailer.send_email(item["email"], item["subject"], item["html"], item["text"])
        tries = item.get("tries", 0) + 1
        if ok:
            db.outbox.update_one({"_id": item["_id"]}, {"$set": {"status": "SENT", "tries": tries, "sentAt": _now(), "lastError": ""}})
            _log(db, item["email"], item.get("alertaId"), item["subject"], "SENT")
        elif tries >= MAX_TRIES:
            db.outbox.update_one({"_id": item["_id"]}, {"$set": {"status": "FAILED", "tries": tries, "lastError": err}})
            _log(db, item["email"], item.get("alertaId"), item["subject"], "FAILED", f"{err} (tras {tries} intentos)")
        else:
            espera = min(RETRY_BASE * (2 ** (tries - 1)), 600)
            db.outbox.update_one({"_id": item["_id"]}, {"$set": {"status": "PENDING", "tries": tries, "lastError": err, "nextAt": _now() + timedelta(seconds=espera)}})
        hechos += 1
    return hechos


class AlertIn(BaseModel):
    model_config = ConfigDict(extra="ignore")
    alertaId: Optional[int] = None
    nivelRiesgo: str
    fenomeno: Optional[str] = None
    comunidadId: Optional[int] = None
    comunidadNombre: Optional[str] = None
    sensorNombre: Optional[str] = None
    valorDetectado: Optional[float] = None
    unidad: Optional[str] = None
    umbral: Optional[str] = None
    mensaje: str = Field(max_length=500)
    fechaEmision: Optional[str] = None


@app.post("/api/notifications/internal/alert", status_code=202)
def notify_alert(alert: AlertIn, _: Principal = Depends(require_roles("Servicio"))):
    peso = LEVELS.get(alert.nivelRiesgo, 0)
    if peso == 0:
        return {"destinatarios": 0}
    q = {"activo": True}
    subs = [s for s in get_db().subscribers.find(q)
            if LEVELS[s["minNivel"]] <= peso and (not s.get("comunidadIds") or alert.comunidadId in s["comunidadIds"])]
    encolados = _enqueue(alert.model_dump(), subs) if subs else 0
    return {"destinatarios": len(subs), "encolados": encolados}


class TestIn(BaseModel):
    model_config = ConfigDict(extra="ignore")
    email: Optional[str] = None


@app.post("/api/notifications/test")
def send_test(body: TestIn, user: Principal = Depends(require_roles("Administrador"))):
    """Envía un correo de prueba al correo indicado (o a todos los suscriptores activos) y devuelve el resultado real del SMTP."""
    db = get_db()
    if body.email:
        email = body.email.strip().lower()
        if not EMAIL_RE.match(email):
            raise HTTPException(400, {"mensaje": "El correo no tiene un formato válido."})
        targets = [{"email": email, "minNivel": "Amarillo"}]
    else:
        targets = [s for s in db.subscribers.find({"activo": True})]
    if not targets:
        raise HTTPException(400, {"mensaje": "No hay destinatarios. Agregue un suscriptor o indique un correo."})
    demo = {"alertaId": 0, "nivelRiesgo": "Naranja", "fenomeno": "Prueba", "comunidadNombre": "Comunidad de prueba",
            "sensorNombre": "Sensor de prueba", "valorDetectado": 42.0, "unidad": "°C", "umbral": "≥ 38",
            "mensaje": "Este es un correo de prueba del sistema de alertas. Si lo recibe, las notificaciones están funcionando."}
    resultados = []
    for t in targets:
        subject, html, text = render_alert(demo, t["minNivel"], _iso(_now()))
        ok, err = mailer.send_email(t["email"], "[PRUEBA] " + subject, html, text)
        _log(db, t["email"], None, "[PRUEBA] " + subject, "SENT" if ok else "FAILED", err)
        resultados.append({"email": t["email"], "enviado": ok, "error": err or None})
    return {"resultados": resultados}


@app.get("/api/notifications/log")
def get_log(limite: int = Query(50, ge=1, le=500), _: Principal = Depends(require_roles("Administrador"))):
    return [{"fecha": _iso(d["fecha"]), "email": d["email"], "alertaId": d.get("alertaId"), "asunto": d.get("asunto"),
             "estado": d["estado"], "error": d.get("error") or None} for d in get_db().log.find().sort("fecha", DESCENDING).limit(limite)]


@app.get("/api/notifications/config")
def get_config(_: Principal = Depends(require_roles("Administrador"))):
    c = mailer.smtp_config()
    pendientes = get_db().outbox.count_documents({"status": {"$in": ["PENDING", "SENDING"]}})
    return {"host": c["host"], "port": c["port"], "seguridad": c["security"], "autenticado": bool(c["user"]), "remitente": c["from"], "pendientes": pendientes}
