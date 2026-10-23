#!/usr/bin/env bash
# Prueba de humo de punta a punta a través del API Gateway.
# Uso:  ADMIN_PASSWORD='...' ./scripts/smoke.sh [http://localhost:8080]
set -uo pipefail
GW="${1:-http://localhost:8080}"; PASS="${ADMIN_PASSWORD:?defina ADMIN_PASSWORD}"
ok=0; fail=0
chk(){ if [ "$2" = "$3" ]; then echo "  OK   $1"; ok=$((ok+1)); else echo "  FAIL $1 (esperado $2, obtenido $3)"; fail=$((fail+1)); fi; }
code(){ curl -s -o /dev/null -w '%{http_code}' "$@"; }
jget(){ python3 -c "import sys,json;d=json.load(sys.stdin);print(eval(sys.argv[1]))" "$1"; }

echo "== Salud"; chk "gateway /health/services" 200 "$(code $GW/health/services)"
echo "== Seguridad"; chk "sin token -> 401" 401 "$(code $GW/api/communities)"
chk "ruta interna oculta -> 404" 404 "$(code -X POST $GW/api/audit/internal)"
chk "login incorrecto -> 401" 401 "$(code -X POST $GW/api/auth/login -H 'Content-Type: application/json' -d '{"correo":"admin@alerta.com","contrasenia":"x"}')"
TOKEN=$(curl -s -X POST $GW/api/auth/login -H 'Content-Type: application/json' -d "{\"correo\":\"admin@alerta.com\",\"contrasenia\":\"$PASS\"}" | jget "d['token']")
chk "login admin" 1 "$([ -n "$TOKEN" ] && echo 1 || echo 0)"; H="Authorization: Bearer $TOKEN"
echo "== Comunidades / Sensores"
NAME="Smoke $(date +%s)"
CID=$(curl -s -X POST $GW/api/communities -H "$H" -H 'Content-Type: application/json' -d "{\"nombre\":\"$NAME\",\"municipio\":\"M\",\"departamento\":\"D\",\"pais\":\"Guatemala\",\"latitud\":14.6,\"longitud\":-90.5,\"descripcion\":\"prueba\",\"estado\":true}" | jget "d['comunidadId']")
chk "crear comunidad" 1 "$([ -n "$CID" ] && echo 1 || echo 0)"
chk "comunidad duplicada -> 409" 409 "$(code -X POST $GW/api/communities -H "$H" -H 'Content-Type: application/json' -d "{\"nombre\":\"$NAME\",\"municipio\":\"M\",\"departamento\":\"D\",\"pais\":\"G\",\"latitud\":1,\"longitud\":1,\"descripcion\":\"x\",\"estado\":true}")"
TIPO=$(curl -s $GW/api/sensors/types -H "$H" | jget "[t['tipoSensorId'] for t in d if 'Temperatura' in t['nombreTipo']][0]")
SID=$(curl -s -X POST $GW/api/sensors -H "$H" -H 'Content-Type: application/json' -d "{\"nombreSensor\":\"Temp smoke\",\"tipoSensorId\":$TIPO,\"ubicacion\":\"Patio\",\"comunidadId\":$CID,\"fechaInstalacion\":\"2026-01-01\",\"descripcion\":\"prueba\",\"estado\":true}" | jget "d['sensorId']")
chk "crear sensor" 1 "$([ -n "$SID" ] && echo 1 || echo 0)"
echo "== Ingesta de dispositivos (Arduino)"
chk "ingesta sin clave -> 401/503" 1 "$([ "$(code -X POST $GW/api/sensors/ingest -H 'Content-Type: application/json' -d '{}')" = 401 ] || [ "$(code -X POST $GW/api/sensors/ingest -H 'Content-Type: application/json' -d '{}')" = 503 ] && echo 1 || echo 0)"
if [ -n "${DEVICE_API_KEY:-}" ]; then
  COD=$(curl -s $GW/api/sensors/$SID -H "$H" | jget "d['codigo']")
  chk "ingesta con clave -> 200" 200 "$(code -X POST $GW/api/sensors/ingest -H "X-Device-Key: $DEVICE_API_KEY" -H 'Content-Type: application/json' -d "{\"lecturas\":[{\"codigo\":\"$COD\",\"valor\":22.5}]}")"
  chk "ingesta código desconocido -> 400" 400 "$(code -X POST $GW/api/sensors/ingest -H "X-Device-Key: $DEVICE_API_KEY" -H 'Content-Type: application/json' -d '{"codigo":"NO-EXISTE","valor":1}')"
else echo "  (defina DEVICE_API_KEY para probar la ingesta con clave)"; fi
echo "== Alertas (lectura crítica -> Sensor -> Alert)"
chk "registrar lectura 45 °C" 201 "$(code -X POST $GW/api/sensors/readings -H "$H" -H 'Content-Type: application/json' -d "{\"sensorId\":$SID,\"valor\":45}")"
sleep 2
N=$(curl -s "$GW/api/alerts?sensorId=$SID" -H "$H" | jget "len(d)")
chk "alerta generada por el motor (>=1)" 1 "$([ "${N:-0}" -ge 1 ] && echo 1 || echo 0)"
echo "== Reportes PDF"
for T in COMMUNITIES SENSORS READINGS ALERTS EVENT_HISTORY SENSOR_STATUS CLIMATE_STATS EXECUTIVE; do
  RID=$(curl -s -X POST $GW/api/reports -H "$H" -H 'Content-Type: application/json' -d "{\"type\":\"$T\",\"filters\":{}}" | jget "d['reportId']")
  ST=""; for i in $(seq 1 30); do ST=$(curl -s $GW/api/reports/$RID -H "$H" | jget "d['status']"); [ "$ST" = COMPLETED ] || [ "$ST" = FAILED ] && break; sleep 1; done
  chk "$T generado" COMPLETED "$ST"
  MAGIC=$(curl -s $GW/api/reports/$RID/download -H "$H" | head -c 4); chk "$T descarga %PDF" "%PDF" "$MAGIC"
done
chk "historial" 200 "$(code $GW/api/reports -H "$H")"
chk "dashboard" 200 "$(code $GW/api/reports/dashboard/resumen -H "$H")"
chk "bitácora (Audit)" 200 "$(code $GW/api/audit -H "$H")"
echo; echo "Resultado: $ok correctas, $fail fallidas"; [ $fail -eq 0 ]
