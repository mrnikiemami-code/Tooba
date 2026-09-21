

namespace Tooba.Promotion.Domain.Merchandising;

/// <summary>
/// ترجمهٔ نمایشی گونهٔ مرچندایزینگ. Locale از Market جداست.
/// </summary>
public sealed class MerchandisingPromotionTypeTranslation
{
    /// <summary>سازندهٔ EF.</summary>
    private MerchandisingPromotionTypeTranslation()
    {
    }

    /// <summary>گونه.</summary>
    public Guid TypeId { get; init; }

    /// <summary>locale نرمال‌شده (مثلاً fa-IR یا en-US).</summary>
    public string Locale { get; init; } = string.Empty;

    /// <summary>نام نمایشی.</summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>
    /// ترجمه می‌سازد.
    /// </summary>
    public static MerchandisingPromotionTypeTranslation Create(
        Guid typeId,
        string locale,
        string displayName)
    {
        if (typeId == Guid.Empty)
        {
            throw new InvalidOperationException("promotion.type.id_required");
        }

        if (string.IsNullOrWhiteSpace(locale))
        {
            throw new InvalidOperationException("promotion.translation.locale_required");
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new InvalidOperationException("promotion.translation.name_required");
        }

        return new MerchandisingPromotionTypeTranslation
        {
            TypeId = typeId,
            Locale = locale.Trim(),
            DisplayName = displayName.Trim(),
        };
    }

    /// <summary>
    /// نام نمایشی را عوض می‌کند.
    /// </summary>
    public void SetDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new InvalidOperationException("promotion.translation.name_required");
        }

        DisplayName = displayName.Trim();
    }
}
