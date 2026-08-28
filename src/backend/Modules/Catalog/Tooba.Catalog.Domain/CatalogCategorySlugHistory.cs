using Tooba.BuildingBlocks;

namespace Tooba.Catalog.Domain;

/// <summary>
/// تاریخچهٔ slug محلی برای redirect امن پس از تغییر مسیر.
/// </summary>
public sealed class CatalogCategorySlugHistory
{
    /// <summary>شناسهٔ ردیف تاریخچه.</summary>
    public Guid HistoryId { get; init; }

    /// <summary>ردهٔ مالک.</summary>
    public Guid CategoryId { get; init; }

    /// <summary>locale نرمال‌شده.</summary>
    public string Locale { get; init; } = "";

    /// <summary>slug قبلی که دیگر جاری نیست.</summary>
    public string OldSlug { get; init; } = "";

    /// <summary>زمان ثبت تغییر.</summary>
    public DateTimeOffset ChangedAt { get; init; }

    /// <summary>ردیفی برای slug قبلی پس از تغییر می‌سازد.</summary>
    public static CatalogCategorySlugHistory Create(
        Guid categoryId,
        string locale,
        string oldSlug,
        DateTimeOffset changedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(oldSlug);
        return new CatalogCategorySlugHistory
        {
            HistoryId = UuidV7.New(),
            CategoryId = categoryId,
            Locale = CatalogCategorySlugNormalizer.NormalizeLocale(locale),
            OldSlug = CatalogCategorySlugNormalizer.NormalizeSlug(oldSlug),
            ChangedAt = changedAt,
        };
    }
}
