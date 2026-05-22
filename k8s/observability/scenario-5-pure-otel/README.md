# Escenario 5 — Pure OpenTelemetry (sin Datadog)

## ¿Qué es?

100% ecosistema open-source CNCF:
- **OTel Collector** recibe las señales
- **Prometheus** almacena métricas
- **Grafana** visualiza todo
- **Jaeger** (opcional) para trazas

Sin Datadog. Útil para:
- Comparar la experiencia UX de Grafana vs Datadog
- Entornos air-gapped o sin licencia de Datadog
- Validar que la instrumentación OTel funciona correctamente

## Arquitectura

```
App (.NET 8 + OTel SDK)
         │
         │  OTLP/gRPC :4317
         ▼
 [OTel Collector]
    ├── prometheusexporter (:8889)  ←── Prometheus scrapes
    ├── otlpexporter → Jaeger :4317 (trazas)
    └── debugexporter (logs de collector)

[Prometheus] → scrape → OTel Collector :8889
[Grafana]    → query  → Prometheus + Jaeger
```

## Activar

```bash
make scenario-5
# No requiere DD_API_KEY
```

## Ver los dashboards

```bash
# Grafana (admin / se imprime en consola)
kubectl port-forward svc/grafana 3001:80 -n observability

# Jaeger UI
kubectl port-forward svc/jaeger-query 16686:16686 -n observability
```
Abre http://localhost:3001 y http://localhost:16686

## Dashboards incluidos

Grafana viene preconfigurado con:
- **GorraShop — API Overview**: request rate, error rate, latency (p50/p95/p99)
- **GorraShop — .NET Runtime**: GC, heap, thread pool
- **GorraShop — Kubernetes**: CPU/Mem por pod
