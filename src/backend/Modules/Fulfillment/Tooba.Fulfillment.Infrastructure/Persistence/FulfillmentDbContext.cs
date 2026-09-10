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
    /// بسته‌های تجمیعی مرکزی.
    /// </summary>
    public DbSet<ConsolidatedPackage> ConsolidatedPackages => Set<ConsolidatedPackage>();

    /// <summary>
    /// اعضای بسته‌های تجمیعی.
    /// </summary>
    public DbSet<ConsolidatedPackageMember> ConsolidatedPackageMembers => Set<ConsolidatedPackageMember>();

    /// <summary>سرویس‌های ارسال دو‌سطحی (والد).</summary>
    public DbSet<ShippingService> ShippingServices => Set<ShippingService>();

    /// <summary>ترجمه‌های سرویس ارسال والد.</summary>
    public DbSet<ShippingServiceTranslation> ShippingServiceTranslations => Set<ShippingServiceTranslation>();

    /// <summary>گزینه‌های نوع سرویس (فرزند).</summary>
    public DbSet<ShippingServiceOption> ShippingServiceOptions => Set<ShippingServiceOption>();

    /// <summary>ترجمه‌های نوع سرویس فرزند.</summary>
    public DbSet<ShippingServiceOptionTranslation> ShippingServiceOptionTranslations => Set<ShippingServiceOptionTranslation>();

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
            entity.Property(x => x.QuantityOrdered).HasColumnType("numeric(18,6)");
            entity.Property(x => x.QuantityProcessing).HasColumnType("numeric(18,6)");
            entity.Property(x => x.QuantityPacked).HasColumnType("numeric(18,6)");
            entity.Property(x => x.QuantityShipped).HasColumnType("numeric(18,6)");
            entity.HasIndex(x => x.FulfillmentId);
        });
        modelBuilder.Entity<Shipment>(entity =>
        {
            entity.ToTable("shipments");
            entity.HasKey(x => x.ShipmentId);
            entity.Property(x => x.ShipmentId).ValueGeneratedNever();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.CarrierDisplayName).HasMaxLength(128);
            entity.Property(x => x.ShippingMethodCode).HasMaxLength(64);
            entity.Property(x => x.ShippingMethodLabel).HasMaxLength(128);
            entity.Property(x => x.ProviderMetadataJson).HasColumnType("text");
            entity.Property(x => x.TrackingReference).HasMaxLength(128);
            entity.Property(x => x.PreviousTrackingReference).HasMaxLength(128);
            entity.Ignore(x => x.Items);
            entity.HasIndex(x => x.FulfillmentId);
            entity.HasIndex(x => x.TrackingReference).IsUnique().HasFilter("tracking_reference IS NOT NULL");
        });
        modelBuilder.Entity<ShipmentItem>(entity =>
        {
            entity.ToTable("shipment_items");
            entity.HasKey(x => x.ShipmentItemId);
            entity.Property(x => x.ShipmentItemId).ValueGeneratedNever();
            entity.Property(x => x.Quantity).HasColumnType("numeric(18,6)");
            entity.HasIndex(x => x.ShipmentId);
        });
        modelBuilder.Entity<ConsolidatedPackage>(entity =>
        {
            entity.ToTable("consolidated_packages");
            entity.HasKey(x => x.ConsolidatedPackageId);
            entity.Property(x => x.ConsolidatedPackageId).ValueGeneratedNever();
            entity.Property(x => x.PackageNumber).HasMaxLength(32);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.ShippingMethodCode).HasMaxLength(64);
            entity.Property(x => x.ShippingMethodLabel).HasMaxLength(128);
            entity.Property(x => x.TrackingReference).HasMaxLength(128);
            entity.Property(x => x.Note).HasMaxLength(512);
            entity.Ignore(x => x.Members);
            entity.HasIndex(x => x.CheckoutId);
            entity.HasIndex(x => x.PackageNumber).IsUnique();
        });
        modelBuilder.Entity<ConsolidatedPackageMember>(entity =>
        {
            entity.ToTable("consolidated_package_members");
            entity.HasKey(x => x.ConsolidatedPackageMemberId);
            entity.Property(x => x.ConsolidatedPackageMemberId).ValueGeneratedNever();
            entity.HasIndex(x => x.ConsolidatedPackageId);
            entity.HasIndex(x => x.ShipmentId)
                .IsUnique()
                .HasFilter("released_at IS NULL");
        });
        modelBuilder.Entity<ShippingService>(entity =>
        {
            entity.ToTable("shipping_services");
            entity.HasKey(x => x.ShippingServiceId);
            entity.Property(x => x.ShippingServiceId).ValueGeneratedNever();
            entity.Property(x => x.Code).HasMaxLength(64);
            entity.Property(x => x.ProviderKind).HasMaxLength(64);
            entity.Property(x => x.IconKey).HasMaxLength(32);
            entity.Property(x => x.ColorKey).HasMaxLength(32);
            entity.HasIndex(x => x.Code).IsUnique();
        });
        modelBuilder.Entity<ShippingServiceTranslation>(entity =>
        {
            entity.ToTable("shipping_service_translations");
            entity.HasKey(x => x.TranslationId);
            entity.Property(x => x.TranslationId).ValueGeneratedNever();
            entity.Property(x => x.Name).HasMaxLength(128);
            entity.Property(x => x.Description).HasMaxLength(512);
            entity.HasIndex(x => new { x.ShippingServiceId, x.LanguageId }).IsUnique();
        });
        modelBuilder.Entity<ShippingServiceOption>(entity =>
        {
            entity.ToTable("shipping_service_options");
            entity.HasKey(x => x.ShippingServiceOptionId);
            entity.Property(x => x.ShippingServiceOptionId).ValueGeneratedNever();
            entity.Property(x => x.Code).HasMaxLength(64);
            entity.HasIndex(x => new { x.ShippingServiceId, x.Code }).IsUnique();
            entity.HasIndex(x => x.ShippingServiceId);
        });
        modelBuilder.Entity<ShippingServiceOptionTranslation>(entity =>
        {
            entity.ToTable("shipping_service_option_translations");
            entity.HasKey(x => x.TranslationId);
            entity.Property(x => x.TranslationId).ValueGeneratedNever();
            entity.Property(x => x.Name).HasMaxLength(128);
            entity.HasIndex(x => new { x.ShippingServiceOptionId, x.LanguageId }).IsUnique();
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
