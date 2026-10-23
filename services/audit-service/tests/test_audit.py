import os, time
os.environ.update(JWT_KEY="k"*40, JWT_ISSUER="iss", JWT_AUDIENCE="aud")
import jwt, mongomock
from fastapi.testclient import TestClient
from app import main

main._state["db"] = mongomock.MongoClient()["t"]
client = TestClient(main.app)

def tok(role, sub="1"):
    return {"Authorization": "Bearer " + jwt.encode(
        {"sub": sub, "role": role, "iss": "iss", "aud": "aud", "exp": int(time.time()) + 600}, "k"*40, "HS256")}

def test_flow():
    assert client.post("/api/audit/internal", json={"accion": "X"}, headers=tok("Administrador")).status_code == 403
    r = client.post("/api/audit/internal", json={"accion": "LOGIN", "usuarioId": 1, "usuarioNombre": "Ana"}, headers=tok("Servicio"))
    assert r.status_code == 201
    assert client.get("/api/audit").status_code == 401
    assert client.get("/api/audit", headers=tok("Operador")).status_code == 403
    rows = client.get("/api/audit", headers=tok("Administrador")).json()
    assert rows[0]["accion"] == "LOGIN" and rows[0]["usuario"]["nombre"] == "Ana"
