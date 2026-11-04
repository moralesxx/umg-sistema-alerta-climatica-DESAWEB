import os, time, threading
os.environ.update(NOTIFY_WORKER="0", NOTIFY_RETRY_BASE_SECONDS="0", NOTIFY_MAX_TRIES="3", JWT_KEY="k" * 40, JWT_ISSUER="iss", JWT_AUDIENCE="aud", SMTP_HOST="127.0.0.1", SMTP_SECURITY="none", SMTP_FROM="Alertas <alertas@test.local>")
import jwt, mongomock
from aiosmtpd.controller import Controller
from fastapi.testclient import TestClient
from app import main

inbox = []
class Handler:
    async def handle_DATA(self, server, session, envelope):
        inbox.append((envelope.rcpt_tos, envelope.content.decode("utf-8", "ignore")))
        return "250 OK"
import socket
_s = socket.socket(); _s.bind(("127.0.0.1", 0)); PORT = _s.getsockname()[1]; _s.close()
smtp = Controller(Handler(), hostname="127.0.0.1", port=PORT); smtp.start()
os.environ["SMTP_PORT"] = str(PORT)

main._state["db"] = mongomock.MongoClient()["t"]
client = TestClient(main.app)
def tok(role, sub="1"):
    return {"Authorization": "Bearer " + jwt.encode({"sub": sub, "name": "Ana", "role": role, "iss": "iss", "aud": "aud", "exp": int(time.time()) + 600}, "k" * 40, "HS256")}
ADMIN, SVC, OPER = tok("Administrador"), tok("Servicio", "svc:alert"), tok("Operador")
ALERT = {"alertaId": 55, "nivelRiesgo": "Rojo", "fenomeno": "Inundación", "comunidadId": 1, "comunidadNombre": "Norte <b>", "sensorNombre": "Río",
         "valorDetectado": 5.4, "unidad": "m", "umbral": "≥ 5", "mensaje": "Riesgo de desbordamiento", "fechaEmision": "2026-10-24T10:00:00Z"}

def esperar(n, t=8):
    for _ in range(int(t * 10)):
        if len(inbox) >= n: return True
        time.sleep(0.1)
    return False

def test_permisos_y_validacion():
    assert client.get("/api/notifications/subscribers").status_code == 401
    assert client.get("/api/notifications/subscribers", headers=OPER).status_code == 403
    assert client.post("/api/notifications/internal/alert", json=ALERT, headers=ADMIN).status_code == 403   # solo servicios
    assert client.post("/api/notifications/subscribers", json={"email": "no-es-correo"}, headers=ADMIN).status_code == 400
    assert client.post("/api/notifications/subscribers", json={"email": "a@x.com", "minNivel": "Verde"}, headers=ADMIN).status_code == 400
    assert client.post("/api/notifications/subscribers", json={"email": "a@x.com\r\nBcc: x@y.com"}, headers=ADMIN).status_code == 400   # inyección de cabeceras

def test_flujo_completo_alerta_a_correo():
    r = client.post("/api/notifications/subscribers", json={"email": "Rojo@Test.com", "minNivel": "Rojo"}, headers=ADMIN)
    assert r.status_code == 201 and r.json()["email"] == "rojo@test.com"
    assert client.post("/api/notifications/subscribers", json={"email": "rojo@test.com"}, headers=ADMIN).status_code == 409
    client.post("/api/notifications/subscribers", json={"email": "todas@test.com", "minNivel": "Amarillo"}, headers=ADMIN)
    client.post("/api/notifications/subscribers", json={"email": "otracom@test.com", "minNivel": "Amarillo", "comunidadIds": [99]}, headers=ADMIN)
    d = client.post("/api/notifications/internal/alert", json=ALERT, headers=SVC)
    assert d.status_code == 202 and d.json()["destinatarios"] == 2          # el de otra comunidad no recibe
    assert d.json()["encolados"] == 2 and len(inbox) == 0                    # primero queda en cola persistente
    assert main.procesar_cola() == 2
    assert esperar(2)
    destinatarios = sorted(x[0][0] for x in inbox)
    assert destinatarios == ["rojo@test.com", "todas@test.com"]
    cuerpo = inbox[0][1]
    assert "ALERTA ROJO" in cuerpo.upper() and "Norte &lt;b&gt;" in cuerpo or "Norte <b>" not in cuerpo   # HTML escapado
    # una alerta Amarilla no llega al suscriptor "solo Rojo"
    n = len(inbox)
    ya = dict(ALERT, alertaId=56, nivelRiesgo="Amarillo")
    assert client.post("/api/notifications/internal/alert", json=ya, headers=SVC).json()["destinatarios"] == 1
    main.procesar_cola()
    assert esperar(n + 1)
    # no se repite la misma alerta
    client.post("/api/notifications/internal/alert", json=ALERT, headers=SVC); main.procesar_cola(); time.sleep(0.5)
    assert len(inbox) == n + 1
    log = client.get("/api/notifications/log", headers=ADMIN).json()
    assert {x["estado"] for x in log} == {"SENT"}

