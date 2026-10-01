namespace BackOps.Worker.Services;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using BackOps.Domain.Interfaces;
using BackOps.Domain.Entities;
using BackOps.Application.Services;

public class WebhookDeliveryWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WebhookDeliveryWorker> _logger;
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(10);

    public WebhookDeliveryWorker(IServiceProvider serviceProvider, ILogger<WebhookDeliveryWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("WebhookDeliveryWorker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DeliverPendingWebhooksAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error delivering webhooks");
            }

            await Task.Delay(_pollInterval, stoppingToken);
        }

        _logger.LogInformation("WebhookDeliveryWorker stopped");
    }

    private async Task DeliverPendingWebhooksAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var paymentRepository = scope.ServiceProvider.GetRequiredService<IPaymentRepository>();
        var paymentService = scope.ServiceProvider.GetRequiredService<IPaymentService>();

        var payments = await paymentRepository.GetAllAsync(cancellationToken);
        var pendingWebhooks = payments.SelectMany(p => p.Webhooks)
            .Where(w => !w.IsDelivered && w.CanRetry() && (w.NextRetryAt == null || w.NextRetryAt <= DateTime.UtcNow))
            .ToList();

        foreach (var webhook in pendingWebhooks.Take(20))
        {
            await paymentService.SendWebhookAsync(webhook.Id, cancellationToken);
        }
    }
}