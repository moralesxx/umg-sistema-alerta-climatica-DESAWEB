# Guía: conectar un Arduino al sistema (paso a paso)

## 00. Conexión rápida Arduino ↔ PC ↔ sistema (Windows)

1. **Levante el sistema** (`docker compose up -d`) y confirme `curl.exe -s http://localhost:8080/health/services`.
2. **Clave de dispositivo:** en el `.env` debe existir `DEVICE_API_KEY=<texto de 16+ caracteres>` (si no, la API responde 503). Tras cambiarla: `docker compose up -d sensor-service`.
3. **Cree los sensores** en el sistema con los códigos `ARD-*` (ver tabla de la sección 11) — pantalla *Sensores*.
4. **Pruebe la API sin Arduino** (debe decir `aceptadas`): `python scripts/demo_alertas.py rojo --arduino`.
5. **Conecte el Arduino por USB**, abra el IDE, suba `iot/sensores_uno_completo/sensores_uno_completo.ino` (o `sensores_uno`) y **cierre el Monitor Serie**.
6. **Busque el puerto:** `cd iot/bridge && pip install -r requirements.txt && python bridge.py --listar` (ej. `COM3`).
7. **Ejecute el puente:** `python bridge.py --puerto COM3 --clave <DEVICE_API_KEY>` → verá `[OK] aceptadas=…` cada 10 s.
8. **Vea los datos** en Dashboard/Lecturas y provoque alertas (potenciómetro, llama, agua); recibirá correo si configuró destinatarios en *Notificaciones*.

> Mientras el Arduino envía datos de un sensor, el simulador lo omite. Para usar **solo** hardware real: `SIMULADOR_HABILITADO=false` en `.env`.
> **Demostración sin hardware:** `python scripts/demo_alertas.py todas` recorre Verde → Amarillo → Naranja → Rojo usando la misma API.

### Claves por dispositivo (recomendado)
En lugar de compartir la clave general del `.env`, el Administrador puede registrar cada Arduino en la pantalla **Dispositivos** (menú lateral): se genera una clave propia (`dk_…`) que **solo se muestra una vez**, puede limitarse a sensores con un prefijo (p. ej. `ARD-`) y se **revoca** con un clic sin afectar a los demás. Se usa igual: `bridge.py --clave dk_…` o la cabecera `X-Device-Key`. La clave general `DEVICE_API_KEY` sigue funcionando como respaldo.

## 0. Cómo funciona (la idea completa)

```
 Sensores ──► Arduino ──USB──► Puente (bridge.py, en su PC) ──HTTP POST──► API Gateway ──► Sensor Service ──► Alert Service
 (humedad,     lee cada 10 s     lee el puerto serie y                      /api/sensors/ingest   guarda la lectura   evalúa reglas
  agua, fuego)  imprime JSON     reenvía a la API                                                  en SQL Server       y crea alertas
                                                                                                       │
                                                         Dashboard (Angular) ◄── Report Service ◄──────┘
```

* El Arduino **no habla directamente con la base de datos**: solo llama a una **API REST** (`POST /api/sensors/ingest`). Eso es lo que pide la fase final.
* El Arduino **UNO no tiene WiFi**, por eso usa un **puente** (script Python en una PC conectada por USB). Si tiene un **ESP32/ESP8266** (con WiFi) puede enviar directo, sin PC: use `iot/sensores_esp32`.
* La API ya está creada en el proyecto (`services/sensor-service`). Se protege con una **clave de dispositivo** (`DEVICE_API_KEY`) enviada en el header `X-Device-Key`, porque un Arduino no puede iniciar sesión como un usuario.
* La **hora la pone el servidor**: el Arduino no tiene reloj confiable.
* Cada sensor físico se representa como un **sensor del sistema con un código** (`ARD-TEMP`, `ARD-HUM`, `ARD-AGUA`, `ARD-FUEGO`). La lectura se une al sensor por ese código.

