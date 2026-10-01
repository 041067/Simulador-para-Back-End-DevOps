namespace BackOps.Infrastructure.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using BackOps.Domain.Interfaces;
using BackOps.Domain.Entities;
using BackOps.Domain.ValueObjects;
using BackOps.Domain.Enums;

public class PurchaseRepository : IPurchaseRepository
{
    private readonly BackOpsDbContext _context;

    public PurchaseRepository(BackOpsDbContext context)
    {
        _context = context;
    }

    public async Task<Purchase?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Purchases
            .Include(p => p.Tickets)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);
    }

    public async Task<IReadOnlyList<Purchase>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Purchases
            .Where(p => !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Purchase>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        return await _context.Purchases
            .Where(p => p.EventId == eventId && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Purchase>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Purchases
            .Where(p => p.UserId == userId && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Purchase?> GetByIdempotencyKeyAsync(IdempotencyKey key, CancellationToken cancellationToken = default)
    {
        return await _context.Purchases
            .FirstOrDefaultAsync(p => p.IdempotencyKey.Value == key.Value && !p.IsDeleted, cancellationToken);
    }

    public async Task<IReadOnlyList<Purchase>> GetPendingPurchasesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Purchases
            .Where(p => p.Status == PurchaseStatus.Pending && !p.IsDeleted)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Purchase> AddAsync(Purchase entity, CancellationToken cancellationToken = default)
    {
        await _context.Purchases.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task UpdateAsync(Purchase entity, CancellationToken cancellationToken = default)
    {
        _context.Purchases.Update(entity);
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
        return await _context.Purchases.AnyAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);
    }
}