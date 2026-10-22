using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Tooba.Persistence;
using Tooba.Fulfillment.Domain.Aggregates;
using Tooba.Fulfillment.Domain.ValueObjects;

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
    /// گروه‌های ارسال پذیرفته‌شدهٔ مالک Fulfillment.
    /// </summary>
    public DbSet<FulfillmentShippingGroup> ShippingGroups => Set<FulfillmentShippingGroup>();

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

    /// <summary>روش‌های ارسال یک‌سطحی (کاتالوگ تازه، افزودنی و مستقل از سرویس/گزینه).</summary>
    public DbSet<ShippingMethod> ShippingMethods => Set<ShippingMethod>();

    /// <summary>ترجمه‌های روش ارسال یک‌سطحی.</summary>
    public DbSet<ShippingMethodTranslation> ShippingMethodTranslations => Set<ShippingMethodTranslation>();

    /// <summary>تعریف‌های مشترک نوع ارسال عملیاتی (TB-196)؛ فقط هویت/ترتیب/فعال‌بودن.</summary>
    public DbSet<ShippingType> ShippingTypes => Set<ShippingType>();

    /// <summary>متن چندزبانهٔ تعریف نوع ارسال.</summary>
    public DbSet<ShippingTypeTranslation> ShippingTypeTranslations => Set<ShippingTypeTranslation>();

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
            // TB-140: مجموعهٔ گروه‌های ارسال فقط-خواندنی است و از طریق DbSet خودش بارگذاری/ذخیره می‌شود؛
            // نگاشت‌نشدن آن با نگاشت سفارشی‌شدهٔ FK کلید بیگانهٔ سایه‌ای «fulfillment_unit_fulfillment_id»
            // می‌ساخت که هیچ‌گاه در اسکیمای مهاجرت‌شده وجود ندارد و هر SaveChanges را با
            // PostgresException 42703 می‌شکست. کلید واقعی همان ستون اسکالر FulfillmentId است.
            entity.Ignore(x => x.ShippingGroups);
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
            entity.HasIndex(x => x.ShippingGroupId).HasFilter("shipping_group_id IS NOT NULL");
        });
        modelBuilder.Entity<FulfillmentShippingGroup>(entity =>
        {
            entity.ToTable("shipping_groups");
            entity.HasKey(x => x.ShippingGroupId);
            entity.Property(x => x.ShippingGroupId).ValueGeneratedNever();
            entity.Property(x => x.MethodCode).HasMaxLength(64);
            entity.Property(x => x.ServiceCode).HasMaxLength(64);
            entity.Property(x => x.OptionCode).HasMaxLength(64);
            entity.Property(x => x.MethodLabel).HasMaxLength(128);
            entity.Property(x => x.OriginContext).HasMaxLength(256);
            entity.Property(x => x.RequestedDeliveryTimeWindow).HasMaxLength(64);
            entity.HasIndex(x => x.FulfillmentId);
            // یک گروه پایدار برای یک فروشنده هرگز دو بار در همان واحد materialize نمی‌شود.
            entity.HasIndex(x => new { x.FulfillmentId, x.ShippingGroupId }).IsUnique();
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
            entity.Property(x => x.ShippingGroupId);
            entity.Ignore(x => x.Items);
            entity.HasIndex(x => x.FulfillmentId);
            entity.HasIndex(x => x.ShippingGroupId).HasFilter("shipping_group_id IS NOT NULL");
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
        modelBuilder.Entity<ShippingMethod>(entity =>
        {
            entity.ToTable("shipping_methods");
            entity.HasKey(x => x.ShippingMethodId);
            entity.Property(x => x.ShippingMethodId).ValueGeneratedNever();
            entity.Property(x => x.MethodCode).HasMaxLength(ShippingMethod.MaxCodeLength);
            entity.Property(x => x.ProviderKind).HasMaxLength(64);
            entity.Property(x => x.LegacyServiceCode).HasMaxLength(ShippingMethod.MaxLegacyCodeLength);
            entity.Property(x => x.LegacyOptionCode).HasMaxLength(ShippingMethod.MaxLegacyCodeLength);
            entity.HasIndex(x => x.MethodCode).IsUnique();
            // پیوند قدیمی یکتا نگه داشته می‌شود تا یک سیاست fallback سطح سرویس هرگز به روش دوم تبدیل نشود.
            entity.HasIndex(x => new { x.LegacyServiceCode, x.LegacyOptionCode })
                .IsUnique()
                .HasFilter("legacy_option_code IS NOT NULL");
            entity.HasIndex(x => x.LegacyServiceCode);
        });
        modelBuilder.Entity<ShippingMethodTranslation>(entity =>
        {
            entity.ToTable("shipping_method_translations");
            entity.HasKey(x => x.TranslationId);
            entity.Property(x => x.TranslationId).ValueGeneratedNever();
            entity.Property(x => x.Name).HasMaxLength(128);
            entity.Property(x => x.Description).HasMaxLength(512);
            entity.HasIndex(x => new { x.ShippingMethodId, x.LanguageId }).IsUnique();
            // بدون این نگاشت، EF ترتیب درج را نمی‌داند و ترجمه را پیش از روش درج می‌کند؛ آن‌وقت
            // محدودیت FK دیتابیس (fk_shipping_method_translations_method) درج را رد می‌کند. رابطه
            // عمداً بدون navigation مدل می‌شود تا شکل دامنه تغییر نکند.
            entity.HasOne<ShippingMethod>()
                .WithMany()
                .HasForeignKey(x => x.ShippingMethodId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196: تعریف مشترک نوع ارسال عملیاتی. فقط هویت،
        // ترتیب، فعال‌بودن و متن LanguageId-keyed؛ هیچ ستون پول/جغرافیا/برنامه/ارز/ظرفیت اینجا نیست و
        // هیچ FK به Localization یا Shipping ساخته نمی‌شود. قیدهای ذخیره‌سازی آینهٔ گاردهای دامنه‌اند تا
        // ردیفی که دامنه رد می‌کند از SQL خام/import/هندلر آینده هم ذخیره نشود.
        modelBuilder.Entity<ShippingType>(entity =>
        {
            entity.ToTable("shipping_types", table =>
            {
                table.HasCheckConstraint(
                    "ck_shipping_types_code_canonical",
                    "char_length(code) BETWEEN 1 AND 64"
                    + " AND code = btrim(code)"
                    + " AND code ~ '^[a-z][a-z0-9_]*$'");
                table.HasCheckConstraint(
                    "ck_shipping_types_sort_order_range",
                    $"sort_order >= {ShippingType.SortOrderMinValue}"
                    + $" AND sort_order <= {ShippingType.SortOrderMaxValue}");
                table.HasCheckConstraint("ck_shipping_types_revision_positive", "revision > 0");
            });
            entity.HasKey(x => x.ShippingTypeId);
            entity.Property(x => x.ShippingTypeId).ValueGeneratedNever();
            entity.Property(x => x.Code).HasMaxLength(ShippingType.CodeMaxLength).IsRequired();
            // predicate واقعی همزمانی: UPDATE فقط وقتی اعمال می‌شود که revision ذخیره‌شده هنوز برابر
            // مقدار خوانده‌شدهٔ همین درخواست باشد.
            entity.Property(x => x.Revision).IsConcurrencyToken();
            entity.HasIndex(x => x.Code).IsUnique().HasDatabaseName("ix_shipping_types_code");
            entity.HasIndex(x => new { x.IsActive, x.SortOrder }).HasDatabaseName("ix_shipping_types_active_sort");

            entity.HasMany(x => x.Translations)
                .WithOne()
                .HasForeignKey(x => x.ShippingTypeId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(x => x.Translations).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<ShippingTypeTranslation>(entity =>
        {
            entity.ToTable("shipping_type_translations", table =>
            {
                table.HasCheckConstraint(
                    "ck_shipping_type_translations_name_nonblank",
                    "char_length(name) BETWEEN 1 AND 128 AND name = btrim(name)");
                table.HasCheckConstraint(
                    "ck_shipping_type_translations_description_nonblank",
                    "description IS NULL OR (char_length(description) BETWEEN 1 AND 512"
                    + " AND description = btrim(description))");
            });
            entity.HasKey(x => x.ShippingTypeTranslationId);
            entity.Property(x => x.ShippingTypeTranslationId).ValueGeneratedNever();
            entity.Property(x => x.Name).HasMaxLength(ShippingType.NameMaxLength).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(ShippingType.DescriptionMaxLength);
            entity.HasIndex(x => new { x.ShippingTypeId, x.LanguageId })
                .IsUnique()
                .HasDatabaseName("ix_shipping_type_translations_type_language");
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
/// <remarks>
/// TB-196-R7: این کارخانه پیکربندی متعارف ماژول (<see cref="Tooba.Persistence.ToobaNpgsql.ConfigureModuleContext"/>)
/// را دوباره به‌کار می‌گیرد تا فرادادهٔ زمان طراحی (snake_case، تاریخچهٔ مهاجرت ماژول) با فرادادهٔ زمان اجرا
/// یکی باشد. رشتهٔ اتصال فقط جایگزین زمان طراحی است و هیچ اتصالی باز نمی‌شود.
/// </remarks>
public sealed class FulfillmentDbContextFactory : IDesignTimeDbContextFactory<FulfillmentDbContext>
{
    /// <summary>جایگزین زمان طراحی؛ نه اعتبارنامهٔ واقعی است و نه برای اتصال استفاده می‌شود.</summary>
    private const string DesignTimePlaceholderConnection =
        "Host=127.0.0.1;Port=5432;Database=tooba_design;Username=tooba;Password=dev-placeholder";

    /// <inheritdoc />
    public FulfillmentDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<FulfillmentDbContext>();
        Tooba.Persistence.ToobaNpgsql.ConfigureModuleContext(
            options,
            DesignTimePlaceholderConnection,
            FulfillmentDbContext.Schema,
            typeof(FulfillmentDbContext));
        return new FulfillmentDbContext(options.Options);
    }
}
