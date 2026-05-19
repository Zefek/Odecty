using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using OdectyStat1.Application;
using OdectyStat1.Contracts;
using OdectyStat1.DataLayer;
using OdectyStat1.DataLayer.Consumers;
using OdectyStat1.Dto;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

const string ServiceName = "OdectyStat";

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseWindowsService();

var otlpEndpoint = new Uri(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://localhost:4317");

builder.Logging.ClearProviders();
builder.Logging.AddOpenTelemetry(opt =>
{
    opt.IncludeFormattedMessage = true;
    opt.IncludeScopes = true;
    opt.ParseStateValues = true;
    opt.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(ServiceName));
    opt.AddOtlpExporter(o => o.Endpoint = otlpEndpoint);
});

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService(ServiceName))
    .WithTracing(t => t
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddOtlpExporter(o => o.Endpoint = otlpEndpoint))
    .WithMetrics(m => m
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddMeter("Npgsql")
        .AddMeter("Microsoft.Data.SqlClient.EventSource")
        .AddOtlpExporter(o => o.Endpoint = otlpEndpoint));

builder.Services.Configure<OdectySettings>(builder.Configuration.GetSection("OdectySettings"));
builder.Services.Configure<GaugeImageLocation>(builder.Configuration.GetSection("GaugeImageLocation"));
builder.Services.AddSingleton<RabbitMQProvider>();
builder.Services.AddScoped<IGaugeContext, GaugeContext>();
builder.Services.AddScoped<IGaugeRepository, GaugeRepository>();
builder.Services.AddScoped<IMeasurementDayRepository, MeasurementDayRepository>();
builder.Services.AddScoped<IGaugeService, GaugeService>();
builder.Services.AddScoped<IMessageQueue, MessageQueue>();
builder.Services.AddScoped<IMeasurementRepository, MeasurementRepository>();
builder.Services.AddScoped<IMeasurementStatisticsRepository, MeasurementStatisticsRepository>();
builder.Services.AddScoped<IHomeAssistantStatisticsRepository, HomeAssistantStatisticsRepository>();
builder.Services.AddScoped<IGaugeQueryService, GaugeQueryService>();

builder.Services.AddDbContext<GaugeDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Odecty")));
builder.Services.AddDbContext<HomeAssistantDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("HomeAssistant")));
builder.Services.AddDbContext<DiagDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Diagnostics")));

builder.Services.AddHostedService<ConsumerBackgroundService>();
builder.Services.AddSingleton<IRabbitMQConsumer, MQClient>();
builder.Services.AddSingleton<IRabbitMQConsumer, RecognizedSuccess>();
builder.Services.AddSingleton<IRabbitMQConsumer, RecognizedFailed>();
builder.Services.AddHostedService<BinaryConsumerBackgroundService>();
builder.Services.AddSingleton<IBinaryMessageHandler, HeaterDiagHandler>();
builder.Services.AddSingleton<IBinaryMessageHandler, LSSensorDiagHandler>();
builder.Services.AddSingleton<IBinaryMessageHandler, GarageDiagHandler>();

builder.Services.AddHealthChecks()
    .AddSqlServer(
        connectionString: builder.Configuration.GetConnectionString("Odecty")!,
        name: "sqlserver-odecty",
        tags: new[] { "ready" })
    .AddNpgSql(
        connectionString: builder.Configuration.GetConnectionString("HomeAssistant")!,
        name: "postgres-homeassistant",
        tags: new[] { "ready" })
    .AddNpgSql(
        connectionString: builder.Configuration.GetConnectionString("Diagnostics")!,
        name: "postgres-diagnostics",
        tags: new[] { "ready" })
    .AddRabbitMQ(
        (sp, opts) =>
        {
            var s = sp.GetRequiredService<IOptions<OdectySettings>>().Value;
            opts.ConnectionUri = new Uri($"amqp://{Uri.EscapeDataString(s.RabbitMQUsername)}:{Uri.EscapeDataString(s.RabbitMQPassword)}@{s.RabbitMQHost}/{Uri.EscapeDataString(s.RabbitMQVHost)}");
        },
        name: "rabbitmq",
        tags: new[] { "ready" });

builder.Services.AddProblemDetails();
builder.Services.AddControllers();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapControllers();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = WriteHealthResponse
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("ready"),
    ResponseWriter = WriteHealthResponse
});

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DiagDbContext>();
    await db.Database.MigrateAsync();
}

await app.RunAsync();

static Task WriteHealthResponse(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";
    var payload = new
    {
        status = report.Status.ToString(),
        totalDuration = report.TotalDuration.ToString(),
        entries = report.Entries.ToDictionary(
            e => e.Key,
            e => new
            {
                data = e.Value.Data,
                description = e.Value.Description,
                duration = e.Value.Duration.ToString(),
                status = e.Value.Status.ToString(),
                tags = e.Value.Tags
            })
    };
    return context.Response.WriteAsync(JsonSerializer.Serialize(payload));
}
