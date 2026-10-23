# Matriz de requisitos — Fase 3

Estado: ✅ implementado y probado automáticamente · 🟡 implementado, pendiente de validar al levantar el stack (`docker compose up` + `scripts/smoke.sh`) · ⬜ requiere acción del equipo (infraestructura/documentos).

| # | Requisito | Dónde | Estado |
|---|---|---|---|
| 1 | Reingeniería a microservicios | `services/*`, monolito en `legacy/` | 🟡 |
| 2 | API Gateway (enrutamiento, JWT, CORS, rate limiting, logging, errores, ocultar infraestructura) | `services/gateway` | 🟡 |
| 3 | Autenticación centralizada + JWT | `services/auth-service` | ✅ (login, claims, roles) |
| 4 | ≥3 lenguajes backend (C#, Java, TypeScript, Python) | los 6 servicios | ✅ |
| 5 | ≥3 gestores de BD + 1 NoSQL (PostgreSQL, SQL Server, MongoDB) | `docker-compose.yml` | 🟡 |
| 6 | Report Service independiente, PDF en backend (Jinja2 + WeasyPrint) | `services/report-service` | ✅ (PDF reales generados en pruebas) |
| 7 | Reportes integrados al dashboard (seleccionar, filtrar, generar/descargar, historial) | `frontend/.../reportes` | ✅ `ng build` de producción compila · 🟡 recorrido manual |
| 8 | RF-REP-01…08 (8 tipos de reporte) | `report-service/app/builders.py` | ✅ lógica · 🟡 datos reales |
| 9 | RF-REP-09 historial con metadata (Id, tipo, usuario, fecha, parámetros, estado, archivo, descarga) | colección `reports` | ✅ |
| 10 | RF-REP-10 descarga solo administradores | permiso `reports:download` | ✅ (prueba automática) |
| 11 | Seguridad: Issuer, Audience, firma, expiración, roles, permisos | todos los servicios | ✅ Python/Auth · 🟡 .NET/Java |
| 12 | RNF-REP-03 presentación (logo, encabezado, nombre, fecha, usuario, tablas, numeración, pie) | `templates/report.html.j2` | ✅ |
| 13 | RNF-REP-05 grandes volúmenes (paginación, filtros, asíncrono) | lecturas por lotes de 2 000, 202 + sondeo, tope configurable | ✅ |
| 14 | Dashboard alimentado por reportería (sensores, comunidades, alertas, eventos, distribución nivel/fenómeno, estadísticas) | `report-service/app/dashboard.py`, overview | 🟡 |
| 15 | Servicios solo por API (sin acceso a BD ajena) | `UpstreamClient`, `clients.py`, `Upstream.java` | ✅ por diseño |
| 16 | Persistencia por servicio (Auth/Community/Alert PG, Sensor SQL Server, Report/Audit Mongo) | compose | 🟡 |
| 17 | Despliegue en VPS con Docker Compose + HTTPS | `docker-compose.yml`, `deploy/` | ⬜ (requiere su VPS y dominio) |
| 18 | Preparación K8s/K3s (stateless, health, env, Secrets, ConfigMaps, PVC, graceful shutdown) | `deploy/k8s/`, Dockerfiles | ✅ manifiestos válidos (YAML) · 🟡 no aplicados a un clúster |
| 19 | Auth listo para OAuth2/OIDC | `identidades_externas`, `emitirToken` | ✅ |
| 20 | Pruebas | pytest (Audit, Report), smoke Auth, JUnit (motor de alertas), `scripts/smoke.sh` E2E | ✅ las que corren aquí · 🟡 JUnit/E2E |

## Pendientes de la Fase 2 detectados y resueltos
* Guía y scripts de despliegue/respaldo citados en el README de Fase 2 pero ausentes → `deploy/LEEME-DESPLIEGUE.md`, `deploy/backup.sh`, `deploy/nginx-edge.conf.example`.
* Sin reglas de alerta iniciales (el simulador no generaba alertas en un sistema nuevo) → reglas de demostración con `SEED_DEMO=true`.
* Pruebas del motor de reglas solo en el monolito → `AlertServiceTest` (JUnit) en Alert Service.

## Para cerrar al 100 %
0. Suba el repositorio a GitHub: `.github/workflows/ci.yml` compila y prueba automáticamente .NET, Java, Node, Python y Angular.
1. `docker compose up -d --build` y corregir cualquier error de compilación.
2. `ADMIN_PASSWORD=... ./scripts/smoke.sh` (debe terminar con 0 fallidas).
3. Desplegar en el VPS (`deploy/LEEME-DESPLIEGUE.md`) y tomar capturas.
4. Entregables: README, diagramas (en `docs/ARQUITECTURA-FASE3.md`), colección Postman (`docs/postman_collection.json`).


Verificación adicional realizada: análisis sintáctico de los 9 archivos C# y 14 Java (0 errores de sintaxis; la compilación completa requiere SDK/NuGet/Maven) y validación del `docker-compose.yml` (13 servicios).
