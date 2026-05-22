# Escenario 1 — DDOT Collector (Recomendado por Datadog)

## ¿Qué es?

**DDOT** (Datadog Distribution of OpenTelemetry Collector) es la distribución oficial de Datadog del OTel Collector. Incluye el `datadogexporter` preconfigurado y componentes adicionales desarrollados por Datadog.

Es la opción **recomendada** cuando quieres usar OpenTelemetry SDK y enviar datos a Datadog, ya que:
- Soporta todos los tipos de señal: trazas, métricas y logs
- Mapeo correcto de semántica OTel → Datadog
- Infraestructura correlation automática (host tags, container tags)
- Compatible con el formato OTLP nativo

## Arquitectura del escenario

```
App (.NET 8 + OTel SDK)
         │
         │  OTLP/gRPC :4317
         ▼
  [DDOT Collector Pod]
         │
         │  Datadog API
         ▼
    [Datadog Backend]
```

## Flujo de datos

1. La app exporta trazas/métricas/logs via **OTLP gRPC** al DDOT Collector
2. El DDOT Collector procesa y enriquece los datos
3. El DDOT Collector exporta a Datadog via la API de Datadog

## Activar

```bash
export DD_API_KEY="tu-api-key"
make scenario-1
```

## Qué ver en Datadog

- **APM → Services** → `gorrashop-backend`, `gorrashop-frontend`
- **APM → Traces** → trazas distribuidas entre frontend y backend
- **Metrics → Summary** → métricas de runtime .NET y HTTP
- **Logs** → logs correlacionados con trazas via `trace_id`

## Variables de entorno que se aplican a los pods

| Variable | Valor en este escenario |
|----------|------------------------|
| `OTEL_EXPORTER_OTLP_ENDPOINT` | `http://ddot-collector.observability:4317` |
| `OTEL_SERVICE_NAME` | `gorrashop-backend` / `gorrashop-frontend` |

## Diferencia con Escenario 2

- Escenario 1 usa **DDOT** (distribución de Datadog, más integración nativa)
- Escenario 2 usa **OTel Collector upstream** con el Datadog Exporter (más genérico)
