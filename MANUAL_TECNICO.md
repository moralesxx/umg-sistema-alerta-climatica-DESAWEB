# 📘 Manual Técnico

# Sistema Web de Monitoreo y Alerta Temprana para Riesgos Climáticos

## 1. Información General

### Nombre del Proyecto

**Sistema Web de Monitoreo y Alerta Temprana para Riesgos Climáticos**

### Descripción

Aplicación web desarrollada para simular el monitoreo de variables climáticas, administración de sensores, generación de alertas y consulta de eventos climáticos.

El sistema está compuesto por un **Frontend Angular**, un **Backend C#/.NET** y una **Base de Datos SQL Server**, utilizando **Docker y Docker Compose** para la infraestructura.

---

# 2. Integrantes

| Integrante                          | Carné           |
| ----------------------------------- | --------------- |
| Isaías Morales Illescas             | `7690-23-705`   |
| Joshua Daniel Aquino Díaz           | `7690-23-5762`  |
| Jimmy Anderson Hernández Valladares | `7690-23-16916` |

---

# 3. Tecnologías Utilizadas

| Componente    | Tecnología            |
| ------------- | --------------------- |
| Frontend      | Angular 20+           |
| Lenguaje      | TypeScript            |
| Backend       | C# / .NET 10+         |
| API           | REST                  |
| Autenticación | JWT                   |
| Autorización  | Roles                 |
| Base de datos | SQL Server 2022+      |
| ORM           | Entity Framework Core |
| Contenedores  | Docker                |
| Orquestación  | Docker Compose        |
| Gráficos      | SVG                   |

---

# 4. Arquitectura del Sistema

El proyecto utiliza una arquitectura desacoplada por capas.

```text
┌─────────────────────────────┐
│          FRONTEND           │
│           Angular           │
│                             │
│ Login / Dashboard           │
│ Sensores / Alertas          │
│ Eventos / Bitácora          │
└──────────────┬──────────────┘
               │
          HTTP / REST
          JSON + JWT
               │
               ▼
┌─────────────────────────────┐
│           BACKEND           │
│          .NET API           │
│                             │
│ API / Application           │
│ Domain / Infrastructure     │
└──────────────┬──────────────┘
               │
       Entity Framework
               │
               ▼
┌─────────────────────────────┐
│         SQL SERVER          │
│                             │
│ Usuarios / Roles            │
│ Sensores / Lecturas         │
│ Alertas / Eventos           │
│ Bitácora                    │
└─────────────────────────────┘

          ┌──────────────┐
          │    Docker    │
          │    Compose   │
          └──────────────┘
```

---

# 5. Estructura del Backend

La solución .NET se divide en proyectos para separar responsabilidades:

```text
AlertaClimatica/
│
├── AlertaClimatica.Api/
├── AlertaClimatica.Application/
├── AlertaClimatica.Domain/
└── AlertaClimatica.Infrastructure/
```

### Api

Es el punto de entrada de la aplicación.

Se encarga de:

* Controladores.
* Endpoints REST.
* Configuración de servicios.
* Middleware.
* Autenticación.
* Autorización.

### Application

Contiene los casos de uso y la lógica de aplicación:

* Gestión de sensores.
* Gestión de alertas.
* Gestión de eventos.
* Gestión de usuarios.
* Bitácora.

### Domain

Contiene las entidades y reglas principales del sistema:

```text
Usuario
Rol
Sensor
LecturaSensor
Alerta
Evento
Bitacora
```

### Infrastructure

Contiene las implementaciones relacionadas con:

* SQL Server.
* Entity Framework Core.
* DbContext.
* Persistencia.
* Identity.
* Servicios externos.

---

# 6. Base de Datos

La aplicación utiliza **SQL Server 2022+**.

La base de datos almacena principalmente:

| Entidad  | Función                                |
| -------- | -------------------------------------- |
| Usuarios | Información y credenciales de usuarios |
| Roles    | Permisos de cada usuario               |
| Sensores | Sensores registrados                   |
| Lecturas | Mediciones climáticas                  |
| Alertas  | Alertas generadas                      |
| Eventos  | Fenómenos climáticos                   |
| Bitácora | Acciones realizadas por usuarios       |

