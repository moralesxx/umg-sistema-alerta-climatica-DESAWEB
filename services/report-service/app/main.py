"""Report Service — generación de reportes PDF (FastAPI, MongoDB/GridFS, Jinja2, WeasyPrint).

* Stateless: metadata en la colección `reports` y PDF en GridFS (nada en disco local).
* La generación es asíncrona (202 + sondeo) para soportar grandes volúmenes.
* Los datos se obtienen exclusivamente por las APIs de los otros servicios.
"""
import os
from datetime import datetime, timedelta, timezone
from typing import Any, Optional

import gridfs
from fastapi import BackgroundTasks, Depends, FastAPI, HTTPException, Query
from fastapi.exceptions import RequestValidationError
from fastapi.responses import JSONResponse, Response
from pydantic import BaseModel
from pymongo import DESCENDING, MongoClient, ReturnDocument

from . import audit, builders, clients, dashboard
from .render import html_to_pdf, render_html
from .security import Principal, current_user, require_permission

_state: dict = {"db": None, "fs": None}
STALE_AFTER = timedelta(minutes=10)


def get_db():
    if _state["db"] is None:
        client = MongoClient(os.environ.get("MONGO_URI", "mongodb://mongo:27017"), serverSelectionTimeoutMS=3000)
        _state["db"] = client[os.environ.get("MONGO_DB", "report_db")]
        _state["fs"] = gridfs.GridFS(_state["db"])
        _state["db"].reports.create_index("reportId", unique=True)
        _state["db"].reports.create_index([("generatedAt", DESCENDING)])
    return _state["db"]


app = FastAPI(title="Report Service")


@app.exception_handler(RequestValidationError)
async def _validation(_, exc):
    return JSONResponse(status_code=400, content={"mensaje": "Datos inválidos.", "errores": exc.errors()[:5]})


@app.exception_handler(clients.UpstreamError)
async def _upstream(_, exc):
    return JSONResponse(status_code=502, content={"mensaje": str(exc)})


@app.get("/health")
def health():
    return {"estado": "ok"}


@app.get("/health/ready")
def ready():
    get_db().command("ping")
    return {"estado": "ok"}


def _iso(d: Optional[datetime]):
    return d.replace(tzinfo=timezone.utc).isoformat().replace("+00:00", "Z") if d else None


def _meta_out(d: dict) -> dict:
    return {
        "reportId": d["reportId"], "type": d["type"], "typeLabel": builders.REPORTS[d["type"]][0], "generatedBy": d["generatedBy"],
        "generatedByName": d.get("generatedByName"), "generatedAt": _iso(d["generatedAt"]), "filters": d.get("filters", {}),
        "status": d["status"], "fileName": d.get("fileName"), "downloadedAt": _iso(d.get("downloadedAt")),
        "downloadCount": d.get("downloadCount", 0), "error": d.get("error"), "rowCount": d.get("rowCount"),
    }


def _next_id(db) -> str:
    year = datetime.now(timezone.utc).year
    doc = db.counters.find_one_and_update({"_id": f"reports-{year}"}, {"$inc": {"seq": 1}}, upsert=True,
                                          return_document=ReturnDocument.AFTER)
    return f"REP-{year}-{doc['seq']:06d}"


# ---------------------------------------------------------------- catálogo
@app.get("/api/reports/types")
def types(_: Principal = Depends(require_permission("reports:generate", "reports:history"))):
    return [{"type": k, "label": v[0], "filters": v[3]} for k, v in builders.REPORTS.items()]


# ---------------------------------------------------------------- generar
class GenerateIn(BaseModel):
    type: str
    filters: dict[str, Any] = {}


def _generate(report_id: str, token: str, user_name: str):
    """Tarea en segundo plano: datos -> HTML (Jinja2) -> PDF (WeasyPrint) -> GridFS."""
    db = get_db()
    doc = db.reports.find_one({"reportId": report_id})
    try:
        db.reports.update_one({"reportId": report_id}, {"$set": {"status": "PROCESSING"}})
        label, fn, fname, _ = builders.REPORTS[doc["type"]]
        data = fn(doc.get("filters", {}), token)
        filters_text = ", ".join(f"{k}={v}" for k, v in doc.get("filters", {}).items() if v not in (None, ""))
        html = render_html(data, {"reportId": report_id, "user": user_name, "filtersText": filters_text})
        pdf = html_to_pdf(html)
        file_id = _state["fs"].put(pdf, filename=fname, contentType="application/pdf", reportId=report_id)
        rows = sum(len(s["rows"]) for s in data["sections"])
        db.reports.update_one({"reportId": report_id}, {"$set": {"status": "COMPLETED", "fileName": fname, "fileId": file_id,
                                                                  "rowCount": rows, "completedAt": datetime.now(timezone.utc).replace(tzinfo=None)}})
    except Exception as exc:  # noqa: BLE001 — se registra en la metadata para que el usuario vea el motivo
        db.reports.update_one({"reportId": report_id}, {"$set": {"status": "FAILED", "error": str(exc)[:300]}})


