# Kubernetes / K3s — despliegue rápido

Dominio público: **https://miguate.lat**

**Antes de desplegar**, cree en su proveedor DNS el registro:

| Tipo | Nombre | Valor |
|---|---|---|
| A | `miguate.lat` | IP pública del VPS |

(Si quiere usar `www.miguate.lat`, cree también el registro A y añada el host al Ingress y al bloque `tls`.)

Abra los puertos **80 y 443** en el firewall del VPS (por ejemplo `sudo ufw allow 80,443/tcp`): Traefik los
necesita para servir la aplicación y para validar el certificado de Let's Encrypt.

**Todo en un comando** (en el servidor con K3s, desde la raíz del proyecto, con el `.env` ya creado):
```bash
./deploy/k8s/deploy.sh
```
Construye las 9 imágenes, las importa a K3s, crea el Secret desde `.env`, instala cert-manager si falta,
aplica los manifiestos (`kubectl apply -k deploy/k8s`) y espera a que el certificado TLS de miguate.lat esté listo.

## Qué incluye
| Archivo | Contenido |
|---|---|
| `00-namespace-config.yaml` | Namespace y `ConfigMap` (configuración no secreta; `PUBLIC_URL=https://miguate.lat`) |
| `10-databases.yaml` | 3×PostgreSQL, SQL Server y MongoDB como `StatefulSet` con volúmenes persistentes |
| `20-apps.yaml` | Gateway, 7 microservicios y frontend: sondas, recursos, usuario no root, `capabilities` fuera, cierre ordenado |
| `25-autoscaling.yaml` | `HorizontalPodAutoscaler` (gateway, frontend, report, alert) y `PodDisruptionBudget` |
| `30-ingress.yaml` | Ingress de Traefik para `miguate.lat` con TLS vía cert-manager |
| `31-clusterissuer.yaml` | `ClusterIssuer` de Let's Encrypt usado por el Ingress |
| `40-networkpolicy.yaml` | Red de confianza cero: solo Ingress→frontend→gateway→servicios→BD |
| `frontend-nginx.conf` | Nginx del frontend adaptado a Kubernetes (DNS de Services, no el de Docker) |
| `kustomization.yaml` | Une todo, genera el `ConfigMap` del Nginx y permite cambiar el registro/etiqueta de las imágenes |
| `crear-secretos.sh` | Crea `app-secrets` desde `.env` (los secretos nunca están en Git). Incluye `GOOGLE_*` y `GITHUB_*` del login OAuth (opcionales; ver `docs/OAUTH-OIDC.md`) |
| `mailpit.yaml.opcional` | Buzón de pruebas para los correos de alertas |
| `_generar.py` | Genera los `.yaml` (editar aquí y ejecutar `python3 deploy/k8s/_generar.py`) |

## Pasos manuales equivalentes
```bash
# 1) Secretos
./deploy/k8s/crear-secretos.sh

# 2) cert-manager (solo la primera vez; deploy.sh ya lo hace)
kubectl apply -f https://github.com/cert-manager/cert-manager/releases/latest/download/cert-manager.yaml
kubectl wait --for=condition=Established crd/clusterissuers.cert-manager.io --timeout=120s
kubectl -n cert-manager wait --for=condition=Available deployment --all --timeout=300s

# 3) Manifiestos
kubectl apply -k deploy/k8s
kubectl -n alerta-climatica get pods -w

# 4) Estado del certificado
kubectl -n alerta-climatica get certificate miguate-lat-tls
```
> Use siempre `kubectl apply -k deploy/k8s`. El archivo `20-services.yaml` es antiguo y **no** debe aplicarse
> (las imágenes `REGISTRY/...` no existen); kustomization usa `20-apps.yaml`.

## Correo real (Gmail)
Edite en `00-namespace-config.yaml`: `SMTP_HOST: smtp.gmail.com`, `SMTP_PORT: "587"`, `SMTP_SECURITY: starttls`, `NOTIFY_DEFAULT_RECIPIENTS`; y en el `.env`: `SMTP_USER` y `SMTP_PASSWORD` (contraseña de aplicación). Luego `./deploy/k8s/crear-secretos.sh` y `kubectl -n alerta-climatica rollout restart deploy/notification-service`.

## HTTPS y dominio
El Ingress ya apunta a `miguate.lat` y pide el certificado al `ClusterIssuer` `letsencrypt`
(creado por `31-clusterissuer.yaml`; el correo de aviso es `admin@miguate.lat`, puede cambiarlo).
Si el certificado queda en `False`, revise que el DNS resuelva a la IP del VPS y ejecute:
```bash
kubectl -n alerta-climatica describe certificate miguate-lat-tls
kubectl -n alerta-climatica describe challenge
```
Para una demostración por IP, quite `host:` y el bloque `tls:` de `30-ingress.yaml`.
Si su clúster **no** usa Traefik (K3s lo trae incluido), cambie `ingressClassName` en `30-ingress.yaml`
y añada el namespace de su controlador a la regla `allow-ingress-controller-to-frontend` de `40-networkpolicy.yaml`.

## Tamaño en un VPS de un solo nodo
Los Deployments arrancan con **1 réplica** y los HPA escalan desde 1 (gateway hasta 3; frontend y alert hasta 2;
report hasta 3). Con eso el clúster pide ~3.4 GiB de RAM; se recomienda un VPS de **4 GB como mínimo**
(8 GB si quiere holgura, porque SQL Server consume ~1.5–2 GB). Si tiene más capacidad, suba `replicas`
en `20-apps.yaml` o los `maxReplicas` de `25-autoscaling.yaml`.
> Nota: la imagen de SQL Server (`mcr.microsoft.com/mssql/server`) solo existe para **amd64**; un VPS ARM
> necesitaría sustituir `sensor-db` por PostgreSQL u otra base compatible.

## Registro de imágenes propio
`cd deploy/k8s && kustomize edit set image alerta/gateway=ghcr.io/USUARIO/alerta-gateway:1.0` (una por servicio), o suba las imágenes y use `./deploy.sh --sin-build`.

## Notas de operación
* **Sensor Service** corre el simulador: déjelo en **1 réplica** (o `Simulador__Habilitado=false` en las demás).
* Las contraseñas de las bases se fijan al crear el volumen: cámbielas solo borrando el PVC.
* `kubectl -n alerta-climatica logs deploy/<servicio>` para diagnosticar; los `/health` ya están enlazados a las sondas.
* Si actualiza el frontend, `frontend-nginx.conf` se monta como `ConfigMap`: cualquier cambio en ese archivo
  cambia el hash y reinicia el Deployment automáticamente.
