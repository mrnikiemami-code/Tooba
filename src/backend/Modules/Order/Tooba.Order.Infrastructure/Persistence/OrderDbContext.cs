using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Tooba.Order.Domain;
using Tooba.Persistence;

namespace Tooba.Order.Infrastructure.Persistence;

/// <summary>
/// DbContext مالک schema <c>order</c>. پرداخت، ارسال و سبد را نگه نمی‌دارد.
/// </summary>
public sealed class OrderDbContext : DbContext
{
    /// <summary>
    /// schema اختصاصی Order.
    /// </summary>
    public const string Schema = "order";

    /// <summary>
    /// DbContext را با گزینه‌های Host می‌سازد.
    /// </summary>
    public OrderDbContext(DbContextOptions<OrderDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// گروه‌های checkout.
    /// </summary>
    public DbSet<CheckoutGroup> Checkouts => Set<CheckoutGroup>();

    /// <summary>
    /// سفارش‌های فروشنده.
    /// </summary>
    public DbSet<SellerOrder> SellerOrders => Set<SellerOrder>();

    /// <summary>
    /// خطوط تصویر قیمت.
    /// </summary>
    public DbSet<OrderLine> Lines => Set<OrderLine>();

    /// <summary>
    /// Outbox همین ماژول.
    /// </summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <summary>
    /// Inbox مصرف payment.succeeded.v1. تکراری بودن delivery را پایدار نگه می‌دارد نه در حافظه.
    /// </summary>
    public DbSet<OrderPaymentInboxRecord> PaymentInbox => Set<OrderPaymentInboxRecord>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.Entity<CheckoutGroup>(entity =>
        {
            entity.ToTable("checkouts");
            entity.HasKey(x => x.CheckoutId);
            entity.Property(x => x.CheckoutId).ValueGeneratedNever();
            entity.Property(x => x.IdempotencyKey).HasMaxLength(128);
            entity.Property(x => x.Market).HasMaxLength(16);
            entity.Property(x => x.Currency).HasMaxLength(3);
            entity.Property(x => x.Mode).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.Channel).HasConversion<string>().HasMaxLength(32);
            entity.Ignore(x => x.DomainEvents);
            entity.HasIndex(x => x.IdempotencyKey).IsUnique();
            entity.HasIndex(x => x.CartId).IsUnique();
            entity.HasIndex(x => x.CartId).IsUnique();
            entity.HasMany(x => x.SellerOrders).WithOne().HasForeignKey(x => x.CheckoutId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<SellerOrder>(entity =>
        {
            entity.ToTable("seller_orders");
            entity.HasKey(x => x.SellerOrderId);
            entity.Property(x => x.SellerOrderId).ValueGeneratedNever();
            entity.Property(x => x.OrderNumber).HasMaxLength(64);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.Currency).HasMaxLength(3);
            entity.Property(x => x.SubtotalSnapshot).HasPrecision(19, 4);
            entity.Property(x => x.TaxSnapshot).HasPrecision(19, 4);
            entity.Property(x => x.DiscountSnapshot).HasPrecision(19, 4);
            entity.Property(x => x.GrandTotalSnapshot).HasPrecision(19, 4);
            entity.HasIndex(x => x.OrderNumber).IsUnique();
            entity.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.SellerOrderId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<OrderLine>(entity =>
        {
            entity.ToTable("order_lines");
            entity.HasKey(x => x.LineId);
            entity.Property(x => x.LineId).ValueGeneratedNever();
            entity.Property(x => x.Currency).HasMaxLength(3);
            entity.Property(x => x.UnitPriceSnapshot).HasPrecision(19, 4);
            entity.Property(x => x.LineTotalSnapshot).HasPrecision(19, 4);
            entity.Property(x => x.TaxOutcomeSnapshot).HasMaxLength(32);
            entity.Property(x => x.TaxRateSnapshot).HasPrecision(19, 8);
            entity.Property(x => x.TaxAmountSnapshot).HasPrecision(19, 4);
            entity.Property(x => x.TaxInclusiveSnapshot).HasPrecision(19, 4);
            entity.Property(x => x.DiscountAmountSnapshot).HasPrecision(19, 4);
            entity.Property(x => x.PromotionNameSnapshot).HasMaxLength(256);
            entity.Property(x => x.PromotionCodeSnapshot).HasMaxLength(64);
            entity.Property(x => x.DiscountKindSnapshot).HasMaxLength(32);
            entity.Property(x => x.PreDiscountTaxExclusiveSnapshot).HasPrecision(19, 4);
            entity.Property(x => x.PostDiscountTaxExclusiveSnapshot).HasPrecision(19, 4);
        });
        modelBuilder.Entity<OrderPaymentInboxRecord>(entity =>
        {
            entity.ToTable("payment_inbox");
            entity.HasKey(x => x.EventId);
            entity.Property(x => x.EventId).ValueGeneratedNever();
            entity.Property(x => x.PaymentId).IsRequired();
            entity.Property(x => x.ProcessedAt).IsRequired();
        });
        OutboxMessageMapping.Map(modelBuilder, Schema);
    }
}

/// <summary>
/// کارخانهٔ design-time مهاجرت Order.
/// </summary>
public sealed class OrderDbContextFactory : IDesignTimeDbContextFactory<OrderDbContext>
{
    /// <inheritdoc />
    public OrderDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            options,
            ToobaNpgsql.DesignTimeConnectionString(),
            OrderDbContext.Schema,
            typeof(OrderDbContext));
        return new OrderDbContext(options.Options);
    }
}
