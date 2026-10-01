namespace BackOps.Infrastructure.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using BackOps.Domain.Interfaces;
using BackOps.Domain.Entities;
using BackOps.Domain.Enums;

public class WorkerRepository : IWorkerRepository
{
    private readonly BackOpsDbContext _context;

    public WorkerRepository(BackOpsDbContext context)
    {
        _context = context;
    }

    public async Task<Worker?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Workers
            .FirstOrDefaultAsync(w => w.Id == id && !w.IsDeleted, cancellationToken);
    }

    public async Task<IReadOnlyList<Worker>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Workers
            .Where(w => !w.IsDeleted)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Worker>> GetAvailableWorkersAsync(string type, CancellationToken cancellationToken = default)
    {
        return await _context.Workers
            .Where(w => w.Type == type && w.Status == WorkerStatus.Idle && !w.IsDeleted)
            .OrderBy(w => w.CurrentJobs)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Worker>> GetByTypeAsync(string type, CancellationToken cancellationToken = default)
    {
        return await _context.Workers
            .Where(w => w.Type == type && !w.IsDeleted)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Worker?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.Workers
            .FirstOrDefaultAsync(w => w.Name == name && !w.IsDeleted, cancellationToken);
    }

    public async Task<Worker> AddAsync(Worker entity, CancellationToken cancellationToken = default)
    {
        await _context.Workers.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task UpdateAsync(Worker entity, CancellationToken cancellationToken = default)
    {
        _context.Workers.Update(entity);
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
        return await _context.Workers.AnyAsync(w => w.Id == id && !w.IsDeleted, cancellationToken);
    }
}