Las lecturas pueden contener:

```text
Temperatura
Humedad
Viento
Lluvia
Nivel del río
Fecha y hora
```

---

# 7. Frontend Angular

El frontend está desarrollado con **Angular 20+** utilizando componentes standalone.

Sus principales responsabilidades son:

* Mostrar la interfaz.
* Gestionar el inicio de sesión.
* Mantener la sesión.
* Consumir la API REST.
* Mostrar sensores.
* Mostrar alertas.
* Mostrar eventos.
* Mostrar gráficos.
* Aplicar restricciones visuales según el rol.

Estructura conceptual:

```text
Frontend/
└── src/
    └── app/
        ├── components/
        ├── services/
        ├── guards/
        └── models/
```

Los servicios Angular centralizan las peticiones HTTP, por ejemplo:

```text
AuthService
SensorService
AlertaService
EventoService
BitacoraService
```

---

# 8. Dashboard

El dashboard concentra la información principal del sistema:

* Temperatura.
* Humedad.
* Velocidad del viento.
* Lluvia.
* Nivel del río.
* Estado global de riesgo.
* Sensores.
* Alertas recientes.
* Eventos.
* Gráficos históricos.

Los gráficos se generan mediante **SVG**, principalmente para temperatura y nivel del río.

Los datos se actualizan de forma simulada cada **5 segundos**.

---

# 9. Sistema de Alertas

Las alertas se generan mediante reglas basadas en umbrales climáticos.

```text
Lectura
   │
   ▼
Evaluación de umbral
   │
   ├── Normal ────────► Verde
   ├── Precaución ────► Amarillo
   ├── Riesgo ────────► Naranja
   └── Crítico ───────► Rojo
```

Los niveles representan:

* **Verde:** condiciones normales.
* **Amarillo:** precaución.
* **Naranja:** alerta.
* **Rojo:** emergencia.

Las alertas pueden generarse automáticamente o mediante una simulación manual para realizar pruebas.

---

# 10. Administración de Sensores

Los sensores representan dispositivos físicos o simulados.

Cada sensor puede contener:

```text
ID
Nombre
Ubicación
Tipo
Estado
```

El administrador puede:

* Agregar sensores.
* Editar sensores.
* Activar sensores.
* Desactivar sensores.
* Reiniciar el sistema.

El visualizador solamente puede consultar la información.

---

# 11. Autenticación JWT

El sistema utiliza **JSON Web Token (JWT)** para autenticar usuarios.

El flujo es:

```text
Usuario
   │
   ▼
Angular
   │
   │ Credenciales
   ▼
.NET API
   │
   │ Validación
   ▼
SQL Server
   │
   ▼
.NET API
   │
   │ Genera JWT
   ▼
Angular
```

Las peticiones protegidas utilizan:

```http
Authorization: Bearer <TOKEN>
```

## Paquete JWT

Desde el proyecto API:

```bash
dotnet add AlertaClimatica.Api package Microsoft.AspNetCore.Authentication.JwtBearer
```

---

# 12. Autorización mediante Roles

El sistema utiliza dos roles:

### Administrador

Tiene permisos de lectura y escritura.

Puede:

* Administrar sensores.
* Simular alertas.
* Limpiar historial.
* Reiniciar el sistema.
* Consultar información.
* Ejecutar acciones administrativas.

### Visualizador

Tiene permisos de solo lectura.

Puede:

* Consultar el dashboard.
* Consultar indicadores.
* Consultar sensores.
* Consultar alertas.
* Consultar eventos.

No puede modificar información.

> La seguridad se valida en el backend. Ocultar botones en Angular no es suficiente para impedir una operación no autorizada.

---

# 13. Identity

Para las funcionalidades relacionadas con Identity se utiliza:

```bash
dotnet add AlertaClimatica.Infrastructure package Microsoft.Extensions.Identity.Core
```

Identity se utiliza como parte de la infraestructura relacionada con la administración de identidad de los usuarios.

---

# 14. Bitácora de Acciones

El sistema registra las acciones importantes realizadas directamente por los usuarios.

Ejemplos:

