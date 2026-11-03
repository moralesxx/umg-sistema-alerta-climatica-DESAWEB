/*
  Arduino UNO — KIT COMPLETO: cubre los 8 tipos de sensor del sistema (con componentes reales o simulados con potenciómetros)

  Variable (sistema)        Código         Componente                                  Pin
  Temperatura (°C)          ARD-TEMP       DHT11/DHT22                                 D2
  Humedad (%)               ARD-HUM        DHT11/DHT22 (mismo módulo)                  D2
  Viento (km/h)             ARD-VIENTO     Potenciómetro 10 kΩ (simula el anemómetro)  A0
  Lluvia (mm, relativo)     ARD-LLUVIA     Sensor de lluvia YL-83 (salida AO)          A1
  Humo (ppm, relativo)      ARD-HUMO       MQ-2 (salida AO)                            A2
  Fuego (0-1000)            ARD-FUEGO      Módulo de llama LM393 (salida AO)           A3
  Nivel de río (m)          ARD-RIO        Ultrasónico HC-SR04 #1  (TRIG D3, ECHO D4)
  Nivel de reservorio (m)   ARD-RESERV     Ultrasónico HC-SR04 #2  (TRIG D5, ECHO D6)
  Presión (hPa)             ARD-PRES       BMP280 por I2C (SDA A4, SCL A5, 3.3V)
  Alarma local              —              LEDs verde D9, amarillo D10, rojo D11; buzzer D8

  Bibliotecas: "DHT sensor library" (Adafruit) y "Adafruit BMP280 Library" (acepte instalar dependencias).
  Cada INTERVALO_MS imprime UNA línea JSON que lee iot/bridge/bridge.py.
  NO PROBADO EN HARDWARE: ajuste las constantes de calibración con sus componentes (ver docs/ARDUINO-GUIA.md).
*/
#include <DHT.h>
#include <Wire.h>
#define USAR_BMP 1                       // ponga 0 si no tiene BMP280
#if USAR_BMP
#include <Adafruit_BMP280.h>
Adafruit_BMP280 bmp;
bool bmpOk = false;
#endif

#define DHTTYPE DHT11                    // DHT22 si corresponde
DHT dht(2, DHTTYPE);

const int PIN_VIENTO = A0, PIN_LLUVIA = A1, PIN_HUMO = A2, PIN_FUEGO = A3;
const int RIO_TRIG = 3, RIO_ECHO = 4, RES_TRIG = 5, RES_ECHO = 6;
const int PIN_BUZZER = 8, LED_V = 9, LED_A = 10, LED_R = 11;

const unsigned long INTERVALO_MS = 10000;

// ---- Calibración ----
const float VIENTO_MAX_KMH   = 120.0;    // giro completo del potenciómetro
const float LLUVIA_MAX_MM    = 200.0;    // sensor completamente mojado
const float ALTURA_SENSOR_CM = 30.0;     // distancia del HC-SR04 al fondo del recipiente
const float NIVEL_MAX_M      = 5.0;      // metros que representa un recipiente lleno (escala del sistema)

unsigned long ultimo = 0;
bool primero = true;

float promedio(int pin) { long s = 0; for (int i = 0; i < 10; i++) { s += analogRead(pin); delay(3); } return s / 10.0; }

float distanciaCm(int trig, int echo) {
  digitalWrite(trig, LOW); delayMicroseconds(2);
  digitalWrite(trig, HIGH); delayMicroseconds(10); digitalWrite(trig, LOW);
  unsigned long us = pulseIn(echo, HIGH, 30000UL);          // timeout 30 ms (~5 m)
  if (us == 0) return -1;                                   // sin eco
  return us * 0.0343 / 2.0;
}

float nivelMetros(int trig, int echo) {
  float d = distanciaCm(trig, echo);
  if (d < 0) return -999;                                   // sin eco: se omite del JSON
  return constrain((ALTURA_SENSOR_CM - d) / ALTURA_SENSOR_CM, 0.0, 1.0) * NIVEL_MAX_M;
}

void emitir(const char* codigo, float valor, bool &primerElemento) {
  if (isnan(valor) || valor < -900) return;                 // no se envían NaN ni lecturas inválidas
  if (!primerElemento) Serial.print(',');
  primerElemento = false;
  Serial.print(F("{\"codigo\":\"")); Serial.print(codigo);
  Serial.print(F("\",\"valor\":"));  Serial.print(valor, 2); Serial.print('}');
}

void semaforo(int nivel) {                                  // 0 verde, 1 amarillo, 2 naranja, 3 rojo
  digitalWrite(LED_V, nivel == 0);
  digitalWrite(LED_A, nivel == 1 || nivel == 2);
  digitalWrite(LED_R, nivel >= 2);
  digitalWrite(PIN_BUZZER, nivel == 3);
}

void setup() {
  Serial.begin(9600);
  dht.begin();
  pinMode(RIO_TRIG, OUTPUT); pinMode(RIO_ECHO, INPUT); pinMode(RES_TRIG, OUTPUT); pinMode(RES_ECHO, INPUT);
  pinMode(PIN_BUZZER, OUTPUT); pinMode(LED_V, OUTPUT); pinMode(LED_A, OUTPUT); pinMode(LED_R, OUTPUT);
#if USAR_BMP
  bmpOk = bmp.begin(0x76) || bmp.begin(0x77);
#endif
  semaforo(0);
}

void loop() {
  if (!primero && millis() - ultimo < INTERVALO_MS) return;
  primero = false; ultimo = millis();

  float temp = dht.readTemperature(), hum = dht.readHumidity();
  float viento = promedio(PIN_VIENTO) / 1023.0 * VIENTO_MAX_KMH;
  float lluvia = (1023.0 - promedio(PIN_LLUVIA)) / 1023.0 * LLUVIA_MAX_MM;   // el YL-83 baja su salida al mojarse
  float humo   = promedio(PIN_HUMO) * 1000.0 / 1023.0;                       // MQ-2: calentar 20-60 s; restar la línea base si hace falta
  float fuego  = (1023.0 - promedio(PIN_FUEGO)) * 1000.0 / 1023.0;
  float rio    = nivelMetros(RIO_TRIG, RIO_ECHO);
  float reserv = nivelMetros(RES_TRIG, RES_ECHO);
  float pres   = -999;
#if USAR_BMP
  if (bmpOk) pres = bmp.readPressure() / 100.0;
#endif

  // Semáforo local con los mismos umbrales de las reglas de demostración
  int nivel = 0;
  if (rio >= 3.5 || (!isnan(hum) && hum <= 15)) nivel = 1;
  if (viento >= 60 || lluvia >= 100 || (!isnan(temp) && temp >= 38)) nivel = 2;
  if (viento >= 90 || lluvia >= 150 || rio >= 5 || humo >= 300 || fuego >= 300) nivel = 3;
  semaforo(nivel);

  bool p = true;
  Serial.print(F("{\"lecturas\":["));
  emitir("ARD-TEMP", temp, p);   emitir("ARD-HUM", hum, p);
  emitir("ARD-VIENTO", viento, p); emitir("ARD-LLUVIA", lluvia, p);
  emitir("ARD-HUMO", humo, p);   emitir("ARD-FUEGO", fuego, p);
  emitir("ARD-RIO", rio, p);     emitir("ARD-RESERV", reserv, p);
  emitir("ARD-PRES", pres, p);
  Serial.println(F("]}"));
}
