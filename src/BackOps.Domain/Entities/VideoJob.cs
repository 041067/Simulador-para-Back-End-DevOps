namespace BackOps.Domain.Entities;

using BackOps.Domain.ValueObjects;
using BackOps.Domain.Enums;
using BackOps.Domain.Events;

public class VideoJob : BaseEntity
{
    public string VideoId { get; private set; } = string.Empty;
    public long VideoSizeBytes { get; private set; }
    public int DurationSeconds { get; private set; }
    public string Operation { get; private set; } = "TRANSCODE";
    public JobPriority Priority { get; private set; }
    public JobStatus Status { get; private set; }
    public string? WorkerId { get; private set; }
    public string? ErrorMessage { get; private set; }
    public int RetryCount { get; private set; }
    public int MaxRetries { get; private set; } = 3;
    public DateTime? QueuedAt { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public TimeSpan? ProcessingTime { get; private set; }

    private VideoJob() { }

    public VideoJob(string videoId, long videoSizeBytes, int durationSeconds, string operation, JobPriority priority)
    {
        VideoId = videoId;
        VideoSizeBytes = videoSizeBytes;
        DurationSeconds = durationSeconds;
        Operation = operation;
        Priority = priority;
        Status = JobStatus.Queued;
        QueuedAt = DateTime.UtcNow;
    }

    public void AssignToWorker(string workerId)
    {
        if (Status != JobStatus.Queued)
            throw new InvalidOperationException($"Cannot assign job with status {Status}");

        WorkerId = workerId;
        Status = JobStatus.Processing;
        StartedAt = DateTime.UtcNow;
        UpdateTimestamp();

        AddDomainEvent(new VideoJobProcessingEvent(Id, workerId));
    }

    public void Complete()
    {
        if (Status != JobStatus.Processing)
            throw new InvalidOperationException($"Cannot complete job with status {Status}");

        Status = JobStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        ProcessingTime = CompletedAt.Value - StartedAt.Value;
        UpdateTimestamp();

        AddDomainEvent(new VideoJobCompletedEvent(Id, ProcessingTime.Value));
    }

    public void Fail(string error)
    {
        if (Status == JobStatus.Completed)
            throw new InvalidOperationException("Cannot fail a completed job");

        ErrorMessage = error;
        RetryCount++;

        if (RetryCount >= MaxRetries)
        {
            Status = JobStatus.Failed;
            CompletedAt = DateTime.UtcNow;
        }
        else
        {
            Status = JobStatus.Queued;
            WorkerId = null;
            StartedAt = null;
        }

        UpdateTimestamp();

        AddDomainEvent(new VideoJobFailedEvent(Id, error));
    }

    public void Cancel()
    {
        if (Status == JobStatus.Completed || Status == JobStatus.Failed)
            throw new InvalidOperationException($"Cannot cancel job with status {Status}");

        Status = JobStatus.Cancelled;
        CompletedAt = DateTime.UtcNow;
        UpdateTimestamp();
    }

    public bool CanRetry() => RetryCount < MaxRetries && Status == JobStatus.Failed;
}