# Login con Google y GitHub — OAuth 2.0 / OpenID Connect

El sistema permite iniciar sesión con **Google (OpenID Connect)** y **GitHub (OAuth 2.0)** además del usuario/contraseña local.
Todo el protocolo vive en el **Auth Service**; al terminar, el usuario recibe **el mismo JWT propio del sistema** (con `sid`,
rol y permisos) que con el login local. Por eso el gateway, los 7 servicios, la revocación de sesión y los roles **no cambian**.

## 1. Flujo (Authorization Code + PKCE)

```
 Navegador            Frontend/Nginx → Gateway → Auth Service                 Google / GitHub
    │  clic "Continuar con Google"                                                    │
    │── GET /api/auth/oauth/google/start ───────────►                                 │
    │                         genera state, nonce y code_verifier (PKCE)              │
    │◄── 302 → accounts.google.com/...?state&nonce&code_challenge  + cookie HttpOnly firmada (oauth_tx)
    │──────────────────────────────────────────────────────────────────────────────►  │ el usuario se autentica
    │◄── 302 → /api/auth/oauth/google/callback?code&state  ◄──────────────────────────│
    │── GET callback (+ cookie) ──────────────────►                                   │
    │                         1. state de la URL == state de la cookie (anti-CSRF)    │
    │                         2. POST token endpoint (code + client_secret + verifier) ──►
    │                         3. valida id_token: firma RS256 (JWKS), iss, aud, exp, nonce
    │                            (GitHub: /user y /user/emails con el access token)   │
    │                         4. busca (proveedor, subject) → usuario | vincula por correo verificado | crea cuenta
    │                         5. emite sesión propia (JWT 15 min + refresh rotativo)
    │◄── 302 → /oauth/callback#rt=<ticket de un solo uso>
    │── POST /api/auth/refresh {refreshToken: ticket} ─►   (la SPA borra el #rt de la URL al instante)
    │◄── { token, refreshToken, usuario }  → /dashboard
```

El ticket `#rt` es un refresh token **rotativo**: se usa una sola vez (reutilizarlo cierra la sesión por seguridad) y viaja en el
*fragmento* de la URL, que el navegador nunca envía a ningún servidor.

## 2. Qué cubre

| Capacidad | Implementación | Evidencia |
|---|---|---|
| Login externo Google (OIDC) y GitHub (OAuth 2.0) | `services/auth-service/src/oauth.service.ts`, `OAuthController` en `auth.controller.ts` | `npm test` (`test/oauth.ts`) |
| Flujo Google/GitHub → Auth Service → JWT propio | `UsersService.accederConIdentidadExterna` → `emitirSesion` | Prueba E2E contra Auth + PostgreSQL reales |
| Vínculo identidad externa ↔ usuario local | tabla `identidades_externas (proveedor, subject)`; vínculo automático solo por **correo verificado** por el proveedor | `test/oauth.ts` §3 |
| Botones y pantalla de retorno en la SPA | `login.*`, `oauth-callback.ts`, `auth.service.ts`, ruta `/oauth/callback` | `ng build` de producción |
| Rutas públicas en el gateway con límite de intentos | `services/gateway/appsettings.json` → `auth-oauth` (10/min por IP, solo GET) | `scripts/security_check.py` §1b |
| Secretos OAuth fuera del código (RNF-OAUTH-01/02) | `GOOGLE_CLIENT_SECRET`, `GITHUB_CLIENT_SECRET` (+ ids) solo en `.env` / Secret `app-secrets` | ver §5 |

## 3. Registrar las aplicaciones en los proveedores

Sea `BASE` la URL pública del sitio (la misma que `PUBLIC_URL`), p. ej. `https://miguate.lat` o `http://localhost:4200` en desarrollo.
**Redirect URI a registrar** (debe coincidir carácter por carácter):

* Google: `BASE/api/auth/oauth/google/callback`
* GitHub: `BASE/api/auth/oauth/github/callback`

