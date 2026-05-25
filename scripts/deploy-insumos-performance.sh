#!/usr/bin/env bash
set -euo pipefail

RESOURCE_GROUP="rg-nanchesoft-dev"
API_APP="api-nanchesoft-prod"
WEB_APP="web-nanchesoft-prod"

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
API_ZIP="$ROOT_DIR/src/Nanchesoft.Api/deploy-api.zip"
WEB_ZIP="$ROOT_DIR/src/Nanchesoft.Web/deploy-web.zip"

if ! command -v az >/dev/null 2>&1; then
  echo "Azure CLI no esta instalado o no esta en PATH." >&2
  exit 1
fi

if [ ! -f "$API_ZIP" ]; then
  echo "No existe el paquete API: $API_ZIP" >&2
  exit 1
fi

if [ ! -f "$WEB_ZIP" ]; then
  echo "No existe el paquete Web: $WEB_ZIP" >&2
  exit 1
fi

echo "Deploy API: $API_ZIP"
az webapp deploy \
  --resource-group "$RESOURCE_GROUP" \
  --name "$API_APP" \
  --src-path "$API_ZIP" \
  --type zip

echo "Restart API: $API_APP"
az webapp restart \
  --resource-group "$RESOURCE_GROUP" \
  --name "$API_APP"

echo "Deploy Web: $WEB_ZIP"
az webapp deploy \
  --resource-group "$RESOURCE_GROUP" \
  --name "$WEB_APP" \
  --src-path "$WEB_ZIP" \
  --type zip

echo "Restart Web: $WEB_APP"
az webapp restart \
  --resource-group "$RESOURCE_GROUP" \
  --name "$WEB_APP"

echo "Deploy terminado. Abre una pestana nueva para verificar Build insumos."
