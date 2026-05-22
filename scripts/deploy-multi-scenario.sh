#!/usr/bin/env bash
# deploy-multi-scenario.sh — Despliega los 4 escenarios de observabilidad simultáneamente
# Cada escenario vive en su propio namespace con su stack completo (app + DB + cache)
set -euo pipefail

ACR_LOGIN_SERVER="${ACR_LOGIN_SERVER:?ACR_LOGIN_SERVER no definido}"
OBS_NS="observability"
K8S_DIR="k8s"
MULTI_DIR="${K8S_DIR}/multi-scenario"
NAMESPACES=("gorrashop-s1" "gorrashop-s2" "gorrashop-s3" "gorrashop-s4" "gorrashop-s5")

export ACR_LOGIN_SERVER

echo "============================================"
echo "  Multi-Scenario GorraShop — Deploy"
echo "============================================"
echo "ACR: ${ACR_LOGIN_SERVER}"
echo "Namespaces: ${NAMESPACES[*]}"
echo ""

# ─── Phase 1: Namespaces ─────────────────────────────────────────────
echo "1/9 Creando namespaces..."
kubectl apply -f "${MULTI_DIR}/namespaces.yaml"
kubectl create namespace "${OBS_NS}" --dry-run=client -o yaml | kubectl apply -f -
echo "    OK"

# ─── Phase 2: Helm repos ─────────────────────────────────────────────
echo "2/9 Actualizando repos de Helm..."
helm repo add bitnami https://charts.bitnami.com/bitnami --force-update 2>/dev/null || true
helm repo add datadog https://helm.datadoghq.com --force-update 2>/dev/null || true
helm repo update
echo "    OK"

# ─── Phase 3: PostgreSQL + Redis per namespace ───────────────────────
echo "3/9 Desplegando PostgreSQL + Redis en cada namespace..."
for ns in "${NAMESPACES[@]}"; do
  echo "    [${ns}] PostgreSQL..."
  helm upgrade --install postgresql bitnami/postgresql \
    --namespace "${ns}" \
    --set auth.username=gorrashop \
    --set auth.password=gorrashop123 \
    --set auth.database=gorrashop \
    --set primary.persistence.enabled=false \
    --set primary.resources.requests.cpu=50m \
    --set primary.resources.requests.memory=128Mi \
    --set primary.resources.limits.cpu=250m \
    --set primary.resources.limits.memory=256Mi \
    --set readReplicas.replicaCount=0 \
    --wait --timeout=180s

  echo "    [${ns}] Redis..."
  helm upgrade --install redis bitnami/redis \
    --namespace "${ns}" \
    --set auth.enabled=false \
    --set architecture=standalone \
    --set master.persistence.enabled=false \
    --set master.resources.requests.cpu=50m \
    --set master.resources.requests.memory=64Mi \
    --set master.resources.limits.cpu=150m \
    --set master.resources.limits.memory=128Mi \
    --wait --timeout=180s
done
echo "    OK"

# ─── Phase 4: Deploy base app in each namespace ──────────────────────
echo "4/9 Desplegando app (backend + frontend) en cada namespace..."
for ns in "${NAMESPACES[@]}"; do
  echo "    [${ns}] Backend + Frontend..."
  for file in ${K8S_DIR}/apps/backend/deployment.yaml \
              ${K8S_DIR}/apps/backend/service.yaml \
              ${K8S_DIR}/apps/frontend/deployment.yaml \
              ${K8S_DIR}/apps/frontend/service.yaml; do
    sed "s/namespace: gorrashop$/namespace: ${ns}/" "${file}" | \
      sed "s/replicas: 2/replicas: 1/" | \
      envsubst | kubectl apply -f -
  done
done
echo "    OK"

# ─── Phase 5: Cleanup old Datadog releases ───────────────────────────
echo "5/9 Limpiando releases Datadog anteriores..."
helm uninstall datadog -n "${OBS_NS}" 2>/dev/null || true
helm uninstall datadog-crds -n "${OBS_NS}" 2>/dev/null || true
kubectl delete -f "${MULTI_DIR}/scenario-1/ddot-collector.yaml" 2>/dev/null || true
echo "    OK"

# ─── Phase 6: Deploy collectors ──────────────────────────────────────
echo "6/9 Desplegando collectors..."

