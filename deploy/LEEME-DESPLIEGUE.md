# Despliegue en VPS (GNU/Linux + Docker Compose + HTTPS)

## 1. Preparar el servidor (Ubuntu 22.04/24.04)
```bash
sudo apt update && sudo apt install -y docker.io docker-compose-v2 nginx certbot python3-certbot-nginx git ufw
sudo ufw allow OpenSSH && sudo ufw allow 'Nginx Full' && sudo ufw enable   # solo 22, 80 y 443 abiertos
```
Requisitos de memoria: ~4 GB (SQL Server 2022 necesita ≥ 2 GB).

## 2. Código y secretos
```bash
git clone <repositorio> /opt/alerta && cd /opt/alerta
cp .env.example .env && nano .env      # JWT_KEY (openssl rand -base64 48), contraseñas, PUBLIC_URL=https://miguate.lat
chmod 600 .env
# Login con Google/GitHub (opcional): registre las apps con el redirect URI https://miguate.lat/api/auth/oauth/<google|github>/callback
# y complete GOOGLE_CLIENT_ID/SECRET y GITHUB_CLIENT_ID/SECRET en .env  → docs/OAUTH-OIDC.md
```
Para producción use `SEED_DEMO=false`.

## 3. Levantar
```bash
docker compose up -d --build
docker compose ps                       # todos "healthy"
curl -s http://127.0.0.1:8080/health/services
```

## 4. DNS + HTTPS
Cree el registro A de `miguate.lat` hacia la IP del VPS. Copie `deploy/nginx-edge.conf.example` a
`/etc/nginx/sites-available/alerta`, actívelo y emita el certificado:
```bash
sudo ln -s /etc/nginx/sites-available/alerta /etc/nginx/sites-enabled/ && sudo nginx -t
sudo certbot --nginx -d miguate.lat && sudo systemctl reload nginx
```

## 5. Actualizar una versión
```bash
./deploy/backup.sh && git pull && docker compose up -d --build
```

## 6. Respaldos y restauración
`./deploy/backup.sh` genera `pg_dump` (3 bases), `.bak` de SQL Server y `mongodump` (reportes + auditoría). Restauración:
```bash
docker compose exec -T auth-db pg_restore -U auth -d auth_db --clean < backups/<fecha>/auth_db.dump
docker compose exec -T mongo mongorestore --archive --gzip --drop < backups/<fecha>/mongo.archive.gz
```

## 7. Operación
`docker compose logs -f gateway report-service` · cada petición lleva `X-Correlation-Id` (se registra en el gateway).
Si un servicio no está sano: `docker compose restart <servicio>`; las bases de datos conservan los datos en volúmenes.

## 8. Kubernetes / K3s (Fase 4)
Manifiestos en `deploy/k8s/`. Despliegue con `./deploy/k8s/deploy.sh` (o `kubectl apply -k deploy/k8s`,
nunca `-f`, porque kustomization genera el ConfigMap del Nginx). Dominio público: `https://miguate.lat`.
Ver `deploy/k8s/LEEME.md`.
