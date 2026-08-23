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
            entity.HasOne<CatalogCategory>()
                .WithMany()
                .HasForeignKey(x => x.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CatalogBrand>(entity =>
        {
            entity.ToTable("brands");
            entity.HasKey(x => x.BrandId);
            entity.Property(x => x.BrandId).ValueGeneratedNever();
            entity.Property(x => x.SlugSeam).HasMaxLength(128);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        });

        modelBuilder.Entity<CatalogAttributeDefinition>(entity =>
        {
            entity.ToTable("attribute_definitions");
            entity.HasKey(x => x.DefinitionId);
            entity.Property(x => x.DefinitionId).ValueGeneratedNever();
            entity.Property(x => x.Code).HasMaxLength(64).IsRequired();
            entity.Property(x => x.ValueKind).HasConversion<string>().HasMaxLength(32);
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
            entity.HasIndex(x => new { x.ProductId, x.MediaAssetId }).IsUnique();
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