### Google (Google Cloud Console)
1. <https://console.cloud.google.com/> → crear/seleccionar proyecto → **APIs y servicios → Pantalla de consentimiento de OAuth**.
   Tipo *Externo*, nombre de la app y correo de soporte. Alcances: `openid`, `email`, `profile` (no sensibles). Si queda en modo *Pruebas*, agregue como **usuarios de prueba** los correos que van a entrar.
2. **Credenciales → Crear credenciales → ID de cliente de OAuth → Aplicación web**.
3. En **URI de redireccionamiento autorizados** agregue la de Google (producción y, si quiere probar local, también `http://localhost:4200/api/auth/oauth/google/callback`).
4. Copie **ID de cliente** → `GOOGLE_CLIENT_ID` y **Secreto** → `GOOGLE_CLIENT_SECRET`.

### GitHub
1. <https://github.com/settings/developers> → **New OAuth App** (o en una organización: *Settings → Developer settings*).
2. *Homepage URL*: `BASE`. *Authorization callback URL*: la de GitHub.
3. **Generate a new client secret**. Copie *Client ID* → `GITHUB_CLIENT_ID` y el secreto → `GITHUB_CLIENT_SECRET`.
4. Una app de GitHub solo admite **una** callback URL: para local y producción cree dos apps.

## 4. Configuración

Variables (ver `.env.example`):

| Variable | Descripción |
|---|---|
| `GOOGLE_CLIENT_ID` / `GOOGLE_CLIENT_SECRET` | Vacíos = Google deshabilitado (su botón no aparece). Deben definirse **juntos**; si no, el servicio no arranca. |
| `GITHUB_CLIENT_ID` / `GITHUB_CLIENT_SECRET` | Ídem para GitHub. |
| `PUBLIC_URL` | URL pública del sitio; de ahí salen los redirect URI. En HTTPS la cookie de contexto lleva `Secure`. |
| `OAUTH_AUTO_ACTIVATE` | `false` (defecto): una cuenta **nueva** creada por Google/GitHub queda *pendiente* hasta que un Administrador la active en **Usuarios**, igual que el registro público. `true`: se activa al instante con el rol mínimo *Usuario de consulta* (útil para una demostración). |

**Docker Compose:** complete `.env` y `docker compose up -d --build` (el servicio `auth-service` ya recibe las variables).

**Kubernetes/K3s:** complete `.env` y ejecute `deploy/k8s/crear-secretos.sh` (crea las claves en el Secret `app-secrets`).
Las claves OAuth son `optional: true`: sin ellas el despliegue funciona y solo el login local queda activo.
`OAUTH_AUTO_ACTIVATE` y `PUBLIC_URL` están en el ConfigMap `app-config`.

## 5. Seguridad (RNF-OAUTH-01/02 y controles del flujo)

* **Secretos:** `client_secret` solo en variables de entorno / Secret de Kubernetes; nunca en el repositorio, la imagen, el frontend, la URL ni los logs (verificado: el secreto solo se envía servidor→proveedor al token endpoint).
* **PKCE (S256)** en ambos proveedores, `state` aleatorio de 192 bits y `nonce` OIDC.
* **Anti-CSRF / login CSRF:** el `state` de la URL debe coincidir con el de una cookie `HttpOnly; SameSite=Lax; Path=/api/auth/oauth` firmada (HS256, audiencia propia `alerta.oauth.tx`, 10 min, de un solo uso). Sin base de datos: funciona con varias réplicas.
* **`id_token` de Google validado** como exige OIDC Core: algoritmo fijo RS256 (rechaza `alg=none`), firma con la clave del JWKS (`kid`, con recarga ante rotación), `iss`, `aud`, `exp` y `nonce`.
* **Sin toma de cuentas:** solo se vincula por correo si el proveedor lo marca como **verificado** (`email_verified` / correo `verified` de GitHub). El identificador estable es `subject`, nunca el correo.
* **Privilegios mínimos:** cuentas nuevas = rol *Usuario de consulta*, sin contraseña local (no pueden entrar por `/login`) y pendientes de aprobación por defecto.
* **Mensajes de error:** al navegador solo viajan códigos (`cuenta_inactiva`, `estado_invalido`…); el detalle queda en el log del servidor y en la bitácora.
* **Auditoría** (Bitácora): `LOGIN_OAUTH`, `LOGIN_OAUTH_FALLIDO`, `VINCULAR_IDENTIDAD`, `REGISTRO_PENDIENTE`, `REGISTRO_OAUTH`.
* **Revocación:** la sesión emitida usa el mismo `sid`: logout, desactivar usuario o cambio de rol la invalidan al instante.
* **CSP del frontend** intacta: los íconos son SVG en línea; la navegación a los proveedores se hace desde el backend (el navegador solo va a `/api/...` del mismo origen).