echo "    [S2] OTel Collector OSS (kubectl)..."
kubectl apply -f "${K8S_DIR}/observability/scenario-2-otel-dd/otel-collector-configmap.yaml" -n "${OBS_NS}"
kubectl apply -f "${K8S_DIR}/observability/scenario-2-otel-dd/otel-collector-deployment.yaml" -n "${OBS_NS}"

echo "    [S1+S3+S4] Datadog Agent + DDOT (Helm)..."
helm upgrade --install datadog datadog/datadog \
  -n "${OBS_NS}" \
  -f "${MULTI_DIR}/datadog-agent-values.yaml" \
  --set-file datadog.otelCollector.config="${MULTI_DIR}/ddot-otel-config.yaml" \
  --wait --timeout=300s
echo "    OK"

# ─── Phase 7: Apply per-scenario patches ─────────────────────────────
echo "7/9 Aplicando patches por escenario..."
for i in 1 2 3 4 5; do
  ns="gorrashop-s${i}"
  echo "    [S${i}] Patcheando backend + frontend en ${ns}..."
  kubectl patch deployment gorrashop-backend -n "${ns}" \
    --type=strategic --patch-file="${MULTI_DIR}/scenario-${i}/patch-backend.yaml"
  kubectl patch deployment gorrashop-frontend -n "${ns}" \
    --type=strategic --patch-file="${MULTI_DIR}/scenario-${i}/patch-frontend.yaml"
done

# Restart S2+S4 pods para que el admission controller inyecte el tracer
echo "    [S2+S4] Reiniciando pods para inyeccion del tracer..."
kubectl rollout restart deployment/gorrashop-backend deployment/gorrashop-frontend \
  -n gorrashop-s2
kubectl rollout restart deployment/gorrashop-backend deployment/gorrashop-frontend \
  -n gorrashop-s4
echo "    OK"

# ─── Phase 8: Wait for rollouts ──────────────────────────────────────
echo "8/9 Esperando rollouts..."
for ns in "${NAMESPACES[@]}"; do
  echo "    [${ns}]..."
  kubectl rollout status deployment/gorrashop-backend  -n "${ns}" --timeout=120s
  kubectl rollout status deployment/gorrashop-frontend -n "${ns}" --timeout=120s
done

echo ""
echo "============================================"
echo "  Multi-Scenario GorraShop desplegado!"
echo "============================================"
echo ""
echo "Pods por namespace:"
for ns in "${NAMESPACES[@]}"; do
  echo "--- ${ns} ---"
  kubectl get pods -n "${ns}" --no-headers 2>/dev/null || echo "  (sin pods)"
done
echo ""
echo "--- ${OBS_NS} (collectors) ---"
kubectl get pods -n "${OBS_NS}" --no-headers 2>/dev/null || echo "  (sin pods)"
echo ""
echo "============================================"
echo "  Acceso via port-forward"
echo "============================================"
echo "  S1 (OTel SDK+DDOT):     kubectl port-forward svc/gorrashop-frontend 3001:3000 -n gorrashop-s1"
echo "  S2 (DD SDK+DDOT):      kubectl port-forward svc/gorrashop-frontend 3002:3000 -n gorrashop-s2"
echo "  S3 (OTel SDK+Agent):   kubectl port-forward svc/gorrashop-frontend 3003:3000 -n gorrashop-s3"
echo "  S4 (Full Datadog):     kubectl port-forward svc/gorrashop-frontend 3004:3000 -n gorrashop-s4"
echo "  S5 (OTel SDK+OSS):     kubectl port-forward svc/gorrashop-frontend 3005:3000 -n gorrashop-s5"
echo ""
echo "============================================"
echo "  Filtros en Datadog APM"
echo "============================================"
echo "  env:scenario-1-ddot          -> 3.2 OTel SDK + DDOT Collector"
echo "  env:scenario-2-ddsdk-ddot    -> 3.1 DD SDK + DDOT (Recomendada)"
echo "  env:scenario-3-agent         -> 3.3 OTel SDK + DD Agent OTLP"
echo "  env:scenario-4-sdk           -> 3.0 Full Datadog (Nativo)"
echo "  env:scenario-5-otel-oss      -> 3.4 OTel SDK + OSS Collector"
echo ""
echo "  Tags adicionales: scenario:1..4, pipeline:<tipo>"
