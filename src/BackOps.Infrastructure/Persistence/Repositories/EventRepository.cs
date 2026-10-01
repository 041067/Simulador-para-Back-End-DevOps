namespace BackOps.Infrastructure.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using BackOps.Domain.Interfaces;
using BackOps.Domain.Entities;

public class EventRepository : IEventRepository
{
    private readonly BackOpsDbContext _context;

    public EventRepository(BackOpsDbContext context)
    {
        _context = context;
    }

    public async Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Events
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted, cancellationToken);
    }

    public async Task<Event?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Events
            .Include(e => e.Tickets)
            .Include(e => e.Purchases)
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted, cancellationToken);
    }

    public async Task<IReadOnlyList<Event>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Events
            .Where(e => !e.IsDeleted)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Event>> GetActiveEventsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Events
            .Where(e => e.IsActive && !e.IsDeleted)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Event?> GetEventWithTicketsAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        return await _context.Events
            .Include(e => e.Tickets)
            .FirstOrDefaultAsync(e => e.Id == eventId && !e.IsDeleted, cancellationToken);
    }

    public async Task<Event> AddAsync(Event entity, CancellationToken cancellationToken = default)
    {
        await _context.Events.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task UpdateAsync(Event entity, CancellationToken cancellationToken = default)
    {
        _context.Events.Update(entity);
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
        return await _context.Events.AnyAsync(e => e.Id == id && !e.IsDeleted, cancellationToken);
    }
}