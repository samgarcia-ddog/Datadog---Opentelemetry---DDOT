#!/usr/bin/env bash
# setup-cluster.sh — Instala los componentes base del cluster
# Se ejecuta UNA vez después de crear el AKS con Terraform
set -euo pipefail

echo "=== GorraShop — Setup del Cluster ==="
echo ""

# ─── Namespaces ────────────────────────────────────────────────────────────────
echo "1/6 Creando namespaces..."
kubectl apply -f k8s/namespace.yaml

# ─── cert-manager ─────────────────────────────────────────────────────────────
echo "2/6 Instalando cert-manager..."
helm repo add jetstack https://charts.jetstack.io --force-update
helm repo update

helm upgrade --install cert-manager jetstack/cert-manager \
  --namespace cert-manager \
  --create-namespace \
  --version v1.16.2 \
  --set crds.enabled=true \
  --wait

echo "   cert-manager listo"

# ─── ingress-nginx ─────────────────────────────────────────────────────────────
echo "3/6 Instalando ingress-nginx..."
helm repo add ingress-nginx https://kubernetes.github.io/ingress-nginx --force-update
helm repo update

helm upgrade --install ingress-nginx ingress-nginx/ingress-nginx \
  --namespace ingress-nginx \
  --create-namespace \
  --set controller.replicaCount=2 \
  --set controller.nodeSelector."kubernetes\.io/os"=linux \
  --set controller.service.type=LoadBalancer \
  --wait

echo "   ingress-nginx listo"
echo "   IP Pública: $(kubectl get svc ingress-nginx-controller -n ingress-nginx -o jsonpath='{.status.loadBalancer.ingress[0].ip}' 2>/dev/null || echo 'pendiente...')"

# ─── PostgreSQL ────────────────────────────────────────────────────────────────
echo "4/6 Instalando PostgreSQL..."
helm repo add bitnami https://charts.bitnami.com/bitnami --force-update
helm repo update

helm upgrade --install postgresql bitnami/postgresql \
  --namespace gorrashop \
  --set auth.username=gorrashop \
  --set auth.password=gorrashop123 \
  --set auth.database=gorrashop \
  --set primary.persistence.size=5Gi \
  --set primary.resources.requests.cpu=100m \
  --set primary.resources.requests.memory=256Mi \
  --wait

echo "   PostgreSQL listo"

# ─── Redis ────────────────────────────────────────────────────────────────────
echo "5/6 Instalando Redis..."
helm upgrade --install redis bitnami/redis \
  --namespace gorrashop \
  --set auth.enabled=false \
  --set master.persistence.size=2Gi \
  --set master.resources.requests.cpu=100m \
  --set master.resources.requests.memory=128Mi \
  --wait

echo "   Redis listo"

# ─── Helm repos para observabilidad ───────────────────────────────────────────
echo "6/6 Agregando repos de Helm para observabilidad..."
helm repo add datadog              https://helm.datadoghq.com
helm repo add prometheus-community https://prometheus-community.github.io/helm-charts
helm repo add grafana              https://grafana.github.io/helm-charts
helm repo update

echo ""
echo "✅ Cluster setup completo!"
echo ""
echo "Siguiente paso:"
echo "  1. make build-push    # Build y push de imágenes al ACR"
echo "  2. make deploy        # Deploy de la app"
echo "  3. make scenario-1    # Activar el primer escenario de observabilidad"
