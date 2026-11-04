#!/usr/bin/env python3
"""Genera los manifiestos de Kubernetes/K3s (ejecutar: python3 deploy/k8s/_generar.py). Los .yaml resultantes se versionan."""
NS = "alerta-climatica"
H = "# Generado por _generar.py — editar el generador y volver a ejecutarlo\n"
out = {}

# ---------------------------------------------------------------- namespace + configuración
out["00-namespace-config.yaml"] = H + f"""apiVersion: v1
kind: Namespace
metadata:
  name: {NS}
  labels: {{ pod-security.kubernetes.io/enforce: baseline }}
---
apiVersion: v1
kind: ConfigMap
metadata: {{ name: app-config, namespace: {NS} }}
data:
  JWT_ISSUER: AlertaClimatica.Auth
  JWT_AUDIENCE: AlertaClimatica.Client
  JWT_EXPIRATION_MINUTES: "15"
  REFRESH_TOKEN_DAYS: "7"
  APP_TZ: America/Guatemala
  SEED_DEMO: "false"                      # "true" crea datos de demostración en bases vacías
  PUBLIC_URL: https://sistema-climatico.example
  RETENCION_DIAS: "30"
  SMTP_HOST: mailpit                      # SMTP real, p. ej. smtp.gmail.com (y SMTP_SECURITY=starttls, SMTP_PORT=587)
  SMTP_PORT: "1025"
  SMTP_SECURITY: none
  SMTP_FROM: "Alerta Climática <alertas@sistema-climatico.example>"
  NOTIFY_DEFAULT_RECIPIENTS: ""           # correos separados por coma (solo si no hay suscriptores)
"""

# ---------------------------------------------------------------- bases de datos
def pg(name, user, db, key):
    return f"""apiVersion: apps/v1
kind: StatefulSet
metadata: {{ name: {name}, namespace: {NS} }}
spec:
  serviceName: {name}
  replicas: 1
  selector: {{ matchLabels: {{ app: {name} }} }}
  template:
    metadata: {{ labels: {{ app: {name}, tier: data }} }}
    spec:
      containers:
        - name: postgres
          image: postgres:16-alpine
          env:
            - {{ name: POSTGRES_DB, value: {db} }}
            - {{ name: POSTGRES_USER, value: {user} }}
            - {{ name: POSTGRES_PASSWORD, valueFrom: {{ secretKeyRef: {{ name: app-secrets, key: {key} }} }} }}
            - {{ name: PGDATA, value: /var/lib/postgresql/data/pgdata }}
          ports: [{{ containerPort: 5432 }}]
          readinessProbe: {{ exec: {{ command: ["pg_isready","-U","{user}","-d","{db}"] }}, periodSeconds: 5 }}
          resources: {{ requests: {{ cpu: 50m, memory: 128Mi }}, limits: {{ memory: 512Mi }} }}
          volumeMounts: [{{ name: data, mountPath: /var/lib/postgresql/data }}]
  volumeClaimTemplates:
    - metadata: {{ name: data }}
      spec: {{ accessModes: ["ReadWriteOnce"], resources: {{ requests: {{ storage: 2Gi }} }} }}
---
apiVersion: v1
kind: Service
metadata: {{ name: {name}, namespace: {NS} }}
spec: {{ selector: {{ app: {name} }}, ports: [{{ port: 5432 }}] }}
---
"""
dbs = H + pg("auth-db", "auth", "auth_db", "AUTH_DB_PASSWORD") + pg("community-db", "community", "community_db", "COMMUNITY_DB_PASSWORD") + pg("alert-db", "alert", "alert_db", "ALERT_DB_PASSWORD")
dbs += f"""apiVersion: apps/v1
kind: StatefulSet
metadata: {{ name: sensor-db, namespace: {NS} }}
spec:
  serviceName: sensor-db
  replicas: 1
  selector: {{ matchLabels: {{ app: sensor-db }} }}
  template:
    metadata: {{ labels: {{ app: sensor-db, tier: data }} }}
    spec:
      containers:
        - name: mssql
          image: mcr.microsoft.com/mssql/server:2022-latest
          env:
            - {{ name: ACCEPT_EULA, value: "Y" }}
            - {{ name: MSSQL_MEMORY_LIMIT_MB, value: "1536" }}
            - {{ name: MSSQL_SA_PASSWORD, valueFrom: {{ secretKeyRef: {{ name: app-secrets, key: SA_PASSWORD }} }} }}
          ports: [{{ containerPort: 1433 }}]
          resources: {{ requests: {{ cpu: 200m, memory: 2Gi }}, limits: {{ memory: 3Gi }} }}
          readinessProbe: {{ tcpSocket: {{ port: 1433 }}, initialDelaySeconds: 25, periodSeconds: 10 }}
          volumeMounts: [{{ name: data, mountPath: /var/opt/mssql }}]
  volumeClaimTemplates:
    - metadata: {{ name: data }}
      spec: {{ accessModes: ["ReadWriteOnce"], resources: {{ requests: {{ storage: 5Gi }} }} }}
---
apiVersion: v1
kind: Service
metadata: {{ name: sensor-db, namespace: {NS} }}
spec: {{ selector: {{ app: sensor-db }}, ports: [{{ port: 1433 }}] }}
---
apiVersion: apps/v1
kind: StatefulSet
metadata: {{ name: mongo, namespace: {NS} }}
spec:
  serviceName: mongo
  replicas: 1
  selector: {{ matchLabels: {{ app: mongo }} }}
  template:
    metadata: {{ labels: {{ app: mongo, tier: data }} }}
    spec:
      containers:
        - name: mongo
          image: mongo:7
          ports: [{{ containerPort: 27017 }}]
          readinessProbe: {{ exec: {{ command: ["mongosh","--quiet","--eval","db.adminCommand('ping').ok"] }}, periodSeconds: 10 }}
          resources: {{ requests: {{ cpu: 50m, memory: 256Mi }}, limits: {{ memory: 768Mi }} }}
          volumeMounts: [{{ name: data, mountPath: /data/db }}]
  volumeClaimTemplates:
    - metadata: {{ name: data }}
      spec: {{ accessModes: ["ReadWriteOnce"], resources: {{ requests: {{ storage: 5Gi }} }} }}
---
apiVersion: v1
kind: Service
metadata: {{ name: mongo, namespace: {NS} }}
spec: {{ selector: {{ app: mongo }}, ports: [{{ port: 27017 }}] }}
"""
out["10-databases.yaml"] = dbs

