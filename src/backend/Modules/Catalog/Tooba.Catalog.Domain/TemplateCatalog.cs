#pragma warning disable CS1591
using Tooba.BuildingBlocks;

namespace Tooba.Catalog.Domain;

/// <summary>
/// ریشهٔ هویت قالب فروشگاه. فقط فیلدهای هویت قالب؛ مالکیت دادهٔ Catalog روی جداول Template* است.
/// </summary>
public sealed class StoreTemplate
{
    public const int KeyMaxLength = 64;
    public const int NameMaxLength = 256;

    public Guid TemplateId { get; init; }

    public string Key { get; set; } = "";

    public string Name { get; set; } = "";

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; set; }

    public static StoreTemplate Create(string key, string name, DateTimeOffset now, Guid? templateId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new StoreTemplate
        {
            TemplateId = templateId is { } id && id != Guid.Empty ? id : UuidV7.New(),
            Key = key.Trim().ToLowerInvariant(),
            Name = name.Trim(),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }
}

/// <summary>آینهٔ <see cref="CatalogCategory"/> با مالکیت قالب.</summary>
public sealed class TemplateCategory
{
    public Guid CategoryId { get; init; }

    public Guid TemplateId { get; init; }

    public Guid? ParentCategoryId { get; set; }

    public CatalogPublicationStatus Status { get; set; }

    public int SortOrder { get; set; }

    public bool IsVisible { get; set; }

    public Guid? ImageMediaAssetId { get; set; }

    public Guid? IconMediaAssetId { get; set; }

