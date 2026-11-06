#!/usr/bin/env bash
# Despliegue COMPLETO en K3s/Kubernetes de un solo comando (en la máquina donde corre K3s):
#     ./deploy/k8s/deploy.sh            # construye imágenes, las importa a K3s, crea secretos y aplica
#     ./deploy/k8s/deploy.sh --sin-build  # solo aplica (imágenes ya importadas o en un registro)
# Dominio público: https://miguate.lat (Ingress 30-ingress.yaml + ClusterIssuer 31-clusterissuer.yaml).
# Requisitos: docker, kubectl y K3s (o cualquier clúster; con registro propio ver kustomization.yaml).
set -euo pipefail
cd "$(dirname "$0")/../.."
NS=alerta-climatica

if [ "${1:-}" != "--sin-build" ]; then
  echo "== 1/4 Construyendo imágenes"
  docker build -t alerta/gateway:latest              -f services/gateway/Dockerfile .
  docker build -t alerta/community-service:latest    -f services/community-service/Dockerfile .
  docker build -t alerta/sensor-service:latest       -f services/sensor-service/Dockerfile .
  docker build -t alerta/frontend:latest             -f frontend/Dockerfile .
  for s in auth-service alert-service report-service audit-service notification-service; do
    docker build -t alerta/$s:latest services/$s
  done
  echo "== 2/4 Importando imágenes a K3s (requiere sudo)"
  IMAGES="alerta/gateway alerta/community-service alerta/sensor-service alerta/frontend alerta/auth-service alerta/alert-service alerta/report-service alerta/audit-service alerta/notification-service"
  docker save $(for i in $IMAGES; do echo $i:latest; done) | sudo k3s ctr images import -
else
  echo "== (se omite construcción e importación de imágenes)"
fi

echo "== 3/4 Secretos"
./deploy/k8s/crear-secretos.sh

echo "== 4/4 cert-manager, manifiestos y espera"
# HTTPS automático con Let's Encrypt: cert-manager se instala una sola vez.
if ! kubectl get crd clusterissuers.cert-manager.io >/dev/null 2>&1; then
  echo "-- cert-manager no está instalado; instalando (necesario para el TLS de miguate.lat)"
  kubectl apply -f https://github.com/cert-manager/cert-manager/releases/latest/download/cert-manager.yaml
  kubectl wait --for=condition=Established crd/clusterissuers.cert-manager.io --timeout=120s
  kubectl -n cert-manager wait --for=condition=Available deployment --all --timeout=300s
fi

kubectl apply -k deploy/k8s
kubectl -n $NS apply -f deploy/k8s/mailpit.yaml.opcional 2>/dev/null || true   # buzón de pruebas (quitar si usa SMTP real)

# Las imágenes usan la etiqueta :latest: reinicie para que los pods tomen la versión recién importada.
kubectl -n $NS rollout restart deployment

echo "Esperando a que los pods estén listos (puede tardar varios minutos: SQL Server y Java)..."
for d in gateway auth-service community-service sensor-service alert-service report-service audit-service notification-service frontend; do
  kubectl -n $NS rollout status deployment/$d --timeout=420s
done
kubectl -n $NS get pods

echo "Esperando el certificado TLS de miguate.lat (requiere que el DNS ya apunte al VPS)..."
for _ in $(seq 1 30); do
  kubectl -n $NS get certificate miguate-lat-tls >/dev/null 2>&1 && break
  sleep 2
done
kubectl -n $NS wait --for=condition=Ready certificate/miguate-lat-tls --timeout=300s \
  || echo "AVISO: el certificado aún no está listo. Compruebe: dig +short miguate.lat  y  kubectl -n $NS describe certificate miguate-lat-tls"

echo; echo "Listo. Acceso: https://miguate.lat/  (Ingress de Traefik; también responde http://miguate.lat/)"
