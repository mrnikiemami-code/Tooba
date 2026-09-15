using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Tooba.Catalog.Domain;
using Tooba.Persistence;

namespace Tooba.Catalog.Infrastructure.Persistence;

/// <summary>
/// DbContext مالک schema <c>catalog</c>. قیمت، موجودی و Offer اینجا نیستند و mega-context نیست.
/// </summary>
public sealed class CatalogDbContext : DbContext
{
    /// <summary>
    /// schema اختصاصی Catalog در پایگاه Tenant یا Marketplace.
    /// </summary>
    public const string Schema = "catalog";

    /// <summary>
    /// DbContext را با گزینه‌های Host می‌سازد.
    /// </summary>
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// محصولات توصیفی.
    /// </summary>
    public DbSet<CatalogProduct> Products => Set<CatalogProduct>();

    /// <summary>واحدهای اندازه‌گیری کالا.</summary>
    public DbSet<UnitOfMeasure> UnitsOfMeasure => Set<UnitOfMeasure>();

    /// <summary>ترجمه‌های واحد با LanguageId.</summary>
    public DbSet<UnitOfMeasureTranslation> UnitOfMeasureTranslations => Set<UnitOfMeasureTranslation>();

    /// <summary>گرد کردن سراسری مقدار کالا.</summary>
    public DbSet<StoreQuantitySettings> StoreQuantitySettings => Set<StoreQuantitySettings>();

    /// <summary>override فروشگاه برای ماندگاری سبد و مهلت پرداخت.</summary>
    public DbSet<StoreHoldPolicySettings> StoreHoldPolicySettings => Set<StoreHoldPolicySettings>();

    /// <summary>سیاست هویت مشتری در فرایند خرید.</summary>
    public DbSet<StoreCheckoutIdentitySettings> StoreCheckoutIdentitySettings => Set<StoreCheckoutIdentitySettings>();

    /// <summary>سقف سفارش باز و سهمیه شروع رزرو.</summary>
    public DbSet<StoreCheckoutAbuseSettings> StoreCheckoutAbuseSettings => Set<StoreCheckoutAbuseSettings>();

    /// <summary>ظاهر فروشگاه: پالت curated و حالت تم.</summary>
    public DbSet<StoreAppearanceSettings> StoreAppearanceSettings => Set<StoreAppearanceSettings>();

    /// <summary>صفحات Landing فروشگاه.</summary>
    public DbSet<StoreLandingPage> StoreLandingPages => Set<StoreLandingPage>();

    /// <summary>بخش‌های Landing متعلق به صفحه.</summary>
    public DbSet<StoreLandingPageSection> StoreLandingPageSections => Set<StoreLandingPageSection>();

    /// <summary>ریشهٔ قالب‌های فروشگاه (Template Catalog).</summary>
    public DbSet<StoreTemplate> StoreTemplates => Set<StoreTemplate>();

    /// <summary>رده‌های آینهٔ قالب.</summary>
    public DbSet<TemplateCategory> TemplateCategories => Set<TemplateCategory>();

    /// <summary>ترجمه‌های ردهٔ قالب.</summary>
    public DbSet<TemplateCategoryTranslation> TemplateCategoryTranslations => Set<TemplateCategoryTranslation>();

    /// <summary>برندهای آینهٔ قالب.</summary>
    public DbSet<TemplateBrand> TemplateBrands => Set<TemplateBrand>();

    /// <summary>متن‌های چندزبانهٔ Template Catalog.</summary>
    public DbSet<TemplateLocalizedText> TemplateLocalizedTexts => Set<TemplateLocalizedText>();

    /// <summary>محصولات آینهٔ قالب.</summary>
    public DbSet<TemplateProduct> TemplateProducts => Set<TemplateProduct>();

    /// <summary>پیوند محصول-ردهٔ قالب.</summary>
    public DbSet<TemplateProductCategory> TemplateProductCategories => Set<TemplateProductCategory>();

    /// <summary>رسانهٔ محصول قالب.</summary>
    public DbSet<TemplateProductMediaReference> TemplateProductMediaReferences => Set<TemplateProductMediaReference>();

    /// <summary>گونهٔ محصول قالب.</summary>
    public DbSet<TemplateVariant> TemplateVariants => Set<TemplateVariant>();

    /// <summary>صفحهٔ Landing قالب.</summary>
    public DbSet<TemplateStoreLandingPage> TemplateStoreLandingPages => Set<TemplateStoreLandingPage>();

    /// <summary>بخش‌های Landing قالب (شامل BannerShowcase).</summary>
    public DbSet<TemplateStoreLandingPageSection> TemplateStoreLandingPageSections => Set<TemplateStoreLandingPageSection>();

