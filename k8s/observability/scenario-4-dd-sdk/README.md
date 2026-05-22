# Escenario 4 — Datadog SDK (Auto-instrumentación via Admission Controller)

## ¿Qué es?

En este escenario la app **no usa OTel SDK**. En cambio, el **Datadog Admission Controller** (parte del Datadog Cluster Agent) **inyecta automáticamente** el tracer de Datadog en el pod cuando arranca.

Para .NET esto significa que el APM de Datadog se inyecta como un **CLR Profiler** sin necesidad de modificar el código ni el Dockerfile.

## Arquitectura

```
App (.NET 8 — sin OTel SDK)
         │
         │  El Admission Controller mutó el pod:
         │  inyectó variables de entorno + initContainer
         │  con el dd-trace-dotnet library
         ▼
  [Datadog Agent DaemonSet]  ← trazas via DD APM protocol
         │
         ▼
    [Datadog Backend]
```

## ¿Cómo funciona la inyección?

Cuando el pod tiene la anotación `admission.datadoghq.com/enabled: "true"`, el Cluster Agent:

1. Muta el pod al momento de creación (via MutatingWebhookConfiguration)
2. Agrega un **initContainer** que copia la librería `dd-trace-dotnet` en un volumen compartido
3. Inyecta las variables de entorno necesarias para el CLR Profiler:
   - `CORECLR_ENABLE_PROFILING=1`
   - `CORECLR_PROFILER={846F5F1C-F9AE-4B07-969E-05C26BC060D8}`
   - `CORECLR_PROFILER_PATH=/dd_tracer/dotnet/Datadog.Trace.ClrProfiler.Native.so`

## Diferencia con los escenarios anteriores

| | Escenarios 1-3 | Escenario 4 |
|--|---------------|-------------|
| Instrumento | OTel SDK | Datadog SDK |
| Cambios en código | Sí (packages NuGet OTel) | **No** |
| Cambios en Dockerfile | No | **No** |
| Configuración | Variables de entorno | Variables de entorno |
| Protocolo | OTLP | DD APM (msgpack) |
| Features | OTel estándar | Datadog nativo (DBM, profiling, etc.) |

## Activar

```bash
export DD_API_KEY="tu-api-key"
make scenario-4
```

El make target:
1. Despliega el Datadog Agent con Admission Controller habilitado
2. Aplica el recurso `Instrumentation` de Datadog en el namespace `gorrashop`
3. Reinicia los pods para que la inyección surta efecto

## Nota sobre OTel SDK en este escenario

Como la app tiene OTel SDK instalado (en los packages NuGet), hay que deshabilitar
su exportación para evitar conflictos. El patch establece `OBSERVABILITY_ENABLED=false`
en la app para desactivar el SDK de OTel y dejar solo el tracer de Datadog.