```text
CERRAR_SESION
SIMULAR_ALERTA
AGREGAR_SENSOR
EDITAR_SENSOR
CAMBIAR_ESTADO_SENSOR
LIMPIAR_HISTORIAL_ALERTAS
REINICIAR_SISTEMA
```

La identificación del usuario se obtiene desde el JWT.

Angular **no debe enviar manualmente el `UsuarioId`** al registrar una acción.

```text
JWT
 │
 ├── UsuarioId
 ├── Nombre
 └── Rol
      │
      ▼
   Backend
      │
      ▼
   Bitácora
```

Las lecturas automáticas generadas por la simulación no se registran como acciones de usuario.

---

# 15. Comunicación entre Componentes

El flujo principal de comunicación es:

```text
Angular
   │
   │ HTTP / JSON / JWT
   ▼
.NET API
   │
   ▼
Application
   │
   ▼
Infrastructure
   │
   ▼
SQL Server
```

La respuesta regresa desde SQL Server hasta Angular utilizando la misma arquitectura en sentido inverso.

---

# 16. Docker y Docker Compose

Docker se utiliza para ejecutar los servicios del proyecto dentro de contenedores.

Docker Compose permite administrar la infraestructura mediante:

```text
docker-compose.yml
```

### Levantar los servicios

```bash
docker-compose up -d
```

### Ver contenedores

```bash
docker ps
```

### Detener los servicios

```bash
docker-compose down
```

---

# 17. Actualización después de `git pull`

Cuando un integrante realiza cambios y se obtiene la actualización:

```bash
git pull
```

Si existen cambios relacionados con Docker, configuración o base de datos, se recomienda reiniciar los contenedores:

```bash
docker-compose down
docker-compose up -d
```

Esto permite que Docker Compose vuelva a levantar los servicios utilizando la configuración actualizada.

Flujo:

```text
git pull
   │
   ▼
docker-compose down
   │
   ▼
docker-compose up -d
```

---

# 18. Ejecución del Backend

Desde la carpeta del backend:

```bash
dotnet restore
```

Después:

```bash
dotnet run
```

Antes de ejecutar se debe comprobar que SQL Server esté disponible y que la cadena de conexión esté correctamente configurada.

---

# 19. Ejecución del Frontend

Desde la carpeta del frontend:

```bash
npm install
```

Después:

```bash
ng serve
```

Angular iniciará el servidor de desarrollo y mostrará la dirección de acceso en la terminal.

---

# 20. Requisitos del Entorno

Para ejecutar el proyecto se necesita:

* .NET SDK 10+.
* Node.js.
* Angular CLI.
* Docker / Docker Desktop.
* Docker Compose.
* SQL Server 2022+.
* Git.

---

# 21. Flujo General del Sistema

```text
                  USUARIO
                     │
                     ▼
                ┌─────────┐
                │ Angular │
                └────┬────┘
                     │
                 Login / JWT
                     │
                     ▼
                ┌─────────┐
                │ .NET API│
                └────┬────┘
                     │
             Autenticación
             Autorización
                     │
                     ▼
              Application
                     │
                     ▼
             Infrastructure
                     │
                     ▼
                SQL Server
```

---

# 22. Seguridad

Las principales medidas de seguridad del sistema son:

* Autenticación mediante JWT.
* Autorización mediante roles.
* Validación de permisos en el backend.
* Protección de endpoints.
* Identificación mediante claims del JWT.
* Registro de acciones importantes.
* Separación de responsabilidades.
* Validación de operaciones tanto en frontend como en backend.

---

# 23. Resumen Técnico

El proyecto utiliza una arquitectura desacoplada:

```text
Angular
   │
   │ REST + JSON + JWT
   ▼
.NET API
   │
   ├── Application
   ├── Domain
   └── Infrastructure
   │
   ▼
SQL Server
```

La infraestructura se administra mediante:

```text
Docker
   │
   └── Docker Compose
```

La combinación de **Angular + .NET + SQL Server + Entity Framework Core + JWT + Docker** permite desarrollar un sistema modular para el monitoreo climático, administración de sensores, generación de alertas y control de acceso mediante roles.