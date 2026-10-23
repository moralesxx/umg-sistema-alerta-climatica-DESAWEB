# Manifiestos para Kubernetes / K3s (base para la Fase 4)
1. Construya y publique las imágenes (`services/*/Dockerfile`, `frontend/Dockerfile`) y reemplace `REGISTRY/alerta` en `20-services.yaml`.
2. `kubectl apply -f 00-namespace-config.yaml` y cree el Secret real (no suba valores a Git).
3. `kubectl apply -f 10-databases.yaml -f 20-services.yaml -f 30-ingress.yaml`.
- Configuración → ConfigMap/Secret; datos → PVC (StatefulSets); health → readiness/liveness; cierre ordenado → `terminationGracePeriodSeconds`.
- El gateway sigue siendo el único punto de entrada (el Ingress solo expone el frontend, que hace proxy a `/api`). Para la Fase 4 (OAuth2/OIDC) basta cambiar el emisor de tokens de Auth Service.
