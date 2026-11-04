/*
  ESP32 -> Sistema de Alerta Climática (WiFi, sin PC intermedia)
  Mismos sensores que la versión UNO. Pines ESP32: DHT11 en 4, agua en 34 (ADC), llama en 35 (ADC).
  El ESP32 tiene ADC de 12 bits (0-4095): se escala a 0-1023 para reutilizar la calibración.
  Bibliotecas: "DHT sensor library" (Adafruit). WiFi/HTTPClient vienen con el paquete ESP32.
*/
#include <WiFi.h>
#include <WiFiClientSecure.h>
#include <HTTPClient.h>
#include <DHT.h>

const char* WIFI_SSID = "SU_WIFI";
const char* WIFI_PASS = "SU_CLAVE_WIFI";
// Use HTTPS en producción. Para pruebas locales por HTTP, ponga http://IP_PC:8080/api/sensors/ingest
const char* URL       = "https://sistema-climatico.example/api/sensors/ingest";
const char* DEVICE_KEY = "PEGUE_AQUI_DEVICE_API_KEY";

#define DHTPIN 4
DHT dht(DHTPIN, DHT11);
const int PIN_AGUA = 34, PIN_FUEGO = 35, PIN_BUZZER = 25;
const unsigned long INTERVALO_MS = 10000;
const int AGUA_CRUDO_MAX = 520; const float AGUA_NIVEL_MAX = 5.0;

float promedio(int pin) { long s = 0; for (int i = 0; i < 10; i++) { s += analogRead(pin); delay(5); } return (s / 10.0) * 1023.0 / 4095.0; }

void conectar() {
  if (WiFi.status() == WL_CONNECTED) return;
  WiFi.begin(WIFI_SSID, WIFI_PASS);
  for (int i = 0; i < 40 && WiFi.status() != WL_CONNECTED; i++) delay(500);
}

void setup() {
  Serial.begin(115200); dht.begin(); pinMode(PIN_BUZZER, OUTPUT); conectar();
}

void loop() {
  conectar();
  float t = dht.readTemperature(), h = dht.readHumidity();
  float nivel = constrain(promedio(PIN_AGUA) / AGUA_CRUDO_MAX, 0.0, 1.0) * AGUA_NIVEL_MAX;
  float fuego = (1023.0 - promedio(PIN_FUEGO)) * 1000.0 / 1023.0;
  digitalWrite(PIN_BUZZER, fuego >= 300 ? HIGH : LOW);

  String j = "{\"lecturas\":[";
  if (!isnan(t)) j += "{\"codigo\":\"ARD-TEMP\",\"valor\":" + String(t, 2) + "},";
  if (!isnan(h)) j += "{\"codigo\":\"ARD-HUM\",\"valor\":" + String(h, 2) + "},";
  j += "{\"codigo\":\"ARD-AGUA\",\"valor\":" + String(nivel, 2) + "},";
  j += "{\"codigo\":\"ARD-FUEGO\",\"valor\":" + String(fuego, 2) + "}]}";

  if (WiFi.status() == WL_CONNECTED) {
    WiFiClientSecure client; client.setInsecure();   // PRUEBAS. En producción: client.setCACert(<certificado raíz>)
    HTTPClient http;
    bool seguro = String(URL).startsWith("https");
    if (seguro ? http.begin(client, URL) : http.begin(URL)) {
      http.addHeader("Content-Type", "application/json");
      http.addHeader("X-Device-Key", DEVICE_KEY);
      int code = http.POST(j);
      Serial.printf("POST %d  %s\n", code, j.c_str());
      http.end();
    }
  }
  delay(INTERVALO_MS);
}
