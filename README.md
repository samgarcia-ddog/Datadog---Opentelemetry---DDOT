# GorraShop — Observability Lab

Laboratorio de observabilidad con una tienda de gorras (e-commerce) que compara **5 arquitecturas de telemetria** corriendo simultaneamente en Kubernetes. Cada escenario vive en su propio namespace con su stack completo (app + base de datos + cache).

El objetivo: desplegar la **misma aplicacion** 5 veces, cambiando unicamente la **pipeline de observabilidad**, y comparar los resultados en Datadog APM lado a lado.

---

## Arquitectura

```
                            ┌───────────────────────────────────────────┐
                            │         observability namespace            │
                            │                                           │
                            │  ┌──────────────┐   ┌─────────────────┐  │
                            │  │ OTel Collector│   │ NGINX Gateway   │  │
                            │  │  OSS :4317    │   │ LB :3001-3005   │  │
                            │  └──────▲────────┘   └────────┬────────┘  │
                            │         │                     │          │
                            │  ┌──────────────────────────────────────┐ │
                            │  │     Datadog Agent (DaemonSet)         │ │
                            │  │  ┌─────────┐ ┌──────┐ ┌───────────┐ │ │
                            │  │  │DDOT :4327│ │OTLP  │ │ Admission │ │ │
                            │  │  │         │ │:4317  │ │ Controller│ │ │
                            │  │  └────▲────┘ └──▲───┘ └─────┬─────┘ │ │
                            │  └───────┼─────────┼───────────┼───────┘ │
                            └──────────┼─────────┼───────────┼─────────┘
                                       │         │           │
         ┌─────────────┐ ┌─────────────┤   ┌─────┤     ┌─────┘
         │             │ │             │   │     │     │
    ┌────┴────┐  ┌─────┴──┐  ┌────────┴┐  ┌┴────┴──┐  ┌┴──────────┐
    │  S1     │  │  S2    │  │  S3     │  │  S4    │  │  S5       │
    │         │  │ (Rec.) │  │         │  │ Nativo │  │           │
    │ OTel SDK│  │ DD SDK │  │ OTel SDK│  │ DD SDK │  │ OTel SDK  │
    │ → DDOT  │  │ + DDOT │  │ → Agent │  │(auto-  │  │ → OTel    │
    │         │  │        │  │   OTLP  │  │ instr) │  │   Coll OSS│
    │         │  │ + RUM  │  │         │  │ + RUM  │  │           │
    │ PG+Redis│  │PG+Redis│  │PG+Redis │  │PG+Redis│  │ PG+Redis  │
    └─────────┘  └────────┘  └─────────┘  └────────┘  └───────────┘
    gorrashop-s1 gorrashop-s2 gorrashop-s3 gorrashop-s4 gorrashop-s5
```

---

## Los 5 Escenarios

| # | Opcion | Pipeline | SDK | Descripcion |
|---|--------|----------|-----|-------------|
| **S1** | 3.2 OTel SDK + DDOT | App → DDOT Collector → Datadog | OTel | OTel SDK envia OTLP al DDOT (distribucion curada por Datadog del OTel Collector) |
| **S2** | 3.1 DD SDK + DDOT **(Recomendada)** | App ← DD Admission Controller → DD Agent | DD | DD SDK inyectado via Admission Controller + DDOT habilitado para flexibilidad OTel |
| **S3** | 3.3 OTel SDK + DD Agent OTLP | App → DD Agent (OTLP receiver) → Datadog | OTel | OTel SDK envia OTLP directamente al Agent (sin collector extra) |
| **S4** | 3.0 Full Datadog (Nativo) | App ← DD Admission Controller → DD Agent | DD | Auto-instrumentacion completa con DD SDK, misma pipeline que S2 sin DDOT |
| **S5** | 3.4 OTel SDK + OSS Collector | App → OTel Collector + DD Exporter → Datadog | OTel | Collector upstream de la comunidad con `datadogexporter` |

### Matriz de Funcionalidades

