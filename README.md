# Sistema de Alerta Climática — Fase 2 (UMG, Desarrollo Web)

Sistema de monitoreo y alerta temprana de riesgos climáticos por comunidad.
Administración de usuarios, comunidades, sensores y reglas; lecturas simuladas;
generación y gestión de alertas; historial de eventos; bitácora de auditoría y
dashboard.

## Tecnologías

| Capa | Tecnología |
|---|---|
| Frontend | Angular 22 (componentes standalone) servido por Nginx |
| Backend | ASP.NET Core (.NET 10), arquitectura por capas (Api / Application / Domain / Infrastructure) |
| Datos | SQL Server 2022 + Entity Framework Core (migraciones) |
| Seguridad | JWT, roles `Administrador`, `Operador`, `Usuario de consulta`, hash con `PasswordHasher` |
| Despliegue | Docker Compose, Nginx, HTTPS (Let's Encrypt) en VPS Linux |

## Estructura

```
backend/        API .NET (Api, Application, Domain, Infrastructure)
frontend/       Aplicación Angular (frontend/angular-app)
tests/          Pruebas unitarias (xUnit)
deploy/         Ejemplo de Nginx, script de respaldo, guía de despliegue
docker-compose.yml        Entorno de desarrollo
docker-compose.prod.yml   Producción (usa variables de .env)
```

## Ejecutar en local (desarrollo)

Requisitos: Docker Desktop, .NET SDK 10, Node.js.

```bash
# 1) Base de datos (SQL Server en el puerto 14333)
docker compose up -d db

# 2) API (desde la carpeta backend) -> http://localhost:5081
cd backend
dotnet run --project AlertaClimatica.Api

# 3) Frontend (otra terminal) -> http://localhost:4200
cd frontend/angular-app
npm install
npm start
```

También se puede levantar todo con `docker compose up -d --build`.

Al iniciar, la API **aplica las migraciones automáticamente** y, si la base está
vacía, siembra roles, tipos de sensor y el usuario administrador inicial:

- Desarrollo: `admin@alerta.com` / `Admin123!`
- Producción: la contraseña inicial viene de la variable `Seed__AdminPassword`.

> El esquema de la base lo define EF Core (carpeta `Migrations`). Los archivos
> de `database/` ya no se usan.

## Módulos y permisos

| Módulo | Administrador | Operador | Usuario de consulta |
|---|:-:|:-:|:-:|
| Dashboard, Lecturas (consulta), Historial | ✔ | ✔ | ✔ |
| Comunidades (ver) | ✔ | ✔ | ✔ |
| Comunidades (crear/editar/activar) | ✔ | | |
| Sensores (ver) | ✔ | ✔ | ✔ |
| Sensores (crear/editar/estado/valor simulado) | ✔ | ✔ | |
| Alertas (ver) | ✔ | ✔ | ✔ |
| Alertas (atender/cerrar) | ✔ | ✔ | |
| Reglas de alerta | ✔ | | |
| Usuarios (crear, editar, rol, activar, filtros) | ✔ | | |
| Bitácora de auditoría | ✔ | | |

## Cómo funciona el motor de alertas

1. El **simulador** (servicio en segundo plano) genera lecturas periódicas para
   cada sensor activo (o usa el *valor simulado* fijado por un operador).
2. Cada lectura pasa por el **evaluador de reglas**: se compara con las reglas
   activas del tipo de sensor, se toma el nivel más alto que coincide y se
   ignora el nivel Verde.
3. Se crea una **alerta** (Activa) sin duplicar mientras exista otra abierta
   para el mismo sensor y regla; los cambios de estado se reflejan en el
   **historial de eventos**.
4. Un operador **atiende** y luego **cierra** la alerta; queda registrado el
   responsable y la fecha.
5. Todas las acciones administrativas se registran en la **bitácora** desde el
   backend (el frontend no puede escribir en ella).

Parámetros del simulador (sección `Simulador` de `appsettings.json` o variables
`Simulador__*`): `Habilitado`, `IntervaloSegundos` y `RetencionDias`

## Configuración y secretos

Nunca se suben credenciales al repositorio. Se leen de variables de entorno:

| Variable | Uso |
|---|---|
| `ConnectionStrings__DefaultConnection` | Cadena de conexión a SQL Server |
| `Jwt__Key` | Clave de firma del JWT (mínimo 32 caracteres) |
| `Seed__AdminPassword` | Contraseña del admin inicial (solo base vacía) |

En producción se usa `docker-compose.prod.yml` con un archivo `.env`
(ver `.env.example`; `.env` está en `.gitignore`). La guía paso a paso del VPS,
Nginx, HTTPS y respaldos está en `deploy/LEEME-DESPLIEGUE.md`.

## Pruebas

```bash
dotnet test tests/AlertaClimatica.Application.Tests
```

## Flujo de trabajo del equipo

1. Trabajar en una rama `feature/...`, probar en local.
2. `git pull` + resolver conflictos, `git push`, pull request.
3. En el VPS: respaldo de la base, `git pull`, reconstruir contenedores. Las
   migraciones se aplican solas al arrancar la API.
