#!/usr/bin/env bash
# Crea/actualiza el Secret "app-secrets" en Kubernetes a partir del .env de la raíz del proyecto (no se sube a Git).
set -euo pipefail
cd "$(dirname "$0")/../.."
NS=alerta-climatica
[ -f .env ] || { echo "No existe .env en la raíz del proyecto"; exit 1; }
get() { grep -E "^$1=" .env | head -1 | cut -d= -f2- | tr -d '\r'; }
kubectl get ns "$NS" >/dev/null 2>&1 || kubectl create namespace "$NS"
kubectl -n "$NS" create secret generic app-secrets \
  --from-literal=JWT_KEY="$(get JWT_KEY)" \
  --from-literal=ADMIN_PASSWORD="$(get ADMIN_PASSWORD)" \
  --from-literal=AUTH_DB_PASSWORD="$(get AUTH_DB_PASSWORD)" \
  --from-literal=COMMUNITY_DB_PASSWORD="$(get COMMUNITY_DB_PASSWORD)" \
  --from-literal=ALERT_DB_PASSWORD="$(get ALERT_DB_PASSWORD)" \
  --from-literal=SA_PASSWORD="$(get SA_PASSWORD)" \
  --from-literal=DEVICE_API_KEY="$(get DEVICE_API_KEY)" \
  --from-literal=SMTP_USER="$(get SMTP_USER)" \
  --from-literal=SMTP_PASSWORD="$(get SMTP_PASSWORD)" \
  --from-literal=GOOGLE_CLIENT_ID="$(get GOOGLE_CLIENT_ID)" \
  --from-literal=GOOGLE_CLIENT_SECRET="$(get GOOGLE_CLIENT_SECRET)" \
  --from-literal=GITHUB_CLIENT_ID="$(get GITHUB_CLIENT_ID)" \
  --from-literal=GITHUB_CLIENT_SECRET="$(get GITHUB_CLIENT_SECRET)" \
  --dry-run=client -o yaml | kubectl apply -f -
echo "Secret app-secrets listo en el namespace $NS"
