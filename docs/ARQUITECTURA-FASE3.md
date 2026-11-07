# Arquitectura — Fase 3

```
Internet → DNS → Nginx (HTTPS, VPS) → frontend (Angular + Nginx) ──/api──► API Gateway (ASP.NET Core 10 + YARP)
                                                                              │ valida JWT · CORS · rate limit · logs · errores JSON
   ┌──────────────┬───────────────┬──────────────┬──────────────┬────────────┴─┬──────────────┐
   │ /api/auth    │ /api/communities │ /api/sensors │ /api/alerts │ /api/reports │ /api/audit   │
   ▼              ▼               ▼              ▼              ▼              ▼
 Auth Service  Community Svc   Sensor Svc    Alert Svc     Report Svc     Audit Svc
 NestJS/TS     .NET 10         .NET 10       Spring Boot   FastAPI        FastAPI
 PostgreSQL    PostgreSQL      SQL Server    PostgreSQL    MongoDB+GridFS MongoDB
```

Lenguajes: C#, Java, TypeScript/Node.js, Python (4). Gestores: PostgreSQL, SQL Server, MongoDB (3, uno NoSQL).

## Responsabilidades y rutas
| Servicio | Prefijo | Contenido |
|---|---|---|
| Auth | `/api/auth` | `login`, `refresh`, `logout`, `register` (pendiente de aprobación), `change-password`, `me`, `usuarios[...]`, `roles` |
| Community | `/api/communities` | CRUD y estado de comunidades (con `cantidadSensores` obtenido de la API de Sensor) |
| Sensor | `/api/sensors` | sensores, `types`, `readings[...]` (+ `export`, `stats`, `dashboard`), `status`, `summary`, simulador |
| Alert | `/api/alerts` | alertas, `rules`, `events`, `summary`, motor de evaluación |
| Report | `/api/reports` | `types`, generar (202), historial, detalle, `download`, `dashboard/resumen` |
| Audit | `/api/audit` | bitácora (solo lectura por Administrador) |
| Notification | `/api/notifications` | destinatarios de correo, envío de avisos de alertas por SMTP, registro de envíos |

Rutas `…/internal/…` (p. ej. `POST /api/alerts/internal/evaluate`, `POST /api/audit/internal`) solo aceptan tokens con rol `Servicio`; el gateway además las bloquea (404).

## Comunicación entre servicios (solo por API)
* Sensor → Alert: cada lectura simulada/manual se envía a `/api/alerts/internal/evaluate` (token de servicio). Alert resuelve nombres de comunidad con la API de Community y guarda una copia desnormalizada (nombres) para no depender de otras bases.
* Sensor → Community: valida `comunidadId` y obtiene nombres. Community → Sensor: conteo de sensores. Ambos degradan con elegancia si el otro servicio no responde.
* Report → Community/Sensor/Alert: reenvía el JWT del usuario; cada servicio vuelve a validar roles. Nunca se accede a una base ajena.
* Alert → Notification: cada alerta nueva se informa por API; el servicio envía el correo (SMTP) a los suscriptores que correspondan.
* Todos → Audit: `POST /api/audit/internal` (mejor esfuerzo, asíncrono).

## Seguridad
JWT HS256 de 15 min emitido por Auth (`iss`, `aud`, `sub`, `sid`, `name`, `email`, `role`, `permissions`, `exp`) + refresh token rotativo. El gateway verifica que la sesión (`sid`) siga vigente: el logout invalida el token al instante. Ver `docs/SEGURIDAD.md`. Gateway y cada servicio validan Issuer, Audience, firma, expiración, rol y permisos. Permisos de reportes: `reports:generate` (Admin, Operador), `reports:history` (Admin, Operador), `reports:download` (solo Admin → RF-REP-10).

## Reportería
`POST /api/reports {type, filters}` → 202 y metadata `PENDING`; una tarea de fondo obtiene datos por API (lecturas por lotes de 2 000, tope `REPORT_MAX_ROWS`), renderiza HTML con Jinja2, lo convierte a PDF con WeasyPrint (encabezado, logo, usuario, fecha, tablas, numeración "Página x de y", pie) y guarda el PDF en GridFS. El estado queda en `reports` (`PENDING→PROCESSING→COMPLETED|FAILED`, `downloadedAt`). Tipos: `COMMUNITIES, SENSORS, READINGS, ALERTS, EVENT_HISTORY, SENSOR_STATUS, CLIMATE_STATS, EXECUTIVE` (RF-REP-01…08).

## Preparación para Kubernetes/K3s y OAuth2/OIDC
* Contenedores stateless (estado solo en BD/GridFS), usuario no root, configuración 100 % por variables de entorno → `ConfigMap` (URLs, TZ) y `Secret` (`JWT_KEY`, contraseñas).
* Health: `GET /health` en todos (liveness/readiness); Report añade `/health/ready` (ping a Mongo); gateway `/health/services`.
* Graceful shutdown: `stop_grace_period`/`ShutdownTimeout` 30 s, `server.shutdown=graceful`, `--timeout-graceful-shutdown`, `enableShutdownHooks()`.
* Volúmenes nombrados ↔ `PersistentVolumeClaim`. Direcciones de servicios por DNS (`http://<servicio>:8080`) ↔ `Service` de K8s.
* Auth: **login con Google (OpenID Connect) y GitHub (OAuth 2.0)** con Authorization Code + PKCE (`OAuthService`, `OAuthController`). La identidad verificada por el proveedor se asocia en `identidades_externas (proveedor, subject, usuario)` y se canjea por la **misma sesión propia** (`UsersService.emitirSesion`: JWT + `sid` + refresh rotativo), así gateway y servicios no cambian. Guía, flujo y seguridad: `docs/OAUTH-OIDC.md`. Pasar a RS256 + JWKS propio queda como mejora independiente.

## Limitaciones conocidas
* El simulador corre dentro de Sensor Service: con varias réplicas, habilitarlo en una sola (`Simulador__Habilitado=false` en las demás).
* Esquemas con `EnsureCreated`/`synchronize`/`ddl-auto=update` (sin migraciones versionadas).
* Los datos de la Fase 2 (monolito en `legacy/`) no se migran automáticamente; los hashes de contraseña cambian de PBKDF2 a bcrypt.
