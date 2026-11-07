#!/usr/bin/env python3
"""Verificación de seguridad de punta a punta contra el gateway (solo biblioteca estándar).

  python scripts/security_check.py --url http://localhost:8080 --admin-password 'TU_ADMIN_PASSWORD'

Reproduce las pruebas que haría un revisor con Postman: peticiones sin token, tokens falsos o vencidos,
token tras cerrar sesión, escalada de privilegios por rol, rutas internas, fuerza bruta, cabeceras y entradas maliciosas.
Termina con código 0 solo si TODAS las comprobaciones pasan.
"""
import argparse, base64, json, sys, time, urllib.error, urllib.request

ap = argparse.ArgumentParser()
ap.add_argument("--url", default="http://localhost:8080")
ap.add_argument("--admin-password", required=True)
ap.add_argument("--admin-email", default="admin@alerta.com")
ap.add_argument("--device-key", default="")
A = ap.parse_args()
URL = A.url.rstrip("/")
ok = fail = 0


def req(method, path, body=None, token=None, headers=None, raw=None):
    h = {"Content-Type": "application/json", **(headers or {})}
    if token:
        h["Authorization"] = "Bearer " + token
    data = raw if raw is not None else (json.dumps(body).encode() if body is not None else None)
    r = urllib.request.Request(URL + path, data=data, method=method, headers=h)
    try:
        with urllib.request.urlopen(r, timeout=20) as resp:
            txt = resp.read().decode() or "{}"
            return resp.status, (json.loads(txt) if txt.startswith(("{", "[")) else txt), resp.headers
    except urllib.error.HTTPError as e:
        txt = e.read().decode() or "{}"
        try:
            return e.code, json.loads(txt), e.headers
        except json.JSONDecodeError:
            return e.code, txt, e.headers
    except (urllib.error.URLError, OSError) as e:
        return 0, str(e), {}


def check(nombre, cond, detalle=""):
    global ok, fail
    if cond:
        ok += 1; print(f"  OK    {nombre}")
    else:
        fail += 1; print(f"  FALLA {nombre}  {detalle}")


def login(email, pw):
    s, b, _ = req("POST", "/api/auth/login", {"correo": email, "contrasenia": pw})
    return s, b


print("== 0. Conectividad")
s, _, _ = req("GET", "/health")
check("gateway responde /health", s == 200, f"(HTTP {s}; ¿está levantado el sistema?)")
if s != 200:
    sys.exit(1)

print("== 1. Sin token o con token inválido -> 401 en TODOS los endpoints protegidos")
RUTAS = [("GET", "/api/communities"), ("GET", "/api/sensors"), ("GET", "/api/sensors/readings"), ("GET", "/api/alerts"),
         ("GET", "/api/alerts/rules"), ("GET", "/api/alerts/events"), ("GET", "/api/alerts/summary"), ("GET", "/api/reports"),
         ("GET", "/api/reports/types"), ("GET", "/api/reports/dashboard/resumen"), ("GET", "/api/audit"), ("GET", "/api/auth/usuarios"),
         ("GET", "/api/auth/roles"), ("GET", "/api/auth/me"), ("GET", "/api/notifications/subscribers"), ("POST", "/api/communities"),
         ("POST", "/api/sensors"), ("POST", "/api/reports"), ("DELETE", "/api/alerts"), ("POST", "/api/auth/change-password")]
malas = [(m, p) for m, p in RUTAS if req(m, p, {} if m != "GET" else None)[0] != 401]
check(f"{len(RUTAS)} rutas rechazan peticiones sin token", not malas, f"aceptaron sin token: {malas}")
malas = [(m, p) for m, p in RUTAS if req(m, p, {} if m != "GET" else None, token="abc.def.ghi")[0] != 401]
check("token basura -> 401", not malas, str(malas))
none_alg = base64.urlsafe_b64encode(b'{"alg":"none","typ":"JWT"}').rstrip(b"=").decode() + "." + \
    base64.urlsafe_b64encode(json.dumps({"sub": "1", "role": "Administrador", "iss": "AlertaClimatica.Auth", "aud": "AlertaClimatica.Client",
                                          "exp": int(time.time()) + 3600, "sid": "x"}).encode()).rstrip(b"=").decode() + "."
check("token con alg=none -> 401", req("GET", "/api/auth/usuarios", token=none_alg)[0] == 401)

