/*
  Arduino UNO -> Sistema de Alerta Climática (vía USB + puente en la PC)

  Sensores:
    DHT11 (temperatura y humedad)  -> pin digital 2
    Sensor de nivel de agua        -> A0   (señal "S")
    Sensor de llama (módulo LM393) -> A1   (salida analógica AO)
    Buzzer activo (opcional)       -> pin 8

  Cada INTERVALO_MS imprime UNA línea JSON por el puerto serie:
    {"lecturas":[{"codigo":"ARD-TEMP","valor":24.50}, ...]}
  El script iot/bridge/bridge.py la lee y la envía a la API del sistema.

  Biblioteca necesaria (Administrar bibliotecas): "DHT sensor library" de Adafruit (acepta instalar "Adafruit Unified Sensor").
*/
#include <DHT.h>

#define DHTPIN 2
#define DHTTYPE DHT11            // si su módulo es DHT22, cambie a DHT22
DHT dht(DHTPIN, DHTTYPE);

const int PIN_AGUA   = A0;
const int PIN_FUEGO  = A1;
const int PIN_BUZZER = 8;

// Códigos: deben existir como sensores en el sistema (pantalla Sensores).
const char C_TEMP[]  = "ARD-TEMP";
const char C_HUM[]   = "ARD-HUM";
const char C_AGUA[]  = "ARD-AGUA";
const char C_FUEGO[] = "ARD-FUEGO";

const unsigned long INTERVALO_MS = 10000;

// --- Calibración del sensor de agua: lea el valor crudo en seco y sumergido (ver guía) ---
const int   AGUA_CRUDO_MAX = 520;   // lectura cruda con el sensor sumergido al máximo
const float AGUA_NIVEL_MAX = 5.0;   // metros que representa ese máximo (escala del "Nivel de Río")

unsigned long ultimo = 0;
bool primero = true;

float promedio(int pin) {            // promedio de 10 muestras para reducir ruido
  long s = 0;
  for (int i = 0; i < 10; i++) { s += analogRead(pin); delay(5); }
  return s / 10.0;
}

void emitir(const char* codigo, float valor, bool &primerElemento) {
  if (!primerElemento) Serial.print(',');
  primerElemento = false;
  Serial.print(F("{\"codigo\":\""));
  Serial.print(codigo);
  Serial.print(F("\",\"valor\":"));
  Serial.print(valor, 2);
  Serial.print('}');
}

void setup() {
  Serial.begin(9600);
  dht.begin();
  pinMode(PIN_BUZZER, OUTPUT);
  digitalWrite(PIN_BUZZER, LOW);
}

void loop() {
  if (!primero && millis() - ultimo < INTERVALO_MS) return;
  primero = false;
  ultimo = millis();

  float temp = dht.readTemperature();
  float hum  = dht.readHumidity();

  float nivel = constrain(promedio(PIN_AGUA) / AGUA_CRUDO_MAX, 0.0, 1.0) * AGUA_NIVEL_MAX;

  // El módulo de llama baja su salida cuando detecta fuego: se invierte a una escala 0-1000
  // (equivale a la escala del tipo "Humo / Incendio", regla Rojo >= 300).
  float fuego = (1023.0 - promedio(PIN_FUEGO)) * 1000.0 / 1023.0;

  digitalWrite(PIN_BUZZER, fuego >= 300 ? HIGH : LOW);   // alarma local

  bool p = true;
  Serial.print(F("{\"lecturas\":["));
  if (!isnan(temp)) emitir(C_TEMP, temp, p);      // si el DHT falla, se omite (no se envían NaN)
  if (!isnan(hum))  emitir(C_HUM, hum, p);
  emitir(C_AGUA, nivel, p);
  emitir(C_FUEGO, fuego, p);
  Serial.println(F("]}"));
}
