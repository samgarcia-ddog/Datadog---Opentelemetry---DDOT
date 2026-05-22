#!/usr/bin/env bash
# deploy-apps.sh — Despliega la app en K8s con envsubst para sustituir variables
set -euo pipefail

ACR_LOGIN_SERVER="${ACR_LOGIN_SERVER:?ACR_LOGIN_SERVER no definido}"
NAMESPACE="gorrashop"

echo "=== Deploy de GorraShop en K8s ==="
echo "Namespace: ${NAMESPACE}"
echo "ACR: ${ACR_LOGIN_SERVER}"
echo ""

export ACR_LOGIN_SERVER

# Aplicar manifiestos sustituyendo ${ACR_LOGIN_SERVER}
apply_with_envsubst() {
  local file="$1"
  echo "  Aplicando: ${file}"
  envsubst < "${file}" | kubectl apply -f -
}

echo "1/4 Aplicando manifiestos del backend..."
apply_with_envsubst "k8s/apps/backend/deployment.yaml"
apply_with_envsubst "k8s/apps/backend/service.yaml"

echo "2/4 Aplicando manifiestos del frontend..."
apply_with_envsubst "k8s/apps/frontend/deployment.yaml"
apply_with_envsubst "k8s/apps/frontend/service.yaml"

echo "3/4 Aplicando Ingress..."
kubectl apply -f k8s/apps/ingress.yaml

echo "4/4 Esperando que los pods estén Ready..."
kubectl rollout status deployment/gorrashop-backend  -n ${NAMESPACE} --timeout=120s
kubectl rollout status deployment/gorrashop-frontend -n ${NAMESPACE} --timeout=120s

echo ""
echo "✅ App desplegada!"
echo ""

INGRESS_IP=$(kubectl get svc ingress-nginx-controller -n ingress-nginx \
  -o jsonpath='{.status.loadBalancer.ingress[0].ip}' 2>/dev/null || echo "pendiente")

echo "Pods:"
kubectl get pods -n ${NAMESPACE}
echo ""
echo "Ingress IP: ${INGRESS_IP}"
if [[ "${INGRESS_IP}" != "pendiente" ]]; then
  echo "Tienda: http://${INGRESS_IP}"
  echo "API:    http://${INGRESS_IP}/api/products"
  echo "Swagger: http://${INGRESS_IP}/swagger"
fi
echo ""
echo "Siguiente paso: make scenario-1"
