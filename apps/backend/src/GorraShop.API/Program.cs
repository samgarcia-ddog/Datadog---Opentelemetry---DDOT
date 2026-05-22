using GorraShop.API.Data;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// ─── Configuración de Observabilidad ──────────────────────────────────────────
// Controlada 100% por variables de entorno — sin cambios de código entre escenarios.
//
// Variables relevantes:
//   OTEL_EXPORTER_OTLP_ENDPOINT  → endpoint del collector (cambia por escenario)
//   OTEL_SERVICE_NAME            → nombre del servicio en Datadog/Grafana
//   OTEL_SERVICE_VERSION         → versión del servicio
//   OTEL_RESOURCE_ATTRIBUTES     → atributos extra (ej: deployment.environment=lab)
//   OBSERVABILITY_ENABLED        → "true"/"false" (default: true)
//
// Escenario 4 (Datadog SDK): el Datadog Agent admission controller inyecta el
// tracer automáticamente — este bloque se ignora en ese escenario.

// Redis — conexión compartida para cache + OTel instrumentation
var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "redis-master:6379";
var redisConnection = ConnectionMultiplexer.Connect(redisConnectionString);
builder.Services.AddSingleton<IConnectionMultiplexer>(redisConnection);

var observabilityEnabled = builder.Configuration.GetValue("OBSERVABILITY_ENABLED", true);

if (observabilityEnabled)
{
    var serviceName    = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME")    ?? "gorrashop-backend";
    var serviceVersion = Environment.GetEnvironmentVariable("OTEL_SERVICE_VERSION") ?? "1.0.0";
    var otlpEndpoint   = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") ?? "http://localhost:4317";
    var environment    = Environment.GetEnvironmentVariable("DD_ENV")
                      ?? Environment.GetEnvironmentVariable("DEPLOYMENT_ENV")
                      ?? "lab";

    builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource => resource
            .AddService(serviceName, serviceVersion: serviceVersion)
            .AddAttributes(new Dictionary<string, object>
            {
                ["deployment.environment"] = environment,
                ["host.name"]              = Environment.MachineName,
            }))
        .WithTracing(tracing => tracing
            .AddAspNetCoreInstrumentation(opts =>
            {
                opts.RecordException = true;
                // Excluir health checks del tracing
                opts.Filter = ctx => !ctx.Request.Path.StartsWithSegments("/health");
            })
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation(opts =>
            {
                opts.SetDbStatementForText = true;
            })
            .AddRedisInstrumentation(redisConnection)
            .AddOtlpExporter(otlp =>
            {
                otlp.Endpoint = new Uri(otlpEndpoint);
            }))
        .WithMetrics(metrics => metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddOtlpExporter(otlp =>
            {
                otlp.Endpoint = new Uri(otlpEndpoint);
            }));

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
