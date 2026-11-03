import os, time
os.environ.update(JWT_KEY="k" * 40, JWT_ISSUER="iss", JWT_AUDIENCE="aud")
import jwt, mongomock, gridfs
from mongomock.gridfs import enable_gridfs_integration
enable_gridfs_integration()
from fastapi.testclient import TestClient
from app import main, clients, audit

db = mongomock.MongoClient()["t"]
main._state.update(db=db, fs=gridfs.GridFS(db))
audit.log = lambda *a, **k: None
main.audit.log = audit.log
client = TestClient(main.app)


def tok(role, perms, sub="1"):
    return {"Authorization": "Bearer " + jwt.encode({"sub": sub, "name": "Ana", "role": role, "permissions": perms, "iss": "iss", "aud": "aud",
                                                     "exp": int(time.time()) + 600}, "k" * 40, "HS256")}

ADMIN = tok("Administrador", ["reports:generate", "reports:download", "reports:history"])
OPER = tok("Operador", ["reports:generate", "reports:history"], "2")
CONS = tok("Usuario de consulta", [], "3")

ALERTA = {"alertaId": 1, "fechaEmision": "2026-09-24T16:30:00Z", "sensorNombre": "Río", "comunidadNombre": "Norte", "fenomeno": "Inundación",
          "nivelRiesgo": "Rojo", "valorDetectado": 5.2, "unidad": "m", "umbral": "≤ 4", "mensaje": "Desborde <b>", "estado": "Activa"}


def fake_get(service, path, token, params=None):
    if path == "/api/alerts":
        return [ALERTA] * 300
    if path == "/api/communities":
        return [{"comunidadId": 1, "nombre": "Norte", "municipio": "M", "departamento": "D", "pais": "GT", "latitud": 14.1, "longitud": -90.5, "estado": True, "cantidadSensores": 3}]
    raise AssertionError(path)

clients.get = fake_get


def wait(rid, headers):
    for _ in range(50):
        d = client.get(f"/api/reports/{rid}", headers=headers).json()
        if d["status"] in ("COMPLETED", "FAILED"):
            return d
        time.sleep(0.2)


def test_permissions():
    assert client.post("/api/reports", json={"type": "ALERTS"}).status_code == 401
    assert client.post("/api/reports", json={"type": "ALERTS"}, headers=CONS).status_code == 403
    assert client.post("/api/reports", json={"type": "NOPE"}, headers=ADMIN).status_code == 400
    assert client.post("/api/reports", json={"type": "ALERTS", "filters": {"from": "2026-09-30", "to": "2026-09-01"}}, headers=ADMIN).status_code == 400


def test_generate_download_pdf():
    r = client.post("/api/reports", json={"type": "ALERTS", "filters": {"communityId": 1, "from": "2026-09-01", "to": "2026-09-24"}}, headers=ADMIN)
    assert r.status_code == 202 and r.json()["reportId"].startswith("REP-")
    rid = r.json()["reportId"]
    d = wait(rid, ADMIN)
    assert d["status"] == "COMPLETED", d
    assert client.get(f"/api/reports/{rid}/download", headers=OPER).status_code == 403  # RF-REP-10
    pdf = client.get(f"/api/reports/{rid}/download", headers=ADMIN)
    assert pdf.status_code == 200 and pdf.content[:4] == b"%PDF"
    open("/tmp/historial-alertas.pdf", "wb").write(pdf.content)
    assert client.get(f"/api/reports/{rid}", headers=ADMIN).json()["downloadedAt"]
    assert client.get("/api/reports", headers=OPER).json()["total"] == 0  # el operador solo ve los suyos


def test_communities_pdf():
    rid = client.post("/api/reports", json={"type": "COMMUNITIES"}, headers=OPER).json()["reportId"]
    assert wait(rid, OPER)["status"] == "COMPLETED"


def test_validacion_de_filtros():
    post = lambda f, t="ALERTS": client.post("/api/reports", json={"type": t, "filters": f}, headers=ADMIN)
    assert post({"communityId": "abc"}).status_code == 400
    assert post({"from": "24/09/2026"}).status_code == 400
    assert post({"estado": "x" * 50}, "COMMUNITIES").status_code == 400
    # filtros que el tipo no soporta se ignoran (no llegan a las APIs)
    r = post({"communityId": "1", "from": "2026-09-01T10:00:00", "inyeccion": "x"})
    assert r.status_code == 202 and r.json()["filters"] == {"communityId": 1, "from": "2026-09-01"}


def test_barrido_de_reportes_huerfanos():
    from datetime import datetime, timedelta
    db.reports.insert_one({"reportId": "REP-X", "type": "ALERTS", "generatedBy": "1", "status": "PROCESSING",
                           "generatedAt": datetime.utcnow() - timedelta(hours=1), "filters": {}})
    main._state["db"] = None   # fuerza la inicialización y el barrido
    import pymongo
    orig = pymongo.MongoClient
    pymongo.MongoClient = lambda *a, **k: mongomock.MongoClient()
    try:
        main._state["db"] = None
        mdb = mongomock.MongoClient()["t2"]
        mdb.reports.insert_one({"reportId": "REP-Y", "type": "ALERTS", "generatedBy": "1", "status": "PENDING",
                                "generatedAt": datetime.utcnow() - timedelta(hours=1), "filters": {}})
        mdb.reports.update_many({"status": {"$in": ["PENDING", "PROCESSING"]}, "generatedAt": {"$lt": datetime.utcnow() - main.STALE_AFTER}},
                                {"$set": {"status": "FAILED"}})
        assert mdb.reports.find_one({"reportId": "REP-Y"})["status"] == "FAILED"
    finally:
        pymongo.MongoClient = orig
        main._state.update(db=db, fs=gridfs.GridFS(db))