| Funcionalidad | S1 DDOT | S2 DD+DDOT | S3 Agent | S4 Nativo | S5 OSS |
|---------------|---------|------------|----------|-----------|--------|
| Distributed Tracing | Si | Si | Si | Si | Si |
| Metricas | Si | Si | Si | Si | Si |
| Logs correlacionados | Si | Si | Si | Si | Si |
| Continuous Profiler | No | **Si** | No | **Si** | No |
| Database Monitoring | No | **Si** | No | **Si** | No |
| RUM (Real User Monitoring) | No | **Si** | No | **Si** | No |
| App & API Protection | No | **Si** | No | **Si** | No |
| Cloud SIEM | Si | Si | Si | Si | Si |
| Multi-backend (fan-out) | Limitado | Limitado | No | No | **Si** |

### Filtros en Datadog APM

Cada escenario se diferencia por el tag `env`:

| Escenario | Filtro Datadog APM |
|-----------|--------------------|
| S1 | `env:scenario-1-ddot` |
| S2 | `env:scenario-2-ddsdk-ddot` |
| S3 | `env:scenario-3-agent` |
| S4 | `env:scenario-4-sdk` |
| S5 | `env:scenario-5-otel-oss` |

---

## Stack Tecnologico

| Capa | Tecnologia |
|------|-----------|
| Frontend | Next.js 14 (App Router + Tailwind CSS + Datadog RUM) |
| Backend | .NET 8 Web API (EF Core + OpenTelemetry SDK) |
| Base de datos | PostgreSQL 16 (Bitnami Helm chart) |
| Cache | Redis 7 (Bitnami Helm chart) |
| Observabilidad | Datadog Agent + DDOT + OTel Collector |
| Gateway | NGINX reverse proxy (LoadBalancer) |

---

## Pre-requisitos

### Herramientas

| Herramienta | Version minima | Para que |
|-------------|---------------|----------|
| `kubectl` | 1.28+ | Gestionar el cluster |
| `helm` | 3.14+ | Instalar charts (PostgreSQL, Redis, Datadog) |
| `docker` | 24+ | Build de imagenes |
| `envsubst` | cualquier | Sustitucion de variables en manifiestos |
| `make` | cualquier | Orquestacion de tareas |

### Cuentas y Credenciales

