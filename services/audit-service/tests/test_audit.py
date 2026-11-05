import sys
import os

sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), '..')))

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


def test_filtros_y_paginacion():
    for i in range(5):
        client.post("/api/audit/internal", json={"accion": f"A{i}", "servicio": "x" if i % 2 else "y"}, headers=tok("Servicio"))
    h = tok("Administrador")
    assert len(client.get("/api/audit?limite=2", headers=h).json()) == 2
    assert len(client.get("/api/audit?servicio=x", headers=h).json()) == 2
    assert client.get("/api/audit?desde=2999-01-01", headers=h).json() == []
    assert client.get("/api/audit?desde=2000-01-01&hasta=2999-12-31", headers=h).json() != []
    assert client.get("/api/audit?desde=ayer", headers=h).status_code == 400
