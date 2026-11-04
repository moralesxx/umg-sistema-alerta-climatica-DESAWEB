# Registro de correcciones y mejoras (Fase 3)

## A. Seguridad

| # | Qué se encontró | Corrección aplicada | Dónde |
|---|---|---|---|
| S1 | El token seguía siendo válido después de cerrar sesión (hasta 60 min); lo detectó el ingeniero con Postman | Sesiones en el servidor: el token lleva `sid` y el gateway consulta al Auth Service en cada petición. Logout, cambio de contraseña, desactivar usuario o cambio de rol lo invalidan al instante. Token de acceso de 15 min + refresh token rotativo de un solo uso con detección de reutilización | `auth-service/src/users.service.ts`, `shared-dotnet/.../SessionRevocation.cs`, `gateway/Program.cs`, frontend `auth.service.ts` y `auth.interceptor.ts` |
| S2 | El registro público daba acceso inmediato a los datos | La cuenta se crea inactiva y un administrador la aprueba; la pantalla de registro lo informa | `users.service.ts`, `registro.ts` |
| S3 | Un Operador podía borrar todo el historial de alertas | Solo Administrador | `alert-service/.../Controllers.java` |
| S4 | Detrás del proxy el límite de intentos se aplicaba a todos los usuarios juntos (misma IP) | Nginx restaura la IP real (`realip`) y la reenvía sin que el cliente pueda falsificarla; el gateway limita por IP real. Límites: 10/min login, registro, refresh y logout; 300/min ingesta; 600/min global | `frontend/angular-app/nginx.conf`, `deploy/nginx-edge.conf.example`, `gateway/appsettings.json` |
| S5 | Sin protección por cuenta contra fuerza bruta; tiempo de respuesta distinto para correos inexistentes | Bloqueo de cuenta (5 intentos / 15 min) y comparación de contraseña siempre ejecutada | `users.service.ts` |
| S6 | Comodines `%` y `_` en el login y la búsqueda de usuarios | Correos normalizados, igualdad exacta y escape de comodines | `users.service.ts`, `seed.ts` |
| S7 | Contraseñas débiles permitidas (6 caracteres) | 8–72 caracteres con letras y números (backend y formularios); cambio de contraseña que cierra las demás sesiones | `users.service.ts`, `usuarios.ts`, `registro.ts` |
| S8 | Violación de unicidad respondía 500 | 409 con mensaje claro (Auth y servicios .NET) | `auth-service/src/main.ts`, `Clima.Shared/Common.cs` |
| S9 | Documentación de la API, cabecera `Server` y detalles de error expuestos | Deshabilitados / mensajes genéricos | `report-service`, `audit-service`, `notification-service`, `Common.cs` |
| S10 | Sin cabeceras de seguridad ni límite de tamaño de solicitud | `nosniff`, `X-Frame-Options`, `no-store`, `Permissions-Policy`; máximo 1 MB por solicitud | `gateway/Program.cs`, `nginx.conf` |
| S11 | Dependencias vulnerables (`body-parser`, `multer`, `zod`) | Actualizadas: `npm audit` de producción = 0 vulnerabilidades en Auth y frontend | `auth-service/package.json`, `frontend/angular-app/package.json` |
| S12 | Contenedores con privilegios por defecto | `no-new-privileges` y `cap_drop: ALL`; en Kubernetes usuario no root, `seccomp` y NetworkPolicy | `docker-compose.yml`, `deploy/k8s/` |
| S13 | El frontend cargaba Bootstrap desde un CDN externo | Bootstrap 5.3.2 empaquetado localmente; sin recursos de terceros | `angular.json`, `index.html`, `package.json` |
| S14 | Sin política de seguridad de contenido (CSP) | CSP estricta en Nginx (scripts propios, sin `eval`, sin frames, `object-src none`) | `nginx.conf` |
| S15 | Una sola clave compartida para todos los dispositivos IoT | Claves individuales por dispositivo: hash SHA-256, revocables, con alcance por prefijo y último contacto; pantalla «Dispositivos» (la clave se muestra una sola vez) | `sensor-service/Endpoints.cs`, `Model.cs`, `dispositivos/` |

## B. Funcionalidad

