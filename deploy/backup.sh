#!/usr/bin/env bash
# Respaldo de las 5 bases de datos (Docker Compose). Uso:  ./deploy/backup.sh [carpeta_destino]
# Programar con cron, p. ej. diario 02:00:   0 2 * * * cd /opt/alerta && ./deploy/backup.sh /var/backups/alerta
set -euo pipefail
cd "$(dirname "$0")/.."
set -a; . ./.env; set +a
DEST="${1:-./backups}/$(date +%Y%m%d-%H%M%S)"; mkdir -p "$DEST"

for pair in "auth-db:auth:auth_db" "community-db:community:community_db" "alert-db:alert:alert_db"; do
  IFS=: read -r svc user db <<<"$pair"
  docker compose exec -T "$svc" pg_dump -U "$user" -d "$db" -Fc > "$DEST/$db.dump"
done

# SQL Server: BACKUP dentro del contenedor y copia al host
docker compose exec -T sensor-db bash -c "/opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P \"\$MSSQL_SA_PASSWORD\" -Q \"BACKUP DATABASE [SensorDb] TO DISK='/var/opt/mssql/data/SensorDb.bak' WITH INIT\"" \
 || docker compose exec -T sensor-db bash -c "/opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P \"\$MSSQL_SA_PASSWORD\" -Q \"BACKUP DATABASE [SensorDb] TO DISK='/var/opt/mssql/data/SensorDb.bak' WITH INIT\""
docker compose cp sensor-db:/var/opt/mssql/data/SensorDb.bak "$DEST/SensorDb.bak"

# MongoDB (reportes + PDF en GridFS, y auditoría)
docker compose exec -T mongo mongodump --archive --gzip > "$DEST/mongo.archive.gz"

echo "Respaldo completo en $DEST"
find "$(dirname "$DEST")" -maxdepth 1 -type d -mtime +14 -name '20*' -exec rm -rf {} + 2>/dev/null || true