    /// <summary>منوهای ساختاریافتهٔ فروشگاه.</summary>
    public DbSet<StoreMenu> StoreMenus => Set<StoreMenu>();

    /// <summary>آیتم‌های درختی منو.</summary>
    public DbSet<StoreMenuItem> StoreMenuItems => Set<StoreMenuItem>();

    /// <summary>override چرخه رزرو Offer/Category.</summary>
    public DbSet<ReservationCyclePolicyOverride> ReservationCyclePolicyOverrides => Set<ReservationCyclePolicyOverride>();

    /// <summary>ممیزی تغییر تنظیم سیاست رزرو.</summary>
    public DbSet<ReservationPolicyAuditEvent> ReservationPolicyAuditEvents => Set<ReservationPolicyAuditEvent>();

    /// <summary>
    /// گونه‌های Catalog.
    /// </summary>
    public DbSet<CatalogVariant> Variants => Set<CatalogVariant>();

    /// <summary>
    /// رده‌های طبقه‌بندی.
    /// </summary>
    public DbSet<CatalogCategory> Categories => Set<CatalogCategory>();

    /// <summary>
    /// ترجمه‌های محلی رده (نام/slug/SEO).
    /// </summary>
    public DbSet<CatalogCategoryTranslation> CategoryTranslations => Set<CatalogCategoryTranslation>();

    /// <summary>
    /// تاریخچهٔ slug محلی برای redirect.
    /// </summary>
    public DbSet<CatalogCategorySlugHistory> CategorySlugHistories => Set<CatalogCategorySlugHistory>();

    /// <summary>
    /// برندهای تحریری.
    /// </summary>
    public DbSet<CatalogBrand> Brands => Set<CatalogBrand>();

    /// <summary>
    /// برچسب‌های تاکسونومی Catalog.
    /// </summary>
    public DbSet<CatalogTag> Tags => Set<CatalogTag>();

    /// <summary>
    /// پیوند محصول ↔ برچسب.
    /// </summary>
    public DbSet<CatalogProductTagAssignment> ProductTagAssignments => Set<CatalogProductTagAssignment>();

    /// <summary>
    /// پیوند رده ↔ برچسب.
    /// </summary>
    public DbSet<CatalogCategoryTagAssignment> CategoryTagAssignments => Set<CatalogCategoryTagAssignment>();

    /// <summary>
    /// تعریف ویژگی تایپ‌شده.
    /// </summary>
    public DbSet<CatalogAttributeDefinition> AttributeDefinitions => Set<CatalogAttributeDefinition>();

    /// <summary>
    /// گزینه‌های شمارشی.
    /// </summary>
    public DbSet<CatalogAttributeOption> AttributeOptions => Set<CatalogAttributeOption>();

    /// <summary>
    /// متن چندزبانه.
    /// </summary>
    public DbSet<CatalogLocalizedText> LocalizedTexts => Set<CatalogLocalizedText>();

    /// <summary>
    /// پیوند محصول-رده.
    /// </summary>
    public DbSet<CatalogProductCategory> ProductCategories => Set<CatalogProductCategory>();

    /// <summary>
    /// مرجع مات رسانه.
    /// </summary>
    public DbSet<CatalogProductMediaReference> MediaReferences => Set<CatalogProductMediaReference>();

    /// <summary>
    /// مشخصات غیرمحور محصول.
    /// </summary>
    public DbSet<CatalogProductAttributeValue> ProductAttributeValues => Set<CatalogProductAttributeValue>();

    /// <summary>
    /// محورهای گونه.
    /// </summary>
    public DbSet<CatalogVariantAttributeValue> VariantAttributeValues => Set<CatalogVariantAttributeValue>();

    /// <summary>
    /// پیوند تعریف ویژگی به رده.
    /// </summary>
    public DbSet<CatalogCategoryAttributeBinding> CategoryAttributeBindings => Set<CatalogCategoryAttributeBinding>();

    /// <summary>پیکربندی facet PLP رده.</summary>
    public DbSet<CatalogCategoryFacetConfiguration> CategoryFacetConfigurations => Set<CatalogCategoryFacetConfiguration>();

    /// <summary>آیتم‌های presentation مگامنو.</summary>
    public DbSet<CatalogMegaMenuItem> MegaMenuItems => Set<CatalogMegaMenuItem>();

    /// <summary>override محلی عنوان مگامنو.</summary>
    public DbSet<CatalogMegaMenuItemTranslation> MegaMenuItemTranslations => Set<CatalogMegaMenuItemTranslation>();

    /// <summary>
    /// محورهای Variant انتخاب‌شدهٔ محصول.
    /// </summary>
    public DbSet<CatalogProductVariantAxis> ProductVariantAxes => Set<CatalogProductVariantAxis>();