    public Guid? BannerMediaAssetId { get; set; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>آینهٔ <see cref="CatalogCategoryTranslation"/>.</summary>
public sealed class TemplateCategoryTranslation
{
    public Guid TranslationId { get; init; }

    public Guid CategoryId { get; init; }

    public string Locale { get; init; } = "";

    public string Name { get; set; } = "";

    public string Slug { get; set; } = "";

    public string? ShortDescription { get; set; }

    public string? Description { get; set; }

    public string? SeoTitle { get; set; }

    public string? SeoDescription { get; set; }

    public string? MetaKeywords { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>آینهٔ <see cref="CatalogBrand"/> با مالکیت قالب.</summary>
public sealed class TemplateBrand
{
    public Guid BrandId { get; init; }

    public Guid TemplateId { get; init; }

    public string? SlugSeam { get; set; }

    public CatalogPublicationStatus Status { get; set; }

    public Guid? LogoMediaAssetId { get; set; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>آینهٔ <see cref="CatalogLocalizedText"/> برای موجودیت‌های Template Catalog.</summary>
public enum TemplateLocalizedOwnerKind
{
    Product = 0,
    Category = 1,
    Brand = 2,
    AttributeDefinition = 3,
    AttributeOption = 4,
    Tag = 5,
}

/// <summary>آینهٔ متن چندزبانهٔ Template Catalog.</summary>
public sealed class TemplateLocalizedText
{
    public Guid TextId { get; init; }

    public TemplateLocalizedOwnerKind OwnerKind { get; init; }

    public Guid OwnerId { get; init; }

    public string FieldKey { get; init; } = "";

    public string Locale { get; init; } = "";

    public string Value { get; set; } = "";
}

/// <summary>آینهٔ <see cref="CatalogProduct"/> با مالکیت قالب.</summary>
public sealed class TemplateProduct
{
    public Guid ProductId { get; init; }

    public Guid TemplateId { get; init; }

    public CatalogProductKind Kind { get; init; }

    public CatalogPublicationStatus Status { get; set; }

    public Guid? BrandId { get; set; }

    public string? SlugSeam { get; set; }

    public string? SeoTitleSeam { get; set; }

    public Guid UnitOfMeasureId { get; set; } = CanonicalUnits.Pcs;

    public int QuantityDecimalPlaces { get; set; }

    public decimal? QuantityStep { get; set; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>آینهٔ <see cref="CatalogProductCategory"/>.</summary>
public sealed class TemplateProductCategory
{
    public Guid AssignmentId { get; init; }

    public Guid ProductId { get; init; }

    public Guid CategoryId { get; init; }

    public CatalogProductCategoryRole Role { get; init; }
}

/// <summary>آینهٔ <see cref="CatalogProductMediaReference"/>.</summary>
public sealed class TemplateProductMediaReference
{
    public Guid ReferenceId { get; init; }

    public Guid ProductId { get; init; }

    public Guid MediaAssetId { get; init; }

    public int DisplayOrder { get; set; }

    public bool IsPrimary { get; set; }

    public string? AltText { get; set; }
}

/// <summary>آینهٔ <see cref="CatalogVariant"/> برای parity ساختاری (بدون domain event).</summary>
public sealed class TemplateVariant
{
    public Guid VariantId { get; init; }

    public Guid ProductId { get; init; }

    public string? CatalogCodeSeam { get; set; }

    public string CombinationFingerprint { get; init; } = "";

    public CatalogPublicationStatus Status { get; set; }

    public int SortOrder { get; set; }

    public bool IsDefault { get; set; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>
/// آینهٔ صفحهٔ Landing قالب. بنرهای قالب داخل ConfigurationJson بخش‌ها می‌مانند
/// (هیچ موجودیت Banner عملیاتی وجود ندارد).
/// </summary>
public sealed class TemplateStoreLandingPage
{
    public Guid PageId { get; init; }

    public Guid TemplateId { get; init; }

    public string Locale { get; set; } = "fa";

    public string Slug { get; set; } = "";

    public string Title { get; set; } = "";

    public string? SeoTitle { get; set; }

    public string? SeoDescription { get; set; }

    public string TemplateKey { get; set; } = StoreLandingPage.DefaultTemplateKey;

    public StoreLandingPageStatus Status { get; set; } = StoreLandingPageStatus.Draft;

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>آینهٔ <see cref="StoreLandingPageSection"/> برای قالب.</summary>
public sealed class TemplateStoreLandingPageSection
{
    public Guid PageSectionId { get; init; }

    public Guid PageId { get; init; }

    public string SectionType { get; set; } = "";

    public int SortOrder { get; set; }

    public bool IsEnabled { get; set; } = true;

    public string ConfigurationJson { get; set; } = "{}";

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>شناسه‌های پایدار قالب Fashion برای seed idempotent (فقط رقم/هگز معتبر).</summary>
public static class FashionTemplateCatalogIds
{
    public const string FashionKey = "fashion";

    public static readonly Guid TemplateId = Guid.Parse("019022a5-0000-7000-8000-00000000f001");

    public static readonly Guid LandingPageId = Guid.Parse("019022a5-0000-7000-8000-00000000f010");

    public static Guid MediaAsset(int index) =>
        Guid.Parse($"019022a5-0000-7000-8000-00000000a{index:000}");

    public static Guid BrandId(int index) =>
        Guid.Parse($"019022a5-0000-7000-8000-00000000b{index:000}");

    public static Guid CategoryRoot(int index) =>
        Guid.Parse($"019022a5-0000-7000-8000-0000000c10{index:00}");

    public static Guid CategoryMid(int root, int mid) =>
        Guid.Parse($"019022a5-0000-7000-8000-0000000c2{root:00}{mid}");

    public static Guid CategoryLeaf(int root, int mid, int leaf) =>
        Guid.Parse($"019022a5-0000-7000-8000-00000c30{root:00}{mid}{leaf}");

    public static Guid ProductId(int index) =>
        Guid.Parse($"019022a5-0000-7000-8000-00000000d{index:000}");

    public static Guid ProductMediaRef(int index) =>
        Guid.Parse($"019022a5-0000-7000-8000-00000000e{index:000}");

    public static Guid ProductCategoryAssignment(int index) =>
        Guid.Parse($"019022a5-0000-7000-8000-00000002b{index:000}");

    public static Guid LocalizedProductName(int index) =>
        Guid.Parse($"019022a5-0000-7000-8000-00000001a{index:000}");

    public static Guid LocalizedBrandName(int index) =>
        Guid.Parse($"019022a5-0000-7000-8000-00000001b{index:000}");

    public static Guid CategoryTranslation(int root, int mid, int leaf) =>
        Guid.Parse($"019022a5-0000-7000-8000-0000001c{root:00}{mid}{leaf}");

    public static Guid BannerSectionId(int index) =>
        Guid.Parse($"019022a5-0000-7000-8000-00000001d{index:000}");

    public static Guid CompositionSectionId(int index) =>
        Guid.Parse($"019022a5-0000-7000-8000-00000001e{index:000}");
}
