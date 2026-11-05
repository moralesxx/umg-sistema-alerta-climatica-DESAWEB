
#include <OneWire.h>
#include <DallasTemperature.h>
#include <DHT.h>

// ==========================================================
// CONFIGURACIÓN DE PINES
// ==========================================================

#define PIN_DS18B20 3
#define PIN_DHT11 4
#define TIPO_DHT DHT11

#define PIN_EMERGENCIA 2
#define PIN_LED 6
#define PIN_BUZZER 7

#define PIN_VIENTO A0

#define PIN_TRIG 9
#define PIN_ECHO 10

// ==========================================================
// SENSORES
// ==========================================================

OneWire oneWire(PIN_DS18B20);
DallasTemperature sensorTemperatura(&oneWire);
DHT sensorDHT(PIN_DHT11, TIPO_DHT);

// ==========================================================
// VARIABLES DE LECTURA
// ==========================================================

// Envío cada 2 s (antes 10 s). El DHT11 no admite lecturas más rápidas que ~1 s.
unsigned long ultimaLectura = 0;
const unsigned long INTERVALO = 2000;

// DS18B20 sin bloquear el programa: la conversión (10 bits ≈ 190 ms) se pide
// y se recoge después, en vez de detener todo ~750 ms con requestTemperatures().
const unsigned long ESPERA_DS18B20 = 200;
unsigned long inicioConversion = 0;
bool convirtiendo = false;

float temperatura = NAN;
float humedad = NAN;
float viento = 0;
float nivelRio = NAN;

// Si el DHT11 falla una lectura se conserva el último valor válido hasta este
// número de fallos seguidos (a 2 s por ciclo = 20 s); después se deja de enviar.
const byte MAX_FALLOS_DHT = 10;
byte fallosDHT = 0;
byte fallosDS = 0;
const byte MAX_FALLOS_DS = 3;

// Río (HC-SR04 sobre una maqueta del cauce). El sensor mira hacia abajo/al frente y mide la distancia
// al "agua" (en la maqueta, un vaso u objeto). Mientras más cerca está el objeto, más alto el río.
//   nivel (m) = (ALTURA_SENSOR_CM - distancia_cm) * ESCALA_METROS_POR_CM
// Con 20 cm y 0.30 m/cm (maqueta 1:30, 1 cm de la maqueta = 30 cm de río):
//   objeto a 20 cm o más -> 0.0 m | 12 cm -> 2.4 m | 8 cm -> 3.6 m (Amarillo) | 4 cm -> 4.8 m | 3 cm -> 5.1 m (Rojo)
// AJUSTAR: ALTURA_SENSOR_CM = distancia del sensor al "fondo del cauce" (mídela con regla).
const float ALTURA_SENSOR_CM = 20.0;
const float ESCALA_METROS_POR_CM = 0.30;

bool emergencia = false;

// ==========================================================
// CONTROL DEL BUZZER
// ==========================================================

enum NivelAlerta {
  VERDE,
  AMARILLO,
  NARANJA,
  ROJO
};

NivelAlerta nivelActual = VERDE;

unsigned long ultimoCambioBuzzer = 0;
bool buzzerActivo = false;

unsigned long ultimaComunicacion = 0;

const unsigned long TIEMPO_SEGURIDAD = 30000;

// ==========================================================
// RECEPCIÓN DE COMANDOS SERIAL
// ==========================================================

char comandoSerial[41];
byte longitudComando = 0;

void procesarComando(char* texto) {
  String comando = String(texto);
  comando.trim();
  comando.toUpperCase();

  if (comando == "NIVEL:VERDE") {
    nivelActual = VERDE;
  }
  else if (comando == "NIVEL:AMARILLO") {
    nivelActual = AMARILLO;
  }
  else if (comando == "NIVEL:NARANJA") {
    nivelActual = NARANJA;
  }
  else if (comando == "NIVEL:ROJO") {
    nivelActual = ROJO;
  }
  else {
    return;
  }

  ultimaComunicacion = millis();

  // Reinicia el patrón al cambiar el nivel.
  ultimoCambioBuzzer = 0;
  buzzerActivo = false;

  noTone(PIN_BUZZER);
}

