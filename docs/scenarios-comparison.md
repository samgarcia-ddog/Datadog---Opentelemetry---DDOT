# Comparacion de Escenarios de Observabilidad

## Tabla de referencia rapida

| | S1: OTel+DDOT | S2: DD SDK+DDOT (Rec.) | S3: OTel+Agent OTLP | S4: Full Datadog | S5: OTel+OSS |
|--|---------------|------------------------|---------------------|------------------|--------------|
| **Opcion doc.** | 3.2 | 3.1 | 3.3 | 3.0 | 3.4 |
| **SDK en la app** | OTel SDK | DD SDK (auto-instr) | OTel SDK | DD SDK (auto-instr) | OTel SDK |
| **Collector** | DDOT (embedded) | DDOT (embedded) | DD Agent (OTLP) | DD Agent (APM) | OTel Collector OSS |
| **Protocolo App→Collector** | OTLP/gRPC | DD APM nativo | OTLP/gRPC | DD APM nativo | OTLP/gRPC |
| **Puerto destino** | 4327 (hostPort) | 8126 (APM) | 4317 (hostPort) | 8126 (APM) | 4317 (svc) |
| **Requiere DD API Key** | Si | Si | Si | Si | Si |
| **Cambios en codigo** | No | No | No | No | No |
| **Collector extra** | No (DDOT en Agent) | No (DDOT en Agent) | No (Agent = collector) | No | Si (OTel Collector) |
| **Admission Controller** | No | **Si** | No | **Si** | No |

## Cuando usar cada uno

### S1 — OTel SDK + DDOT Collector (Opcion 3.2)
**Usa cuando**: quieres mantener OTel SDK en tu codigo y tener la mejor integracion con Datadog.
El DDOT es la distribucion curada por Datadog del OTel Collector, con el exporter preconfigurado. Tu app usa instrumentacion estandar OTel y no depende de ningun vendor en el codigo.

### S2 — DD SDK + DDOT (Opcion 3.1, Recomendada)
**Usa cuando**: quieres el maximo de funcionalidades Datadog (Profiling, DBM, RUM, AAP) sin sacrificar flexibilidad OTel.
El Admission Controller inyecta `dd-trace` automaticamente — no hay cambios en codigo ni Dockerfile. El DDOT queda habilitado para poder ingestar telemetria OTel de otros servicios si los hubiera.

### S3 — OTel SDK + DD Agent OTLP (Opcion 3.3)
**Usa cuando**: ya tienes el Datadog Agent desplegado y quieres evitar un collector adicional.
El Agent recibe OTLP directamente por su puerto 4317. Infraestructura mas simple pero sin acceso a features exclusivos de DD SDK.

### S4 — Full Datadog Nativo (Opcion 3.0)
**Usa cuando**: estas 100% en el ecosistema Datadog y quieres todas las funcionalidades. Equivalente a S2 sin el DDOT — el DD SDK envia directamente al Agent via APM protocol.

### S5 — OTel SDK + OSS Collector (Opcion 3.4)
**Usa cuando**: necesitas enviar telemetria a multiples backends (fan-out) o quieres control total del pipeline con el Collector upstream de la comunidad. Usa el `datadogexporter` en el config del Collector.

## Matriz de funcionalidades Datadog

| Funcionalidad | S1 DDOT | S2 DD+DDOT | S3 Agent | S4 Nativo | S5 OSS |
|---------------|---------|------------|----------|-----------|--------|
| Distributed Tracing | Si | Si | Si | Si | Si |
| Metricas de runtime | Si | Si | Si | Si | Si |
| Logs correlacionados | Si | Si | Si | Si | Si |
| Service Map | Si | Si | Si | Si | Si |
| Continuous Profiler | No | **Si** | No | **Si** | No |
| Database Monitoring (DBM) | No | **Si** | No | **Si** | No |
| RUM (Real User Monitoring) | No | **Si** | No | **Si** | No |
| App & API Protection (AAP) | No | **Si** | No | **Si** | No |
| Cloud SIEM | Si | Si | Si | Si | Si |
| Error Tracking avanzado | No | **Si** | No | **Si** | No |
| Multi-backend (fan-out) | Limitado | Limitado | No | No | **Si** |

## Flujo de datos detallado

```
┌───────────────────────────────────────────────────────────────────┐
│                    S1, S3, S5 — OTel SDK                          │
│                                                                   │
│  App (.NET 8 / Next.js)                                           │
│  ├── OpenTelemetry SDK (NuGet/npm)                                │
│  ├── Instrumenta: HTTP, EF Core, Redis, Runtime                   │
│  └── Exporta via OTLP/gRPC → Collector → Datadog                 │
│                                                                   │
│  OTEL_EXPORTER_OTLP_ENDPOINT controla el destino                  │
└───────────────────────────────────────────────────────────────────┘

┌───────────────────────────────────────────────────────────────────┐
│                    S2, S4 — DD SDK                                 │
│                                                                   │
│  App (.NET 8 / Next.js) — sin OTel SDK activo                     │
│  ├── DD Admission Controller detecta labels/annotations            │
│  ├── Inyecta dd-trace-dotnet (CLR Profiler) o dd-trace-js          │
│  │   Sin cambios en codigo, sin cambios en Dockerfile              │
│  ├── Profiling: DD_PROFILING_ENABLED=true                          │
│  └── Exporta via DD APM protocol → DD Agent → Datadog             │
│                                                                   │
│  Pods requieren:                                                   │
│    label:      admission.datadoghq.com/enabled: "true"             │
│    annotation: admission.datadoghq.com/dotnet-lib.version: "v2"    │
│    annotation: admission.datadoghq.com/js-lib.version: "v5"        │
└───────────────────────────────────────────────────────────────────┘
```

## Tags y etiquetas

Todos los escenarios usan etiquetas consistentes para filtrar en Datadog:

| Tag | Valores | Uso |
|-----|---------|-----|
| `env` | `scenario-{1..5}-*` | Filtro principal en APM, RUM, Logs |
| `service` | `gorrashop-backend`, `gorrashop-frontend` | Dropdown de servicios |
| `version` | `1.0.0` | Tracking de versiones |
| `scenario` | `1`..`5` | Tag custom para agrupacion |
| `pipeline` | `ddot-collector`, `dd-sdk-ddot`, `dd-agent-otlp`, `dd-sdk`, `otel-collector-oss` | Identifica la pipeline |

## Recomendacion

Para la mayoria de clientes Datadog, la **Opcion 3.1 (S2 — DD SDK + DDOT)** es la recomendada porque:

1. **Maximo de funcionalidades** — Profiling, DBM, RUM, AAP solo estan disponibles con DD SDK
2. **Cero cambios en codigo** — El Admission Controller inyecta todo automaticamente
3. **Flexibilidad OTel** — El DDOT queda habilitado para ingestar telemetria de servicios OTel si los hubiera
4. **Upgrade path** — Facil migrar hacia/desde OTel SDK en el futuro
