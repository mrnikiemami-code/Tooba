using Tooba.BuildingBlocks;

namespace Tooba.Catalog.Domain;

/// <summary>
/// ترجمهٔ محلی رده. هویت مسیر locale+slug است؛ ستون‌های NameFa/NameEn وجود ندارد.
/// </summary>
public sealed class CatalogCategoryTranslation
{
    /// <summary>شناسهٔ ردیف ترجمه.</summary>
    public Guid TranslationId { get; init; }

    /// <summary>ردهٔ مالک.</summary>
    public Guid CategoryId { get; init; }

    /// <summary>برچسب زبان نرمال‌شده (مثلاً fa-IR).</summary>
    public string Locale { get; init; } = "";

    /// <summary>نام نمایشی الزامی.</summary>
    public string Name { get; set; } = "";

    /// <summary>slug محلی الزامی برای مسیر قابل‌مسیریابی.</summary>
    public string Slug { get; set; } = "";

    /// <summary>شرح کوتاه اختیاری.</summary>
    public string? ShortDescription { get; set; }

    /// <summary>شرح کامل اختیاری.</summary>
    public string? Description { get; set; }

    /// <summary>عنوان SEO اختیاری.</summary>
    public string? SeoTitle { get; set; }

    /// <summary>توضیح SEO اختیاری.</summary>
    public string? SeoDescription { get; set; }

    /// <summary>کلمات کلیدی SEO اختیاری.</summary>
    public string? MetaKeywords { get; set; }

    /// <summary>زمان آخرین به‌روزرسانی ترجمه.</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>ترجمهٔ جدید برای یک رده+locale می‌سازد.</summary>
    public static CatalogCategoryTranslation Create(
        Guid categoryId,
        string locale,
        string name,
        string slug,
        DateTimeOffset now,
        string? shortDescription = null,
        string? description = null,
        string? seoTitle = null,
        string? seoDescription = null,
        string? metaKeywords = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        var normalizedLocale = CatalogCategorySlugNormalizer.NormalizeLocale(locale);
        var normalizedSlug = CatalogCategorySlugNormalizer.NormalizeSlug(slug);
        return new CatalogCategoryTranslation
        {
            TranslationId = UuidV7.New(),
            CategoryId = categoryId,
            Locale = normalizedLocale,
            Name = name.Trim(),
            Slug = normalizedSlug,
            ShortDescription = NormalizeOptional(shortDescription),
            Description = NormalizeOptional(description),
            SeoTitle = NormalizeOptional(seoTitle),
            SeoDescription = NormalizeOptional(seoDescription),
            MetaKeywords = NormalizeOptional(metaKeywords),
            UpdatedAt = now,
        };
    }

    /// <summary>
    /// فیلدهای ترجمه را به‌روز می‌کند. اگر slug عوض شود، فراخواننده باید history بنویسد.
    /// </summary>
    /// <returns>slug قبلی وقتی تغییر کرده؛ در غیر این صورت null.</returns>
    public string? Update(
        string name,
        string slug,
        DateTimeOffset now,
        string? shortDescription = null,
        string? description = null,
        string? seoTitle = null,
        string? seoDescription = null,
        string? metaKeywords = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        var normalizedSlug = CatalogCategorySlugNormalizer.NormalizeSlug(slug);
        string? previousSlug = null;
        if (!string.Equals(Slug, normalizedSlug, StringComparison.Ordinal))
        {
            previousSlug = Slug;
            Slug = normalizedSlug;
        }

        Name = name.Trim();
        ShortDescription = NormalizeOptional(shortDescription);
        Description = NormalizeOptional(description);
        SeoTitle = NormalizeOptional(seoTitle);
        SeoDescription = NormalizeOptional(seoDescription);
        MetaKeywords = NormalizeOptional(metaKeywords);
        UpdatedAt = now;
        return previousSlug;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
