SHELL := /bin/bash
.DEFAULT_GOAL := help

# ─── Variables ────────────────────────────────────────────────────────────────
TF_DIR         := infrastructure/terraform
SCRIPTS_DIR    := scripts
K8S_DIR        := k8s
APPS_DIR       := apps
NAMESPACE      := gorrashop
OBS_NS         := observability

# Leer ACR login server desde Terraform output (después de apply)
ACR_LOGIN_SERVER ?= $(shell cd $(TF_DIR) && terraform output -raw acr_login_server 2>/dev/null || echo "acr.azurecr.io")
BACKEND_IMAGE    := $(ACR_LOGIN_SERVER)/gorrashop-backend:latest
FRONTEND_IMAGE   := $(ACR_LOGIN_SERVER)/gorrashop-frontend:latest

.PHONY: help
help: ## Muestra esta ayuda
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | sort | \
		awk 'BEGIN {FS = ":.*?## "}; {printf "\033[36m%-25s\033[0m %s\n", $$1, $$2}'

# ─── Infraestructura (Terraform) ──────────────────────────────────────────────
.PHONY: infra-init
infra-init: ## Inicializa Terraform
	cd $(TF_DIR) && terraform init

.PHONY: infra-plan
infra-plan: ## Plan de Terraform (dry-run)
	cd $(TF_DIR) && terraform plan

.PHONY: infra-apply
infra-apply: ## Crea AKS + ACR con Terraform
	cd $(TF_DIR) && terraform apply -auto-approve

.PHONY: infra-destroy
infra-destroy: ## Destruye toda la infraestructura
	@echo "⚠️  Esto destruirá todo. Confirma con ENTER o Ctrl+C para cancelar."
	@read confirm
	cd $(TF_DIR) && terraform destroy -auto-approve

# ─── Cluster Setup ────────────────────────────────────────────────────────────
.PHONY: kubeconfig
kubeconfig: ## Configura kubectl con el cluster de AKS
	@RG=$$(cd $(TF_DIR) && terraform output -raw resource_group_name) && \
	AKS=$$(cd $(TF_DIR) && terraform output -raw aks_name) && \
	SUB=$$(cd $(TF_DIR) && terraform output -raw subscription_id) && \
	az aks get-credentials --resource-group $$RG --name $$AKS --subscription $$SUB --overwrite-existing
	@echo "✅ kubectl configurado"

.PHONY: cluster-setup
cluster-setup: ## Instala ingress-nginx, cert-manager, postgres, redis
	bash $(SCRIPTS_DIR)/setup-cluster.sh

# ─── Build & Push ─────────────────────────────────────────────────────────────
.PHONY: acr-login
acr-login: ## Login al ACR de Azure
	@ACR=$$(cd $(TF_DIR) && terraform output -raw acr_name) && \
	az acr login --name $$ACR

.PHONY: build-push
build-push: acr-login ## Build y push de imágenes al ACR
	ACR_LOGIN_SERVER=$(ACR_LOGIN_SERVER) bash $(SCRIPTS_DIR)/build-push.sh

.PHONY: build-backend
build-backend: ## Build solo el backend
	docker build -t $(BACKEND_IMAGE) $(APPS_DIR)/backend

.PHONY: build-frontend
build-frontend: ## Build solo el frontend
	docker build -t $(FRONTEND_IMAGE) $(APPS_DIR)/frontend

# ─── Deploy de la App ─────────────────────────────────────────────────────────
.PHONY: datadog-secret
datadog-secret: ## Crea el secret de Datadog en K8s (requiere DD_API_KEY y DD_APP_KEY)
	@[ -n "$$DD_API_KEY" ] || (echo "❌ DD_API_KEY no definido" && exit 1)
	kubectl create namespace $(OBS_NS) --dry-run=client -o yaml | kubectl apply -f -
	kubectl create secret generic datadog-secret \
		--from-literal=api-key=$$DD_API_KEY \
		--from-literal=app-key=$${DD_APP_KEY:-""} \
		-n $(OBS_NS) \
		--dry-run=client -o yaml | kubectl apply -f -
	@echo "✅ Secret de Datadog creado en namespace $(OBS_NS)"

.PHONY: deploy
deploy: ## Despliega la app (frontend + backend) en K8s
	ACR_LOGIN_SERVER=$(ACR_LOGIN_SERVER) bash $(SCRIPTS_DIR)/deploy-apps.sh

.PHONY: undeploy
undeploy: ## Elimina la app del cluster
	kubectl delete namespace $(NAMESPACE) --ignore-not-found

