

namespace Tooba.Promotion.Domain.Merchandising;

/// <summary>
/// ترجمهٔ کمپین. Locale متعلق به بخش صفحه نیست.
/// </summary>
public sealed class MerchandisingCampaignTranslation
{
    /// <summary>سازندهٔ EF.</summary>
    private MerchandisingCampaignTranslation()
    {
    }

    /// <summary>کمپین.</summary>
    public Guid CampaignId { get; init; }

    /// <summary>locale نرمال‌شده.</summary>
    public string Locale { get; init; } = string.Empty;

    /// <summary>عنوان.</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>زیرعنوان اختیاری.</summary>
    public string? Subtitle { get; private set; }

    /// <summary>متن بج اختیاری.</summary>
    public string? BadgeText { get; private set; }

    /// <summary>
    /// ترجمه می‌سازد.
    /// </summary>
    public static MerchandisingCampaignTranslation Create(
        Guid campaignId,
        string locale,
        string title,
        string? subtitle,
        string? badgeText)
    {
        if (campaignId == Guid.Empty)
        {
            throw new InvalidOperationException("promotion.campaign.id_required");
        }

        if (string.IsNullOrWhiteSpace(locale))
        {
            throw new InvalidOperationException("promotion.translation.locale_required");
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new InvalidOperationException("promotion.translation.title_required");
        }

        return new MerchandisingCampaignTranslation
        {
            CampaignId = campaignId,
            Locale = locale.Trim(),
            Title = title.Trim(),
            Subtitle = string.IsNullOrWhiteSpace(subtitle) ? null : subtitle.Trim(),
            BadgeText = string.IsNullOrWhiteSpace(badgeText) ? null : badgeText.Trim(),
        };
    }

    /// <summary>
    /// فیلدهای نمایشی را عوض می‌کند.
    /// </summary>
    public void Upsert(string title, string? subtitle, string? badgeText)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new InvalidOperationException("promotion.translation.title_required");
        }

        Title = title.Trim();
        Subtitle = string.IsNullOrWhiteSpace(subtitle) ? null : subtitle.Trim();
        BadgeText = string.IsNullOrWhiteSpace(badgeText) ? null : badgeText.Trim();
    }
}
