namespace BackOps.Infrastructure.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using BackOps.Domain.Interfaces;
using BackOps.Domain.Entities;
using BackOps.Domain.ValueObjects;

public class TicketRepository : ITicketRepository
{
    private readonly BackOpsDbContext _context;

    public TicketRepository(BackOpsDbContext context)
    {
        _context = context;
    }

    public async Task<Ticket?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Tickets
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted, cancellationToken);
    }

    public async Task<IReadOnlyList<Ticket>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Tickets
            .Where(t => !t.IsDeleted)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Ticket>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        return await _context.Tickets
            .Where(t => t.EventId == eventId && !t.IsDeleted)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Ticket>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Tickets
            .Where(t => t.UserId == userId && !t.IsDeleted)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Ticket?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _context.Tickets
            .FirstOrDefaultAsync(t => t.Code == code && !t.IsDeleted, cancellationToken);
    }

    public async Task<Ticket?> GetByIdempotencyKeyAsync(IdempotencyKey key, CancellationToken cancellationToken = default)
    {
        return await _context.Tickets
            .FirstOrDefaultAsync(t => t.IdempotencyKey.Value == key.Value && !t.IsDeleted, cancellationToken);
    }

    public async Task<int> GetAvailableCountAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        return await _context.Tickets
            .CountAsync(t => t.EventId == eventId && t.Status == TicketStatus.Available && !t.IsDeleted, cancellationToken);
    }

    public async Task<Ticket> AddAsync(Ticket entity, CancellationToken cancellationToken = default)
    {
        await _context.Tickets.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task UpdateAsync(Ticket entity, CancellationToken cancellationToken = default)
    {
        _context.Tickets.Update(entity);
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
        return await _context.Tickets.AnyAsync(t => t.Id == id && !t.IsDeleted, cancellationToken);
    }
}