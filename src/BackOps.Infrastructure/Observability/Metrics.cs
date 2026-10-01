namespace BackOps.Infrastructure.Observability;

using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using OpenTelemetry.Exporter;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

public static class ObservabilityExtensions
{
    public static IServiceCollection AddBackOpsObservability(this IServiceCollection services, IConfiguration configuration)
    {
        var serviceName = configuration["Observability:ServiceName"] ?? "backops-simulator";
        var otlpEndpoint = configuration["Observability:OtlpEndpoint"] ?? "http://localhost:4317";

        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(serviceName)
            .AddAttributes(new[]
            {
                new KeyValuePair<string, object>("deployment.environment", configuration["ASPNETCORE_ENVIRONMENT"] ?? "Development"),
                new KeyValuePair<string, object>("service.version", "1.0.0")
            });

        services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(serviceName))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddRedisInstrumentation()
                    .AddSource("BackOps.Api", "BackOps.Worker", "BackOps.Application")
                    .SetResourceBuilder(resourceBuilder);

                if (!string.IsNullOrEmpty(otlpEndpoint))
                {
                    tracing.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(otlpEndpoint);
                        options.Protocol = OtlpExportProtocol.Grpc;
                    });
                }

                tracing.AddConsoleExporter();
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddProcessInstrumentation()
                    .AddMeter("BackOps.Api", "BackOps.Worker", "BackOps.Application")
                    .SetResourceBuilder(resourceBuilder);

                if (!string.IsNullOrEmpty(otlpEndpoint))
                {
                    metrics.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(otlpEndpoint);
                        options.Protocol = OtlpExportProtocol.Grpc;
                    });
                }

                metrics.AddConsoleExporter();
            });

        return services;
    }
}

public static class Metrics
{
    public static readonly Meter Meter = new("BackOps.Api");
    public static readonly Meter WorkerMeter = new("BackOps.Worker");

    // API Metrics
    public static readonly Counter<long> HttpRequestsTotal = Meter.CreateCounter<long>("http_requests_total", "Total HTTP requests");
    public static readonly Histogram<double> HttpRequestDuration = Meter.CreateHistogram<double>("http_request_duration_seconds", "HTTP request duration in seconds");
    public static readonly Counter<long> HttpErrorsTotal = Meter.CreateCounter<long>("http_errors_total", "Total HTTP errors");

    // Queue Metrics
    public static readonly Counter<long> QueueMessagesTotal = Meter.CreateCounter<long>("queue_messages_total", "Total queue messages");
    public static readonly UpDownCounter<long> QueuePendingMessages = Meter.CreateUpDownCounter<long>("queue_pending_messages", "Pending queue messages");
    public static readonly Histogram<double> QueueProcessingTime = Meter.CreateHistogram<double>("queue_processing_time_seconds", "Queue processing time");
    public static readonly Counter<long> QueueFailedMessages = Meter.CreateCounter<long>("queue_failed_messages", "Failed queue messages");

    // Worker Metrics
    public static readonly Counter<long> WorkerJobsTotal = WorkerMeter.CreateCounter<long>("worker_jobs_total", "Total worker jobs");
    public static readonly Counter<long> WorkerJobsFailed = WorkerMeter.CreateCounter<long>("worker_jobs_failed", "Failed worker jobs");
    public static readonly Histogram<double> WorkerProcessingDuration = WorkerMeter.CreateHistogram<double>("worker_processing_duration_seconds", "Worker processing duration");
    public static readonly UpDownCounter<long> WorkerActive = WorkerMeter.CreateUpDownCounter<long>("worker_active", "Active workers");

    // Payment Metrics
    public static readonly Counter<long> PaymentsTotal = Meter.CreateCounter<long>("payments_total", "Total payments");
    public static readonly Counter<long> PaymentsSuccess = Meter.CreateCounter<long>("payments_success", "Successful payments");
    public static readonly Counter<long> PaymentsFailed = Meter.CreateCounter<long>("payments_failed", "Failed payments");
    public static readonly Counter<long> PaymentsRetried = Meter.CreateCounter<long>("payments_retried", "Retried payments");
    public static readonly Counter<long> PaymentsIdempotentHits = Meter.CreateCounter<long>("payments_idempotent_hits", "Idempotent key hits");

    // Resilience Metrics
    public static readonly UpDownCounter<long> CircuitBreakerOpen = Meter.CreateUpDownCounter<long>("circuit_breaker_open", "Circuit breaker open count");
    public static readonly Counter<long> RetryTotal = Meter.CreateCounter<long>("retry_total", "Total retries");
    public static readonly Counter<long> TimeoutTotal = Meter.CreateCounter<long>("timeout_total", "Total timeouts");
    public static readonly Counter<long> RateLimitRejected = Meter.CreateCounter<long>("rate_limit_rejected", "Rate limit rejected requests");
}