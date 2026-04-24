#!/bin/bash
# Respaldo diario de la base de datos de rentas
DB_PATH="/home/admin/rentas-system/rentas.db"
BACKUP_DIR="/home/admin/rentas-system/backups"
FECHA=$(date +%Y-%m-%d_%H-%M-%S)
ZIP_FILE="$BACKUP_DIR/rentas_$FECHA.zip"
DEST_EMAIL="adrianlf@yahoo.com"

mkdir -p "$BACKUP_DIR"

# Crear ZIP
zip -j "$ZIP_FILE" "$DB_PATH"

# Enviar con mutt (maneja adjuntos correctamente)
echo "Respaldo automático de la base de datos de Rentas. Fecha: $FECHA" \
  | mutt -s "Respaldo DB Rentas - $FECHA" \
         -a "$ZIP_FILE" \
         -- "$DEST_EMAIL"

# Limpiar backups con más de 90 días
find "$BACKUP_DIR" -name "rentas_*.zip" -mtime +90 -delete

echo "[$FECHA] Respaldo enviado a $DEST_EMAIL"
