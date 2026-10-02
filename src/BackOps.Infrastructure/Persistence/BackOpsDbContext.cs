namespace BackOps.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using BackOps.Domain.Entities;
using BackOps.Domain.ValueObjects;
using BackOps.Domain.Enums;

public class BackOpsDbContext : DbContext
{
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<VideoJob> VideoJobs => Set<VideoJob>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentAttempt> PaymentAttempts => Set<PaymentAttempt>();
    public DbSet<Webhook> Webhooks => Set<Webhook>();
    public DbSet<Worker> Workers => Set<Worker>();

    public BackOpsDbContext(DbContextOptions<BackOpsDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BackOpsDbContext).Assembly);

        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.ComplexProperty(e => e.TicketPrice, p =>
            {
                p.Property(m => m.Amount).HasColumnName("TicketPriceAmount").HasPrecision(18, 2);
                p.Property(m => m.Currency).HasColumnName("TicketPriceCurrency").HasMaxLength(3);
            });
        });

        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(20);
            entity.ComplexProperty(e => e.Price, p =>
            {
                p.Property(m => m.Amount).HasColumnName("PriceAmount").HasPrecision(18, 2);
                p.Property(m => m.Currency).HasColumnName("PriceCurrency").HasMaxLength(3);
            });
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => e.IdempotencyKey).IsUnique();
            entity.HasIndex(e => e.EventId);
            entity.HasIndex(e => e.UserId);
        });

        modelBuilder.Entity<Purchase>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ComplexProperty(e => e.TotalAmount, p =>
            {
                p.Property(m => m.Amount).HasColumnName("TotalAmountAmount").HasPrecision(18, 2);
                p.Property(m => m.Currency).HasColumnName("TotalAmountCurrency").HasMaxLength(3);
            });
            entity.HasIndex(e => e.IdempotencyKey).IsUnique();
            entity.HasIndex(e => e.EventId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Status);
        });

        modelBuilder.Entity<VideoJob>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.VideoId).IsRequired().HasMaxLength(128);
            entity.Property(e => e.Operation).HasMaxLength(50);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.WorkerId);
            entity.HasIndex(e => e.Priority);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ExternalReference).IsRequired().HasMaxLength(50);
            entity.ComplexProperty(e => e.Amount, p =>
            {
                p.Property(m => m.Amount).HasColumnName("AmountAmount").HasPrecision(18, 2);
                p.Property(m => m.Currency).HasColumnName("AmountCurrency").HasMaxLength(3);
            });
            entity.Property(e => e.CardToken).HasMaxLength(128);
            entity.Property(e => e.PayerEmail).HasMaxLength(256);
            entity.Property(e => e.PayerName).HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.HasIndex(e => e.ExternalReference).IsUnique();
            entity.HasIndex(e => e.IdempotencyKey).IsUnique();
            entity.HasIndex(e => e.Status);
        });

        modelBuilder.Entity<PaymentAttempt>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ErrorMessage).HasMaxLength(1000);
            entity.Property(e => e.AuthorizationCode).HasMaxLength(50);
            entity.HasIndex(e => e.PaymentId);
            entity.HasIndex(e => e.Status);
        });

        modelBuilder.Entity<Webhook>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Payload).IsRequired();
            entity.Property(e => e.LastError).HasMaxLength(1000);
            entity.HasIndex(e => e.PaymentId);
            entity.HasIndex(e => e.IsDelivered);
            entity.HasIndex(e => e.NextRetryAt);
        });

        modelBuilder.Entity<Worker>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ErrorMessage).HasMaxLength(1000);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.Status);
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdateTimestamp();
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
