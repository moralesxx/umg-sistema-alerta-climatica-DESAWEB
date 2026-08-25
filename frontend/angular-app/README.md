# Sistema Web de Monitoreo y Alerta Temprana para Riesgos Climáticos

Aplicación web diseñada para simular un sistema de monitoreo y alerta temprana ante riesgos climáticos en comunidades rurales. El sistema permite visualizar información meteorológica en tiempo real, generar alertas automáticas por eventos extremos, administrar sensores de forma interactiva y consultar el historial de incidentes de manera clara.

El sistema cuenta con autenticación de usuarios mediante JWT y control de acceso basado en roles, permitiendo diferenciar entre usuarios administradores y usuarios visualizadores.

---

## Integrantes del Equipo

* **Isaías Morales Illescas** — Carné: `7690-23-705`
* **Joshua Daniel Aquino Díaz** — Carné: `7690-23-5762`
* **Jimmy Anderson Hernández Valladares** — Carné: `7690-23-16916`

---

## Tecnologías del Proyecto

* **Frontend:** Angular (versión 20+) con componentes standalone, formularios, servicios HTTP y gráficos nativos integrados mediante SVG.
* **Backend:** C# .NET (versión 10+) estructurado bajo una arquitectura de API REST.
* **Autenticación:** JWT (JSON Web Token).
* **Autorización:** Control de acceso basado en roles.
* **Base de Datos:** SQL Server (versión 2022+).
* **Contenedores e Infraestructura:** Docker y Docker Compose para la orquestación de servicios en un entorno GNU/Linux.

---

## Arquitectura y Estructura de la Solución

La aplicación está diseñada bajo una arquitectura desacoplada, dividida en capas independientes:

1. **Capa de Presentación (Frontend - Angular):**
   Interfaz de usuario moderna y adaptable (Responsive Design para escritorios y dispositivos móviles), encargada de consumir los servicios de la API, administrar la sesión del usuario y renderizar los paneles de control.

2. **Capa de Lógica de Negocio y API (Backend - .NET):**
   Gestiona los endpoints de comunicación, autenticación, autorización, lógica de validación de umbrales climáticos, procesamiento de lecturas y emisión de alertas.

3. **Capa de Infraestructura:**
   Contiene la implementación relacionada con persistencia, acceso a datos, seguridad y servicios utilizados por la aplicación.

4. **Capa de Persistencia (Base de Datos - SQL Server):**
   Diseñada para almacenar de forma centralizada la información del sistema.

---

## Modelado de Datos (SQL Server)

* **Usuarios:** Control de accesos, credenciales y roles.
* **Roles:** Definición de permisos de los usuarios del sistema.
* **Sensores:** Dispositivos físicos o virtuales registrados en el sistema.
* **Lecturas de Sensores:** Registro métrico temporal de temperatura, humedad, viento, lluvia y nivel de río.
* **Alertas Generadas:** Avisos clasificados por niveles de peligro.
* **Historial de Eventos:** Registro de fenómenos extremos detectados, como inundaciones, sequías, tormentas, heladas e incendios forestales.
* **Bitácora de Acciones:** Auditoría de operaciones realizadas por los usuarios del sistema.

---

# Autenticación y Autorización

El sistema utiliza autenticación mediante JWT para identificar al usuario que realiza las peticiones a la API.

Después de iniciar sesión correctamente, el backend genera un token JWT que contiene la información necesaria para identificar al usuario y determinar sus permisos.

El frontend utiliza este token para realizar peticiones autenticadas hacia la API.

## Roles del Sistema

Actualmente se contemplan dos roles principales:

### Administrador

El usuario administrador tiene permisos de lectura y escritura sobre el sistema.

Puede:

* Visualizar el dashboard.
* Consultar sensores.
* Agregar sensores.
* Editar sensores.
* Activar y desactivar sensores.
* Reiniciar el sistema de monitoreo.
* Consultar alertas.
* Simular alertas.
* Limpiar el historial de alertas.
* Consultar eventos.
* Realizar las acciones correspondientes que queden registradas en la bitácora.
* Cerrar sesión.

### Visualizador

El usuario visualizador tiene permisos de solo lectura.

Puede:

* Visualizar el dashboard.
* Consultar los indicadores meteorológicos.
* Consultar los sensores.
* Consultar el estado de los sensores.
* Consultar las alertas.
* Consultar el historial de eventos.

No puede:

* Agregar sensores.
* Editar sensores.
* Activar o desactivar sensores.
* Reiniciar el sistema.
* Simular alertas.
* Limpiar el historial de alertas.
* Ejecutar otras operaciones que modifiquen información.

