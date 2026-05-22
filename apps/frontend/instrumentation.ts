/**
 * instrumentation.ts — OTel SDK para Next.js
 *
 * Next.js carga este archivo automáticamente en el servidor Node.js.
 * Se ejecuta UNA vez al arrancar el servidor, antes de cualquier request.
 *
 * Variables de entorno relevantes (cambian por escenario en K8s):
 *   OTEL_EXPORTER_OTLP_ENDPOINT  — endpoint del collector
 *   OTEL_SERVICE_NAME            — nombre del servicio
 *   OTEL_SERVICE_VERSION         — versión
 *   NEXT_PUBLIC_OBSERVABILITY_ENABLED — "true"/"false"
 */
export async function register() {
  if (process.env.NEXT_RUNTIME === "nodejs") {
    const enabled = process.env.NEXT_PUBLIC_OBSERVABILITY_ENABLED !== "false";
    if (!enabled) return;

    const { NodeSDK }        = await import("@opentelemetry/sdk-node");
    const { Resource }       = await import("@opentelemetry/resources");
    const { SEMRESATTRS_SERVICE_NAME, SEMRESATTRS_SERVICE_VERSION, SEMRESATTRS_DEPLOYMENT_ENVIRONMENT }
                             = await import("@opentelemetry/semantic-conventions");
    const { OTLPTraceExporter }
                             = await import("@opentelemetry/exporter-trace-otlp-grpc");
    const { OTLPMetricExporter }
                             = await import("@opentelemetry/exporter-metrics-otlp-grpc");
    const { PeriodicExportingMetricReader }
                             = await import("@opentelemetry/sdk-metrics");
    const { getNodeAutoInstrumentations }
                             = await import("@opentelemetry/auto-instrumentations-node");

    const otlpEndpoint = process.env.OTEL_EXPORTER_OTLP_ENDPOINT ?? "http://localhost:4317";

    const sdk = new NodeSDK({
      resource: new Resource({
        [SEMRESATTRS_SERVICE_NAME]:              process.env.OTEL_SERVICE_NAME    ?? "gorrashop-frontend",
        [SEMRESATTRS_SERVICE_VERSION]:           process.env.OTEL_SERVICE_VERSION ?? "1.0.0",
        [SEMRESATTRS_DEPLOYMENT_ENVIRONMENT]:   process.env.DD_ENV               ?? "lab",
      }),
      traceExporter: new OTLPTraceExporter({
        url: otlpEndpoint,
      }),
      metricReader: new PeriodicExportingMetricReader({
        exporter: new OTLPMetricExporter({
          url: otlpEndpoint,
        }),
        exportIntervalMillis: 10_000,
      }),
      instrumentations: [
        getNodeAutoInstrumentations({
          "@opentelemetry/instrumentation-http":    { enabled: true },
          "@opentelemetry/instrumentation-fs":      { enabled: false }, // demasiado verboso
          "@opentelemetry/instrumentation-dns":     { enabled: false },
        }),
      ],
    });

    sdk.start();
    console.log(`[OTel] SDK started — exporting to ${otlpEndpoint}`);
  }
}