| Credencial | Donde obtenerla |
|------------|----------------|
| **Datadog API Key** | [Datadog > Organization Settings > API Keys](https://app.datadoghq.com/organization-settings/api-keys) |
| **Datadog RUM Application** | [Datadog > RUM > Applications > New Application](https://app.datadoghq.com/rum/application-create) |
| **Container Registry** | Cualquiera: Docker Hub, ACR, ECR, GCR, Harbor, etc. |

### Cluster Kubernetes

Cualquier cluster con:
- **3+ nodos** (2 CPU, 4 GB RAM minimo por nodo)
- Acceso a internet (pull de imagenes, envio de telemetria a Datadog)
- Soporte para Services tipo `LoadBalancer` (o usar `NodePort`/`Ingress` alternativo)

Validado en:
- Azure AKS
- Amazon EKS
- Google GKE
- Minikube / kind (con tuneles para LoadBalancer)

---

## Instalacion Paso a Paso

### 1. Clonar el repositorio

```bash
git clone <repo-url>
cd gorrashop-observability-lab
```

### 2. Configurar el Container Registry

Exporta la URL de tu registry. Esta variable se usa en todo el lab:

```bash
# Ejemplos segun tu registry:
export ACR_LOGIN_SERVER=myregistry.azurecr.io       # Azure ACR
export ACR_LOGIN_SERVER=123456789.dkr.ecr.us-east-1.amazonaws.com  # AWS ECR
export ACR_LOGIN_SERVER=docker.io/miusuario          # Docker Hub
export ACR_LOGIN_SERVER=gcr.io/mi-proyecto           # Google GCR
```

### 3. Build y push de imagenes

```bash
# Login a tu registry (ajusta segun tu provider)
# Azure:   az acr login --name <nombre>
# AWS:     aws ecr get-login-password | docker login --username AWS --password-stdin <url>
# Docker:  docker login
# GCP:     gcloud auth configure-docker

# Build y push
make build-push
```

Esto construye dos imagenes:
- `${ACR_LOGIN_SERVER}/gorrashop-backend:latest` (.NET 8, ~200MB)
- `${ACR_LOGIN_SERVER}/gorrashop-frontend:latest` (Next.js 14, ~150MB)

### 4. Crear el secret de Datadog

```bash
export DD_API_KEY="tu-datadog-api-key"

kubectl create namespace observability --dry-run=client -o yaml | kubectl apply -f -

kubectl create secret generic datadog-secret \
  --from-literal=api-key="${DD_API_KEY}" \
  -n observability --dry-run=client -o yaml | kubectl apply -f -
```

### 5. Configurar RUM (opcional — solo S2 y S4)

Si quieres habilitar Real User Monitoring, crea una aplicacion RUM en Datadog y actualiza los valores en los patches:

```bash
# Edita los archivos con tus valores de RUM:
# k8s/multi-scenario/scenario-2/patch-frontend.yaml
# k8s/multi-scenario/scenario-4/patch-frontend.yaml
#
# Busca y reemplaza:
#   DD_RUM_APPLICATION_ID  → tu Application ID
#   DD_RUM_CLIENT_TOKEN    → tu Client Token
```

### 6. Desplegar todo

```bash
make multi-scenario
```

Este comando ejecuta `scripts/deploy-multi-scenario.sh` que:

1. Crea 5 namespaces (`gorrashop-s1` a `gorrashop-s5`)
2. Instala **PostgreSQL + Redis** en cada namespace (Helm)
3. Despliega **backend + frontend** en cada namespace
4. Instala el **Datadog Agent** con DDOT + OTLP + Admission Controller
5. Instala el **OTel Collector OSS** para S5
6. Aplica **patches por escenario** (env vars, labels, annotations)
7. Reinicia pods de S2 y S4 para inyeccion del tracer
8. Espera a que todos los rollouts completen

Tiempo estimado: **5-8 minutos** (la mayor parte es Helm instalando PostgreSQL/Redis).

### 7. Desplegar el gateway (acceso externo)

```bash
kubectl apply -f k8s/multi-scenario/nginx-gateway.yaml
```

Espera a que Azure/AWS/GCP asigne una IP publica:

```bash
kubectl get svc nginx-gateway -n observability -w
# Espera hasta que EXTERNAL-IP deje de ser <pending>
```

### 8. Desplegar los generadores de trafico

```bash
# Trafico API (curl — todos los escenarios)
kubectl apply -f k8s/multi-scenario/traffic-generator.yaml

# Trafico RUM (Puppeteer — S2 y S4 solamente)
kubectl apply -f k8s/multi-scenario/rum-traffic-generator.yaml
```

---

## Verificacion

### Estado de los pods

```bash
# Ver pods en todos los namespaces del lab
for ns in gorrashop-s1 gorrashop-s2 gorrashop-s3 gorrashop-s4 gorrashop-s5 observability; do
  echo "--- ${ns} ---"
  kubectl get pods -n "${ns}" --no-headers
done
```

Resultado esperado: cada namespace `gorrashop-s*` debe tener 4 pods (backend, frontend, postgresql, redis).

### Acceso a los frontends

Obtener la IP del gateway:

```bash
GATEWAY_IP=$(kubectl get svc nginx-gateway -n observability -o jsonpath='{.status.loadBalancer.ingress[0].ip}')
echo "Gateway: ${GATEWAY_IP}"
```

| Escenario | URL |
|-----------|-----|
| S1 — OTel SDK + DDOT | `http://${GATEWAY_IP}:3001` |
| S2 — DD SDK + DDOT (Rec.) | `http://${GATEWAY_IP}:3002` |
| S3 — OTel SDK + DD Agent | `http://${GATEWAY_IP}:3003` |
| S4 — Full Datadog | `http://${GATEWAY_IP}:3004` |
| S5 — OTel SDK + OSS | `http://${GATEWAY_IP}:3005` |

### Verificar trazas en Datadog

1. Ir a **Datadog > APM > Traces**
2. En el dropdown de `env`, seleccionar cada escenario:
   - `scenario-1-ddot`
   - `scenario-2-ddsdk-ddot`
   - `scenario-3-agent`
   - `scenario-4-sdk`
   - `scenario-5-otel-oss`
3. Verificar que aparecen trazas de `gorrashop-backend` y `gorrashop-frontend`

### Verificar RUM (S2 y S4)

1. Ir a **Datadog > RUM > Sessions**
2. Filtrar por `service:gorrashop-frontend`
3. Verificar sesiones con vistas: Home → Catalog → Product → Cart

### Verificar generadores de trafico

```bash
# Logs del traffic generator (API)
kubectl logs deployment/traffic-generator -n observability --tail=10

# Logs del RUM traffic generator (Puppeteer)
kubectl logs deployment/rum-traffic-generator -n observability --tail=10
```

---

## Acceso sin LoadBalancer (Minikube, kind, etc.)

Si tu cluster no soporta `LoadBalancer`, usa port-forward:

```bash
# Opcion A: Port-forward directo a cada frontend
kubectl port-forward svc/gorrashop-frontend 3001:3000 -n gorrashop-s1 &
kubectl port-forward svc/gorrashop-frontend 3002:3000 -n gorrashop-s2 &
kubectl port-forward svc/gorrashop-frontend 3003:3000 -n gorrashop-s3 &
kubectl port-forward svc/gorrashop-frontend 3004:3000 -n gorrashop-s4 &
kubectl port-forward svc/gorrashop-frontend 3005:3000 -n gorrashop-s5 &

# Opcion B: Port-forward al gateway NGINX
kubectl port-forward svc/nginx-gateway 3001:3001 3002:3002 3003:3003 3004:3004 3005:3005 -n observability &
```

Luego accede via `http://localhost:3001` ... `http://localhost:3005`.

---

## Limpieza

```bash
# Eliminar todo el lab
make multi-scenario-stop

# O manualmente:
for ns in gorrashop-s1 gorrashop-s2 gorrashop-s3 gorrashop-s4 gorrashop-s5; do
  kubectl delete namespace "${ns}"
done
helm uninstall datadog -n observability
kubectl delete -f k8s/multi-scenario/nginx-gateway.yaml
kubectl delete -f k8s/multi-scenario/traffic-generator.yaml
kubectl delete -f k8s/multi-scenario/rum-traffic-generator.yaml
```

---

## Estructura del Proyecto

```
.
├── Makefile                              # Orquestador principal
├── apps/
│   ├── backend/                          # .NET 8 Web API
│   │   ├── Dockerfile
│   │   └── src/GorraShop.API/
│   │       └── Program.cs                # OTel SDK config (trazas, metricas, logs)
│   └── frontend/                         # Next.js 14
│       ├── Dockerfile
│       ├── instrumentation.ts            # OTel Node.js SDK
│       └── src/components/DatadogRum.tsx  # RUM browser SDK (configurable via env vars)
│
├── k8s/
│   ├── apps/                             # Manifiestos base (deployment + service)
│   │   ├── backend/
│   │   └── frontend/
│   ├── observability/                    # Configs de collectors (reutilizados por multi-scenario)
│   │   └── scenario-2-otel-dd/           # OTel Collector OSS con Datadog Exporter
│   └── multi-scenario/                   # << Directorio principal del lab >>
│       ├── namespaces.yaml               # 5 namespaces con labels
│       ├── datadog-agent-values.yaml     # DD Agent combinado (DDOT + OTLP + Admission)
│       ├── ddot-otel-config.yaml         # Config del DDOT embedded en el Agent
│       ├── nginx-gateway.yaml            # NGINX reverse proxy (LB + puertos 3001-3005)
│       ├── traffic-generator.yaml        # curl-based (API) — 5 escenarios
│       ├── rum-traffic-generator.yaml    # Puppeteer (browser) — S2 y S4
│       ├── scenario-1/                   # Patches: OTel SDK → DDOT
│       ├── scenario-2/                   # Patches: DD SDK + DDOT + RUM
│       ├── scenario-3/                   # Patches: OTel SDK → DD Agent OTLP
│       ├── scenario-4/                   # Patches: DD SDK nativo + RUM
│       └── scenario-5/                   # Patches: OTel SDK → OSS Collector
│
├── scripts/
│   ├── build-push.sh                     # Build Docker + push al registry
│   ├── deploy-multi-scenario.sh          # Despliega los 5 escenarios
│   └── setup-cluster.sh                  # Setup base (namespaces, helm repos)
│
└── docs/
    └── scenarios-comparison.md           # Tabla comparativa detallada
```

---

## Como Funciona (para curiosos)

### Patron de Patches

La misma imagen Docker se despliega 5 veces. Lo unico que cambia son las **variables de entorno** inyectadas via `kubectl patch --type=strategic`:

```yaml
# Ejemplo: k8s/multi-scenario/scenario-1/patch-backend.yaml
spec:
  template:
    spec:
      containers:
        - name: gorrashop-backend
          env:
            - name: OTEL_EXPORTER_OTLP_ENDPOINT
              value: "http://$(HOST_IP):4327"    # ← DDOT en el host
            - name: DD_ENV
              value: "scenario-1-ddot"            # ← Tag de environment
```

El codigo de la app **no cambia** entre escenarios. El OTel SDK lee `OTEL_EXPORTER_OTLP_ENDPOINT` para saber donde enviar la telemetria.

### Datadog Agent Combinado

Un solo Datadog Agent DaemonSet sirve a los 5 escenarios simultaneamente:

| Puerto | Protocolo | Quien lo usa |
|--------|-----------|-------------|
| 4327 | OTLP/gRPC (DDOT) | S1 (OTel SDK → DDOT) |
| 4317 | OTLP/gRPC (Agent) | S3 (OTel SDK → Agent OTLP) |
| 8126 | DD APM nativo | S2, S4 (DD SDK via Admission Controller) |

### RUM (Real User Monitoring)

El componente `DatadogRum.tsx` se configura 100% via env vars de Kubernetes:

| Variable | Proposito |
|----------|-----------|
| `DD_RUM_ENABLED` | `"true"` activa RUM (solo S2 y S4) |
| `DD_RUM_APPLICATION_ID` | ID de la app RUM en Datadog |
| `DD_RUM_CLIENT_TOKEN` | Token publico del browser SDK |
| `DD_ENV` | Tag de environment (correlaciona RUM con APM) |

El layout de Next.js (server component) lee estas variables y las pasa como props al componente client-side. En escenarios sin `DD_RUM_ENABLED`, el SDK no se inicializa.

---

## Troubleshooting

### Los pods no arrancan

```bash
# Ver eventos del pod
kubectl describe pod <nombre> -n <namespace>

# Causa comun: imagen no encontrada
# Verifica que ACR_LOGIN_SERVER es correcto y que hiciste push
kubectl get events -n <namespace> --sort-by='.lastTimestamp'
```

### No veo trazas en Datadog

```bash
# 1. Verificar que el DD Agent esta corriendo
kubectl get pods -n observability -l app=datadog

# 2. Verificar el secret
kubectl get secret datadog-secret -n observability

# 3. Ver logs del Agent
kubectl logs -l app=datadog -n observability -c agent --tail=20

# 4. Verificar env vars del backend
kubectl exec deployment/gorrashop-backend -n gorrashop-s1 -- env | grep OTEL
```

### El frontend no conecta al backend

```bash
# Verificar que el service del backend existe
kubectl get svc gorrashop-backend -n gorrashop-s1

# Verificar conectividad desde el frontend
kubectl exec deployment/gorrashop-frontend -n gorrashop-s1 -- \
  wget -q -O- http://gorrashop-backend:8080/health
```

### RUM no genera sesiones

1. RUM requiere un **navegador real** (curl no ejecuta JavaScript)
2. Verificar que `DD_RUM_ENABLED=true` esta en el pod:
   ```bash
   kubectl exec deployment/gorrashop-frontend -n gorrashop-s2 -- env | grep DD_RUM
   ```
3. El `rum-traffic-generator` usa Puppeteer (headless Chrome) — verificar sus logs:
   ```bash
   kubectl logs deployment/rum-traffic-generator -n observability --tail=10
   ```

---

## FAQ

**P: Puedo correr esto sin Datadog?**
R: El S5 (OTel SDK + OSS Collector) envia telemetria al Collector upstream. Para tener Prometheus + Grafana + Jaeger, usa los manifiestos en `k8s/observability/scenario-5-pure-otel/`.

**P: Cuantos recursos necesita el cluster?**
R: Con los 5 escenarios corriendo: ~8 CPU y ~16 GB RAM totales. Cada namespace usa ~1.5 CPU y ~2 GB. El Datadog Agent usa ~1 CPU y ~2 GB adicionales.

**P: Puedo correr solo 1 escenario?**
R: Si. Usa `make scenario-N` (N=1..5) para activar un solo escenario en el namespace `gorrashop`. Esto es independiente del modo multi-scenario.

**P: Que pasa si mi registry no es ACR?**
R: El lab usa `ACR_LOGIN_SERVER` como nombre de variable por herencia de Azure, pero funciona con cualquier registry. Solo exporta la URL correcta.

**P: Como agrego mi propio escenario?**
R: Crea un directorio `k8s/multi-scenario/scenario-N/` con `patch-backend.yaml` y `patch-frontend.yaml`. Agrega el namespace a `namespaces.yaml` y el loop en `deploy-multi-scenario.sh`.