El control de permisos debe implementarse tanto en el frontend como en el backend.

El frontend debe ocultar las acciones que el usuario no tiene permitido ejecutar.

El backend debe validar el rol recibido mediante el JWT para impedir que un usuario visualizador pueda ejecutar directamente los endpoints de modificación.

No se debe confiar únicamente en la ocultación de botones de Angular como mecanismo de seguridad.

---

# Bitácora de Acciones

El sistema cuenta con un módulo de bitácora para registrar las acciones realizadas por los usuarios.

La identificación del usuario se obtiene desde el JWT en el backend.

Angular no debe enviar manualmente el `UsuarioId` al registrar una acción.

Ejemplo de acciones registradas:

* `CERRAR_SESION`
* `SIMULAR_ALERTA`
* `AGREGAR_SENSOR`
* `EDITAR_SENSOR`
* `CAMBIAR_ESTADO_SENSOR`
* `LIMPIAR_HISTORIAL_ALERTAS`
* `REINICIAR_SISTEMA`

Las acciones automáticas de simulación de lecturas en tiempo real no se registran en la bitácora, debido a que no representan una acción realizada directamente por el usuario.

---

# Requisitos del Sistema

## 1. Monitoreo Climático

* Visualización en tiempo real de temperatura ambiente.
* Visualización de humedad relativa.
* Visualización de velocidad del viento.
* Visualización del nivel de lluvia.
* Visualización del nivel de río o reservorio.
* Actualización simulada de los indicadores principales.
* Actualización automática de los datos cada 5 segundos.

---

## 2. Generación de Alertas

* Sistema automático de detección de condiciones críticas basado en umbrales.
* Clasificación por niveles de peligro:
  * **Verde:** Normal.
  * **Amarillo:** Precaución.
  * **Naranja:** Alerta.
  * **Rojo:** Emergencia.
* Mensajes descriptivos para cada nivel.
* Visualización de alertas recientes.
* Simulación manual de alertas para pruebas del sistema.
* Registro de las alertas generadas en la API.

---

## 3. Sensores

El dashboard permite administrar sensores simulados.

Las operaciones disponibles para usuarios administradores son:

* Agregar sensor.
* Editar sensor.
* Activar sensor.
* Desactivar sensor.
* Reiniciar el sistema de monitoreo.

Cada sensor contiene información como:

* ID del sensor.
* Nombre del sensor.
* Ubicación.
* Estado.
* Tipo de sensor.

Los usuarios visualizadores únicamente pueden consultar esta información.

---

## 4. Dashboard

El dashboard principal contiene:

* Estado global de riesgo.
* Temperatura.
* Humedad.
* Velocidad del viento.
* Nivel de lluvia.
* Nivel de río.
* Gráfico histórico y en tiempo real.
* Listado de sensores.
* Estado de sensores.
* Alertas recientes.
* Historial de eventos.

El dashboard utiliza SVG para representar los gráficos sin depender de librerías externas de visualización.

---

## 5. Gráficos

Se implementaron gráficos dinámicos mediante SVG para representar:

* Historial de temperatura.
* Historial del nivel del río.

Los datos se actualizan automáticamente durante la simulación de tiempo real.

El historial mantiene una cantidad limitada de lecturas para evitar un crecimiento indefinido de los datos mostrados.

---

## 6. Historial de Eventos

El dashboard permite consultar eventos y fenómenos climáticos registrados por el sistema.

Cada evento puede contener:

* ID del evento.
* Tipo de fenómeno.
* Descripción.
* Fecha y hora.

Los eventos se cargan desde la API y pueden actualizarse manualmente desde el dashboard.

---

## 7. Cierre de Sesión

El dashboard incorpora una función de cierre de sesión.

Al cerrar sesión:

1. Se registra la acción en la bitácora.
2. Se elimina la información de sesión almacenada en el frontend.
3. Se redirige al usuario hacia `/login`.

La identificación del usuario para la bitácora se obtiene desde el JWT en el backend.

---

# Backend

El backend está desarrollado utilizando C# y .NET.

La API debe encargarse de:

* Autenticación.
* Generación de JWT.
* Validación del JWT.
* Autorización mediante roles.
* Gestión de usuarios.
* Gestión de sensores.
* Gestión de alertas.
* Gestión de eventos.
* Gestión de bitácora.
* Validación de operaciones permitidas según el rol.

---

# Paquetes necesarios para autenticación

Para implementar la autenticación JWT en el proyecto backend se necesitan los siguientes paquetes.

Desde la carpeta donde se encuentre la solución/backend ejecutar:

```bash
dotnet add AlertaClimatica.Api package Microsoft.AspNetCore.Authentication.JwtBearer