## 1. Materiales
| Componente | Para qué | Conexión (UNO) |
|---|---|---|
| Arduino UNO + cable USB | cerebro | — |
| DHT11 (o DHT22) | temperatura y humedad | VCC→5V, GND→GND, DATA→D2 (use módulo de 3 pines o resistencia 10 kΩ DATA↔5V) |
| Sensor de nivel de agua (analógico) | agua / lluvia / nivel de río | +→5V, −→GND, S→A0 |
| Módulo sensor de llama (LM393) | fuego / calor | VCC→5V, GND→GND, **AO**→A1 |
| Buzzer activo (opcional) | alarma local | +→D8, −→GND |
| Protoboard y cables dupont | — | — |

> Agua: mantenga la placa del sensor lejos de las conexiones; **solo sumerja la parte con las pistas**, y no lo deje permanentemente mojado (se corroe).
> Fuego: pruebe con un encendedor a 20–30 cm; **no** acerque la llama al sensor ni a la protoboard.

## 2. Preparar el Arduino
1. Instale el **Arduino IDE 2.x**.
2. *Herramientas → Administrar bibliotecas* → instale **“DHT sensor library” (Adafruit)** (acepte instalar “Adafruit Unified Sensor”).
3. Abra `iot/sensores_uno/sensores_uno.ino`, elija *Herramientas → Placa → Arduino UNO* y el **Puerto** (COM3, /dev/ttyACM0…).
4. **Subir**. Abra el *Monitor Serie* a **9600** baudios: cada 10 s debe verse una línea como  
   `{"lecturas":[{"codigo":"ARD-TEMP","valor":24.50},{"codigo":"ARD-HUM","valor":55.00},{"codigo":"ARD-AGUA","valor":0.00},{"codigo":"ARD-FUEGO","valor":3.91}]}`
5. **Cierre el Monitor Serie** antes del paso 5 (solo un programa puede usar el puerto).

## 3. Preparar el sistema
1. Genere la clave y póngala en `.env`:  
   `openssl rand -hex 24` → `DEVICE_API_KEY=<resultado>` (mínimo 16 caracteres; vacía = ingesta deshabilitada).
2. Reinicie: `docker compose up -d --build sensor-service gateway`.
3. Entre al sistema como administrador → **Sensores → Nuevo** y cree 4 sensores **con estos códigos exactos** y comunidad real:

| Código | Tipo de sensor | Nombre sugerido |
|---|---|---|
| `ARD-TEMP` | Temperatura Ambiente | Arduino – Temperatura |
| `ARD-HUM` | Humedad Relativa | Arduino – Humedad |
| `ARD-AGUA` | Nivel de Río | Arduino – Nivel de agua |
| `ARD-FUEGO` | Humo / Incendio | Arduino – Fuego |

   Mientras un sensor reciba lecturas reales, **el simulador lo omite** (vuelve a simular si pasan 2 min sin datos).
4. **Pruebe la API sin hardware** (debe responder `{"aceptadas":1,...}`):
```bash
curl -X POST http://localhost:8080/api/sensors/ingest \
  -H "Content-Type: application/json" -H "X-Device-Key: SU_CLAVE" \
  -d '{"lecturas":[{"codigo":"ARD-TEMP","valor":27.3}]}'
```
   Errores típicos: `401` clave incorrecta · `503` `DEVICE_API_KEY` vacía · `400` código inexistente o sensor inactivo.

## 4. Conectar el puente
```bash
cd iot/bridge && pip install -r requirements.txt
python bridge.py --simular --clave SU_CLAVE                       # 1) pruebe todo SIN Arduino
python bridge.py --puerto COM3 --clave SU_CLAVE                   # 2) con el Arduino (Linux/Mac: --puerto /dev/ttyACM0)
# si la API no está en localhost:  --url https://sistema-climatico.example/api/sensors/ingest
```
Verá `[OK] aceptadas=4` cada 10 s. Si se cae la red, el puente guarda hasta 200 envíos y los reintenta.

## 5. Ver los datos en el sistema
* **Dashboard**: valores actuales y gráfica por tipo (se actualizan solos).
* **Lecturas**: filtre por sensor `ARD-…` — verá las lecturas con su hora.
* **Alertas**: acerque una llama (ARD-FUEGO ≥ 300 → alerta **Roja** de *Incendio forestal*) o sumerja el sensor de agua (≥ 3.5 m Amarilla, ≥ 5 m Roja). Se crea la alerta, el evento y queda en la bitácora.
* **Reportes**: genere “Estado de sensores” o “Lecturas” filtrando por sensor y descargue el PDF.