    /// <summary>
    /// تاریخچهٔ append-only محصول.
    /// </summary>
    public DbSet<CatalogProductHistoryEntry> ProductHistoryEntries => Set<CatalogProductHistoryEntry>();

    /// <summary>
    /// Outbox همین ماژول برای تصویر Search آینده.
    /// </summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<CatalogCategory>(entity =>
        {
            entity.ToTable("categories");
            entity.HasKey(x => x.CategoryId);
            entity.Property(x => x.CategoryId).ValueGeneratedNever();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.SortOrder).HasDefaultValue(0);
            entity.Property(x => x.IsVisible).HasDefaultValue(true);
            entity.HasIndex(x => new { x.ParentCategoryId, x.SortOrder });
            entity.HasOne<CatalogCategory>()
                .WithMany()
                .HasForeignKey(x => x.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CatalogCategoryTranslation>(entity =>
        {
            entity.ToTable("category_translations");
            entity.HasKey(x => x.TranslationId);
            entity.Property(x => x.TranslationId).ValueGeneratedNever();
            entity.Property(x => x.Locale).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(256).IsRequired();
            entity.Property(x => x.Slug).HasMaxLength(160).IsRequired();
            entity.Property(x => x.ShortDescription).HasMaxLength(512);
            entity.Property(x => x.Description).HasMaxLength(4000);
            entity.Property(x => x.SeoTitle).HasMaxLength(256);
            entity.Property(x => x.SeoDescription).HasMaxLength(512);
            entity.Property(x => x.MetaKeywords).HasMaxLength(512);
            entity.HasIndex(x => new { x.CategoryId, x.Locale }).IsUnique();
            entity.HasIndex(x => new { x.Locale, x.Slug }).IsUnique();
            entity.HasOne<CatalogCategory>()
                .WithMany()
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CatalogCategorySlugHistory>(entity =>
        {
            entity.ToTable("category_slug_histories");
            entity.HasKey(x => x.HistoryId);
            entity.Property(x => x.HistoryId).ValueGeneratedNever();
            entity.Property(x => x.Locale).HasMaxLength(32).IsRequired();
            entity.Property(x => x.OldSlug).HasMaxLength(160).IsRequired();
            entity.HasIndex(x => new { x.Locale, x.OldSlug });
            entity.HasOne<CatalogCategory>()
                .WithMany()
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CatalogBrand>(entity =>
        {
            entity.ToTable("brands");
            entity.HasKey(x => x.BrandId);
            entity.Property(x => x.BrandId).ValueGeneratedNever();
            entity.Property(x => x.SlugSeam).HasMaxLength(128);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.LogoMediaAssetId);
        });

        modelBuilder.Entity<CatalogTag>(entity =>
        {
            entity.ToTable("tags");
            entity.HasKey(x => x.TagId);
            entity.Property(x => x.TagId).ValueGeneratedNever();
            entity.Property(x => x.Code).HasMaxLength(64).IsRequired();
            entity.Property(x => x.SlugSeam).HasMaxLength(128);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            entity.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<CatalogProductTagAssignment>(entity =>
        {
            entity.ToTable("product_tag_assignments");
            entity.HasKey(x => x.AssignmentId);
            entity.Property(x => x.AssignmentId).ValueGeneratedNever();
            entity.HasIndex(x => new { x.ProductId, x.TagId }).IsUnique();
            entity.HasIndex(x => x.TagId);
            entity.HasOne<CatalogProduct>()
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<CatalogTag>()
                .WithMany()
                .HasForeignKey(x => x.TagId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CatalogCategoryTagAssignment>(entity =>
        {
            entity.ToTable("category_tag_assignments");
            entity.HasKey(x => x.AssignmentId);
            entity.Property(x => x.AssignmentId).ValueGeneratedNever();
            entity.HasIndex(x => new { x.CategoryId, x.TagId }).IsUnique();
            entity.HasIndex(x => x.TagId);
            entity.HasOne<CatalogCategory>()
                .WithMany()
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<CatalogTag>()
                .WithMany()
                .HasForeignKey(x => x.TagId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CatalogAttributeDefinition>(entity =>
        {
            entity.ToTable("attribute_definitions");
            entity.HasKey(x => x.DefinitionId);
            entity.Property(x => x.DefinitionId).ValueGeneratedNever();
            entity.Property(x => x.Code).HasMaxLength(64).IsRequired();
            entity.Property(x => x.ValueKind).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.Unit).HasMaxLength(32);
            entity.Property(x => x.ValidationMin).HasPrecision(18, 4);
            entity.Property(x => x.ValidationMax).HasPrecision(18, 4);
            entity.Ignore(x => x.IsVariantAxisAllowed);
            entity.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<CatalogAttributeOption>(entity =>
        {
            entity.ToTable("attribute_options");
            entity.HasKey(x => x.OptionId);
            entity.Property(x => x.OptionId).ValueGeneratedNever();
            entity.Property(x => x.Code).HasMaxLength(64).IsRequired();
            entity.HasIndex(x => new { x.DefinitionId, x.Code }).IsUnique();
            entity.HasOne<CatalogAttributeDefinition>()
                .WithMany()
                .HasForeignKey(x => x.DefinitionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CatalogCategoryAttributeBinding>(entity =>
        {
            entity.ToTable("category_attribute_bindings");
            entity.HasKey(x => x.BindingId);
            entity.Property(x => x.BindingId).ValueGeneratedNever();
            entity.HasIndex(x => new { x.CategoryId, x.DefinitionId }).IsUnique();
            entity.HasOne<CatalogCategory>()
                .WithMany()
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<CatalogAttributeDefinition>()
                .WithMany()
                .HasForeignKey(x => x.DefinitionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CatalogCategoryFacetConfiguration>(entity =>
        {
            entity.ToTable("category_facet_configurations");
            entity.HasKey(x => x.FacetConfigurationId);
            entity.Property(x => x.FacetConfigurationId).ValueGeneratedNever();
            entity.Property(x => x.DisplayType).HasConversion<string>().HasMaxLength(32);
            entity.HasIndex(x => new { x.CategoryId, x.DefinitionId }).IsUnique();
            entity.HasOne<CatalogCategory>()
                .WithMany()
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<CatalogAttributeDefinition>()
                .WithMany()
                .HasForeignKey(x => x.DefinitionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CatalogMegaMenuItem>(entity =>
        {
            entity.ToTable("mega_menu_items");
            entity.HasKey(x => x.MegaMenuItemId);
            entity.Property(x => x.MegaMenuItemId).ValueGeneratedNever();
            entity.Property(x => x.ItemType).HasConversion<string>().HasMaxLength(32);
            entity.HasIndex(x => x.CategoryId).IsUnique();
            entity.HasIndex(x => new { x.ParentMegaMenuItemId, x.SortOrder });
            entity.HasOne<CatalogCategory>()
                .WithMany()
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<CatalogMegaMenuItem>()
                .WithMany()
                .HasForeignKey(x => x.ParentMegaMenuItemId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CatalogMegaMenuItemTranslation>(entity =>
        {
            entity.ToTable("mega_menu_item_translations");
            entity.HasKey(x => x.MegaMenuItemTranslationId);
            entity.Property(x => x.MegaMenuItemTranslationId).ValueGeneratedNever();
            entity.Property(x => x.Locale).HasMaxLength(32).IsRequired();
            entity.Property(x => x.TitleOverride).HasMaxLength(256);
            entity.Property(x => x.BadgeText).HasMaxLength(64);
            entity.Property(x => x.ShortLabel).HasMaxLength(128);
            entity.HasIndex(x => new { x.MegaMenuItemId, x.Locale }).IsUnique();
            entity.HasOne<CatalogMegaMenuItem>()
                .WithMany()
                .HasForeignKey(x => x.MegaMenuItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CatalogProductVariantAxis>(entity =>
        {
            entity.ToTable("product_variant_axes");
            entity.HasKey(x => x.AxisId);
            entity.Property(x => x.AxisId).ValueGeneratedNever();
            entity.HasIndex(x => new { x.ProductId, x.DefinitionId }).IsUnique();
            entity.HasOne<CatalogProduct>()
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<CatalogAttributeDefinition>()
                .WithMany()
                .HasForeignKey(x => x.DefinitionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CatalogProductHistoryEntry>(entity =>
        {
            entity.ToTable("product_history_entries");
            entity.HasKey(x => x.HistoryId);
            entity.Property(x => x.HistoryId).ValueGeneratedNever();
            entity.Property(x => x.EventType).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Section).HasMaxLength(32).IsRequired();
            entity.Property(x => x.SummaryFa).HasMaxLength(512).IsRequired();
            entity.Property(x => x.BeforeSummary).HasMaxLength(512);
            entity.Property(x => x.AfterSummary).HasMaxLength(512);
            entity.Property(x => x.ActorDisplayName).HasMaxLength(256);
            entity.HasIndex(x => new { x.ProductId, x.OccurredAt });
            entity.HasIndex(x => new { x.ProductId, x.Section, x.OccurredAt });
            entity.HasOne<CatalogProduct>()
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CatalogLocalizedText>(entity =>
        {
            entity.ToTable("localized_texts");
            entity.HasKey(x => x.TextId);
            entity.Property(x => x.TextId).ValueGeneratedNever();
            entity.Property(x => x.OwnerKind).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.FieldKey).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Locale).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Value).HasMaxLength(1024).IsRequired();
            entity.HasIndex(x => new { x.OwnerKind, x.OwnerId, x.FieldKey, x.Locale }).IsUnique();
        });

        modelBuilder.Entity<CatalogProduct>(entity =>
        {
            entity.ToTable("products");
            entity.HasKey(x => x.ProductId);
            entity.Property(x => x.ProductId).ValueGeneratedNever();
            entity.Property(x => x.Kind).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.SlugSeam).HasMaxLength(160);
            entity.Property(x => x.SeoTitleSeam).HasMaxLength(256);
            entity.Property(x => x.QuantityDecimalPlaces).HasDefaultValue(0);
            entity.Property(x => x.QuantityStep).HasColumnType("numeric(18,6)");
            entity.Ignore(x => x.DomainEvents);
            entity.HasOne<CatalogBrand>()
                .WithMany()
                .HasForeignKey(x => x.BrandId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasMany(x => x.CategoryAssignments).WithOne().HasForeignKey(x => x.ProductId);
            entity.HasMany(x => x.Variants).WithOne().HasForeignKey(x => x.ProductId);
        });

        modelBuilder.Entity<CatalogProductCategory>(entity =>
        {
            entity.ToTable("product_categories");
            entity.HasKey(x => x.AssignmentId);
            entity.Property(x => x.AssignmentId).ValueGeneratedNever();
            entity.Property(x => x.Role)
                .HasConversion<byte>()
                .HasColumnName("role")
                .HasDefaultValue(CatalogProductCategoryRole.Primary);
            entity.HasIndex(x => new { x.ProductId, x.CategoryId }).IsUnique();
            entity.HasIndex(x => new { x.CategoryId, x.Role });
            // حداکثر یک Primary برای هر محصول
            entity.HasIndex(x => x.ProductId)
                .IsUnique()
                .HasFilter("\"role\" = 0")
                .HasDatabaseName("ix_product_categories_one_primary_per_product");
            entity.HasOne<CatalogCategory>()
                .WithMany()
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CatalogProductMediaReference>(entity =>
        {
            entity.ToTable("product_media_references");
            entity.HasKey(x => x.ReferenceId);
            entity.Property(x => x.ReferenceId).ValueGeneratedNever();
            entity.Property(x => x.AltText).HasMaxLength(512);
            entity.Property(x => x.DisplayOrder).HasDefaultValue(0);
            entity.Property(x => x.IsPrimary).HasDefaultValue(false);
            entity.HasIndex(x => new { x.ProductId, x.MediaAssetId }).IsUnique();
            entity.HasIndex(x => new { x.ProductId, x.DisplayOrder });
        });

        modelBuilder.Entity<CatalogProductAttributeValue>(entity =>
        {
            entity.ToTable("product_attribute_values");
            entity.HasKey(x => x.ValueId);
            entity.Property(x => x.ValueId).ValueGeneratedNever();
            entity.Property(x => x.CanonicalValue).HasMaxLength(256).IsRequired();
            entity.HasIndex(x => new { x.ProductId, x.DefinitionId }).IsUnique();
            entity.HasOne<CatalogAttributeDefinition>()
                .WithMany()
                .HasForeignKey(x => x.DefinitionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CatalogVariant>(entity =>
        {
            entity.ToTable("variants");
            entity.HasKey(x => x.VariantId);
            entity.Property(x => x.VariantId).ValueGeneratedNever();
            entity.Property(x => x.CatalogCodeSeam).HasMaxLength(64);
            entity.Property(x => x.CombinationFingerprint).HasMaxLength(512).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.SortOrder).HasDefaultValue(0);
            entity.Property(x => x.IsDefault).HasDefaultValue(false);
            entity.Ignore(x => x.DomainEvents);
            entity.HasIndex(x => new { x.ProductId, x.CombinationFingerprint }).IsUnique();
            entity.HasMany(x => x.AttributeValues).WithOne().HasForeignKey(x => x.VariantId);
        });

        modelBuilder.Entity<CatalogVariantAttributeValue>(entity =>
        {
            entity.ToTable("variant_attribute_values");
            entity.HasKey(x => x.ValueId);
            entity.Property(x => x.ValueId).ValueGeneratedNever();
            entity.Property(x => x.CanonicalValue).HasMaxLength(256).IsRequired();
            entity.HasIndex(x => new { x.VariantId, x.DefinitionId }).IsUnique();
            entity.HasOne<CatalogAttributeDefinition>()
                .WithMany()
                .HasForeignKey(x => x.DefinitionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<UnitOfMeasure>(entity =>
        {
            entity.ToTable("units_of_measure");
            entity.HasKey(x => x.UnitOfMeasureId);
            entity.Property(x => x.UnitOfMeasureId).ValueGeneratedNever();
            entity.Property(x => x.Code).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Dimension).HasConversion<string>().HasMaxLength(16);
            entity.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<UnitOfMeasureTranslation>(entity =>
        {
            entity.ToTable("unit_of_measure_translations");
            entity.HasKey(x => x.TranslationId);
            entity.Property(x => x.TranslationId).ValueGeneratedNever();
            entity.Property(x => x.Name).HasMaxLength(64).IsRequired();
            entity.Property(x => x.ShortName).HasMaxLength(16).IsRequired();
            entity.HasIndex(x => new { x.UnitOfMeasureId, x.LanguageId }).IsUnique();
        });

        modelBuilder.Entity<StoreQuantitySettings>(entity =>
        {
            entity.ToTable("store_quantity_settings");
            entity.HasKey(x => x.SettingsId);
            entity.Property(x => x.SettingsId).ValueGeneratedNever();
            entity.Property(x => x.RoundingMode).HasConversion<string>().HasMaxLength(16);
        });

        modelBuilder.Entity<StoreHoldPolicySettings>(entity =>
        {
            entity.ToTable("store_hold_policy_settings");
            entity.HasKey(x => x.SettingsId);
            entity.Property(x => x.SettingsId).ValueGeneratedNever();
        });

        modelBuilder.Entity<StoreCheckoutIdentitySettings>(entity =>
        {
            entity.ToTable("store_checkout_identity_settings");
            entity.HasKey(x => x.SettingsId);
            entity.Property(x => x.SettingsId).ValueGeneratedNever();
            entity.Property(x => x.Policy).HasConversion<string>().HasMaxLength(32);
        });

        modelBuilder.Entity<StoreCheckoutAbuseSettings>(entity =>
        {
            entity.ToTable("store_checkout_abuse_settings");
            entity.HasKey(x => x.SettingsId);
            entity.Property(x => x.SettingsId).ValueGeneratedNever();
        });

        modelBuilder.Entity<StoreAppearanceSettings>(entity =>
        {
            entity.ToTable("store_appearance_settings");
            entity.HasKey(x => x.SettingsId);
            entity.Property(x => x.SettingsId).ValueGeneratedNever();
            entity.Property(x => x.PaletteKey).HasMaxLength(32).IsRequired();
            entity.Property(x => x.ThemeMode).HasConversion<string>().HasMaxLength(16);
            entity.Property(x => x.ProductCardSkin).HasMaxLength(16).IsRequired();
            entity.Property(x => x.BackgroundStyle).HasConversion<string>().HasMaxLength(16);
            entity.Property(x => x.HomePageId);
            entity.Property(x => x.HeaderMenuId);
        });

        modelBuilder.Entity<StoreLandingPage>(entity =>
        {
            entity.ToTable("store_landing_pages");
            entity.HasKey(x => x.PageId);
            entity.Property(x => x.PageId).ValueGeneratedNever();
            entity.Property(x => x.Locale).HasMaxLength(16).IsRequired();
            entity.Property(x => x.Slug).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.SeoTitle).HasMaxLength(200);
            entity.Property(x => x.SeoDescription).HasMaxLength(500);
            entity.Property(x => x.TemplateKey).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
            entity.HasIndex(x => new { x.Locale, x.Slug }).IsUnique();
        });

        modelBuilder.Entity<StoreLandingPageSection>(entity =>
        {
            entity.ToTable("store_landing_page_sections");
            entity.HasKey(x => x.PageSectionId);
            entity.Property(x => x.PageSectionId).ValueGeneratedNever();
            entity.Property(x => x.SectionType).HasMaxLength(64).IsRequired();
            entity.Property(x => x.ConfigurationJson).HasMaxLength(12000).IsRequired();
            entity.HasIndex(x => new { x.PageId, x.SortOrder });
        });

        modelBuilder.Entity<StoreMenu>(entity =>
        {
            entity.ToTable("store_menus");
            entity.HasKey(x => x.MenuId);
            entity.Property(x => x.MenuId).ValueGeneratedNever();
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Locale).HasMaxLength(16).IsRequired();
            entity.Property(x => x.MenuKey).HasMaxLength(64).IsRequired();
            entity.HasIndex(x => new { x.Locale, x.MenuKey }).IsUnique();
        });

        modelBuilder.Entity<StoreMenuItem>(entity =>
        {
            entity.ToTable("store_menu_items");
            entity.HasKey(x => x.MenuItemId);
            entity.Property(x => x.MenuItemId).ValueGeneratedNever();
            entity.Property(x => x.Label).HasMaxLength(120).IsRequired();
            entity.Property(x => x.LinkType).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.ExternalUrl).HasMaxLength(500);
            entity.HasIndex(x => new { x.MenuId, x.ParentMenuItemId, x.SortOrder });
        });

        modelBuilder.Entity<ReservationCyclePolicyOverride>(entity =>
        {
            entity.ToTable("reservation_cycle_policy_overrides");
            entity.HasKey(x => x.OverrideId);
            entity.Property(x => x.OverrideId).ValueGeneratedNever();
            entity.Property(x => x.ScopeKind).HasMaxLength(16);
            entity.HasIndex(x => new { x.ScopeKind, x.ScopeId }).IsUnique();
        });

        modelBuilder.Entity<ReservationPolicyAuditEvent>(entity =>
        {
            entity.ToTable("reservation_policy_audit_events");
            entity.HasKey(x => x.EventId);
            entity.Property(x => x.EventId).ValueGeneratedNever();
            entity.Property(x => x.Level).HasMaxLength(16);
            entity.Property(x => x.Field).HasMaxLength(64);
            entity.Property(x => x.OldOverride).HasMaxLength(32);
            entity.Property(x => x.NewOverride).HasMaxLength(32);
            entity.HasIndex(x => x.OccurredAt);
        });

        modelBuilder.Entity<StoreTemplate>(entity =>
        {
            entity.ToTable("store_templates");
            entity.HasKey(x => x.TemplateId);
            entity.Property(x => x.TemplateId).ValueGeneratedNever();
            entity.Property(x => x.Key).HasMaxLength(StoreTemplate.KeyMaxLength).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(StoreTemplate.NameMaxLength).IsRequired();
            entity.HasIndex(x => x.Key).IsUnique();
        });

        modelBuilder.Entity<TemplateCategory>(entity =>
        {
            entity.ToTable("template_categories");
            entity.HasKey(x => x.CategoryId);
            entity.Property(x => x.CategoryId).ValueGeneratedNever();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.SortOrder).HasDefaultValue(0);
            entity.Property(x => x.IsVisible).HasDefaultValue(true);
            entity.HasIndex(x => x.TemplateId);
            entity.HasIndex(x => new { x.TemplateId, x.ParentCategoryId, x.SortOrder });
            entity.HasOne<StoreTemplate>()
                .WithMany()
                .HasForeignKey(x => x.TemplateId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<TemplateCategory>()
                .WithMany()
                .HasForeignKey(x => x.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TemplateCategoryTranslation>(entity =>
        {
            entity.ToTable("template_category_translations");
            entity.HasKey(x => x.TranslationId);
            entity.Property(x => x.TranslationId).ValueGeneratedNever();
            entity.Property(x => x.Locale).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(256).IsRequired();
            entity.Property(x => x.Slug).HasMaxLength(160).IsRequired();
            entity.Property(x => x.ShortDescription).HasMaxLength(512);
            entity.Property(x => x.Description).HasMaxLength(4000);
            entity.Property(x => x.SeoTitle).HasMaxLength(256);
            entity.Property(x => x.SeoDescription).HasMaxLength(512);
            entity.Property(x => x.MetaKeywords).HasMaxLength(512);
            entity.HasIndex(x => new { x.CategoryId, x.Locale }).IsUnique();
            entity.HasIndex(x => new { x.Locale, x.Slug }).IsUnique();
            entity.HasOne<TemplateCategory>()
                .WithMany()
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TemplateBrand>(entity =>
        {
            entity.ToTable("template_brands");
            entity.HasKey(x => x.BrandId);
            entity.Property(x => x.BrandId).ValueGeneratedNever();
            entity.Property(x => x.SlugSeam).HasMaxLength(128);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            entity.HasIndex(x => x.TemplateId);
            entity.HasOne<StoreTemplate>()
                .WithMany()
                .HasForeignKey(x => x.TemplateId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TemplateLocalizedText>(entity =>
        {
            entity.ToTable("template_localized_texts");
            entity.HasKey(x => x.TextId);
            entity.Property(x => x.TextId).ValueGeneratedNever();
            entity.Property(x => x.OwnerKind).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.FieldKey).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Locale).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Value).HasMaxLength(1024).IsRequired();
            entity.HasIndex(x => new { x.OwnerKind, x.OwnerId, x.FieldKey, x.Locale }).IsUnique();
        });

        modelBuilder.Entity<TemplateProduct>(entity =>
        {
            entity.ToTable("template_products");
            entity.HasKey(x => x.ProductId);
            entity.Property(x => x.ProductId).ValueGeneratedNever();
            entity.Property(x => x.Kind).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.SlugSeam).HasMaxLength(160);
            entity.Property(x => x.SeoTitleSeam).HasMaxLength(256);
            entity.Property(x => x.QuantityDecimalPlaces).HasDefaultValue(0);
            entity.Property(x => x.QuantityStep).HasColumnType("numeric(18,6)");
            entity.HasIndex(x => x.TemplateId);
            entity.HasOne<StoreTemplate>()
                .WithMany()
                .HasForeignKey(x => x.TemplateId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<TemplateBrand>()
                .WithMany()
                .HasForeignKey(x => x.BrandId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<TemplateProductCategory>(entity =>
        {
            entity.ToTable("template_product_categories");
            entity.HasKey(x => x.AssignmentId);
            entity.Property(x => x.AssignmentId).ValueGeneratedNever();
            entity.Property(x => x.Role)
                .HasConversion<byte>()
                .HasColumnName("role")
                .HasDefaultValue(CatalogProductCategoryRole.Primary);
            entity.HasIndex(x => new { x.ProductId, x.CategoryId }).IsUnique();
            entity.HasIndex(x => new { x.CategoryId, x.Role });
            entity.HasIndex(x => x.ProductId)
                .IsUnique()
                .HasFilter("\"role\" = 0")
                .HasDatabaseName("ix_template_product_categories_one_primary_per_product");
            entity.HasOne<TemplateProduct>()
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<TemplateCategory>()
                .WithMany()
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TemplateProductMediaReference>(entity =>
        {
            entity.ToTable("template_product_media_references");
            entity.HasKey(x => x.ReferenceId);
            entity.Property(x => x.ReferenceId).ValueGeneratedNever();
            entity.Property(x => x.AltText).HasMaxLength(512);
            entity.Property(x => x.DisplayOrder).HasDefaultValue(0);
            entity.Property(x => x.IsPrimary).HasDefaultValue(false);
            entity.HasIndex(x => new { x.ProductId, x.MediaAssetId }).IsUnique();
            entity.HasIndex(x => new { x.ProductId, x.DisplayOrder });
            entity.HasOne<TemplateProduct>()
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TemplateVariant>(entity =>
        {
            entity.ToTable("template_variants");
            entity.HasKey(x => x.VariantId);
            entity.Property(x => x.VariantId).ValueGeneratedNever();
            entity.Property(x => x.CatalogCodeSeam).HasMaxLength(64);
            entity.Property(x => x.CombinationFingerprint).HasMaxLength(512).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.SortOrder).HasDefaultValue(0);
            entity.Property(x => x.IsDefault).HasDefaultValue(false);
            entity.HasIndex(x => new { x.ProductId, x.CombinationFingerprint }).IsUnique();
            entity.HasOne<TemplateProduct>()
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TemplateStoreLandingPage>(entity =>
        {
            entity.ToTable("template_store_landing_pages");
            entity.HasKey(x => x.PageId);
            entity.Property(x => x.PageId).ValueGeneratedNever();
            entity.Property(x => x.Locale).HasMaxLength(16).IsRequired();
            entity.Property(x => x.Slug).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.SeoTitle).HasMaxLength(200);
            entity.Property(x => x.SeoDescription).HasMaxLength(500);
            entity.Property(x => x.TemplateKey).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
            entity.HasIndex(x => x.TemplateId);
            entity.HasIndex(x => new { x.TemplateId, x.Locale, x.Slug }).IsUnique();
            entity.HasOne<StoreTemplate>()
                .WithMany()
                .HasForeignKey(x => x.TemplateId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TemplateStoreLandingPageSection>(entity =>
        {
            entity.ToTable("template_store_landing_page_sections");
            entity.HasKey(x => x.PageSectionId);
            entity.Property(x => x.PageSectionId).ValueGeneratedNever();
            entity.Property(x => x.SectionType).HasMaxLength(64).IsRequired();
            entity.Property(x => x.ConfigurationJson).HasMaxLength(12000).IsRequired();
            entity.HasIndex(x => new { x.PageId, x.SortOrder });
            entity.HasOne<TemplateStoreLandingPage>()
                .WithMany()
                .HasForeignKey(x => x.PageId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        OutboxMessageMapping.Map(modelBuilder, Schema);
    }
}

/// <summary>
/// کارخانهٔ design-time مهاجرت Catalog. Tenant را از Host نمی‌خواند.
/// </summary>
public sealed class CatalogDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    /// <inheritdoc />
    public CatalogDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            options,
            ToobaNpgsql.DesignTimeConnectionString(),
            CatalogDbContext.Schema,
            typeof(CatalogDbContext));
        return new CatalogDbContext(options.Options);
    }
}
