import json, threading
from collections import deque
from http.server import BaseHTTPRequestHandler, HTTPServer
import bridge

def test_procesar_linea():
    assert bridge.procesar_linea("Iniciando...") is None
    assert bridge.procesar_linea('{"lecturas":[{"codigo":"A","valor":1.5},{"codigo":"B"}]}') == {"lecturas": [{"codigo": "A", "valor": 1.5}]}
    assert bridge.procesar_linea('{"lecturas":[{"codigo":"A","valor":') is None
    assert bridge.procesar_linea('{"otra":1}') is None

class H(BaseHTTPRequestHandler):
    recibido = []
    def do_POST(self):
        body = json.loads(self.rfile.read(int(self.headers["Content-Length"])))
        if self.headers.get("X-Device-Key") != "secreta":
            self.send_response(401); self.end_headers(); self.wfile.write(b'{"mensaje":"no"}'); return
        H.recibido.append(body); self.send_response(200); self.end_headers(); self.wfile.write(b'{"aceptadas":1}')
    def log_message(self, *a): pass

def test_enviar_y_cola():
    srv = HTTPServer(("127.0.0.1", 0), H); threading.Thread(target=srv.serve_forever, daemon=True).start()
    url = f"http://127.0.0.1:{srv.server_port}/api/sensors/ingest"
    p = {"lecturas": [{"codigo": "ARD-TEMP", "valor": 24.5}]}
    assert bridge.enviar(url, "mala", p)[0] == 401
    cola = deque([p, p])
    assert bridge.volcar_cola(cola, url, "secreta") and not cola and len(H.recibido) == 2
    # servidor caído: no pierde datos
    srv.shutdown(); srv.server_close()
    cola = deque([p]); assert bridge.volcar_cola(cola, url, "secreta") is False and len(cola) == 1
