namespace BackOps.Infrastructure.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using BackOps.Domain.Interfaces;
using BackOps.Domain.Entities;
using BackOps.Domain.ValueObjects;
using BackOps.Domain.Enums;

public class PaymentRepository : IPaymentRepository
{
    private readonly BackOpsDbContext _context;

    public PaymentRepository(BackOpsDbContext context)
    {
        _context = context;
    }

    public async Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Payments
            .Include(p => p.Attempts)
            .Include(p => p.Webhooks)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);
    }

    public async Task<IReadOnlyList<Payment>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Payments
            .Include(p => p.Attempts)
            .Include(p => p.Webhooks)
            .Where(p => !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Payment?> GetByIdempotencyKeyAsync(IdempotencyKey key, CancellationToken cancellationToken = default)
    {
        return await _context.Payments
            .Include(p => p.Attempts)
            .Include(p => p.Webhooks)
            .FirstOrDefaultAsync(p => p.IdempotencyKey.Value == key.Value && !p.IsDeleted, cancellationToken);
    }

    public async Task<Payment?> GetByExternalReferenceAsync(string reference, CancellationToken cancellationToken = default)
    {
        return await _context.Payments
            .Include(p => p.Attempts)
            .Include(p => p.Webhooks)
            .FirstOrDefaultAsync(p => p.ExternalReference == reference && !p.IsDeleted, cancellationToken);
    }

    public async Task<IReadOnlyList<Payment>> GetByStatusAsync(PaymentStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.Payments
            .Include(p => p.Attempts)
            .Include(p => p.Webhooks)
            .Where(p => p.Status == status && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Payment>> GetPendingPaymentsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Payments
            .Include(p => p.Attempts)
            .Include(p => p.Webhooks)
            .Where(p => p.Status == PaymentStatus.Processing && !p.IsDeleted)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Payment> AddAsync(Payment entity, CancellationToken cancellationToken = default)
    {
        await _context.Payments.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task UpdateAsync(Payment entity, CancellationToken cancellationToken = default)
    {
        _context.Payments.Update(entity);
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
        return await _context.Payments.AnyAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);
    }
}