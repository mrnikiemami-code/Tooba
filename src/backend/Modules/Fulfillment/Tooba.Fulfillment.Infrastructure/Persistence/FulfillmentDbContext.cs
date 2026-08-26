using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Tooba.Persistence;
using Tooba.Fulfillment.Domain;

namespace Tooba.Fulfillment.Infrastructure.Persistence;

/// <summary>
/// DbContext مالک schema <c>fulfillment</c>. سفارش و موجودی را نگه نمی‌دارد.
/// </summary>
public sealed class FulfillmentDbContext : DbContext
{
    /// <summary>
    /// schema اختصاصی Fulfillment.
    /// </summary>
    public const string Schema = "fulfillment";

    /// <summary>
    /// DbContext را با گزینه‌های Host می‌سازد.
    /// </summary>
    public FulfillmentDbContext(DbContextOptions<FulfillmentDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// واحدهای fulfillment.
    /// </summary>
    public DbSet<FulfillmentUnit> Fulfillments => Set<FulfillmentUnit>();

    /// <summary>
    /// خطوط fulfillment.
    /// </summary>
    public DbSet<FulfillmentItem> Items => Set<FulfillmentItem>();

    /// <summary>
    /// محموله‌ها.
    /// </summary>
    public DbSet<Shipment> Shipments => Set<Shipment>();

    /// <summary>
    /// خطوط محموله.
    /// </summary>
    public DbSet<ShipmentItem> ShipmentItems => Set<ShipmentItem>();

    /// <summary>
    /// Outbox همین ماژول.
    /// </summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <summary>
    /// dedup رویداد payment.succeeded برای ایجاد fulfillment.
    /// </summary>
    public DbSet<FulfillmentPaymentInboxRecord> PaymentInbox => Set<FulfillmentPaymentInboxRecord>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.Entity<FulfillmentUnit>(entity =>
        {
            entity.ToTable("fulfillments");
            entity.HasKey(x => x.FulfillmentId);
            entity.Property(x => x.FulfillmentId).ValueGeneratedNever();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.RecipientName).HasMaxLength(256);
            entity.Property(x => x.ContactMobile).HasMaxLength(32);
            entity.Property(x => x.ProvinceName).HasMaxLength(128);
            entity.Property(x => x.CityName).HasMaxLength(128);
            entity.Property(x => x.PostalAddress).HasMaxLength(512);
            entity.Property(x => x.PostalCode).HasMaxLength(32);
            entity.Property(x => x.ShippingMethodCode).HasMaxLength(64);
            entity.Property(x => x.ShippingMethodLabel).HasMaxLength(128);
            entity.Ignore(x => x.DomainEvents);
            entity.Ignore(x => x.Items);
            entity.Ignore(x => x.Shipments);
            entity.HasIndex(x => x.SellerOrderId).IsUnique();
            entity.HasIndex(x => x.SellerPartyId);
            entity.HasIndex(x => x.CheckoutId);
        });
        modelBuilder.Entity<FulfillmentItem>(entity =>
        {
            entity.ToTable("items");
            entity.HasKey(x => x.FulfillmentItemId);
            entity.Property(x => x.FulfillmentItemId).ValueGeneratedNever();
            entity.Property(x => x.FulfillmentId);
            entity.HasIndex(x => x.FulfillmentId);
        });
        modelBuilder.Entity<Shipment>(entity =>
        {
            entity.ToTable("shipments");
            entity.HasKey(x => x.ShipmentId);
            entity.Property(x => x.ShipmentId).ValueGeneratedNever();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.CarrierDisplayName).HasMaxLength(128);
            entity.Property(x => x.TrackingReference).HasMaxLength(128);
            entity.Ignore(x => x.Items);
            entity.HasIndex(x => x.FulfillmentId);
            entity.HasIndex(x => x.TrackingReference).IsUnique().HasFilter("tracking_reference IS NOT NULL");
        });
        modelBuilder.Entity<ShipmentItem>(entity =>
        {
            entity.ToTable("shipment_items");
            entity.HasKey(x => x.ShipmentItemId);
            entity.Property(x => x.ShipmentItemId).ValueGeneratedNever();
            entity.HasIndex(x => x.ShipmentId);
        });
        modelBuilder.Entity<FulfillmentPaymentInboxRecord>(entity =>
        {
            entity.ToTable("payment_inbox");
            entity.HasKey(x => x.EventId);
            entity.Property(x => x.EventId).ValueGeneratedNever();
        });
        OutboxMessageMapping.Map(modelBuilder, Schema);
    }
}

/// <summary>
/// کارخانهٔ زمان طراحی مهاجرت.
/// </summary>
public sealed class FulfillmentDbContextFactory : IDesignTimeDbContextFactory<FulfillmentDbContext>
{
    /// <inheritdoc />
    public FulfillmentDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<FulfillmentDbContext>()
            .UseNpgsql("Host=127.0.0.1;Database=tooba_design;Username=tooba;Password=dev-placeholder")
            .Options;
        return new FulfillmentDbContext(options);
    }
}
