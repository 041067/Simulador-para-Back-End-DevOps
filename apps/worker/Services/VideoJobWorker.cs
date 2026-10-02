namespace BackOps.Worker.Services;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using BackOps.Domain.Interfaces;
using BackOps.Domain.Entities;
using BackOps.Domain.Enums;
using BackOps.Application.Services;
using BackOps.Infrastructure.Observability;

public class VideoJobWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<VideoJobWorker> _logger;
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(5);

    public VideoJobWorker(IServiceProvider serviceProvider, ILogger<VideoJobWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("VideoJobWorker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessQueuedJobsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing video jobs");
            }

            await Task.Delay(_pollInterval, stoppingToken);
        }

        _logger.LogInformation("VideoJobWorker stopped");
    }

    private async Task ProcessQueuedJobsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var videoJobRepository = scope.ServiceProvider.GetRequiredService<IVideoJobRepository>();
        var workerRepository = scope.ServiceProvider.GetRequiredService<IWorkerRepository>();
        var highLoadService = scope.ServiceProvider.GetRequiredService<IHighLoadService>();

        var queuedJobs = await videoJobRepository.GetQueuedJobsAsync(10, cancellationToken);
        if (!queuedJobs.Any()) return;

        var availableWorkers = await workerRepository.GetAvailableWorkersAsync("video", cancellationToken);
        if (!availableWorkers.Any()) return;

        foreach (var job in queuedJobs)
        {
            var worker = availableWorkers.OrderBy(w => w.CurrentJobs).FirstOrDefault();
            if (worker == null) break;

            await highLoadService.AssignVideoJobAsync(job.Id, worker.Name, cancellationToken);
            await highLoadService.WorkerStartJobAsync(worker.Id, job.Id.ToString(), cancellationToken);

            Metrics.WorkerJobsTotal.Add(1, new KeyValuePair<string, object?>("worker", worker.Name));

            _ = Task.Run(async () =>
            {
                try
                {
                    await SimulateVideoProcessingAsync(job, worker, scope.ServiceProvider, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing video job {JobId}", job.Id);
                }
            }, cancellationToken);
        }
    }

    private async Task SimulateVideoProcessingAsync(VideoJob job, Worker worker, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var processingTime = TimeSpan.FromSeconds(Random.Shared.Next(1, 10));
        await Task.Delay(processingTime, cancellationToken);

        using var scope = serviceProvider.CreateScope();
        var highLoadService = scope.ServiceProvider.GetRequiredService<IHighLoadService>();

        var success = Random.Shared.NextDouble() > 0.05;

        if (success)
        {
            await highLoadService.CompleteVideoJobAsync(job.Id, cancellationToken);
            await highLoadService.WorkerCompleteJobAsync(worker.Id, true, cancellationToken);
            Metrics.WorkerProcessingDuration.Record(processingTime.TotalSeconds, new KeyValuePair<string, object?>("worker", worker.Name));
        }
        else
        {
            await highLoadService.FailVideoJobAsync(job.Id, "Simulated processing failure", cancellationToken);
            await highLoadService.WorkerCompleteJobAsync(worker.Id, false, cancellationToken);
            Metrics.WorkerJobsFailed.Add(1, new KeyValuePair<string, object?>("worker", worker.Name));
        }
    }
}