# ---------------------------------------------------------------- aplicaciones
def env(items):
    L = []
    for n, v in items:
        if isinstance(v, tuple):
            kind, key = v
            ref, src = ("configMapKeyRef", "app-config") if kind == "cm" else ("secretKeyRef", "app-secrets")
            L.append(f"            - {{ name: {n}, valueFrom: {{ {ref}: {{ name: {src}, key: {key} }} }} }}")
        else:
            L.append(f'            - {{ name: {n}, value: "{v}" }}')
    return "\n".join(L)

JWT_NET = [("Jwt__Key", ("sec", "JWT_KEY")), ("Jwt__Issuer", ("cm", "JWT_ISSUER")), ("Jwt__Audience", ("cm", "JWT_AUDIENCE"))]
JWT_PY = [("JWT_KEY", ("sec", "JWT_KEY")), ("JWT_ISSUER", ("cm", "JWT_ISSUER")), ("JWT_AUDIENCE", ("cm", "JWT_AUDIENCE"))]
SMTP = [("SMTP_HOST", ("cm", "SMTP_HOST")), ("SMTP_PORT", ("cm", "SMTP_PORT")), ("SMTP_SECURITY", ("cm", "SMTP_SECURITY")), ("SMTP_FROM", ("cm", "SMTP_FROM")),
        ("SMTP_USER", ("sec", "SMTP_USER")), ("SMTP_PASSWORD", ("sec", "SMTP_PASSWORD")), ("NOTIFY_DEFAULT_RECIPIENTS", ("cm", "NOTIFY_DEFAULT_RECIPIENTS"))]
