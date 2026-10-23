# Sistema de Alerta Climática — Fase 3 (UMG, Desarrollo Web)

Monitoreo y alerta temprana de riesgos climáticos. En la Fase 3 el monolito .NET se reingenierizó en **microservicios**
detrás de un **API Gateway**, y se añadió un **módulo de reportería PDF** generado en backend.
Detalle de la arquitectura: [`docs/ARQUITECTURA-FASE3.md`](docs/ARQUITECTURA-FASE3.md).

| Servicio | Tecnología | Base de datos |
|---|---|---|
| `services/gateway` | ASP.NET Core 10 + YARP | — |
| `services/auth-service` | Node.js · NestJS · TypeScript · JWT | PostgreSQL |
| `services/community-service` | C# · .NET 10 | PostgreSQL |
| `services/sensor-service` | C# · .NET 10 | SQL Server 2022 |
| `services/alert-service` | Java 21 · Spring Boot 3 | PostgreSQL |
| `services/report-service` | Python · FastAPI · Jinja2 · WeasyPrint | MongoDB (+GridFS) |
| `services/audit-service` | Python · FastAPI | MongoDB |
| `frontend/angular-app` | Angular 22 (+ sección Reportes) | — |
| `legacy/` | Monolito de la Fase 2 (referencia) | — |

## Ejecutar
```bash
cp .env.example .env        # cambie TODOS los secretos
docker compose up -d --build
# Frontend: http://localhost:4200   ·   Gateway (loopback): http://localhost:8080/health/services
```
Usuario inicial: `admin@alerta.com` con la contraseña `ADMIN_PASSWORD` del `.env`. Con `SEED_DEMO=true` se crean 3 comunidades y 5 sensores de ejemplo.

Desarrollo del frontend: `cd frontend/angular-app && npm install && npm start` (proxy a `localhost:8080`).

## Producción (VPS + HTTPS)
Nginx de borde con Let's Encrypt: ver `deploy/nginx-edge.conf.example` (dominio `https://sistema-climatico.example`). Reenvía al puerto del contenedor `frontend`; el gateway no se expone a Internet.

## Roles
| Módulo | Administrador | Operador | Usuario de consulta |
|---|:-:|:-:|:-:|
| Dashboard, lecturas, historial, comunidades/sensores/alertas (ver) | ✔ | ✔ | ✔ |
| Sensores y alertas (gestionar) | ✔ | ✔ | |
| Comunidades (gestionar), reglas, usuarios, bitácora | ✔ | | |
| Reportes: generar e historial | ✔ | ✔ | |
| Reportes: **descargar PDF** | ✔ | | |

## Arduino / IoT (base de la fase final)
API `POST /api/sensors/ingest` (clave `X-Device-Key`), sketches para UNO y ESP32 en `iot/`, puente USB `iot/bridge/bridge.py` (con `--simular`). Guía paso a paso: [`docs/ARDUINO-GUIA.md`](docs/ARDUINO-GUIA.md).

## Verificación y operación
```bash
ADMIN_PASSWORD='su_clave' ./scripts/smoke.sh      # prueba E2E vía gateway (login, CRUD, alerta, 8 reportes PDF)
./deploy/backup.sh                                # respaldo de las 5 bases
```
Guía de VPS: `deploy/LEEME-DESPLIEGUE.md` · Kubernetes/K3s: `deploy/k8s/` · API: `docs/postman_collection.json` · Estado de requisitos: `docs/MATRIZ-REQUISITOS-FASE3.md`.

## Pruebas automáticas incluidas
```bash
(cd services/audit-service  && pip install -r requirements.txt && pytest)
(cd services/report-service && pip install -r requirements.txt && pytest)   # genera PDFs reales
(cd services/auth-service   && npm ci && npm test)
```
