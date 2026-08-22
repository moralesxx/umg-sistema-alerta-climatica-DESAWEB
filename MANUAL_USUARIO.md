# Manual de Usuario

# Sistema Web de Monitoreo y Alerta Temprana para Riesgos Climáticos

## 1. Introducción

El **Sistema Web de Monitoreo y Alerta Temprana para Riesgos Climáticos** permite consultar condiciones climáticas, supervisar sensores, visualizar alertas y revisar el historial de eventos de una comunidad.

El sistema cuenta con acceso mediante usuario y contraseña y utiliza diferentes permisos según el rol del usuario.

---

## 2. Requisitos de Uso

Para utilizar el sistema se necesita:

- Un navegador web actualizado.
- Acceso a la aplicación.
- Una cuenta registrada.
- Conexión con los servicios del sistema.

---

## 3. Crear una Cuenta

Si el usuario todavía no posee una cuenta, puede registrarse desde la pantalla de inicio de sesión.

![Crear cuenta](02-registro.png)

### Pasos

1. Seleccione **Crear cuenta**.
2. Escriba su **nombre completo**.
3. Ingrese su **correo electrónico**.
4. Escriba una contraseña de mínimo 6 caracteres.
5. Confirme la contraseña.
6. Acepte los términos de uso.
7. Presione **Crear cuenta**.

Al completar correctamente el registro, el usuario podrá ingresar al sistema.

---

## 4. Inicio de Sesión

![Inicio de sesión](01-login.png)

En la pantalla de acceso se solicitan:

- Correo electrónico.
- Contraseña.

### Para iniciar sesión

1. Ingrese el correo electrónico registrado.
2. Escriba la contraseña.
3. Presione **Iniciar sesión**.
4. Si las credenciales son correctas, se mostrará el dashboard principal.

La opción **Mostrar** permite visualizar temporalmente la contraseña.

La opción **Recordarme** permite mantener la información de acceso según la configuración de la aplicación.

---

## 5. Dashboard Principal

Después de iniciar sesión se muestra el panel principal del sistema.

![Dashboard](03-dashboard.png)

El dashboard presenta información climática en tiempo real y diferentes herramientas de monitoreo.

### Indicadores climáticos

En la parte superior se muestran:

- **Temperatura:** temperatura ambiente.
- **Humedad:** porcentaje de humedad relativa.
- **Viento:** velocidad del viento.
- **Lluvia:** cantidad registrada por el pluviómetro.
- **Río / Cauce:** nivel del río o reservorio.

Los valores se actualizan automáticamente cada **5 segundos**.

---

## 6. Estado de Riesgo

En la parte superior del dashboard se muestra el **Estado de Riesgo** de la comunidad.

Los niveles utilizados son:

| Nivel | Significado |
|---|---|
| Verde | Condiciones normales |
| Amarillo | Precaución |
| Naranja | Alerta |
| Rojo | Emergencia |

El nivel mostrado depende de las condiciones climáticas detectadas por el sistema.

---

## 7. Gráfico de Monitoreo

El dashboard contiene un gráfico de evolución histórica y en tiempo real.

El gráfico permite observar:

- Evolución de la temperatura.
- Evolución del nivel del río.

La información se actualiza automáticamente y permite visualizar cambios en las condiciones monitoreadas.

---

## 8. Sensores

La sección **Sensores Activos en la Comunidad** muestra los sensores registrados.

![Sensores, alertas y eventos](04-sensores-alertas-eventos.png)

La información mostrada incluye:

- ID del sensor.
- Nombre.
- Ubicación.
- Estado.
- Acciones disponibles.

Un sensor puede aparecer como **Activo** o **Inactivo**.

---

## 9. Administración de Sensores

Los usuarios con permisos de administrador pueden gestionar los sensores.

### Agregar sensor

1. Presione **+ Agregar Sensor**.
2. Complete la información solicitada.
3. Guarde el sensor.
4. El nuevo sensor aparecerá en la lista.

### Editar sensor

1. Localice el sensor.
2. Presione **Editar**.
3. Modifique la información necesaria.
4. Guarde los cambios.

