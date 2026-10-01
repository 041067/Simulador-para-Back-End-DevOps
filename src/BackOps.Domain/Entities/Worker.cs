namespace BackOps.Domain.Entities;

using BackOps.Domain.Enums;
using BackOps.Domain.Events;

public class Worker : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Type { get; private set; } = string.Empty;
    public WorkerStatus Status { get; private set; }
    public int MaxConcurrency { get; private set; }
    public int CurrentJobs { get; private set; }
    public int TotalJobsProcessed { get; private set; }
    public int TotalJobsFailed { get; private set; }
    public DateTime? LastHeartbeat { get; private set; }
    public string? CurrentJobId { get; private set; }
    public string? ErrorMessage { get; private set; }

    private Worker() { }

    public Worker(string name, string type, int maxConcurrency = 4)
    {
        Name = name;
        Type = type;
        MaxConcurrency = maxConcurrency;
        Status = WorkerStatus.Idle;
        CurrentJobs = 0;
        TotalJobsProcessed = 0;
        TotalJobsFailed = 0;
    }

    public void StartJob(string jobId)
    {
        if (CurrentJobs >= MaxConcurrency)
            throw new InvalidOperationException("Worker at maximum concurrency");

        CurrentJobs++;
        CurrentJobId = jobId;
        LastHeartbeat = DateTime.UtcNow;

        if (Status == WorkerStatus.Idle)
        {
            var oldStatus = Status;
            Status = WorkerStatus.Processing;
            AddDomainEvent(new WorkerStatusChangedEvent(Id.ToString(), oldStatus, Status));
        }

        UpdateTimestamp();
    }

    public void CompleteJob(bool success)
    {
        CurrentJobs = Math.Max(0, CurrentJobs - 1);
        CurrentJobId = null;
        LastHeartbeat = DateTime.UtcNow;

        if (success)
            TotalJobsProcessed++;
        else
            TotalJobsFailed++;

        if (CurrentJobs == 0 && Status == WorkerStatus.Processing)
        {
            var oldStatus = Status;
            Status = WorkerStatus.Idle;
            AddDomainEvent(new WorkerStatusChangedEvent(Id.ToString(), oldStatus, Status));
        }

        UpdateTimestamp();
    }

    public void Heartbeat()
    {
        LastHeartbeat = DateTime.UtcNow;
    }

    public void Fail(string error)
    {
        ErrorMessage = error;
        Status = WorkerStatus.Failed;
        AddDomainEvent(new WorkerStatusChangedEvent(Id.ToString(), Status, WorkerStatus.Failed));
        UpdateTimestamp();
    }

    public void Stop()
    {
        Status = WorkerStatus.Stopped;
        AddDomainEvent(new WorkerStatusChangedEvent(Id.ToString(), Status, WorkerStatus.Stopped));
        UpdateTimestamp();
    }

    public bool IsHealthy() => LastHeartbeat.HasValue && (DateTime.UtcNow - LastHeartbeat.Value).TotalSeconds < 60;
    public bool IsAvailable() => Status == WorkerStatus.Idle || (Status == WorkerStatus.Processing && CurrentJobs < MaxConcurrency);
    public double Utilization => MaxConcurrency > 0 ? (double)CurrentJobs / MaxConcurrency : 0;
}