using Tooba.BuildingBlocks;

namespace Tooba.Catalog.Domain.Aggregates;

/// <summary>
/// override نمایشی محلی برای آیتم مگامنو.
/// </summary>
public sealed class CatalogMegaMenuItemTranslation
{
    /// <summary>شناسهٔ ترجمه.</summary>
    public Guid MegaMenuItemTranslationId { get; init; }

    /// <summary>آیتم منو.</summary>
    public Guid MegaMenuItemId { get; init; }

    /// <summary>locale نرمال‌شده.</summary>
    public string Locale { get; init; } = string.Empty;

    /// <summary>عنوان متفاوت در مگامنو.</summary>
    public string? TitleOverride { get; set; }

    /// <summary>متن badge اختیاری.</summary>
    public string? BadgeText { get; set; }

    /// <summary>برچسب کوتاه اختیاری.</summary>
    public string? ShortLabel { get; set; }

    /// <summary>ایجاد یا به‌روزرسانی override.</summary>
    public static CatalogMegaMenuItemTranslation Create(
        Guid megaMenuItemId,
        string locale,
        string? titleOverride,
        string? badgeText,
        string? shortLabel) =>
        new()
        {
            MegaMenuItemTranslationId = UuidV7.New(),
            MegaMenuItemId = megaMenuItemId,
            Locale = locale.Trim(),
            TitleOverride = string.IsNullOrWhiteSpace(titleOverride) ? null : titleOverride.Trim(),
            BadgeText = string.IsNullOrWhiteSpace(badgeText) ? null : badgeText.Trim(),
            ShortLabel = string.IsNullOrWhiteSpace(shortLabel) ? null : shortLabel.Trim(),
        };
}
