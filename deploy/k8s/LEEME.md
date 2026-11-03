# Kubernetes / K3s — despliegue rápido

**Todo en un comando** (en el servidor con K3s, desde la raíz del proyecto, con el `.env` ya creado):
```bash
./deploy/k8s/deploy.sh
```
Construye las 9 imágenes, las importa a K3s, crea el Secret desde `.env` y aplica los manifiestos (`kubectl apply -k deploy/k8s`).

## Qué incluye
| Archivo | Contenido |
|---|---|
| `00-namespace-config.yaml` | Namespace y `ConfigMap` (configuración no secreta) |
| `10-databases.yaml` | 3×PostgreSQL, SQL Server y MongoDB como `StatefulSet` con volúmenes persistentes |
| `20-apps.yaml` | Gateway, 7 microservicios y frontend: sondas, recursos, usuario no root, `capabilities` fuera, cierre ordenado |
| `25-autoscaling.yaml` | `HorizontalPodAutoscaler` (gateway, frontend, report, alert) y `PodDisruptionBudget` |
| `30-ingress.yaml` | Ingress (Traefik de K3s) con TLS vía cert-manager |
| `40-networkpolicy.yaml` | Red de confianza cero: solo Internet→frontend→gateway→servicios→BD |
| `kustomization.yaml` | Une todo y permite cambiar el registro/etiqueta de las imágenes |
| `crear-secretos.sh` | Crea `app-secrets` desde `.env` (los secretos nunca están en Git) |
| `mailpit.yaml.opcional` | Buzón de pruebas para los correos de alertas |
| `_generar.py` | Genera los `.yaml` (editar aquí y ejecutar `python3 deploy/k8s/_generar.py`) |

## Pasos manuales equivalentes
```bash
./deploy/k8s/crear-secretos.sh
kubectl apply -k deploy/k8s
kubectl -n alerta-climatica get pods -w
```
## Correo real (Gmail)
Edite en `00-namespace-config.yaml`: `SMTP_HOST: smtp.gmail.com`, `SMTP_PORT: "587"`, `SMTP_SECURITY: starttls`, `NOTIFY_DEFAULT_RECIPIENTS`; y en el `.env`: `SMTP_USER` y `SMTP_PASSWORD` (contraseña de aplicación). Luego `./deploy/k8s/crear-secretos.sh` y `kubectl -n alerta-climatica rollout restart deploy/notification-service`.

## HTTPS y dominio
Instale cert-manager, cree un `ClusterIssuer` llamado `letsencrypt` y ajuste el `host` de `30-ingress.yaml`. Para una demostración por IP, quite `host:` y el bloque `tls:`.

## Registro de imágenes propio
`cd deploy/k8s && kustomize edit set image alerta/gateway=ghcr.io/USUARIO/alerta-gateway:1.0` (una por servicio), o suba las imágenes y use `./deploy.sh --sin-build`.

## Notas de operación
* **Sensor Service** corre el simulador: déjelo en **1 réplica** (o `Simulador__Habilitado=false` en las demás).
* Las contraseñas de las bases se fijan al crear el volumen: cámbielas solo borrando el PVC.
* `kubectl -n alerta-climatica logs deploy/<servicio>` para diagnosticar; los `/health` ya están enlazados a las sondas.