def test_correo_de_prueba_y_config():
    inbox.clear()
    r = client.post("/api/notifications/test", json={"email": "prueba@test.com"}, headers=ADMIN).json()
    assert r["resultados"][0]["enviado"] is True and esperar(1)
    assert "[PRUEBA]" in inbox[0][1]
    assert "password" not in str(client.get("/api/notifications/config", headers=ADMIN).json()).lower()

def test_cola_persistente_reintenta_y_falla_con_motivo():
    from datetime import datetime
    from datetime import datetime
    db = main.get_db(); db.outbox.delete_many({}); inbox.clear()
    main.RETRY_BASE = 3600                                                   # espera larga entre reintentos (como en producción)
    adelantar = lambda: db.outbox.update_many({"status": "PENDING"}, {"$set": {"nextAt": datetime.utcnow()}})
    os.environ["SMTP_PORT"] = "1"                                            # SMTP caído
    client.post("/api/notifications/subscribers", json={"email": "cola@test.com", "minNivel": "Amarillo"}, headers=ADMIN)
    ev = dict(ALERT, alertaId=900)
    assert client.post("/api/notifications/internal/alert", json=ev, headers=SVC).json()["encolados"] >= 1
    main.procesar_cola()
    item = db.outbox.find_one({"alertaId": 900, "email": "cola@test.com"})
    assert item["status"] == "PENDING" and item["tries"] == 1 and item["lastError"]    # sigue en cola, no se pierde
    assert client.get("/api/notifications/config", headers=ADMIN).json()["pendientes"] >= 1
    assert main.procesar_cola() == 0                                         # aún no toca reintentar (nextAt en el futuro)
    os.environ["SMTP_PORT"] = str(PORT)                                      # el SMTP vuelve
    adelantar(); main.procesar_cola()
    assert db.outbox.find_one({"alertaId": 900, "email": "cola@test.com"})["status"] == "SENT" and esperar(1)
    # tras agotar los intentos queda FAILED con el motivo
    os.environ["SMTP_PORT"] = "1"
    client.post("/api/notifications/internal/alert", json=dict(ALERT, alertaId=901), headers=SVC)
    for _ in range(3): adelantar(); main.procesar_cola()
    assert db.outbox.find_one({"alertaId": 901, "email": "cola@test.com"})["status"] == "FAILED"
    assert any(x["estado"] == "FAILED" and "intentos" in (x["error"] or "") for x in client.get("/api/notifications/log", headers=ADMIN).json())
    os.environ["SMTP_PORT"] = str(PORT); main.RETRY_BASE = 0


def test_envio_interrumpido_vuelve_a_la_cola():
    from datetime import datetime, timedelta
    db = main.get_db(); inbox.clear()
    db.outbox.insert_one({"email": "huerfano@test.com", "alertaId": 950, "subject": "s", "html": "<b>x</b>", "text": "x", "status": "SENDING",
                          "tries": 0, "nextAt": datetime.utcnow(), "createdAt": datetime.utcnow(), "leaseUntil": datetime.utcnow() - timedelta(minutes=5)})
    main.procesar_cola()
    assert db.outbox.find_one({"alertaId": 950})["status"] == "SENT" and esperar(1)


def test_fallo_smtp_queda_registrado():
    os.environ["SMTP_PORT"] = "1"      # nadie escucha
    r = client.post("/api/notifications/test", json={"email": "x@test.com"}, headers=ADMIN).json()
    assert r["resultados"][0]["enviado"] is False and r["resultados"][0]["error"]
    os.environ["SMTP_PORT"] = str(PORT)
