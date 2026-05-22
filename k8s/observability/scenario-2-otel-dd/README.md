# Escenario 2 — OTel Collector (upstream) + Datadog Exporter

## ¿Qué es?

Usa el **OpenTelemetry Collector** oficial (upstream de la CNCF) configurado con el `datadogexporter`. A diferencia del DDOT, aquí usas el collector "vanilla" de la comunidad.

### Cuándo elegir este sobre el DDOT

- Tienes **múltiples backends** (no solo Datadog) y quieres un collector agnóstico
- Usas **GitOps** con charts de la comunidad OTel
- Quieres control total sobre los componentes del collector

## Arquitectura

```
App (.NET 8 + OTel SDK)
         │
         │  OTLP/gRPC :4317
         ▼
[OTel Collector - otelcol/contrib]
    ├── datadogexporter  → Datadog
    └── (podría agregar prometheusexporter, etc.)
```

## Activar

```bash
export DD_API_KEY="tu-api-key"
make scenario-2
```

## Diferencia clave vs Escenario 1

| | DDOT (S1) | OTel Collector upstream (S2) |
|--|-----------|------------------------------|
| Mantenedor | Datadog | CNCF / comunidad |
| Componentes | Curado por Datadog | Todos los contrib |
| Integración DD | Nativa | Via `datadogexporter` |
| Multi-backend | Limitado | Completo |