print("== 1b. Login con Google/GitHub (OAuth 2.0 / OIDC): el flujo es público pero no explotable")


class _SinRedireccion(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, *a, **k):
        return None   # queremos VER la redirección (302), no seguirla


def get_sin_redireccion(path):
    try:
        r = urllib.request.build_opener(_SinRedireccion).open(URL + path, timeout=20)
        return r.status, r.headers
    except urllib.error.HTTPError as e:
        return e.code, e.headers
    except (urllib.error.URLError, OSError):
        return 0, {}


s, b, h = req("GET", "/api/auth/oauth/providers")
check("la lista de proveedores es pública (sin token) y no contiene secretos", s == 200 and isinstance(b, dict) and "providers" in b and "secret" not in json.dumps(b).lower(), f"(HTTP {s}: {b})")
habilitados = b.get("providers", []) if isinstance(b, dict) else []
s, h = get_sin_redireccion("/api/auth/oauth/proveedor-inexistente/start")
check("proveedor desconocido -> redirige al login con código de error (no 500)", s == 302 and "oauth_error=proveedor_no_disponible" in (h.get("Location") or ""), f"(HTTP {s} {h.get('Location')})")
s, h = get_sin_redireccion("/api/auth/oauth/google/callback?code=falso&state=falso")
check("callback sin cookie/state válido -> rechazado con código (protección CSRF)", s == 302 and "oauth_error=" in (h.get("Location") or ""), f"(HTTP {s} {h.get('Location')})")
check("el rechazo no revela detalles internos en la URL", "stack" not in (h.get("Location") or "").lower() and len(h.get("Location") or "") < 120)
s, _, _ = req("POST", "/api/auth/oauth/google/start", {})
check("el flujo OAuth solo acepta GET (POST -> 404/405)", s in (404, 405), f"(HTTP {s})")
for prov, host in (("google", "accounts.google.com"), ("github", "github.com")):
    if prov in habilitados:
        s, h = get_sin_redireccion(f"/api/auth/oauth/{prov}/start")
        loc = h.get("Location") or ""
        cookie = h.get("Set-Cookie") or ""
        check(f"{prov}: redirige al proveedor con state, PKCE S256 y SIN client_secret", s == 302 and host in loc and "state=" in loc and "code_challenge_method=S256" in loc and "client_secret" not in loc, f"(HTTP {s} {loc[:120]})")
        check(f"{prov}: la cookie de contexto es HttpOnly y SameSite", "HttpOnly" in cookie and "SameSite=Lax" in cookie, cookie[:80])
if not habilitados:
    print("  (info) Ningún proveedor OAuth configurado (GOOGLE_*/GITHUB_* vacíos): se omiten las pruebas de /start")

print("== 2. Login, fuerza bruta e inyección")
s, adm = login(A.admin_email, A.admin_password)
check("login del administrador", s == 200 and adm.get("token"), f"(HTTP {s}: {adm})")
if s != 200:
    sys.exit(1)
check("la respuesta incluye refresh token y expiración corta", adm.get("refreshToken") and adm.get("expiresIn", 9999) <= 1800)
check("login con contraseña mala -> 401", login(A.admin_email, "incorrecta-1")[0] == 401)
s, b = login("' OR '1'='1' --", "x")
check("inyección SQL en el correo -> 401/400 (nunca 200/500)", s in (400, 401), f"(HTTP {s})")
s, b = login("%", "x")
check("comodín LIKE en el correo no entra", s in (400, 401))
codes = [req("POST", "/api/auth/login", {"correo": f"nadie{i}@x.com", "contrasenia": "x"})[0] for i in range(14)]
check("límite de intentos por IP (429 tras ~10 por minuto)", 429 in codes, f"códigos: {codes}")

print("== 3. Cerrar sesión invalida el token AL INSTANTE")
time.sleep(1)
s, sesion = login(A.admin_email, A.admin_password)
if s == 429:
    print("      (esperando 62 s a que se libere el límite de login...)"); time.sleep(62); s, sesion = login(A.admin_email, A.admin_password)
tok, rt = sesion.get("token"), sesion.get("refreshToken")
check("el token funciona antes del logout", req("GET", "/api/auth/usuarios", token=tok)[0] == 200)
check("logout responde 200", req("POST", "/api/auth/logout", {"refreshToken": rt})[0] == 200)
time.sleep(0.5)
for _ in range(2):
    time.sleep(2.5)  # supera el caché de 2 s del gateway
    s = req("GET", "/api/auth/usuarios", token=tok)[0]