## 6. Calibración (importante para que los valores tengan sentido)
* **Agua**: con el sensor seco anote la lectura cruda (`analogRead(A0)`, ≈0) y sumergido al máximo (típico 450–600). Ponga ese valor en `AGUA_CRUDO_MAX` y en `AGUA_NIVEL_MAX` los metros que representa (por defecto 5 m = escala del sistema).
* **Fuego**: el módulo de llama tiene un potenciómetro; ajústelo hasta que sin llama la lectura sea ≈0 (en la escala 0–1000) y con llama cercana > 300.
* **Reglas**: las reglas de alerta (pantalla *Reglas de alerta*) definen los umbrales; ajústelas a sus valores reales.

## 7. Alternativa: ESP32 por WiFi (sin PC)
Abra `iot/sensores_esp32/sensores_esp32.ino`, ponga `WIFI_SSID`, `WIFI_PASS`, `URL` (para pruebas en la misma red: `http://IP_DE_SU_PC:8080/api/sensors/ingest`; el gateway publica el puerto 8080 solo en loopback — para eso cambie el mapeo de puertos en `docker-compose.yml` o use el dominio con HTTPS) y `DEVICE_KEY`. Sube con el paquete “esp32 by Espressif”.

## 8. Seguridad (lo que evaluarán)
* **HTTPS** en producción (el Nginx de borde ya lo termina); la clave viaja en un header, nunca en la URL.
* No suba la clave a Git (`.env` está ignorado). Si se filtra, cámbiela en `.env` y reinicie `sensor-service`.
* El endpoint tiene **límite de 300 solicitudes/min por IP** en el gateway y la comparación de clave es de tiempo constante.
* Validaciones: sensor existente y activo, valor numérico en rango, máximo 50 lecturas por solicitud.

## 9. Para la fase final (ideas probables)
* **Una clave por dispositivo** (tabla de dispositivos con revocación y “último contacto”).
* **MQTT** (Mosquitto) en lugar de HTTP para muchos dispositivos.
* **Alerta de dispositivo sin comunicación** (ya existe “Sin comunicación” en el estado de sensores: pasa a rojo si deja de enviar > 5 min).
* **OAuth2/OIDC** para usuarios; los dispositivos seguirían con clave o con *client credentials*.
* Más sensores: suelo (YL-69), lluvia (YL-83), MQ-2 (humo), presión BMP280.

## 10. Problemas frecuentes
| Síntoma | Causa / solución |
|---|---|
| `Permission denied /dev/ttyACM0` | `sudo usermod -aG dialout $USER` y reinicie sesión |
| El puente no muestra nada | Monitor Serie abierto, puerto equivocado o baudios ≠ 9600 |
| `nan` / falta temperatura | DHT mal conectado (se omite, no se envían NaN) |
| `[ERROR 400] Sensor no registrado` | Cree el sensor con el **código exacto** (`ARD-TEMP`…) |
| Valores de agua siempre 0 o 5 | Calibre `AGUA_CRUDO_MAX`; revise que use A0 y no un pin digital |

## 11. Kit completo: componentes para simular TODO lo posible

El sistema maneja **8 tipos de sensor**. Con los componentes de esta tabla puede cubrir los 8; donde no sea posible reproducir el fenómeno real (viento, lluvia fuerte, río), un **potenciómetro** de 10 kΩ lo simula: al girarlo, el valor sube o baja y puede provocar alertas de cualquier nivel en la demostración.

