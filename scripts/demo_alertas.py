#!/usr/bin/env python3
"""Simulador de alertas para la demostración (sin hardware): envía lecturas por la MISMA API que usará el Arduino.

  python scripts/demo_alertas.py rojo            # viento 95 km/h -> alerta ROJA de Tormenta
  python scripts/demo_alertas.py amarillo naranja rojo
  python scripts/demo_alertas.py todas           # recorre los niveles con pausa
  python scripts/demo_alertas.py normal          # valores normales (no genera alertas)

Lee DEVICE_API_KEY del .env (o use --clave). Códigos de la demostración: SEN-01 temperatura, SEN-02 humedad,
SEN-03 viento, SEN-04 lluvia, SEN-05 río. Con --arduino usa los códigos ARD-* del kit.
"""
import argparse, json, os, sys, time, urllib.error, urllib.request

ESCENARIOS = {
    "normal":   [("SEN-01", 24), ("SEN-02", 55), ("SEN-03", 10), ("SEN-04", 5), ("SEN-05", 1.5)],
    "amarillo": [("SEN-05", 3.6)],                         # río en alerta (inundación)
    "naranja":  [("SEN-03", 70)],                          # viento fuerte (tormenta)
    "rojo":     [("SEN-03", 95), ("SEN-05", 5.4)],         # viento extremo + desbordamiento
    "incendio": [("SEN-01", 41)],                          # temperatura extrema
    "inundacion": [("SEN-04", 160), ("SEN-05", 5.2)],
}
ARD = {"SEN-01": "ARD-TEMP", "SEN-02": "ARD-HUM", "SEN-03": "ARD-VIENTO", "SEN-04": "ARD-LLUVIA", "SEN-05": "ARD-RIO"}


def clave_env():
    for d in (".", ".."):
        try:
            for line in open(os.path.join(d, ".env"), encoding="utf-8"):
                if line.startswith("DEVICE_API_KEY="):
                    return line.split("=", 1)[1].strip()
        except OSError:
            pass
    return os.environ.get("DEVICE_API_KEY", "")


def enviar(url, clave, lecturas):
    body = json.dumps({"lecturas": [{"codigo": c, "valor": v} for c, v in lecturas]}).encode()
    r = urllib.request.Request(url, data=body, method="POST", headers={"Content-Type": "application/json", "X-Device-Key": clave})
    try:
        with urllib.request.urlopen(r, timeout=15) as resp:
            return resp.status, json.loads(resp.read() or b"{}")
    except urllib.error.HTTPError as e:
        return e.code, json.loads(e.read() or b"{}")
    except OSError as e:
        return 0, {"mensaje": str(e)}


ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
ap.add_argument("escenarios", nargs="+", choices=list(ESCENARIOS) + ["todas"])
ap.add_argument("--url", default="http://localhost:8080/api/sensors/ingest")
ap.add_argument("--clave", default=clave_env())
ap.add_argument("--arduino", action="store_true", help="usar los códigos ARD-* en lugar de SEN-*")
ap.add_argument("--pausa", type=float, default=20, help="segundos entre escenarios con 'todas'")
a = ap.parse_args()
if not a.clave:
    sys.exit("Falta la clave de dispositivo (DEVICE_API_KEY en .env o --clave).")
lista = ["normal", "amarillo", "naranja", "rojo"] if "todas" in a.escenarios else a.escenarios
for i, esc in enumerate(lista):
    lecturas = [(ARD.get(c, c) if a.arduino else c, v) for c, v in ESCENARIOS[esc]]
    st, resp = enviar(a.url, a.clave, lecturas)
    print(f"[{esc}] {lecturas} -> HTTP {st} {json.dumps(resp, ensure_ascii=False)}")
    if st == 401:
        sys.exit("Clave de dispositivo incorrecta.")
    if st == 503:
        sys.exit("La ingesta está deshabilitada: defina DEVICE_API_KEY (mínimo 16 caracteres) en .env y reinicie sensor-service.")
    if i < len(lista) - 1:
        time.sleep(a.pausa)