# nombre, puerto, sondaReadiness, sondaLiveness, env, réplicas, uid, memoria(Mi)
SVC = [
 ("gateway", 8080, "/health", "/health", JWT_NET + [("Cors__AllowedOrigins__0", ("cm", "PUBLIC_URL")), ("APP_TZ", ("cm", "APP_TZ"))], 2, 1654, 384),
 ("auth-service", 8080, "/health", "/health", JWT_PY + [("JWT_EXPIRATION_MINUTES", ("cm", "JWT_EXPIRATION_MINUTES")), ("REFRESH_TOKEN_DAYS", ("cm", "REFRESH_TOKEN_DAYS")),
     ("AUTH_DB_PASSWORD", ("sec", "AUTH_DB_PASSWORD")), ("DATABASE_URL", "postgres://auth:$(AUTH_DB_PASSWORD)@auth-db:5432/auth_db"),
     ("ADMIN_PASSWORD", ("sec", "ADMIN_PASSWORD")), ("AUDIT_SERVICE_URL", "http://audit-service:8000")], 2, 1000, 384),
 ("community-service", 8080, "/health", "/health", JWT_NET + [("COMMUNITY_DB_PASSWORD", ("sec", "COMMUNITY_DB_PASSWORD")),
     ("ConnectionStrings__Default", "Host=community-db;Database=community_db;Username=community;Password=$(COMMUNITY_DB_PASSWORD)"),
     ("Services__Sensor", "http://sensor-service:8080"), ("Services__Audit", "http://audit-service:8000"), ("Seed__Demo", ("cm", "SEED_DEMO"))], 2, 1654, 384),
 ("sensor-service", 8080, "/health", "/health", JWT_NET + [("SA_PASSWORD", ("sec", "SA_PASSWORD")),
     ("ConnectionStrings__Default", "Server=sensor-db;Database=SensorDb;User Id=sa;Password=$(SA_PASSWORD);TrustServerCertificate=True;"),
     ("Services__Community", "http://community-service:8080"), ("Services__Alert", "http://alert-service:8080"), ("Services__Audit", "http://audit-service:8000"),
     ("Devices__ApiKey", ("sec", "DEVICE_API_KEY")), ("Retencion__Dias", ("cm", "RETENCION_DIAS")), ("Seed__Demo", ("cm", "SEED_DEMO")), ("APP_TZ", ("cm", "APP_TZ"))], 1, 1654, 512),
 ("alert-service", 8080, "/actuator/health/readiness", "/actuator/health/liveness", JWT_PY + [("DATABASE_URL", "jdbc:postgresql://alert-db:5432/alert_db"), ("DATABASE_USER", "alert"),
     ("DATABASE_PASSWORD", ("sec", "ALERT_DB_PASSWORD")), ("COMMUNITY_SERVICE_URL", "http://community-service:8080"), ("SENSOR_SERVICE_URL", "http://sensor-service:8080"),
     ("AUDIT_SERVICE_URL", "http://audit-service:8000"), ("NOTIFICATION_SERVICE_URL", "http://notification-service:8000"), ("SEED_DEMO", ("cm", "SEED_DEMO")), ("APP_TZ", ("cm", "APP_TZ"))], 2, 10001, 768),
 ("report-service", 8000, "/health/ready", "/health", JWT_PY + [("MONGO_URI", "mongodb://mongo:27017"), ("MONGO_DB", "report_db"),
     ("COMMUNITY_SERVICE_URL", "http://community-service:8080"), ("SENSOR_SERVICE_URL", "http://sensor-service:8080"), ("ALERT_SERVICE_URL", "http://alert-service:8080"),
     ("AUDIT_SERVICE_URL", "http://audit-service:8000"), ("REPORT_TZ", ("cm", "APP_TZ"))], 2, 10001, 512),
 ("audit-service", 8000, "/health/ready", "/health", JWT_PY + [("MONGO_URI", "mongodb://mongo:27017"), ("MONGO_DB", "audit_db")], 2, 10001, 256),
 ("notification-service", 8000, "/health/ready", "/health", JWT_PY + [("MONGO_URI", "mongodb://mongo:27017"), ("MONGO_DB", "notification_db")] + SMTP, 1, 10001, 256),
]
apps = H
for name, port, ready, live, e, rep, uid, mem in SVC:
    apps += f"""apiVersion: apps/v1
kind: Deployment
metadata: {{ name: {name}, namespace: {NS} }}
spec:
  replicas: {rep}
  selector: {{ matchLabels: {{ app: {name} }} }}
  template:
    metadata: {{ labels: {{ app: {name}, tier: backend }} }}
    spec:
      terminationGracePeriodSeconds: 30        # cierre ordenado (graceful shutdown)
      securityContext: {{ runAsNonRoot: true, runAsUser: {uid}, seccompProfile: {{ type: RuntimeDefault }} }}
      containers:
        - name: {name}
          image: alerta/{name}:latest          # kustomization.yaml reemplaza nombre y etiqueta de la imagen
          imagePullPolicy: IfNotPresent
          ports: [{{ containerPort: {port} }}]
          env:
{env(e)}
          securityContext: {{ allowPrivilegeEscalation: false, capabilities: {{ drop: ["ALL"] }} }}
          lifecycle: {{ preStop: {{ exec: {{ command: ["sleep", "5"] }} }} }}   # da tiempo a retirar el pod del balanceador
          readinessProbe: {{ httpGet: {{ path: {ready}, port: {port} }}, initialDelaySeconds: 10, periodSeconds: 10, failureThreshold: 12 }}
          livenessProbe:  {{ httpGet: {{ path: {live}, port: {port} }}, initialDelaySeconds: 60, periodSeconds: 20, failureThreshold: 5 }}
          resources: {{ requests: {{ cpu: 100m, memory: {mem // 2}Mi }}, limits: {{ memory: {mem}Mi }} }}
---
apiVersion: v1
kind: Service
metadata: {{ name: {name}, namespace: {NS} }}
spec: {{ selector: {{ app: {name} }}, ports: [{{ port: {port}, targetPort: {port} }}] }}
---
"""
apps += f"""apiVersion: apps/v1
kind: Deployment
metadata: {{ name: frontend, namespace: {NS} }}
spec:
  replicas: 2
  selector: {{ matchLabels: {{ app: frontend }} }}
  template:
    metadata: {{ labels: {{ app: frontend, tier: edge }} }}
    spec:
      containers:
        - name: frontend
          image: alerta/frontend:latest
          imagePullPolicy: IfNotPresent
          ports: [{{ containerPort: 80 }}]
          readinessProbe: {{ httpGet: {{ path: /index.html, port: 80 }}, periodSeconds: 10 }}
          resources: {{ requests: {{ cpu: 20m, memory: 32Mi }}, limits: {{ memory: 128Mi }} }}
---
apiVersion: v1
kind: Service
metadata: {{ name: frontend, namespace: {NS} }}
spec: {{ selector: {{ app: frontend }}, ports: [{{ port: 80 }}] }}
"""
out["20-apps.yaml"] = apps

