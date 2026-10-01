namespace BackOps.Worker.Services;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using BackOps.Domain.Interfaces;
using BackOps.Domain.Entities;
using BackOps.Domain.Enums;
using BackOps.Application.Services;

public class PurchaseProcessorWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PurchaseProcessorWorker> _logger;
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(2);

    public PurchaseProcessorWorker(IServiceProvider serviceProvider, ILogger<PurchaseProcessorWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PurchaseProcessorWorker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingPurchasesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing purchases");
            }

            await Task.Delay(_pollInterval, stoppingToken);
        }

        _logger.LogInformation("PurchaseProcessorWorker stopped");
    }

    private async Task ProcessPendingPurchasesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var purchaseRepository = scope.ServiceProvider.GetRequiredService<IPurchaseRepository>();
        var highLoadService = scope.ServiceProvider.GetRequiredService<IHighLoadService>();

        var pendingPurchases = await purchaseRepository.GetPendingPurchasesAsync(cancellationToken);
        if (!pendingPurchases.Any()) return;

        foreach (var purchase in pendingPurchases.Take(50))
        {
            await highLoadService.ProcessPurchaseAsync(purchase.Id, cancellationToken);
        }
    }
}