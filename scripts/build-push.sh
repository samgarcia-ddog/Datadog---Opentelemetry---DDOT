#!/usr/bin/env bash
# build-push.sh — Build y push de imágenes Docker al ACR
set -euo pipefail

ACR_LOGIN_SERVER="${ACR_LOGIN_SERVER:?ACR_LOGIN_SERVER no definido}"

BACKEND_IMAGE="${ACR_LOGIN_SERVER}/gorrashop-backend:latest"
FRONTEND_IMAGE="${ACR_LOGIN_SERVER}/gorrashop-frontend:latest"

echo "=== Build & Push de imágenes ==="
echo "ACR: ${ACR_LOGIN_SERVER}"
echo ""

# ─── Backend .NET 8 ───────────────────────────────────────────────────────────
echo "1/2 Build: gorrashop-backend (.NET 8)..."
docker build \
  --platform linux/amd64 \
  -t "${BACKEND_IMAGE}" \
  -f apps/backend/Dockerfile \
  apps/backend/

echo "    Push: ${BACKEND_IMAGE}..."
docker push "${BACKEND_IMAGE}"
echo "    ✅ Backend listo"

# ─── Frontend Next.js ─────────────────────────────────────────────────────────
echo "2/2 Build: gorrashop-frontend (Next.js 14)..."
docker build \
  --platform linux/amd64 \
  -t "${FRONTEND_IMAGE}" \
  -f apps/frontend/Dockerfile \
  apps/frontend/

echo "    Push: ${FRONTEND_IMAGE}..."
docker push "${FRONTEND_IMAGE}"
echo "    ✅ Frontend listo"

echo ""
echo "✅ Imágenes publicadas en ${ACR_LOGIN_SERVER}:"
echo "   - ${BACKEND_IMAGE}"
echo "   - ${FRONTEND_IMAGE}"
