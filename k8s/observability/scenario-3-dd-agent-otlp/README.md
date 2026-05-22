# Escenario 3 — Datadog Agent con OTLP Receiver

## ¿Qué es?

El **Datadog Agent** (desde v6.32 / v7.32) puede recibir datos en formato **OTLP** directamente, sin necesidad de un collector separado. Actúa como receptor OTLP y reenvía a Datadog.

### Cuándo elegir este

- Ya tienes el **Datadog Agent** desplegado en tu cluster
- Quieres **simplificar** la infraestructura (sin collector extra)
- Quieres aprovechar las capacidades del Agent: NPM, USM, Live Processes, etc.
- Quieres correlación automática con métricas de infraestructura de Datadog

## Arquitectura

```
App (.NET 8 + OTel SDK)
         │
         │  OTLP/gRPC  → Agent Pod en el mismo nodo (hostPort)
         │  o
         │  OTLP/gRPC  → Servicio del Agent (ClusterIP)
         ▼
  [Datadog Agent DaemonSet]
    ├── OTLP Receiver (:4317)
    └── Datadog Forwarder → Datadog Backend
```

## OTLP al Agent — Dos formas de apuntar

### Opción A: Via `status.hostIP` (recomendada, un Agent por nodo)
```yaml
env:
  - name: HOST_IP
    valueFrom:
      fieldRef:
        fieldPath: status.hostIP
  - name: OTEL_EXPORTER_OTLP_ENDPOINT
    value: "$(HOST_IP):4317"
```

### Opción B: Via Service (más simple para el lab)
```yaml
env:
  - name: OTEL_EXPORTER_OTLP_ENDPOINT
    value: "http://datadog-agent.observability:4317"
```

Este escenario usa la **Opción B** para simplificar el lab.

## Activar

```bash
export DD_API_KEY="tu-api-key"
make scenario-3
```
