# Seguridad — revisión y controles

Resultado de revisar el sistema como lo haría un atacante o un revisor con Postman. Cada control se puede comprobar con
`python scripts/security_check.py --admin-password "TU_ADMIN_PASSWORD"` (debe terminar con 0 fallas).

## 1. Hallazgos corregidos
| # | Hallazgo | Riesgo | Corrección |
|---|---|---|---|
| 1 | El token seguía siendo válido después de cerrar sesión (hasta 60 min) | Alto | Sesiones en el servidor: el token lleva `sid` y el gateway consulta al Auth Service en cada petición. Logout, cambio de contraseña, desactivar usuario o cambio de rol **invalidan el token al instante**. Token de acceso reducido a **15 min** con refresh token rotativo |
| 2 | Registro público daba acceso inmediato a datos | Alto | El registro crea una solicitud **inactiva**; un administrador la aprueba |
| 3 | Un Operador podía borrar todo el historial de alertas | Medio | Solo Administrador |
| 4 | Detrás del proxy, el límite de intentos se aplicaba a todos los usuarios juntos (misma IP) | Medio | Nginx usa `realip` y reenvía la IP real; el gateway limita por IP real |
| 5 | Sin protección por cuenta contra fuerza bruta; tiempo de respuesta distinto para correos inexistentes | Medio | Bloqueo de cuenta (5 intentos / 15 min) y comparación de contraseña siempre ejecutada |
| 6 | Búsqueda de usuarios/login con comodines `%` y `_` (LIKE) | Medio | Correos normalizados a minúsculas, igualdad exacta y escape de comodines |
| 7 | Contraseñas débiles permitidas (6 caracteres) | Medio | 8–72 caracteres con letras y números |
| 8 | Violación de unicidad devolvía 500 | Bajo | 409 con mensaje claro (Auth y servicios .NET) |
| 9 | Documentación de API (`/docs`, OpenAPI), cabecera `Server` y errores detallados | Bajo | Deshabilitados / mensajes genéricos |
| 10 | Sin cabeceras de seguridad ni límite de tamaño | Bajo | `nosniff`, `X-Frame-Options`, `no-store`, cuerpo máx. 1 MB |
| 11 | Dependencias con vulnerabilidades (`body-parser`, `multer`) | Alto | Actualizadas: `npm audit` del Auth Service = 0 vulnerabilidades |
| 12 | Contenedores con privilegios por defecto | Medio | `no-new-privileges` y sin capacidades (`cap_drop: ALL`); en Kubernetes usuario no root, `seccomp` y NetworkPolicy |
| 13 | El frontend cargaba Bootstrap desde un CDN externo (dependencia de terceros sin control de integridad y bloqueo de una CSP estricta) | Medio | Bootstrap 5.3.2 empaquetado localmente; sin recursos externos |
| 14 | Sin política de seguridad de contenido (CSP) en el frontend | Medio | CSP estricta en Nginx: solo scripts propios, sin `eval`, sin `frame`, `object-src 'none'` |
| 15 | Una sola clave compartida para todos los dispositivos IoT | Medio | Claves individuales por dispositivo (hash SHA-256, revocables, con alcance por prefijo y último contacto); pantalla *Dispositivos* |
| 16 | Los avisos por correo podían perderse si el servicio se reiniciaba | Bajo | Cola persistente en MongoDB con reintentos (5 con espera creciente), arrendamiento de envíos huérfanos y motivo del fallo registrado |
| 17 | Login solo con usuario/contraseña local | Mejora | Login con Google (OIDC) y GitHub (OAuth 2.0): Authorization Code + PKCE, `state` anti-CSRF en cookie `HttpOnly` firmada, `id_token` validado (RS256/JWKS, `iss`, `aud`, `exp`, `nonce`), vínculo solo por correo verificado, cuentas nuevas pendientes de aprobación y secretos solo en `.env`/Secret. Ver `docs/OAUTH-OIDC.md` |

> **Postman y CORS:** CORS es una protección del *navegador*; Postman no la aplica. La seguridad real está en el servidor:
> sin token válido y con sesión vigente, o sin el rol adecuado, la petición se rechaza (401/403) venga de donde venga.