@app.post("/api/reports", status_code=202)
def generate(body: GenerateIn, bg: BackgroundTasks, user: Principal = Depends(require_permission("reports:generate"))):
    if body.type not in builders.REPORTS:
        raise HTTPException(400, {"mensaje": "Tipo de reporte no válido."})
    f = {k: v for k, v in body.filters.items() if v not in (None, "")}
    if f.get("from") and f.get("to") and str(f["from"]) > str(f["to"]):
        raise HTTPException(400, {"mensaje": "La fecha inicial no puede ser mayor que la final."})
    db = get_db()
    doc = {"reportId": _next_id(db), "type": body.type, "generatedBy": user.sub, "generatedByName": user.name,
           "generatedAt": datetime.now(timezone.utc).replace(tzinfo=None), "filters": f, "status": "PENDING",
           "fileName": None, "downloadedAt": None, "downloadCount": 0}
    db.reports.insert_one(doc)
    audit.log(user, "GENERAR_REPORTE", f"Se solicitó el reporte {doc['reportId']} ({body.type}).", "Reporte")
    bg.add_task(_generate, doc["reportId"], user.token, user.name or user.email)
    return _meta_out(doc)


# ---------------------------------------------------------------- consulta
def _load(report_id: str, user: Principal) -> dict:
    d = get_db().reports.find_one({"reportId": report_id})
    if not d or ("Administrador" not in user.roles and d["generatedBy"] != user.sub):
        raise HTTPException(404, {"mensaje": "Reporte no encontrado."})
    if d["status"] in ("PENDING", "PROCESSING") and datetime.now(timezone.utc).replace(tzinfo=None) - d["generatedAt"] > STALE_AFTER:
        get_db().reports.update_one({"reportId": report_id}, {"$set": {"status": "FAILED", "error": "La generación excedió el tiempo máximo."}})
        d["status"], d["error"] = "FAILED", "La generación excedió el tiempo máximo."
    return d


@app.get("/api/reports")
def history(page: int = Query(1, ge=1), size: int = Query(20, ge=1, le=100), type: Optional[str] = None,
            user: Principal = Depends(require_permission("reports:history"))):
    q: dict = {}
    if "Administrador" not in user.roles:
        q["generatedBy"] = user.sub
    if type:
        q["type"] = type
    db = get_db()
    cur = db.reports.find(q).sort("generatedAt", DESCENDING).skip((page - 1) * size).limit(size)
    return {"items": [_meta_out(d) for d in cur], "total": db.reports.count_documents(q), "page": page, "size": size}


@app.get("/api/reports/dashboard/resumen")
def dashboard_resumen(comunidadId: Optional[int] = None, horas: int = Query(1, ge=1, le=24), user: Principal = Depends(current_user)):
    return dashboard.resumen(user.token, comunidadId, horas)


@app.get("/api/reports/{report_id}")
def detail(report_id: str, user: Principal = Depends(require_permission("reports:history"))):
    return _meta_out(_load(report_id, user))


@app.get("/api/reports/{report_id}/download")
def download(report_id: str, user: Principal = Depends(require_permission("reports:download"))):
    d = _load(report_id, user)
    if d["status"] != "COMPLETED" or not d.get("fileId"):
        raise HTTPException(409, {"mensaje": "El reporte aún no está disponible."})
    pdf = _state["fs"].get(d["fileId"]).read()
    get_db().reports.update_one({"reportId": report_id}, {"$set": {"downloadedAt": datetime.now(timezone.utc).replace(tzinfo=None)},
                                                         "$inc": {"downloadCount": 1}})
    audit.log(user, "DESCARGAR_REPORTE", f"Se descargó el reporte {report_id}.", "Reporte")
    return Response(pdf, media_type="application/pdf",
                    headers={"Content-Disposition": f'attachment; filename="{report_id}-{d["fileName"]}"', "Cache-Control": "no-store"})
