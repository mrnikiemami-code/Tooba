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

    /// <summary>
    /// محورهای Variant انتخاب‌شدهٔ محصول.
    /// </summary>
    public DbSet<CatalogProductVariantAxis> ProductVariantAxes => Set<CatalogProductVariantAxis>();

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
            entity.HasIndex(x => new { x.ProductId, x.CategoryId }).IsUnique();
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
