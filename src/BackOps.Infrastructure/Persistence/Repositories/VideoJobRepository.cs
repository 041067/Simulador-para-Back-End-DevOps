namespace BackOps.Infrastructure.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using BackOps.Domain.Interfaces;
using BackOps.Domain.Entities;
using BackOps.Domain.Enums;

public class VideoJobRepository : IVideoJobRepository
{
    private readonly BackOpsDbContext _context;

    public VideoJobRepository(BackOpsDbContext context)
    {
        _context = context;
    }

    public async Task<VideoJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.VideoJobs
            .FirstOrDefaultAsync(j => j.Id == id && !j.IsDeleted, cancellationToken);
    }

    public async Task<IReadOnlyList<VideoJob>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.VideoJobs
            .Where(j => !j.IsDeleted)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<VideoJob>> GetByStatusAsync(JobStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.VideoJobs
            .Where(j => j.Status == status && !j.IsDeleted)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<VideoJob>> GetQueuedJobsAsync(int count, CancellationToken cancellationToken = default)
    {
        return await _context.VideoJobs
            .Where(j => j.Status == JobStatus.Queued && !j.IsDeleted)
            .OrderBy(j => j.Priority)
            .ThenBy(j => j.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<VideoJob>> GetByWorkerIdAsync(string workerId, CancellationToken cancellationToken = default)
    {
        return await _context.VideoJobs
            .Where(j => j.WorkerId == workerId && !j.IsDeleted)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetQueueDepthAsync(CancellationToken cancellationToken = default)
    {
        return await _context.VideoJobs
            .CountAsync(j => j.Status == JobStatus.Queued && !j.IsDeleted, cancellationToken);
    }

    public async Task<VideoJob> AddAsync(VideoJob entity, CancellationToken cancellationToken = default)
    {
        await _context.VideoJobs.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task UpdateAsync(VideoJob entity, CancellationToken cancellationToken = default)
    {
        _context.VideoJobs.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity != null)
        {
            entity.MarkAsDeleted();
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.VideoJobs.AnyAsync(j => j.Id == id && !j.IsDeleted, cancellationToken);
    }
}