### Activar o desactivar

El administrador puede cambiar el estado de los sensores para indicar si se encuentran disponibles para el monitoreo.

---

## 10. Reiniciar Sistema

El botón **Reiniciar Sistema** permite reiniciar el estado de monitoreo utilizado por la aplicación.

Esta función está disponible para usuarios con permisos administrativos.

---

## 11. Alertas en Tiempo Real

La sección **Alertas en Tiempo Real** muestra las alertas generadas por el sistema.

Cada alerta indica:

- Nivel de riesgo.
- Hora de generación.
- Descripción de la situación detectada.

Las alertas pueden corresponder a diferentes niveles de riesgo, desde condiciones normales hasta emergencias.

---

## 12. Simular una Alerta

El sistema permite generar alertas de prueba mediante el botón **Simular Alerta**.

Esta función sirve para comprobar el funcionamiento del sistema de alertas.

### Pasos

1. Presione **Simular Alerta**.
2. El sistema genera una alerta de prueba.
3. La alerta aparece en la sección de alertas recientes.
4. El evento correspondiente puede aparecer también en el historial.

Esta función está disponible para usuarios administradores.

---

## 13. Limpiar Alertas

El botón **Limpiar** permite eliminar las alertas mostradas en el historial de alertas.

Esta operación modifica información y, por lo tanto, requiere permisos de administrador.

---

## 14. Historial de Eventos

El historial permite consultar los fenómenos climáticos registrados por el sistema.

Cada registro puede mostrar:

- ID.
- Tipo de fenómeno.
- Descripción del riesgo.
- Fecha y hora.

Entre los fenómenos pueden encontrarse:

- Inundaciones.
- Incendios forestales.
- Sequías.
- Tormentas.
- Otros eventos climáticos.

El botón **Actualizar** permite consultar nuevamente la información disponible.

---

## 15. Roles de Usuario

El sistema contempla dos tipos principales de usuarios.

### Administrador

Puede:

- Consultar el dashboard.
- Consultar sensores.
- Agregar sensores.
- Editar sensores.
- Activar o desactivar sensores.
- Reiniciar el sistema.
- Consultar alertas.
- Simular alertas.
- Limpiar alertas.
- Consultar eventos.
- Realizar operaciones administrativas.

### Visualizador

Puede:

- Consultar el dashboard.
- Ver indicadores climáticos.
- Consultar sensores.
- Consultar alertas.
- Consultar eventos.

No puede realizar operaciones que modifiquen la información del sistema.

---

## 16. Cerrar Sesión

Para salir del sistema:

1. Presione **Cerrar sesión** en la parte superior derecha.
2. El sistema finalizará la sesión actual.
3. El usuario será enviado nuevamente a la pantalla de inicio de sesión.

Se recomienda cerrar sesión al terminar de utilizar el sistema, especialmente cuando se comparte el equipo con otras personas.

---

## 17. Actualización de Información

Los indicadores principales del dashboard se actualizan automáticamente cada **5 segundos**.

Además, algunas secciones cuentan con botones para actualizar manualmente la información.

Esto permite mantener los datos mostrados en pantalla sincronizados con la información disponible en el sistema.

---

## 18. Recomendaciones de Uso

- Mantener las credenciales de acceso privadas.
- Cerrar sesión al terminar.
- Verificar el nivel de riesgo antes de realizar acciones.
- Revisar las alertas recientes cuando el estado de riesgo cambie.
- Los administradores deben realizar cambios en sensores únicamente cuando sea necesario.
- Utilizar la simulación de alertas principalmente para pruebas.

---

## 19. Resumen del Uso

```text
Crear cuenta
     ↓
Iniciar sesión
     ↓
Cerrar sesión
```

## 20. Conclusión

El sistema proporciona una interfaz centralizada para consultar información climática, supervisar sensores y visualizar alertas tempranas.

La separación de permisos permite que los administradores gestionen la información mientras que los usuarios visualizadores puedan consultar el estado del sistema sin realizar modificaciones.