check("DESPUÉS del logout el MISMO token ya no sirve (401)", s == 401, f"(HTTP {s})")
check("el refresh token tampoco renueva", req("POST", "/api/auth/refresh", {"refreshToken": rt})[0] == 401)

print("== 4. Rutas internas y superficie expuesta")
for p in ["/api/audit/internal", "/api/alerts/internal/evaluate", "/api/auth/internal/sessions/x", "/api/notifications/internal/alert"]:
    check(f"{p} oculta (404)", req("POST", p, {})[0] == 404 and req("GET", p)[0] == 404)
for p in ["/docs", "/openapi.json", "/swagger", "/actuator/env", "/actuator/heapdump"]:
    check(f"{p} no expuesto", req("GET", p)[0] in (401, 404))
s, _, h = req("GET", "/health")
check("sin cabecera Server", "Server" not in h or not h.get("Server"), f"({h.get('Server')})")
check("X-Content-Type-Options: nosniff", (h.get("X-Content-Type-Options") or "").lower() == "nosniff")
check("X-Frame-Options presente", bool(h.get("X-Frame-Options")))
s, _, h = req("OPTIONS", "/api/communities", headers={"Origin": "http://sitio-malicioso.example", "Access-Control-Request-Method": "GET"})
check("CORS no autoriza orígenes ajenos", (h.get("Access-Control-Allow-Origin") or "") not in ("*", "http://sitio-malicioso.example"))
check("cuerpo enorme rechazado (413, 400 o conexión cortada)", req("POST", "/api/auth/login", raw=b'{"x":"' + b"A" * 2_000_000 + b'"}')[0] in (413, 400, 0))

print("== 5. Registro público y roles (escalada de privilegios)")
H = adm["token"] if req("GET", "/api/auth/me", token=adm["token"])[0] == 200 else login(A.admin_email, A.admin_password)[1]["token"]
suf = str(int(time.time()))
s, reg, _ = req("POST", "/api/auth/register", {"nombre": "Visitante", "correo": f"visitante{suf}@prueba.com", "contrasenia": "Clave12345", "rolId": 1})
check("registro público creado (201)", s == 201, f"(HTTP {s}: {reg})")
check("el registro NO puede iniciar sesión hasta ser aprobado", login(f"visitante{suf}@prueba.com", "Clave12345")[0] == 401)
check("contraseña débil rechazada (400)", req("POST", "/api/auth/register", {"nombre": "x", "correo": f"d{suf}@p.com", "contrasenia": "12345"})[0] == 400)

roles = {r["nombreRol"]: r["rolId"] for r in req("GET", "/api/auth/roles", token=H)[1]}
tokens = {}
for rol in ("Operador", "Usuario de consulta"):
    mail = f"{rol[:4].lower()}{suf}@prueba.com".replace(" ", "")
    s, u, _ = req("POST", "/api/auth/usuarios/administracion", {"nombre": rol + " prueba", "correo": mail, "contrasenia": "Clave12345", "rolId": roles[rol]}, token=H)
    check(f"admin crea usuario {rol}", s in (200, 201), f"(HTTP {s}: {u})")
    s, tk = login(mail, "Clave12345")
    tokens[rol] = tk.get("token")