void leerComandosSerial() {
  while (Serial.available() > 0) {
    char caracter = Serial.read();

    if (caracter == '\n') {
      if (longitudComando > 0) {
        comandoSerial[longitudComando] = '\0';
        procesarComando(comandoSerial);
        longitudComando = 0;
      }
    }
    else if (caracter != '\r') {
      if (longitudComando < 40) {
        comandoSerial[longitudComando++] = caracter;
      }
      else {
        longitudComando = 0;   // línea demasiado larga: se descarta
      }
    }
  }
}

// ==========================================================
// PATRONES DE SONIDO
// ==========================================================

void actualizarBuzzer() {
  unsigned long ahora = millis();

  // Si se pierde la comunicación, volver a Verde.
  if (
    nivelActual != VERDE &&
    ahora - ultimaComunicacion >= TIEMPO_SEGURIDAD
  ) {
    nivelActual = VERDE;
    buzzerActivo = false;
    noTone(PIN_BUZZER);
  }

  unsigned long intervaloEncendido = 0;
  unsigned long intervaloApagado = 0;
  unsigned int frecuencia = 0;

  switch (nivelActual) {

    case VERDE:
      noTone(PIN_BUZZER);
      buzzerActivo = false;
      return;

    case AMARILLO:
      intervaloEncendido = 180;
      intervaloApagado = 1200;
      frecuencia = 1000;
      break;

    case NARANJA:
      intervaloEncendido = 250;
      intervaloApagado = 450;
      frecuencia = 1500;
      break;

    case ROJO:
      intervaloEncendido = 180;
      intervaloApagado = 120;
      frecuencia = 2200;
      break;
  }

  unsigned long intervaloActual =
    buzzerActivo ? intervaloEncendido : intervaloApagado;

  if (ahora - ultimoCambioBuzzer >= intervaloActual) {
    ultimoCambioBuzzer = ahora;
    buzzerActivo = !buzzerActivo;

    if (buzzerActivo) {
      tone(PIN_BUZZER, frecuencia);
    }
    else {
      noTone(PIN_BUZZER);
    }
  }
}

// ==========================================================
// CONFIGURACIÓN INICIAL
// ==========================================================

void setup() {
  Serial.begin(9600);

  sensorTemperatura.begin();
  sensorTemperatura.setResolution(10);            // 0.25 °C, conversión ~190 ms
  sensorTemperatura.setWaitForConversion(false);  // no bloquear el loop
  sensorDHT.begin();

  pinMode(PIN_EMERGENCIA, INPUT_PULLUP);
  pinMode(PIN_LED, OUTPUT);

  pinMode(PIN_BUZZER, OUTPUT);
  noTone(PIN_BUZZER);

  pinMode(PIN_TRIG, OUTPUT);
  pinMode(PIN_ECHO, INPUT);

  digitalWrite(PIN_LED, LOW);
  digitalWrite(PIN_TRIG, LOW);

  ultimaComunicacion = millis();
  iniciarConversionTemperatura();   // primera conversión lista para el primer envío
}

// ==========================================================
// LECTURA DE TEMPERATURA (DS18B20, asíncrona)
// ==========================================================

void iniciarConversionTemperatura() {
  sensorTemperatura.requestTemperatures();   // no bloquea (setWaitForConversion(false))
  inicioConversion = millis();
  convirtiendo = true;
}

void recogerTemperatura() {
  if (!convirtiendo || millis() - inicioConversion < ESPERA_DS18B20) return;
  convirtiendo = false;

  float valor = sensorTemperatura.getTempCByIndex(0);

  if (valor != DEVICE_DISCONNECTED_C && valor >= -55 && valor <= 125 && valor != 85.0) {
    // 85.0 °C es el valor de arranque del DS18B20 cuando aún no convirtió (cable/alimentación débil).
    temperatura = valor;
    fallosDS = 0;
  }
  else if (++fallosDS >= MAX_FALLOS_DS) {
    temperatura = NAN;
  }
}

// ==========================================================
// LECTURA DE HUMEDAD (DHT11)
// ==========================================================

