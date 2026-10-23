"""Audit Service — bitácora de auditoría (FastAPI + MongoDB).

Solo los servicios internos (rol "Servicio") pueden escribir; solo el
Administrador puede consultar. Stateless: todo el estado vive en MongoDB.
"""
import itertools
import os
import signal
from contextlib import asynccontextmanager
from datetime import datetime, timezone
from typing import Optional

from fastapi import Depends, FastAPI, Query
from fastapi.exceptions import RequestValidationError
from fastapi.responses import JSONResponse
from pydantic import BaseModel, Field
from pymongo import DESCENDING, MongoClient, ReturnDocument

from .security import Principal, require_roles

_state = {"db": None}


def get_db():
    if _state["db"] is None:
        client = MongoClient(os.environ.get("MONGO_URI", "mongodb://mongo:27017"), serverSelectionTimeoutMS=3000)
        _state["db"] = client[os.environ.get("MONGO_DB", "audit_db")]
        _state["db"].bitacora.create_index([("fechaHora", DESCENDING)])
        _state["db"].bitacora.create_index("usuarioId")
    return _state["db"]


@asynccontextmanager
async def lifespan(app: FastAPI):
    yield  # uvicorn gestiona SIGTERM (graceful shutdown); no hay estado local que vaciar.


app = FastAPI(title="Audit Service", lifespan=lifespan)


class RegistroIn(BaseModel):
    usuarioId: Optional[int] = None
    usuarioNombre: Optional[str] = None
    accion: str = Field(min_length=1, max_length=100)
    detalle: str = ""
    entidad: Optional[str] = None
    entidadId: Optional[int] = None
    servicio: Optional[str] = None


def _next_id(db) -> int:
    doc = db.counters.find_one_and_update(
        {"_id": "bitacora"}, {"$inc": {"seq": 1}}, upsert=True, return_document=ReturnDocument.AFTER
    )
    return doc["seq"]


def _out(d: dict) -> dict:
    return {
        "bitacoraId": d["bitacoraId"], "usuarioId": d.get("usuarioId"),
        "usuarioNombre": d.get("usuarioNombre"),
        "usuario": {"nombre": d.get("usuarioNombre")},
        "accion": d["accion"], "detalle": d.get("detalle", ""),
        "fechaHora": d["fechaHora"].replace(tzinfo=timezone.utc).isoformat().replace("+00:00", "Z"),
        "entidad": d.get("entidad"), "entidadId": d.get("entidadId"), "servicio": d.get("servicio"),
    }


@app.exception_handler(RequestValidationError)
async def _validation(_, exc):
    return JSONResponse(status_code=400, content={"mensaje": "Datos inválidos.", "errores": exc.errors()[:5]})


@app.get("/health")
def health():
    return {"estado": "ok"}


@app.get("/health/ready")
def ready():
    get_db().command("ping")
    return {"estado": "ok"}


@app.post("/api/audit/internal", status_code=201)
def registrar(body: RegistroIn, _: Principal = Depends(require_roles("Servicio"))):
    db = get_db()
    doc = body.model_dump()
    doc["bitacoraId"] = _next_id(db)
    doc["fechaHora"] = datetime.now(timezone.utc).replace(tzinfo=None)
    db.bitacora.insert_one(doc)
    return {"bitacoraId": doc["bitacoraId"]}


@app.get("/api/audit")
def listar(
    limite: int = Query(500, ge=1, le=2000),
    usuarioId: Optional[int] = None,
    accion: Optional[str] = None,
    _: Principal = Depends(require_roles("Administrador")),
):
    q: dict = {}
    if usuarioId is not None:
        q["usuarioId"] = usuarioId
    if accion:
        q["accion"] = accion
    docs = get_db().bitacora.find(q).sort("fechaHora", DESCENDING).limit(limite)
    return [_out(d) for d in docs]
