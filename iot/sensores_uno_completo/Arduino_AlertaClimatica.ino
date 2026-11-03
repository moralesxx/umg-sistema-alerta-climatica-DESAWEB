
#include <OneWire.h>
#include <DallasTemperature.h>
#include <DHT.h>

// ==========================================
// CONFIGURACION DE PINES
// ==========================================

#define PIN_DS18B20 3
#define PIN_DHT11 4
#define TIPO_DHT DHT11

#define PIN_EMERGENCIA 2
#define PIN_LED 6

#define PIN_VIENTO A0

#define PIN_TRIG 9
#define PIN_ECHO 10

// ==========================================
// OBJETOS
// ==========================================

OneWire oneWire(PIN_DS18B20);
DallasTemperature sensorTemperatura(&oneWire);

DHT sensorDHT(PIN_DHT11, TIPO_DHT);

// ==========================================
// VARIABLES
// ==========================================

unsigned long ultimaLectura = 0;
const unsigned long INTERVALO = 10000;

float temperatura = NAN;
float humedad = NAN;
float viento = 0;

bool emergencia = false;

// ==========================================
// INICIALIZACION
// ==========================================

void setup() {
  Serial.begin(9600);

  sensorTemperatura.begin();
  sensorDHT.begin();

  pinMode(PIN_EMERGENCIA, INPUT_PULLUP);
  pinMode(PIN_LED, OUTPUT);

  pinMode(PIN_TRIG, OUTPUT);
  pinMode(PIN_ECHO, INPUT);

  digitalWrite(PIN_LED, LOW);
  digitalWrite(PIN_TRIG, LOW);
}

// ==========================================
// LECTURA DE SENSORES
// ==========================================

void leerTemperatura() {
  sensorTemperatura.requestTemperatures();

  float valor = sensorTemperatura.getTempCByIndex(0);

  if (valor != DEVICE_DISCONNECTED_C &&
      valor >= -55 && valor <= 125) {
    temperatura = valor;
  } else {
    temperatura = NAN;
  }
}

void leerHumedad() {
  float valor = dhtReadHumidity();

  if (!isnan(valor) && valor >= 0 && valor <= 100) {
    humedad = valor;
  } else {
    humedad = NAN;
  }
}

// Funcion auxiliar para leer el DHT11
float dhtReadHumidity() {
  return sensorDHT.readHumidity();
}

void leerViento() {
  int lectura = analogRead(PIN_VIENTO);

  viento = (lectura / 1023.0) * 120.0;
}

void leerEmergencia() {
  emergencia = (digitalRead(PIN_EMERGENCIA) == LOW);

  digitalWrite(PIN_LED, emergencia ? HIGH : LOW);
}

// ==========================================
// ENVIO JSON POR SERIAL
// ==========================================

void enviarLecturas() {
  Serial.print("{\"lecturas\":[");

  bool primera = true;

  // SEN-01: Temperatura
  if (!isnan(temperatura)) {
    Serial.print("{\"codigo\":\"SEN-01\",\"valor\":");
    Serial.print(temperatura, 2);
    Serial.print("}");
    primera = false;
  }

  // SEN-02: Humedad
  if (!isnan(humedad)) {
    if (!primera) Serial.print(",");

    Serial.print("{\"codigo\":\"SEN-02\",\"valor\":");
    Serial.print(humedad, 2);
    Serial.print("}");
    primera = false;
  }

  // SEN-03: Viento
  if (!primera) Serial.print(",");

  Serial.print("{\"codigo\":\"SEN-03\",\"valor\":");
  Serial.print(viento, 2);
  Serial.print("}");

  Serial.println("]}");
}

// ==========================================
// CICLO PRINCIPAL
// ==========================================

void loop() {
  leerEmergencia();

  if (millis() - ultimaLectura >= INTERVALO) {
    ultimaLectura = millis();

    leerTemperatura();
    leerHumedad();
    leerViento();

    enviarLecturas();
  }
}