void leerHumedad() {
  float valor = sensorDHT.readHumidity();

  if (!isnan(valor) && valor >= 0 && valor <= 100) {
    humedad = valor;
    fallosDHT = 0;
  }
  else if (++fallosDHT >= MAX_FALLOS_DHT) {
    humedad = NAN;   // se conserva el último valor válido mientras no pasen demasiados fallos
  }
}

// ==========================================================
// LECTURA DE VIENTO (potenciómetro en A0, promedio para quitar ruido)
// ==========================================================

void leerViento() {
  analogRead(PIN_VIENTO);            // descarta la primera lectura tras cambiar de canal
  long suma = 0;

  for (byte i = 0; i < 8; i++) {
    suma += analogRead(PIN_VIENTO);
  }

  viento = ((suma / 8.0) / 1023.0) * 120.0;
}

// ==========================================================
// NIVEL DEL RÍO (HC-SR04, mediana de 5 mediciones)
// ==========================================================

float medirDistanciaCm() {
  digitalWrite(PIN_TRIG, LOW);
  delayMicroseconds(2);
  digitalWrite(PIN_TRIG, HIGH);
  delayMicroseconds(10);
  digitalWrite(PIN_TRIG, LOW);

  // Timeout 25 ms (~4 m): evita bloquear el programa si no hay eco.
  unsigned long duracion = pulseIn(PIN_ECHO, HIGH, 25000UL);

  if (duracion == 0) return NAN;

  return duracion * 0.0343 / 2.0;
}

void leerNivelRio() {
  float m[5];
  byte n = 0;

  for (byte i = 0; i < 5; i++) {
    float d = medirDistanciaCm();
    if (!isnan(d) && d >= 2 && d <= 400) m[n++] = d;
    delay(30);   // deja morir el eco anterior
  }

  if (n < 3) {
    nivelRio = NAN;   // sensor sin eco fiable: no se envía
    return;
  }

  for (byte i = 1; i < n; i++) {          // ordenamiento por inserción
    float x = m[i];
    int j = i - 1;
    while (j >= 0 && m[j] > x) { m[j + 1] = m[j]; j--; }
    m[j + 1] = x;
  }

  float nivel = (ALTURA_SENSOR_CM - m[n / 2]) * ESCALA_METROS_POR_CM;   // metros de río
  nivelRio = constrain(nivel, 0.0, 10.0);
}

// ==========================================================
// BOTÓN DE EMERGENCIA
// ==========================================================

void leerEmergencia() {
  emergencia = (digitalRead(PIN_EMERGENCIA) == LOW);

  // LED: fijo con el botón de emergencia; parpadea mientras el sistema esté en alerta ROJA.
  bool parpadeo = (nivelActual == ROJO) && ((millis() / 250) % 2 == 0);
  digitalWrite(PIN_LED, (emergencia || parpadeo) ? HIGH : LOW);
}

// ==========================================================
// ENVÍO DE LECTURAS JSON
// ==========================================================

void enviarLecturas() {
  Serial.print("{\"lecturas\":[");

  bool primera = true;

  // SEN-01: Temperatura DS18B20
  if (!isnan(temperatura)) {
    Serial.print("{\"codigo\":\"SEN-01\",\"valor\":");
    Serial.print(temperatura, 2);
    Serial.print("}");

    primera = false;
  }

  // SEN-02: Humedad DHT11
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

  // SEN-05: Nivel del río (HC-SR04)
  if (!isnan(nivelRio)) {
    Serial.print(",{\"codigo\":\"SEN-05\",\"valor\":");
    Serial.print(nivelRio, 2);
    Serial.print("}");
  }

  Serial.println("]}");
}

// ==========================================================
// BUCLE PRINCIPAL
// ==========================================================

void loop() {
  leerComandosSerial();

  actualizarBuzzer();

  leerEmergencia();

  recogerTemperatura();

  if (millis() - ultimaLectura >= INTERVALO) {
    ultimaLectura = millis();

    leerHumedad();
    leerViento();
    leerNivelRio();      // ~150 ms con eco; el buzzer se atiende de nuevo al volver al loop

    enviarLecturas();

    iniciarConversionTemperatura();   // la temperatura se recoge ~200 ms después y sale en el siguiente envío
  }
}
