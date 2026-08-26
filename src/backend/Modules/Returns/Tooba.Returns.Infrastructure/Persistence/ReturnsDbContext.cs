using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Tooba.Persistence;
using Tooba.Returns.Domain;

namespace Tooba.Returns.Infrastructure.Persistence;

/// <summary>
/// DbContext مالک schema <c>returns</c>. سفارش و پرداخت را نگه نمی‌دارد.
/// </summary>
public sealed class ReturnsDbContext : DbContext
{
    /// <summary>
    /// schema اختصاصی Returns.
    /// </summary>
    public const string Schema = "returns";

    /// <summary>
    /// DbContext را با گزینه‌های Host می‌سازد.
    /// </summary>
    public ReturnsDbContext(DbContextOptions<ReturnsDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// درخواست‌های مرجوعی.
    /// </summary>
    public DbSet<ReturnRequest> ReturnRequests => Set<ReturnRequest>();

    /// <summary>
    /// خطوط مرجوعی.
    /// </summary>
    public DbSet<ReturnItem> ReturnItems => Set<ReturnItem>();

    /// <summary>
    /// تلاش‌های refund.
    /// </summary>
    public DbSet<RefundAttempt> RefundAttempts => Set<RefundAttempt>();

    /// <summary>
    /// Outbox همین ماژول.
    /// </summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.Entity<ReturnRequest>(entity =>
        {
            entity.ToTable("return_requests");
            entity.HasKey(x => x.ReturnRequestId);
            entity.Property(x => x.ReturnRequestId).ValueGeneratedNever();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.IdempotencyKey).HasMaxLength(128);
            entity.Property(x => x.Reason).HasMaxLength(512);
            entity.Property(x => x.Currency).HasMaxLength(8);
            entity.Property(x => x.RefundAmount).HasPrecision(18, 4);
            entity.Ignore(x => x.DomainEvents);
            entity.Ignore(x => x.Items);
            entity.Ignore(x => x.RefundAttempts);
            entity.HasIndex(x => x.IdempotencyKey).IsUnique();
            entity.HasIndex(x => x.SellerOrderId);
            entity.HasIndex(x => x.SellerPartyId);
            entity.HasIndex(x => x.CheckoutId);
            entity.HasIndex(x => x.RequestedByUserId);
        });
        modelBuilder.Entity<ReturnItem>(entity =>
        {
            entity.ToTable("return_items");
            entity.HasKey(x => x.ReturnItemId);
            entity.Property(x => x.ReturnItemId).ValueGeneratedNever();
            entity.Property(x => x.Currency).HasMaxLength(8);
            entity.Property(x => x.UnitPriceSnapshot).HasPrecision(18, 4);
            entity.HasIndex(x => x.ReturnRequestId);
            entity.HasIndex(x => x.OrderLineId);
        });
        modelBuilder.Entity<RefundAttempt>(entity =>
        {
            entity.ToTable("refund_attempts");
            entity.HasKey(x => x.RefundAttemptId);
            entity.Property(x => x.RefundAttemptId).ValueGeneratedNever();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.Currency).HasMaxLength(8);
            entity.Property(x => x.Amount).HasPrecision(18, 4);
            entity.Property(x => x.IdempotencyKey).HasMaxLength(128);
            entity.Property(x => x.ProviderReference).HasMaxLength(256);
            entity.Property(x => x.FailureCode).HasMaxLength(64);
            entity.HasIndex(x => x.ReturnRequestId);
            entity.HasIndex(x => x.IdempotencyKey).IsUnique();
        });
        OutboxMessageMapping.Map(modelBuilder, Schema);
    }
}

/// <summary>
/// کارخانهٔ زمان طراحی مهاجرت.
/// </summary>
public sealed class ReturnsDbContextFactory : IDesignTimeDbContextFactory<ReturnsDbContext>
{
    /// <inheritdoc />
    public ReturnsDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ReturnsDbContext>()
            .UseNpgsql("Host=127.0.0.1;Database=tooba_design;Username=tooba;Password=dev-placeholder")
            .Options;
        return new ReturnsDbContext(options);
    }
}