| Variable del sistema | Componente real | Alternativa para simular | Pin (UNO) | Código en el sistema |
|---|---|---|---|---|
| Temperatura Ambiente | DHT11 / DHT22 (o LM35) | Encendedor/secador a distancia segura | D2 | `ARD-TEMP` |
| Humedad Relativa | DHT11 / DHT22 (mismo módulo) | Aliento cerca del sensor | D2 | `ARD-HUM` |
| Velocidad del Viento | Anemómetro de copas (interruptor reed) | **Potenciómetro** (o ventilador + hélice con sensor de efecto Hall) | A0 | `ARD-VIENTO` |
| Nivel de Lluvia | Sensor de lluvia YL-83 / pluviómetro de balancín | Gotas de agua sobre la placa | A1 | `ARD-LLUVIA` |
| Humo / Incendio (humo) | MQ-2 (humo/gas) | Humo de fósforo apagado a distancia | A2 | `ARD-HUMO` |
| Humo / Incendio (fuego) | Módulo de llama LM393 | Encendedor a 20–30 cm | A3 | `ARD-FUEGO` |
| Nivel de Río | **Ultrasónico HC-SR04** sobre un recipiente | Mover la mano/objeto frente al sensor | D3 (TRIG), D4 (ECHO) | `ARD-RIO` |
| Nivel de Reservorio | Segundo HC-SR04 | Mover un objeto | D5 (TRIG), D6 (ECHO) | `ARD-RESERV` |
| Presión Atmosférica | BMP280 (I2C, 3.3 V) | — | A4 (SDA), A5 (SCL) | `ARD-PRES` |
| Alarma local (opcional) | 3 LEDs (verde, amarillo, rojo) + resistencias 220 Ω + buzzer activo | — | D9, D10, D11, D8 | — |

**Lista de compra mínima:** Arduino UNO + cable USB, protoboard, cables dupont, DHT11 (o DHT22), 2 × HC-SR04, 2–3 potenciómetros 10 kΩ, sensor de lluvia YL-83, MQ-2, módulo de llama LM393, BMP280, 3 LEDs, 3 resistencias 220 Ω y un buzzer activo. **Opcional:** ESP32 (WiFi directo), pantalla LCD 16×2 I2C para ver valores, botón pulsador (simula un «botón de pánico» como evento manual).

**Sketch listo:** `iot/sensores_uno_completo/sensores_uno_completo.ino` (lee todo, imprime el JSON y maneja el semáforo local con los mismos umbrales de las reglas de demostración). Para empezar simple use `iot/sensores_uno`.

**Crear los sensores en el sistema** (pantalla *Sensores*, con estos códigos exactos): `ARD-TEMP` (Temperatura Ambiente), `ARD-HUM` (Humedad Relativa), `ARD-VIENTO` (Velocidad del Viento), `ARD-LLUVIA` (Nivel de Lluvia), `ARD-HUMO` y `ARD-FUEGO` (Humo / Incendio), `ARD-RIO` (Nivel de Río), `ARD-RESERV` (Nivel de Reservorio), `ARD-PRES` (Presión Atmosférica). Un código que no exista se rechaza solo para esa lectura; las demás se aceptan.

**Umbrales de las reglas de demostración** (se pueden cambiar en *Reglas de Alerta*): viento ≥ 60 Naranja y ≥ 90 Rojo · lluvia ≥ 100 Naranja y ≥ 150 Rojo · río ≥ 3.5 Amarillo y ≥ 5 Rojo · humedad ≤ 15 Amarillo · temperatura ≥ 38 Naranja y ≤ 2 Naranja (helada) · humo ≥ 300 Rojo. Presión y reservorio no traen regla: créela si la necesita.

**Precauciones del montaje**
* **MQ-2:** necesita 1–2 minutos de calentamiento y su valor «en reposo» varía; anote la línea base y réstela o ajuste la regla.
* **HC-SR04:** funciona a 5 V. Con un **ESP32** (lógica de 3.3 V) use un divisor de tensión en el pin ECHO.
* **BMP280:** alimentar a **3.3 V**, no a 5 V (salvo módulo con regulador).
* **Agua:** nunca moje la electrónica; solo las pistas del sensor, y sin alimentar permanentemente (se corroe).
* **Fuego:** use un encendedor a distancia y supervisión; nunca cerca de la protoboard ni de materiales inflamables.
* **Simulador:** con hardware real, desactívelo en `.env` (`SIMULADOR_HABILITADO=false`) para que no «rellene» datos falsos si el Arduino se desconecta más de 2 minutos.
