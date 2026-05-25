using GorraShop.API.Data;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Logs;          // [OTEL] Paquete: OpenTelemetry.Exporter.OpenTelemetryProtocol
using OpenTelemetry.Metrics;       // [OTEL] Paquete: OpenTelemetry.Extensions.Hosting
using OpenTelemetry.Resources;     // [OTEL] Paquete: OpenTelemetry.Extensions.Hosting
using OpenTelemetry.Trace;         // [OTEL] Paquete: OpenTelemetry.Extensions.Hosting
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// ╔══════════════════════════════════════════════════════════════════════════════╗
// ║  BLOQUE OTel SDK — Todo esto es necesario para Escenarios 1, 3 y 5        ║
// ║                                                                            ║
// ║  Con DD SDK (Escenarios 2 y 4) NADA de esto se necesita.                   ║
// ║  El Admission Controller inyecta dd-trace-dotnet automáticamente           ║
// ║  y este bloque se apaga con OBSERVABILITY_ENABLED=false                    ║
// ║                                                                            ║
// ║  Paquetes NuGet requeridos (8 paquetes):                                   ║
// ║    - OpenTelemetry.Extensions.Hosting                                      ║
// ║    - OpenTelemetry.Instrumentation.AspNetCore                              ║
// ║    - OpenTelemetry.Instrumentation.Http                                    ║
// ║    - OpenTelemetry.Instrumentation.Runtime                                 ║
// ║    - OpenTelemetry.Instrumentation.EntityFrameworkCore                     ║
// ║    - OpenTelemetry.Instrumentation.StackExchangeRedis                      ║
// ║    - OpenTelemetry.Exporter.OpenTelemetryProtocol                          ║
// ║    - OpenTelemetry.Exporter.Console                                        ║
// ╚══════════════════════════════════════════════════════════════════════════════╝

// Redis — conexión compartida para cache + OTel instrumentation
var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "redis-master:6379";
var redisConnection = ConnectionMultiplexer.Connect(redisConnectionString);
builder.Services.AddSingleton<IConnectionMultiplexer>(redisConnection);

// [OTEL] Flag para apagar OTel cuando DD SDK se encarga (S2/S4)
var observabilityEnabled = builder.Configuration.GetValue("OBSERVABILITY_ENABLED", true);

if (observabilityEnabled)
{
    // [OTEL] Variables de entorno que TÚ defines por escenario en K8s
    var serviceName    = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME")    ?? "gorrashop-backend";
    var serviceVersion = Environment.GetEnvironmentVariable("OTEL_SERVICE_VERSION") ?? "1.0.0";
    var otlpEndpoint   = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") ?? "http://localhost:4317";
    var environment    = Environment.GetEnvironmentVariable("DD_ENV")
                      ?? Environment.GetEnvironmentVariable("DEPLOYMENT_ENV")
                      ?? "lab";

    builder.Services.AddOpenTelemetry()
        // [OTEL] Identidad del servicio — con DD SDK esto es automático via DD_SERVICE
        .ConfigureResource(resource => resource
            .AddService(serviceName, serviceVersion: serviceVersion)
            .AddAttributes(new Dictionary<string, object>
            {
                ["deployment.environment"] = environment,
                ["host.name"]              = Environment.MachineName,
            }))
        // ──── TRACING ─────────────────────────────────────────────────────────
        // [OTEL] Cada instrumentación es un paquete NuGet que TÚ agregas y configuras
        // Con DD SDK todo esto es automático — zero configuración
        .WithTracing(tracing => tracing
            // [OTEL] Captura requests HTTP entrantes (spans de ASP.NET Core)
            .AddAspNetCoreInstrumentation(opts =>
            {
                opts.RecordException = true;
                opts.Filter = ctx => !ctx.Request.Path.StartsWithSegments("/health");
            })
            // [OTEL] Captura requests HTTP salientes (calls a otros servicios)
            .AddHttpClientInstrumentation()
            // [OTEL] Captura queries SQL de Entity Framework Core
            // Sin esto NO ves las queries SQL en los traces
            .AddEntityFrameworkCoreInstrumentation(opts =>
            {
                opts.SetDbStatementForText = true; // Muestra el texto de la query
            })
            // [OTEL] Captura comandos Redis (GET, SET, EVAL, etc.)
            .AddRedisInstrumentation(redisConnection)
            // [OTEL] Exportador — a dónde se envía la telemetría
            // S1: DDOT Collector | S3: DD Agent OTLP | S5: OTel Collector OSS
            .AddOtlpExporter(otlp =>
            {
                otlp.Endpoint = new Uri(otlpEndpoint);
            }))
        // ──── MÉTRICAS ────────────────────────────────────────────────────────
        // [OTEL] Métricas de runtime, HTTP y ASP.NET Core
        .WithMetrics(metrics => metrics
            .AddAspNetCoreInstrumentation()   // [OTEL] Request rate, duration, error rate
            .AddHttpClientInstrumentation()   // [OTEL] Outbound HTTP metrics
            .AddRuntimeInstrumentation()      // [OTEL] GC, ThreadPool, memory
            .AddOtlpExporter(otlp =>
            {
                otlp.Endpoint = new Uri(otlpEndpoint);
            }));

    // ──── LOGS ────────────────────────────────────────────────────────────────
    // [OTEL] Correlaciona logs con traces via trace_id/span_id
    builder.Logging.AddOpenTelemetry(logs =>
    {
        logs.IncludeFormattedMessage = true;
        logs.IncludeScopes           = true;
        logs.AddOtlpExporter(otlp =>
        {
            otlp.Endpoint = new Uri(otlpEndpoint);
        });
    });
}
// ╔══════════════════════════════════════════════════════════════════════════════╗
// ║  FIN BLOQUE OTel — Las ~70 líneas anteriores + 8 paquetes NuGet           ║
// ║  se reemplazan con 2 annotations de Kubernetes en DD SDK:                  ║
// ║                                                                            ║
// ║    admission.datadoghq.com/enabled: "true"                                 ║
// ║    admission.datadoghq.com/dotnet-lib.version: "v3"                        ║
// ╚══════════════════════════════════════════════════════════════════════════════╝

// ─── Servicios ────────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "GorraShop API", Version = "v1" });
});

// PostgreSQL con Entity Framework Core
builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

builder.Services.AddStackExchangeRedisCache(opts =>
{
    opts.ConnectionMultiplexerFactory = () => Task.FromResult<IConnectionMultiplexer>(redisConnection);
});

// CORS — permite llamadas desde el frontend Next.js
builder.Services.AddCors(opts =>
{
    opts.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

// Health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("postgres")
    .AddRedis(builder.Configuration.GetConnectionString("Redis") ?? "redis-master:6379", name: "redis");

var app = builder.Build();

// ─── Middleware ────────────────────────────────────────────────────────────────
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors();
app.MapControllers();
app.MapHealthChecks("/health");

// Crear schema y seed de datos al arrancar
// EnsureCreated() crea las tablas directamente desde el modelo de EF Core,
// sin necesitar archivos de migración generados con "dotnet ef migrations add".
// Es la opción ideal para este lab — en producción usarías MigrateAsync().
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
    await SeedData.SeedAsync(db);
}

app.Run();