# ─── Escenarios de Observabilidad ─────────────────────────────────────────────
.PHONY: scenario-stop
scenario-stop: ## Detiene el escenario activo de observabilidad
	@echo "🛑 Limpiando escenario activo..."
	helm uninstall ddot-collector -n $(OBS_NS) 2>/dev/null || true
	helm uninstall otel-collector -n $(OBS_NS) 2>/dev/null || true
	helm uninstall datadog -n $(OBS_NS) 2>/dev/null || true
	helm uninstall prometheus -n $(OBS_NS) 2>/dev/null || true
	helm uninstall grafana -n $(OBS_NS) 2>/dev/null || true
	@echo "✅ Escenario detenido"

.PHONY: scenario-1
scenario-1: scenario-stop ## Escenario 1: OTel SDK → DDOT Collector → Datadog (Recomendado)
	@echo "🚀 Activando Escenario 1: DDOT Collector"
	@[ -n "$$DD_API_KEY" ] || (echo "❌ DD_API_KEY no definido" && exit 1)
	$(MAKE) datadog-secret
	helm repo add datadog https://helm.datadoghq.com && helm repo update
	helm upgrade --install ddot-collector datadog/datadog-operator \
		-n $(OBS_NS) \
		-f $(K8S_DIR)/observability/scenario-1-ddot/ddot-values.yaml \
		--wait
	kubectl apply -f $(K8S_DIR)/observability/scenario-1-ddot/app-patch.yaml -n $(NAMESPACE)
	@echo "✅ Escenario 1 activo — revisa https://app.datadoghq.com/apm/services"

.PHONY: scenario-2
scenario-2: scenario-stop ## Escenario 2: OTel SDK → OTel Collector + Datadog Exporter → Datadog
	@echo "🚀 Activando Escenario 2: OTel Collector + Datadog Exporter"
	@[ -n "$$DD_API_KEY" ] || (echo "❌ DD_API_KEY no definido" && exit 1)
	$(MAKE) datadog-secret
	kubectl apply -f $(K8S_DIR)/observability/scenario-2-otel-dd/ -n $(OBS_NS)
	kubectl apply -f $(K8S_DIR)/observability/scenario-2-otel-dd/app-patch.yaml -n $(NAMESPACE)
	@echo "✅ Escenario 2 activo"

.PHONY: scenario-3
scenario-3: scenario-stop ## Escenario 3: OTel SDK → Datadog Agent (OTLP endpoint) → Datadog
	@echo "🚀 Activando Escenario 3: Datadog Agent con OTLP"
	@[ -n "$$DD_API_KEY" ] || (echo "❌ DD_API_KEY no definido" && exit 1)
	$(MAKE) datadog-secret
	helm repo add datadog https://helm.datadoghq.com && helm repo update
	helm upgrade --install datadog datadog/datadog \
		-n $(OBS_NS) \
		-f $(K8S_DIR)/observability/scenario-3-dd-agent-otlp/datadog-values.yaml \
		--wait
	kubectl apply -f $(K8S_DIR)/observability/scenario-3-dd-agent-otlp/app-patch.yaml -n $(NAMESPACE)
	@echo "✅ Escenario 3 activo"

.PHONY: scenario-4
scenario-4: scenario-stop ## Escenario 4: Datadog SDK (auto-instrumentación via Admission Controller)
	@echo "🚀 Activando Escenario 4: Datadog SDK auto-instrumentación"
	@[ -n "$$DD_API_KEY" ] || (echo "❌ DD_API_KEY no definido" && exit 1)
	$(MAKE) datadog-secret
	helm repo add datadog https://helm.datadoghq.com && helm repo update
	helm upgrade --install datadog datadog/datadog \
		-n $(OBS_NS) \
		-f $(K8S_DIR)/observability/scenario-4-dd-sdk/datadog-values.yaml \
		--wait
	kubectl patch deployment gorrashop-backend -n $(NAMESPACE) \
		--type=strategic \
		--patch-file=$(K8S_DIR)/observability/scenario-4-dd-sdk/patch-backend.yaml
	kubectl patch deployment gorrashop-frontend -n $(NAMESPACE) \
		--type=strategic \
		--patch-file=$(K8S_DIR)/observability/scenario-4-dd-sdk/patch-frontend.yaml
	@echo "✅ Escenario 4 activo — reiniciando pods para aplicar auto-instrumentación"
	kubectl rollout restart deployment/gorrashop-backend deployment/gorrashop-frontend -n $(NAMESPACE)