OP, CO = tokens.get("Operador"), tokens.get("Usuario de consulta")
if OP and CO:
    com = {"nombre": "Hack", "municipio": "m", "departamento": "d", "pais": "p", "latitud": 1, "longitud": 1, "descripcion": "x", "estado": True}
    check("consulta NO puede crear comunidades (403)", req("POST", "/api/communities", com, token=CO)[0] == 403)
    check("operador NO puede crear comunidades (403)", req("POST", "/api/communities", com, token=OP)[0] == 403)
    check("consulta NO puede crear sensores (403)", req("POST", "/api/sensors", {}, token=CO)[0] == 403)
    check("consulta NO puede atender alertas (403)", req("POST", "/api/alerts/1/atender", token=CO)[0] == 403)
    check("operador NO puede borrar el historial de alertas (403)", req("DELETE", "/api/alerts", token=OP)[0] == 403)
    check("operador NO puede crear reglas (403)", req("POST", "/api/alerts/rules", {}, token=OP)[0] == 403)
    for n, t in (("operador", OP), ("consulta", CO)):
        check(f"{n} NO ve usuarios (403)", req("GET", "/api/auth/usuarios", token=t)[0] == 403)
        check(f"{n} NO ve la bitácora (403)", req("GET", "/api/audit", token=t)[0] == 403)
        check(f"{n} NO administra notificaciones (403)", req("GET", "/api/notifications/subscribers", token=t)[0] == 403)
    check("consulta NO genera reportes (403)", req("POST", "/api/reports", {"type": "COMMUNITIES"}, token=CO)[0] == 403)
    s, r, _ = req("POST", "/api/reports", {"type": "COMMUNITIES"}, token=OP)
    check("operador SÍ genera reportes (202)", s == 202, f"(HTTP {s})")
    if s == 202:
        check("operador NO puede descargar (403)", req("GET", f"/api/reports/{r['reportId']}/download", token=OP)[0] == 403)
    check("consulta SÍ ve el dashboard (200)", req("GET", "/api/reports/dashboard/resumen", token=CO)[0] == 200)
    check("el token de un usuario no consulta sesiones internas", req("GET", "/api/auth/internal/sessions/x", token=CO)[0] in (403, 404))

print("== 6. Ingesta de dispositivos")
body = {"codigo": "NO-EXISTE", "valor": 1}
check("ingesta sin clave rechazada (401/503)", req("POST", "/api/sensors/ingest", body)[0] in (401, 503))
check("ingesta con clave incorrecta rechazada", req("POST", "/api/sensors/ingest", body, headers={"X-Device-Key": "x" * 32})[0] in (401, 503))
if A.device_key:
    check("ingesta con clave correcta pero sensor inexistente -> 400", req("POST", "/api/sensors/ingest", body, headers={"X-Device-Key": A.device_key})[0] == 400)
    check("ingesta con 51 lecturas -> 400", req("POST", "/api/sensors/ingest", {"lecturas": [{"codigo": "A", "valor": 1}] * 51}, headers={"X-Device-Key": A.device_key})[0] == 400)

print("== 6b. Claves individuales por dispositivo (revocables)")
s_, d_, _ = req("POST", "/api/sensors/devices", {"nombre": f"Prueba {suf}", "prefijo": "PRUEBA-"}, token=H)
check("el administrador registra un dispositivo y recibe su clave", s_ == 201 and str(d_.get("clave", "")).startswith("dk_"), f"(HTTP {s_}: {d_})")
if s_ == 201:
    key, did = d_["clave"], d_["dispositivoId"]
    s2, b2, _ = req("POST", "/api/sensors/ingest", {"codigo": "SEN-01", "valor": 1}, headers={"X-Device-Key": key})
    check("la clave del dispositivo funciona pero solo dentro de su alcance (400: sin permiso para ese sensor)", s2 == 400 and "permiso" in json.dumps(b2), f"(HTTP {s2}: {b2})")
    check("la clave de dispositivo NO sirve como token de usuario (401)", req("GET", "/api/sensors", token=key)[0] == 401)
    if OP:
        check("un operador NO puede crear dispositivos (403)", req("POST", "/api/sensors/devices", {"nombre": "x"}, token=OP)[0] == 403)
        check("un operador NO puede listar dispositivos (403)", req("GET", "/api/sensors/devices", token=OP)[0] == 403)
    check("el listado no revela las claves", "dk_" not in json.dumps(req("GET", "/api/sensors/devices", token=H)[1]))
    check("revocar el dispositivo (200)", req("PATCH", f"/api/sensors/devices/{did}/estado", False, token=H)[0] == 200)
    check("tras revocarlo su clave ya no funciona (401)", req("POST", "/api/sensors/ingest", {"codigo": "SEN-01", "valor": 1}, headers={"X-Device-Key": key})[0] == 401)
    req("DELETE", f"/api/sensors/devices/{did}", token=H)

print("== 7. Limpieza")
for rol in ("Operador", "Usuario de consulta"):
    pass  # los usuarios de prueba (…@prueba.com) quedan creados; desactívelos desde Usuarios si no los necesita
print(f"\nResultado: {ok} correctas, {fail} con falla")
sys.exit(0 if fail == 0 else 1)
