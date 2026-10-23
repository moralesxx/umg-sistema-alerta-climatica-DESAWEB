#!/usr/bin/env python3
"""Puente USB: lee las líneas JSON que imprime el Arduino UNO y las envía a la API del sistema.

  python bridge.py --puerto COM3 --url http://localhost:8080/api/sensors/ingest --clave TU_DEVICE_API_KEY
  python bridge.py --simular --url ... --clave ...      # sin Arduino: genera lecturas falsas para probar la API

Variables de entorno equivalentes: BRIDGE_PUERTO, BRIDGE_URL, BRIDGE_CLAVE.
"""
import argparse, json, os, random, sys, time, urllib.error, urllib.request
from collections import deque


def procesar_linea(linea):
    """Devuelve el dict {"lecturas":[...]} si la línea es válida; None si es ruido/depuración."""
    linea = linea.strip()
    if not linea.startswith("{"):
        return None
    try:
        d = json.loads(linea)
    except json.JSONDecodeError:
        return None
    lecturas = d.get("lecturas")
    if not isinstance(lecturas, list):
        return None
    ok = [x for x in lecturas if isinstance(x, dict) and isinstance(x.get("codigo"), str) and isinstance(x.get("valor"), (int, float))]
    return {"lecturas": ok} if ok else None


def enviar(url, clave, payload, timeout=10):
    req = urllib.request.Request(url, data=json.dumps(payload).encode(), method="POST",
                                 headers={"Content-Type": "application/json", "X-Device-Key": clave})
    try:
        with urllib.request.urlopen(req, timeout=timeout) as r:
            return r.status, json.loads(r.read() or b"{}")
    except urllib.error.HTTPError as e:
        try:
            cuerpo = json.loads(e.read() or b"{}")
        except Exception:
            cuerpo = {}
        return e.code, cuerpo
    except (urllib.error.URLError, TimeoutError, OSError) as e:
        return 0, {"mensaje": str(e)}


def volcar_cola(cola, url, clave):
    """Reintenta lo pendiente en orden; se detiene en el primer fallo de red."""
    while cola:
        status, cuerpo = enviar(url, clave, cola[0])
        if status == 0:
            return False
        cola.popleft()  # aceptado o rechazado de forma definitiva (400/401): no se reintenta
        informar(status, cuerpo)
    return True


def informar(status, cuerpo):
    if status == 200:
        rech = cuerpo.get("rechazadas") or []
        print(f"[OK] aceptadas={cuerpo.get('aceptadas')}" + (f" rechazadas={rech}" if rech else ""))
    elif status == 401:
        print("[ERROR 401] Clave de dispositivo incorrecta (revise --clave / DEVICE_API_KEY).")
    else:
        print(f"[ERROR {status}] {cuerpo.get('mensaje', cuerpo)}")


def lineas_serie(puerto, baudios):
    import serial  # pip install pyserial
    with serial.Serial(puerto, baudios, timeout=2) as s:
        print(f"Escuchando {puerto} a {baudios} baudios... (Ctrl+C para salir)")
        while True:
            raw = s.readline()
            if raw:
                yield raw.decode(errors="ignore")


def lineas_simuladas():
    print("MODO SIMULADO: generando lecturas falsas cada 5 s (Ctrl+C para salir)")
    while True:
        yield json.dumps({"lecturas": [
            {"codigo": "ARD-TEMP", "valor": round(random.uniform(20, 32), 2)},
            {"codigo": "ARD-HUM", "valor": round(random.uniform(40, 80), 2)},
            {"codigo": "ARD-AGUA", "valor": round(random.uniform(0.5, 3), 2)},
            {"codigo": "ARD-FUEGO", "valor": round(random.uniform(0, 80), 2)}]})
        time.sleep(5)


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--puerto", default=os.getenv("BRIDGE_PUERTO"), help="COM3 (Windows) o /dev/ttyACM0 (Linux/Mac)")
    ap.add_argument("--baudios", type=int, default=9600)
    ap.add_argument("--url", default=os.getenv("BRIDGE_URL", "http://localhost:8080/api/sensors/ingest"))
    ap.add_argument("--clave", default=os.getenv("BRIDGE_CLAVE"), help="valor de DEVICE_API_KEY")
    ap.add_argument("--simular", action="store_true")
    a = ap.parse_args()
    if not a.clave:
        sys.exit("Falta --clave (DEVICE_API_KEY).")
    if not a.simular and not a.puerto:
        sys.exit("Indique --puerto (o use --simular).")

    cola = deque(maxlen=200)  # si se cae la red/servidor, se guardan hasta 200 envíos y se reintentan
    try:
        for linea in (lineas_simuladas() if a.simular else lineas_serie(a.puerto, a.baudios)):
            payload = procesar_linea(linea)
            if payload is None:
                if linea.strip():
                    print("[arduino]", linea.strip())
                continue
            cola.append(payload)
            if not volcar_cola(cola, a.url, a.clave):
                print(f"[SIN CONEXIÓN] {len(cola)} envío(s) en espera; se reintentará.")
    except KeyboardInterrupt:
        print("\nPuente detenido.")


if __name__ == "__main__":
    main()