.PHONY: scenario-5
scenario-5: scenario-stop ## Escenario 5: OTel SDK → OTel Collector → Prometheus + Grafana (puro OTel)
	@echo "🚀 Activando Escenario 5: Pure OTel (Prometheus + Grafana)"
	kubectl apply -f $(K8S_DIR)/observability/scenario-5-pure-otel/ -n $(OBS_NS)
	helm repo add prometheus-community https://prometheus-community.github.io/helm-charts
	helm repo add grafana https://grafana.github.io/helm-charts
	helm repo update
	helm upgrade --install prometheus prometheus-community/prometheus \
		-n $(OBS_NS) \
		-f $(K8S_DIR)/observability/scenario-5-pure-otel/prometheus-values.yaml \
		--wait
	helm upgrade --install grafana grafana/grafana \
		-n $(OBS_NS) \
		-f $(K8S_DIR)/observability/scenario-5-pure-otel/grafana-values.yaml \
		--wait
	kubectl apply -f $(K8S_DIR)/observability/scenario-5-pure-otel/app-patch.yaml -n $(NAMESPACE)
	@echo "✅ Escenario 5 activo"
	@echo "📊 Grafana password: $$(kubectl get secret --namespace $(OBS_NS) grafana -o jsonpath='{.data.admin-password}' | base64 --decode)"

# ─── Multi-Scenario (4 escenarios simultáneos) ────────────────────────────────
.PHONY: multi-scenario
multi-scenario: ## Despliega los 4 escenarios simultáneamente en namespaces separados
	@echo "🚀 Desplegando 4 escenarios simultáneamente"
	@[ -n "$$DD_API_KEY" ] || (echo "❌ DD_API_KEY no definido" && exit 1)
	$(MAKE) datadog-secret
	ACR_LOGIN_SERVER=$(ACR_LOGIN_SERVER) bash $(SCRIPTS_DIR)/deploy-multi-scenario.sh

.PHONY: multi-scenario-stop
multi-scenario-stop: ## Elimina todos los escenarios simultáneos
	@echo "🛑 Eliminando escenarios simultáneos..."
	kubectl delete namespace gorrashop-s1 gorrashop-s2 gorrashop-s3 gorrashop-s4 --ignore-not-found
	kubectl delete -f $(K8S_DIR)/observability/scenario-2-otel-dd/otel-collector-deployment.yaml -n $(OBS_NS) 2>/dev/null || true
	kubectl delete -f $(K8S_DIR)/observability/scenario-2-otel-dd/otel-collector-configmap.yaml -n $(OBS_NS) 2>/dev/null || true
	helm uninstall datadog -n $(OBS_NS) 2>/dev/null || true
	@echo "✅ Escenarios simultáneos eliminados"

.PHONY: multi-scenario-status
multi-scenario-status: ## Estado de los pods en los 4 escenarios
	@for ns in gorrashop-s1 gorrashop-s2 gorrashop-s3 gorrashop-s4; do \
		echo "=== Namespace: $$ns ==="; \
		kubectl get pods,svc -n $$ns 2>/dev/null || echo "  (namespace no existe)"; \
		echo ""; \
	done
	@echo "=== Namespace: $(OBS_NS) ==="
	@kubectl get pods,svc -n $(OBS_NS)

# ─── Utilidades ───────────────────────────────────────────────────────────────
.PHONY: status
status: ## Estado de todos los pods
	@echo "=== Namespace: $(NAMESPACE) ==="
	kubectl get pods,svc,ingress -n $(NAMESPACE)
	@echo ""
	@echo "=== Namespace: $(OBS_NS) ==="
	kubectl get pods,svc -n $(OBS_NS)

.PHONY: logs-backend
logs-backend: ## Logs del backend
	kubectl logs -l app=gorrashop-backend -n $(NAMESPACE) --tail=100 -f

.PHONY: logs-frontend
logs-frontend: ## Logs del frontend
	kubectl logs -l app=gorrashop-frontend -n $(NAMESPACE) --tail=100 -f

.PHONY: port-forward-backend
port-forward-backend: ## Port-forward backend al puerto 8080
	kubectl port-forward svc/gorrashop-backend 8080:8080 -n $(NAMESPACE)

.PHONY: port-forward-frontend
port-forward-frontend: ## Port-forward frontend al puerto 3000
	kubectl port-forward svc/gorrashop-frontend 3000:3000 -n $(NAMESPACE)