| # | Qué se encontró | Corrección aplicada | Dónde |
|---|---|---|---|
| F1 | El ingeniero pidió avisos por correo de las alertas | Notification Service (FastAPI + MongoDB + SMTP): destinatarios con nivel mínimo y comunidades, correo HTML, registro de envíos, correo de prueba, pantalla «Notificaciones». Compatible con Gmail y con Mailpit para pruebas | `services/notification-service`, `alert-service/.../Upstream.java`, `notificaciones/` |
| F2 | Los avisos por correo podían perderse si el servicio se reiniciaba | Cola persistente en MongoDB con reintentos (5, espera creciente), recuperación de envíos interrumpidos y motivo del fallo registrado | `notification-service/app/main.py` |
| F3 | Conexión del Arduino al sistema | API de ingesta `POST /api/sensors/ingest`, sketches (UNO, UNO completo con 9 sensores, ESP32), puente USB con reintentos y `--listar`, guía paso a paso y simulador de alertas sin hardware | `sensor-service`, `iot/`, `docs/ARDUINO-GUIA.md`, `scripts/demo_alertas.py` |
| F4 | Las lecturas se borraban solo si el simulador estaba activo (con hardware real la tabla crecería sin límite) | Servicio de retención independiente del simulador (30 días configurables) | `sensor-service/Simulator.cs` |
| F5 | Si el Alert Service caía, las lecturas no se evaluaban | Cola de reintento en memoria (hasta 200 lotes) y un reintento en las consultas entre servicios; la auditoría reintenta 3 veces | `sensor-service/Simulator.cs`, `Common.cs` |
| F6 | Regla de demostración de temperatura con el umbral invertido (alertas continuas) | Corregida en el código y en la regla creada | `alert-service/.../DemoSeed.java` |
| F7 | Reportes: filtros sin validar y reportes que quedaban «procesando» tras un reinicio | Validación de filtros al solicitar (400) y barrido de reportes huérfanos al iniciar | `report-service/app/main.py` |
| F8 | Bitácora sin filtros ni paginación | Filtros por servicio, usuario, acción y fechas, y paginación | `audit-service/app/main.py` |

## C. Despliegue e infraestructura

| # | Qué se encontró | Corrección aplicada | Dónde |
|---|---|---|---|
| I1 | Error de compilación de contenedores .NET (`obj/bin` de Windows copiados a la imagen) | `.dockerignore` en la raíz y por servicio; los Dockerfile .NET limpian `obj/bin` y restauran | `.dockerignore`, `services/*/Dockerfile` |
| I2 | El gateway se caía al arrancar (tiempo de espera `00:00:60` inválido) | Corregido a `00:01:00` | `gateway/appsettings.json` |
| I3 | SQL Server terminado por falta de memoria | Límite de memoria configurado (`MSSQL_MEMORY_LIMIT_MB`) | `docker-compose.yml` |
| I4 | Kubernetes: manifiestos básicos y sin forma rápida de desplegar | Manifiestos completos (sondas, recursos, usuario no root, autoescalado, PodDisruptionBudget, Ingress, NetworkPolicy), kustomize, script `deploy.sh` de un comando y secretos desde `.env`. Validados: `kustomize build` y `kubeconform` (46 recursos) | `deploy/k8s/` |
| I5 | Guía de despliegue y respaldos citados en la Fase 2 pero ausentes | Guía de VPS, Nginx con HTTPS, script de respaldo de las 5 bases de datos | `deploy/LEEME-DESPLIEGUE.md`, `deploy/backup.sh` |

## D. Pruebas y verificación

| # | Qué se encontró | Corrección aplicada | Dónde |
|---|---|---|---|
| Q1 | Faltaban pruebas automáticas | pytest: Audit (2), Report (5), Notification (6), puente Arduino (2); Auth: sesiones, refresh, bloqueo, políticas y registro; xUnit (librería compartida .NET) y JUnit (motor de alertas) definidos; CI en GitHub Actions | `services/*/tests`, `.github/workflows/ci.yml` |
| Q2 | No había forma de reproducir las pruebas del ingeniero | `scripts/security_check.py`: sin token, token falso o `alg=none`, token tras logout, escalada de roles, rutas internas, fuerza bruta, cabeceras, CORS, cuerpo enorme, ingesta y claves de dispositivo. `scripts/smoke.sh` de punta a punta | `scripts/` |

## Decisiones y pendientes conocidos

* **Firma de tokens:** HS256 con clave compartida. Se mantiene a propósito hasta la Fase 4: con un proveedor OIDC los tokens serán RS256 y se validarán con JWKS; cambiarlo ahora exige tocar la validación de los siete servicios sin poder probarlo en Docker.
* **Revocación de sesión:** Se aplica en el gateway (único punto de entrada); los servicios internos se protegen por la red privada. Si el Auth Service cae, el gateway tolera 60 s lo último que sabía y luego deniega.
* **Tokens en el navegador:** Se guardan en `localStorage` (mitigado con token corto y revocación). Alternativa: cookies `HttpOnly` con OIDC.
* **Migraciones de base de datos:** Las tablas se crean al arrancar (`EnsureCreated`, `synchronize`, `ddl-auto`). Para producción: migraciones versionadas.
* **Mensajería:** La evaluación de lecturas y la auditoría usan reintentos en memoria, no un broker persistente.
* **Verificación en Docker:** El gateway, Community, Sensor y Alert (Java) se validaron por sintaxis y CI, no compilados en el entorno de desarrollo; deben probarse al levantar el sistema (`security_check.py` y `smoke.sh`).