# ---------------------------------------------------------------- autoescalado y disponibilidad
auto = H
for name, mn, mx in [("gateway", 2, 6), ("frontend", 2, 4), ("report-service", 2, 5), ("alert-service", 2, 4)]:
    auto += f"""apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata: {{ name: {name}, namespace: {NS} }}
spec:
  scaleTargetRef: {{ apiVersion: apps/v1, kind: Deployment, name: {name} }}
  minReplicas: {mn}
  maxReplicas: {mx}
  metrics:
    - type: Resource
      resource: {{ name: cpu, target: {{ type: Utilization, averageUtilization: 70 }} }}
---
"""
for name in ["gateway", "frontend"]:
    auto += f"""apiVersion: policy/v1
kind: PodDisruptionBudget
metadata: {{ name: {name}, namespace: {NS} }}
spec:
  minAvailable: 1
  selector: {{ matchLabels: {{ app: {name} }} }}
---
"""
out["25-autoscaling.yaml"] = auto.rstrip("-\n") + "\n"

# ---------------------------------------------------------------- ingress
out["30-ingress.yaml"] = H + f"""# K3s incluye Traefik. Para HTTPS automático instale cert-manager y cree el ClusterIssuer "letsencrypt".
# Sin dominio (demostración por IP): elimine `host:` y el bloque `tls:`.
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: alerta
  namespace: {NS}
  annotations:
    cert-manager.io/cluster-issuer: letsencrypt
spec:
  tls: [{{ hosts: [sistema-climatico.example], secretName: alerta-tls }}]
  rules:
    - host: sistema-climatico.example
      http:
        paths:
          - {{ path: /, pathType: Prefix, backend: {{ service: {{ name: frontend, port: {{ number: 80 }} }} }} }}
"""