## 2. Matriz de acceso por rol (verificada en `security_check.py`)
| Operación | Sin token | Consulta | Operador | Administrador |
|---|:-:|:-:|:-:|:-:|
| Ver comunidades, sensores, lecturas, alertas, dashboard | 401 | ✔ | ✔ | ✔ |
| Crear/editar sensores, lecturas, atender/cerrar alertas | 401 | 403 | ✔ | ✔ |
| Comunidades (crear/editar), reglas de alerta | 401 | 403 | 403 | ✔ |
| Borrar historial de alertas | 401 | 403 | 403 | ✔ |
| Usuarios, bitácora, notificaciones | 401 | 403 | 403 | ✔ |
| Generar reportes / ver historial | 401 | 403 | ✔ (propios) | ✔ (todos) |
| Descargar reportes PDF | 401 | 403 | 403 | ✔ |
| Rutas `/internal/…` | 404 | 404 | 404 | 404 |
| Ingesta de dispositivos | clave de dispositivo (`X-Device-Key`) | | | |
| Registrar/revocar dispositivos IoT | 401 | 403 | 403 | ✔ |

## 3. Controles por capa
* **Entrada (gateway):** única superficie pública; JWT + sesión vigente; CORS por lista; límites 10/min (login, registro, refresh, logout), 300/min (ingesta), 600/min global por IP; 1 MB por solicitud; rutas `/internal/` ocultas; errores JSON sin detalles.
* **Identidad externa (Auth):** PKCE S256, `state` y `nonce`, cookie de contexto `HttpOnly; SameSite=Lax` firmada y de un solo uso, validación completa del `id_token`, `client_secret` solo servidor→proveedor, auditoría `LOGIN_OAUTH`/`LOGIN_OAUTH_FALLIDO`/`VINCULAR_IDENTIDAD`.
* **Identidad (Auth):** bcrypt, bloqueo de cuenta, refresh token rotativo con detección de reutilización, política de contraseñas, auditoría de eventos de seguridad (`LOGIN`, `LOGOUT`, `CUENTA_BLOQUEADA`, `REFRESH_REUTILIZADO`, `REGISTRO_PENDIENTE`…).
* **Servicios:** cada uno valida firma, emisor, audiencia, expiración (solo HS256), rol y permiso; endpoints internos exigen rol `Servicio`; ORMs parametrizados (sin SQL concatenado); PDF con autoescape (sin inyección HTML).
* **Ingesta Arduino:** clave comparada en tiempo constante, límite de solicitudes, validación de sensor/valor, máx. 50 lecturas por petición; deshabilitada si no hay clave.
* **Correos:** validación de direcciones, saneado de cabeceras (sin inyección), límite por destinatario, solo el Administrador gestiona destinatarios.
* **Infraestructura:** secretos solo en `.env`/Secret de Kubernetes; contenedores sin privilegios; bases de datos sin puertos publicados; el gateway solo en loopback; HTTPS en el Nginx de borde; Kubernetes con NetworkPolicy de confianza cero.

## 4. Riesgos residuales (conocidos y aceptados para esta fase)
1. **HS256 con clave compartida:** cualquiera que conozca `JWT_KEY` puede firmar tokens. Mejora pendiente: RS256 + JWKS propio (el login con Google/GitHub ya se canjea por la sesión propia, no requiere que los servicios validen tokens de terceros).
2. **Revocación en el gateway:** los servicios internos no la consultan; se protegen porque solo son alcanzables por la red privada (en Kubernetes, NetworkPolicy). Si el Auth Service cae, el gateway tolera hasta 60 s lo último que sabía y luego deniega.
3. **Tokens en `localStorage`:** expuestos ante un XSS; mitigado con token corto y revocación. Alternativa: cookies `HttpOnly`.
4. **La CSP permite estilos en línea** (`style-src 'unsafe-inline'`), necesarios para los estilos de componentes de Angular; los scripts sí están restringidos a los propios.
5. **Dependencias de desarrollo del frontend** con avisos de `npm audit` (herramientas de compilación): no se despliegan; las dependencias de producción tienen 0 vulnerabilidades.
6. **Auditoría de mejor esfuerzo** con 3 reintentos y sin cola persistente (el correo sí tiene cola persistente).

## 5. Cómo verificar
```bash
python scripts/security_check.py --url http://localhost:8080 --admin-password "TU_ADMIN_PASSWORD" --device-key "TU_DEVICE_API_KEY"
```
Reproduce manualmente en Postman: (1) `POST /api/auth/login` → copiar `token`; (2) `GET /api/auth/usuarios` → 200;
(3) `POST /api/auth/logout` con `{"refreshToken": "..."}`; (4) repetir (2) con el mismo token → **401**.
