using BackOps.Api.Middleware;
using BackOps.Application;
using BackOps.Infrastructure;
using BackOps.Infrastructure.HealthChecks;
using BackOps.Infrastructure.Observability;
using BackOps.Infrastructure.Persistence;
using BackOps.Infrastructure.Resilience;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "BackOps.Worker")
        .WriteTo.Console()
        .WriteTo.Seq(context.Configuration["Seq:Url"] ?? "http://localhost:5341");
});

builder.Services.AddDbContext<BackOpsDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Host=localhost;Database=backops;Username=postgres;Password=postgres";
    options.UseNpgsql(connectionString, npgsql =>
    {
        npgsql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
        npgsql.CommandTimeout(30);
    });
});

builder.Services.AddStackExchangeRedisCache(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("Redis")
        ?? "localhost:6379";
    options.Configuration = connectionString;
    options.InstanceName = "backops:";
});

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var connectionString = builder.Configuration.GetConnectionString("Redis")
        ?? "localhost:6379";
    return ConnectionMultiplexer.Connect(connectionString);
});

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(BackOps.Application.Commands.CreateEventCommand).Assembly);
});

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssembly(typeof(BackOps.Application.Validators.CommandValidators).Assembly);

builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<ITicketRepository, TicketRepository>();
builder.Services.AddScoped<IPurchaseRepository, PurchaseRepository>();
builder.Services.AddScoped<IVideoJobRepository, VideoJobRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IWorkerRepository, WorkerRepository>();

builder.Services.AddScoped<IMessageBus, RedisMessageBus>();
builder.Services.AddScoped<IQueueProvider, RedisMessageBus>();

builder.Services.AddSingleton<ICircuitBreaker>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<CircuitBreaker>>();
    var options = Options.Create(new CircuitBreakerOptions
    {
        FailureThreshold = builder.Configuration.GetValue("Resilience:CircuitBreaker:FailureThreshold", 5),
        SuccessThreshold = builder.Configuration.GetValue("Resilience:CircuitBreaker:SuccessThreshold", 2),
        Timeout = TimeSpan.FromSeconds(builder.Configuration.GetValue("Resilience:CircuitBreaker:TimeoutSeconds", 30))
    });
    return new CircuitBreaker("bank", options, logger);
});

builder.Services.AddSingleton<IRetryPolicy>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<RetryPolicy>>();
    var options = Options.Create(new RetryPolicyOptions
    {
        MaxRetries = builder.Configuration.GetValue("Resilience:Retry:MaxRetries", 3),
        BaseDelayMs = builder.Configuration.GetValue("Resilience:Retry:BaseDelayMs", 100),
        Multiplier = builder.Configuration.GetValue("Resilience:Retry:Multiplier", 2.0),
        JitterMs = builder.Configuration.GetValue("Resilience:Retry:JitterMs", 100)
    });
    return new RetryPolicy(options, logger);
});

builder.Services.AddSingleton<IRateLimiter>(sp =>
{
    var connection = sp.GetRequiredService<IConnectionMultiplexer>();
    var logger = sp.GetRequiredService<ILogger<RedisRateLimiter>>();
    return new RedisRateLimiter(connection, logger);
});

builder.Services.AddSingleton<IIdempotencyService>(sp =>
{
    var connection = sp.GetRequiredService<IConnectionMultiplexer>();
    var logger = sp.GetRequiredService<ILogger<RedisIdempotencyService>>();
    return new RedisIdempotencyService(connection, logger);
});

builder.Services.AddSingleton<IFailureInjector, InMemoryFailureInjector>();

builder.Services.AddScoped<IHighLoadService, HighLoadService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

builder.Services.AddBackOpsObservability(builder.Configuration);

builder.Services.AddHostedService<VideoJobWorker>();
builder.Services.AddHostedService<PurchaseProcessorWorker>();
builder.Services.AddHostedService<WebhookDeliveryWorker>();

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<BackOpsDbContext>();
    await context.Database.MigrateAsync();
}

host.Run();