# ---------------------------------------------------------------- políticas de red (confianza cero)
np = H + f"""# Todo el tráfico entrante está denegado salvo lo declarado aquí:
#   Internet -> Ingress(Traefik) -> frontend -> gateway -> servicios -> bases de datos
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata: {{ name: default-deny-ingress, namespace: {NS} }}
spec: {{ podSelector: {{}}, policyTypes: [Ingress] }}
---
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata: {{ name: allow-ingress-controller-to-frontend, namespace: {NS} }}
spec:
  podSelector: {{ matchLabels: {{ app: frontend }} }}
  policyTypes: [Ingress]
  ingress:
    - from: [{{ namespaceSelector: {{ matchLabels: {{ kubernetes.io/metadata.name: kube-system }} }} }}]
      ports: [{{ port: 80 }}]
---
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata: {{ name: allow-frontend-to-gateway, namespace: {NS} }}
spec:
  podSelector: {{ matchLabels: {{ app: gateway }} }}
  policyTypes: [Ingress]
  ingress:
    - from: [{{ podSelector: {{ matchLabels: {{ app: frontend }} }} }}]
      ports: [{{ port: 8080 }}]
---
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata: {{ name: allow-backend-internal, namespace: {NS} }}
spec:
  podSelector: {{ matchLabels: {{ tier: backend }} }}
  policyTypes: [Ingress]
  ingress:
    - from: [{{ podSelector: {{ matchLabels: {{ tier: backend }} }} }}]
---
"""
for db, apps_ in [("auth-db", ["auth-service"]), ("community-db", ["community-service"]), ("alert-db", ["alert-service"]), ("sensor-db", ["sensor-service"]),
                  ("mongo", ["report-service", "audit-service", "notification-service"])]:
    froms = ", ".join(f"{{ podSelector: {{ matchLabels: {{ app: {a} }} }} }}" for a in apps_)
    np += f"""apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata: {{ name: allow-{db}-from-owners, namespace: {NS} }}
spec:
  podSelector: {{ matchLabels: {{ app: {db} }} }}
  policyTypes: [Ingress]
  ingress:
    - from: [{froms}]
---
"""
out["40-networkpolicy.yaml"] = np.rstrip("-\n") + "\n"

# ---------------------------------------------------------------- kustomization
out["kustomization.yaml"] = H + f"""# Aplicar todo:  kubectl apply -k deploy/k8s     (los Secrets se crean aparte con crear-secretos.sh)
apiVersion: kustomize.config.k8s.io/v1beta1
kind: Kustomization
namespace: {NS}
resources:
  - 00-namespace-config.yaml
  - 10-databases.yaml
  - 20-apps.yaml
  - 25-autoscaling.yaml
  - 30-ingress.yaml
  - 40-networkpolicy.yaml
# Para un registro propio:  kustomize edit set image alerta/gateway=ghcr.io/USUARIO/alerta-gateway:1.0 (una por servicio)
images:
""" + "".join(f"  - {{ name: alerta/{n}, newName: alerta/{n}, newTag: latest }}\n" for n in [s[0] for s in SVC] + ["frontend"])

# ---------------------------------------------------------------- opcionales
out["mailpit.yaml.opcional"] = H + f"""# Buzón de pruebas (captura correos). Aplicar solo si no se usa un SMTP real:  kubectl apply -n {NS} -f mailpit.yaml.opcional
apiVersion: apps/v1
kind: Deployment
metadata: {{ name: mailpit, namespace: {NS} }}
spec:
  replicas: 1
  selector: {{ matchLabels: {{ app: mailpit }} }}
  template:
    metadata: {{ labels: {{ app: mailpit }} }}
    spec:
      containers:
        - name: mailpit
          image: axllent/mailpit:latest
          ports: [{{ containerPort: 1025 }}, {{ containerPort: 8025 }}]
          resources: {{ requests: {{ cpu: 20m, memory: 32Mi }}, limits: {{ memory: 128Mi }} }}
---
apiVersion: v1
kind: Service
metadata: {{ name: mailpit, namespace: {NS} }}
spec: {{ selector: {{ app: mailpit }}, ports: [{{ name: smtp, port: 1025 }}, {{ name: web, port: 8025 }}] }}
---
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata: {{ name: allow-notification-to-mailpit, namespace: {NS} }}
spec:
  podSelector: {{ matchLabels: {{ app: mailpit }} }}
  policyTypes: [Ingress]
  ingress:
    - from: [{{ podSelector: {{ matchLabels: {{ app: notification-service }} }} }}]
      ports: [{{ port: 1025 }}]
"""
out["secret.example.yaml.plantilla"] = H + f"""# SOLO REFERENCIA de las claves. No suba valores reales a Git. Cree el Secret real con ./crear-secretos.sh
apiVersion: v1
kind: Secret
metadata: {{ name: app-secrets, namespace: {NS} }}
type: Opaque
stringData:
  JWT_KEY: "mínimo 32 caracteres aleatorios"
  ADMIN_PASSWORD: "..."
  AUTH_DB_PASSWORD: "..."
  COMMUNITY_DB_PASSWORD: "..."
  ALERT_DB_PASSWORD: "..."
  SA_PASSWORD: "..."
  DEVICE_API_KEY: "..."
  SMTP_USER: ""
  SMTP_PASSWORD: ""
"""
import pathlib
here = pathlib.Path(__file__).parent
for k, v in out.items():
    (here / k).write_text(v)
print("generados:", ", ".join(sorted(out)))
