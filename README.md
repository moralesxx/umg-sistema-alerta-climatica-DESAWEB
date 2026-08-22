# Sistema Web de Monitoreo y Alerta Temprana para Riesgos Climáticos

## Descripción del Proyecto

El **Sistema Web de Monitoreo y Alerta Temprana para Riesgos Climáticos** es una aplicación web desarrollada para simular el monitoreo de condiciones climáticas en comunidades rurales y generar alertas ante posibles situaciones de riesgo.

El sistema permite visualizar información meteorológica, administrar sensores, generar alertas automáticamente mediante umbrales establecidos y consultar el historial de eventos climáticos detectados.

La aplicación cuenta con un sistema de autenticación mediante **JWT (JSON Web Token)** y autorización basada en roles, permitiendo diferenciar las funciones disponibles para usuarios **Administradores** y **Visualizadores**.

El proyecto está compuesto por un frontend desarrollado con **Angular**, un backend desarrollado con **C# y .NET**, y una base de datos **SQL Server**, utilizando **Docker y Docker Compose** para facilitar la ejecución y administración del entorno.

---

# Objetivo del Sistema

El objetivo principal es proporcionar una plataforma que permita simular un sistema de monitoreo climático capaz de:

* Supervisar diferentes variables meteorológicas.
* Mostrar información climática en tiempo real.
* Administrar sensores.
* Detectar condiciones climáticas de riesgo.
* Generar alertas automáticamente.
* Clasificar las alertas según su nivel de peligro.
* Consultar eventos climáticos históricos.
* Registrar las acciones realizadas por los usuarios.
* Controlar el acceso según el rol del usuario.

---

# Integrantes del Equipo

* **Isaías Morales Illescas** — Carné: `7690-23-705`
* **Joshua Daniel Aquino Díaz** — Carné: `7690-23-5762`
* **Jimmy Anderson Hernández Valladares** — Carné: `7690-23-16916`

---

# Tecnologías Utilizadas

| Componente                 | Tecnología       |
| -------------------------- | ---------------- |
| Frontend                   | Angular 20+      |
| Backend                    | C# / .NET 10+    |
| API                        | REST API         |
| Autenticación              | JWT              |
| Autorización               | Roles            |
| Base de Datos              | SQL Server 2022+ |
| Contenedores               | Docker           |
| Orquestación               | Docker Compose   |
| Gráficos                   | SVG              |
| Sistema operativo objetivo | GNU/Linux        |

---

# Funcionalidades Principales

## Monitoreo Climático

El sistema permite visualizar diferentes indicadores climáticos:

* Temperatura ambiente.
* Humedad relativa.
* Velocidad del viento.
* Nivel de lluvia.
* Nivel de río o reservorio.

Los valores pueden actualizarse de manera simulada cada **5 segundos**, permitiendo representar el comportamiento de un sistema de monitoreo en tiempo real.

---

# Sistema de Alertas

El sistema analiza las condiciones climáticas utilizando umbrales definidos para determinar el nivel de riesgo.

Las alertas se clasifican en cuatro niveles:

| Nivel       | Significado          |
| ----------- | -------------------- |
| 🟢 Verde    | Condiciones normales |
| 🟡 Amarillo | Precaución           |
| 🟠 Naranja  | Alerta               |
| 🔴 Rojo     | Emergencia           |

El sistema permite:

* Generar alertas automáticamente.
* Mostrar alertas recientes.
* Describir el motivo de cada alerta.
* Registrar las alertas generadas.
* Simular alertas manualmente para realizar pruebas.

---

# Administración de Sensores

Los sensores representan dispositivos físicos o virtuales utilizados para obtener información climática.

Cada sensor puede contener:

* ID.
* Nombre.
* Ubicación.
* Estado.
* Tipo de sensor.

Los usuarios administradores pueden:

* Agregar sensores.
* Editar sensores.
* Activar sensores.
* Desactivar sensores.
* Reiniciar el sistema de monitoreo.

Los usuarios visualizadores únicamente pueden consultar la información.

---

# Dashboard

El dashboard principal concentra la información más importante del sistema.

Incluye:

* Estado global del riesgo.
* Temperatura.
* Humedad.
* Velocidad del viento.
* Nivel de lluvia.
* Nivel del río.
* Gráficos históricos.
* Gráficos en tiempo real.
* Lista de sensores.
* Estado de sensores.
* Alertas recientes.
* Historial de eventos.

Los gráficos se generan utilizando **SVG**, evitando depender de librerías externas de visualización.

---

# Historial de Eventos

El sistema permite consultar eventos y fenómenos climáticos registrados.

Entre los eventos que pueden representarse se encuentran:

* Inundaciones.
* Sequías.
* Tormentas.
* Heladas.
* Incendios forestales.

Cada evento puede almacenar:

* ID del evento.
* Tipo de fenómeno.
* Descripción.
* Fecha y hora.

---

# Usuarios y Roles

El sistema maneja dos roles principales.

## Administrador

El administrador tiene permisos de lectura y escritura.

Puede:

* Visualizar el dashboard.
* Consultar sensores.
* Agregar sensores.
* Editar sensores.
* Activar sensores.
* Desactivar sensores.
* Reiniciar el sistema.
* Consultar alertas.
* Simular alertas.
* Limpiar el historial de alertas.
* Consultar eventos.
* Ejecutar acciones registradas en la bitácora.
* Cerrar sesión.

## Visualizador

El visualizador tiene permisos de solo lectura.

Puede:

* Visualizar el dashboard.
* Consultar indicadores meteorológicos.
* Consultar sensores.
* Consultar el estado de los sensores.
* Consultar alertas.
* Consultar eventos históricos.

No puede realizar operaciones que modifiquen información.

Por seguridad, los permisos se validan tanto en Angular como en el backend. Ocultar un botón en el frontend no constituye por sí solo un mecanismo de seguridad.

---

# Autenticación

La aplicación utiliza **JWT (JSON Web Token)** para autenticar a los usuarios.

El flujo general es:

1. El usuario ingresa sus credenciales.
2. Angular envía las credenciales al backend.
3. El backend valida la información.
4. Si las