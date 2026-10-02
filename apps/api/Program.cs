using BackOps.Api.Middleware;
using BackOps.Application;
using BackOps.Infrastructure;
using BackOps.Infrastructure.HealthChecks;
using BackOps.Infrastructure.Observability;
using BackOps.Infrastructure.Persistence;
using BackOps.Infrastructure.Resilience;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "BackOps.Api")
        .WriteTo.Console()
        .WriteTo.Seq(context.Configuration["Seq:Url"] ?? "http://localhost:5341");
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "BackOps Simulator API",
        Version = "v1",
        Description = "A hands-on laboratory for distributed systems, backend engineering, DevOps and SRE"
    });
    c.AddSecurityDefinition("CorrelationId", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Name = "X-Correlation-ID",
        Description = "Correlation ID for request tracing"
    });
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

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database")
    .AddCheck<RedisHealthCheck>("redis")
    .AddCheck<QueueHealthCheck>("queue")
    .AddCheck<CircuitBreakerHealthCheck>("circuit-breaker");

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? new[] { "http://localhost:5173" };
        
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Urls.Clear();
app.Urls.Add($"http://0.0.0.0:{port}");

app.UseSerilogRequestLogging();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "BackOps Simulator API v1");
    c.RoutePrefix = "swagger";
});

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

app.UseHttpsRedirection();
app.UseCors();

app.MapControllers();
app.MapHealthChecks("/health");
app.MapHealthChecks("/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<BackOpsDbContext>();
    await context.Database.MigrateAsync();
}

app.Run();