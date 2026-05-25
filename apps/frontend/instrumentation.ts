// ╔══════════════════════════════════════════════════════════════════════════════╗
// ║  BLOQUE OTel SDK — Todo esto es necesario para Escenarios 1, 3 y 5        ║
// ║                                                                            ║
// ║  Con DD SDK (Escenarios 2 y 4) NADA de esto se necesita.                   ║
// ║  El Admission Controller inyecta dd-trace-js automáticamente               ║
// ║  y este archivo se apaga con NEXT_PUBLIC_OBSERVABILITY_ENABLED=false        ║
// ║                                                                            ║
// ║  Paquetes npm requeridos (7 paquetes):                                     ║
// ║    - @opentelemetry/sdk-node                                               ║
// ║    - @opentelemetry/resources                                              ║
// ║    - @opentelemetry/semantic-conventions                                   ║
// ║    - @opentelemetry/exporter-trace-otlp-grpc                               ║
// ║    - @opentelemetry/exporter-metrics-otlp-grpc                             ║
// ║    - @opentelemetry/sdk-metrics                                            ║
// ║    - @opentelemetry/auto-instrumentations-node                             ║
// ╚══════════════════════════════════════════════════════════════════════════════╝
export async function register() {
  if (process.env.NEXT_RUNTIME === "nodejs") {
    // [OTEL] Flag para apagar OTel cuando DD SDK se encarga (S2/S4)
    const enabled = process.env.NEXT_PUBLIC_OBSERVABILITY_ENABLED !== "false";
    if (!enabled) return;

    // [OTEL] 7 imports — cada uno es un paquete npm que TÚ instalas y mantienes
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

    // [OTEL] Endpoint del collector — definido por env var en cada escenario
    const otlpEndpoint = process.env.OTEL_EXPORTER_OTLP_ENDPOINT ?? "http://localhost:4317";

    const sdk = new NodeSDK({
      // [OTEL] Identidad del servicio — con DD SDK es automático via DD_SERVICE
      resource: new Resource({
        [SEMRESATTRS_SERVICE_NAME]:              process.env.OTEL_SERVICE_NAME    ?? "gorrashop-frontend",
        [SEMRESATTRS_SERVICE_VERSION]:           process.env.OTEL_SERVICE_VERSION ?? "1.0.0",
        [SEMRESATTRS_DEPLOYMENT_ENVIRONMENT]:   process.env.DD_ENV               ?? "lab",
      }),
      // [OTEL] Exportador de traces — TÚ configuras el protocolo y destino
      traceExporter: new OTLPTraceExporter({
        url: otlpEndpoint,
      }),
      // [OTEL] Exportador de métricas — TÚ defines el intervalo de export
      metricReader: new PeriodicExportingMetricReader({
        exporter: new OTLPMetricExporter({
          url: otlpEndpoint,
        }),
        exportIntervalMillis: 10_000,
      }),
      // [OTEL] Instrumentaciones — TÚ eliges cuáles habilitar/deshabilitar
      instrumentations: [
        getNodeAutoInstrumentations({
          "@opentelemetry/instrumentation-http":    { enabled: true },  // Captura HTTP requests
          "@opentelemetry/instrumentation-fs":      { enabled: false }, // Demasiado verboso
          "@opentelemetry/instrumentation-dns":     { enabled: false }, // No necesario
        }),
      ],
    });

    sdk.start();
    console.log(`[OTel] SDK started — exporting to ${otlpEndpoint}`);
  }
}
// ╔══════════════════════════════════════════════════════════════════════════════╗
// ║  FIN BLOQUE OTel — Las ~50 líneas anteriores + 7 paquetes npm             ║
// ║  se reemplazan con 2 annotations de Kubernetes en DD SDK:                  ║
// ║                                                                            ║
// ║    admission.datadoghq.com/enabled: "true"                                 ║
// ║    admission.datadoghq.com/js-lib.version: "v5"                            ║
// ╚══════════════════════════════════════════════════════════════════════════════╝