## 6. Endpoints

| Método y ruta | Descripción |
|---|---|
| `GET /api/auth/oauth/providers` | Proveedores habilitados `{ "providers": ["google","github"] }` (público) |
| `GET /api/auth/oauth/{proveedor}/start` | Redirige al proveedor |
| `GET /api/auth/oauth/{proveedor}/callback` | Recibe `code` y `state`; redirige a `/oauth/callback#rt=…` o a `/login?oauth_error=<código>` |
| `POST /api/auth/refresh` | Canjea el ticket por `{ token, refreshToken, usuario }` (existente) |

Códigos de `oauth_error`: `acceso_denegado`, `estado_invalido`, `cuenta_inactiva`, `email_no_verificado`, `proveedor_no_disponible`, `proveedor_error`.

## 7. Cómo probar

```bash
# Backend (flujo completo contra proveedores simulados: incluye ataques de state, nonce, firma, alg=none, aud/iss/exp)
cd services/auth-service && npm ci && npm test

# Con el sistema levantado (verifica rutas públicas, redirección con PKCE, cookie HttpOnly, rechazo de callbacks falsos)
python scripts/security_check.py --url http://localhost:8080 --admin-password "TU_ADMIN_PASSWORD"
```

Prueba manual: abra el sitio → **Continuar con Google/GitHub** → autorícese → debe llegar al dashboard.
Si es una cuenta nueva y `OAUTH_AUTO_ACTIVATE=false`, verá *"pendiente de aprobación"*: entre como administrador → **Usuarios** → active la cuenta → vuelva a intentarlo.

## 8. Solución de problemas

| Síntoma | Causa probable |
|---|---|
| Google: `redirect_uri_mismatch` | El redirect URI registrado no es **idéntico** a `PUBLIC_URL/api/auth/oauth/google/callback` (esquema, dominio, puerto, barra final). |
| Google: `access_denied` / "app no verificada" | La app está en modo *Pruebas*: agregue el correo como usuario de prueba o publique la app. |
| No aparecen los botones | El proveedor no tiene `CLIENT_ID` **y** `CLIENT_SECRET` en el entorno de `auth-service`. |
| Vuelve a `/login` con `estado_invalido` | Cookie bloqueada o intento caducado (10 min); verifique que `PUBLIC_URL` coincide con la URL que ve el navegador. |
| `email_no_verificado` en GitHub | La cuenta no tiene un correo verificado; verifíquelo en *GitHub → Settings → Emails* (se pide el alcance `user:email`). |
| `cuenta_inactiva` | Cuenta nueva pendiente de aprobación (ver `OAUTH_AUTO_ACTIVATE`) o usuario desactivado. |
| El Auth Service no arranca: "CLIENT_ID y CLIENT_SECRET deben definirse juntos" | Se definió solo uno de los dos valores de un proveedor. |

## 9. Decisiones de diseño

* **El sistema sigue emitiendo su propio JWT (HS256).** Las identidades externas se *canjean* por la sesión propia, de modo que los servicios .NET/Java/Python no necesitan conocer a Google ni GitHub. Pasar a RS256 + JWKS propio sigue siendo una mejora independiente.
* **Sin librerías OAuth adicionales:** el flujo usa `fetch` y `crypto` de Node 22 (cero dependencias nuevas, `npm audit` sin cambios).
* **Ticket en fragmento + refresh rotativo** en lugar de tokens en la URL o una tabla extra de códigos temporales.
