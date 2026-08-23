using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Tooba.Persistence;
using Tooba.Payment.Domain;

namespace Tooba.Payment.Infrastructure.Persistence;

/// <summary>
/// DbContext مالک schema <c>payment</c>. سفارش و کارت را نگه نمی‌دارد.
/// </summary>
public sealed class PaymentDbContext : DbContext
{
    /// <summary>
    /// schema اختصاصی Payment.
    /// </summary>
    public const string Schema = "payment";

    /// <summary>
    /// DbContext را با گزینه‌های Host می‌سازد.
    /// </summary>
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// پرداخت‌های مشتری.
    /// </summary>
    public DbSet<CustomerPayment> Payments => Set<CustomerPayment>();

    /// <summary>
    /// تلاش‌های درگاه.
    /// </summary>
    public DbSet<PaymentAttempt> Attempts => Set<PaymentAttempt>();

    /// <summary>
    /// تخصیص چندفروشنده.
    /// </summary>
    public DbSet<PaymentAllocation> Allocations => Set<PaymentAllocation>();

    /// <summary>
    /// Outbox همین ماژول.
    /// </summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.Entity<CustomerPayment>(entity =>
        {
            entity.ToTable("payments");
            entity.HasKey(x => x.PaymentId);
            entity.Property(x => x.PaymentId).ValueGeneratedNever();
            entity.Property(x => x.Currency).HasMaxLength(8);
            entity.Property(x => x.ProviderCode).HasMaxLength(64);
            entity.Property(x => x.IdempotencyKey).HasMaxLength(128);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.Amount).HasPrecision(19, 4);
            entity.Ignore(x => x.DomainEvents);
            entity.Ignore(x => x.Attempts);
            entity.Ignore(x => x.Allocations);
            entity.HasIndex(x => x.IdempotencyKey).IsUnique();
            entity.HasMany<PaymentAttempt>().WithOne().HasForeignKey(x => x.PaymentId);
            entity.HasMany<PaymentAllocation>().WithOne().HasForeignKey(x => x.PaymentId);
        });
        modelBuilder.Entity<PaymentAttempt>(entity =>
        {
            entity.ToTable("attempts");
            entity.HasKey(x => x.AttemptId);
            entity.Property(x => x.AttemptId).ValueGeneratedNever();
            entity.Property(x => x.ProviderCode).HasMaxLength(64);
            entity.Property(x => x.ProviderRequestReference).HasMaxLength(128);
            entity.Property(x => x.ProviderTransactionReference).HasMaxLength(128);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            entity.HasIndex(x => x.ProviderTransactionReference).IsUnique().HasFilter("provider_transaction_reference IS NOT NULL");
        });
        modelBuilder.Entity<PaymentAllocation>(entity =>
        {
            entity.ToTable("allocations");
            entity.HasKey(x => x.AllocationId);
            entity.Property(x => x.AllocationId).ValueGeneratedNever();
            entity.Property(x => x.Currency).HasMaxLength(8);
            entity.Property(x => x.AllocatedAmount).HasPrecision(19, 4);
        });
        OutboxMessageMapping.Map(modelBuilder, Schema);
    }
}

/// <summary>
/// کارخانهٔ زمان طراحی مهاجرت.
/// </summary>
public sealed class PaymentDbContextFactory : IDesignTimeDbContextFactory<PaymentDbContext>
{
    /// <inheritdoc />
    public PaymentDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseNpgsql("Host=127.0.0.1;Database=tooba_design;Username=tooba;Password=dev-placeholder")
            .Options;
        return new PaymentDbContext(options);
    }
}
