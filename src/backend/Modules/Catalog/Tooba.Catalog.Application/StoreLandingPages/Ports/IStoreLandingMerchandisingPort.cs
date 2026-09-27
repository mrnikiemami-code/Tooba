namespace Tooba.Catalog.Application.StoreLandingPages.Ports;

/// <summary>
/// درز Promotion برای منبع ProductCollection=PromotionCampaign روی Landing.
/// پیاده‌سازی Host روی IMerchandisingCampaignQuery؛ Catalog به Promotion.Application وابسته نمی‌شود.
/// </summary>
public interface IStoreLandingMerchandisingPort
{
    /// <summary>سقف take اعضای کمپین (هم‌تراز runtime Promotion).</summary>
    int MaxMemberTake { get; }

    /// <summary>کد پیش‌فرض گونه Amazing.</summary>
    string AmazingTypeCode { get; }

    /// <summary>StoreId مرچندایزینگ برای tenant جاری؛ null اگر قابل‌حل نباشد.</summary>
    Guid? ResolveStoreId();

    /// <summary>اعضای یک کمپین صریح.</summary>
    Task<IReadOnlyList<StoreLandingMerchandisingMember>> ResolveCampaignMembersAsync(
        Guid campaignId,
        Guid storeId,
        string locale,
        DateTimeOffset now,
        int take,
        CancellationToken cancellationToken);

    /// <summary>اعضای کمپین فعال بر اساس کد گونه.</summary>
    Task<StoreLandingMerchandisingCampaign?> ResolveActiveByTypeAsync(
        Guid storeId,
        string typeCode,
        string locale,
        DateTimeOffset now,
        int take,
        CancellationToken cancellationToken);
}

/// <summary>عضو زمان‌اجرای کمپین برای Landing (DTO باریک Catalog).</summary>
public sealed record StoreLandingMerchandisingMember(
    Guid CatalogVariantId,
    bool IsMarketable,
    decimal AvailableQuantity,
    decimal? PriceAmount,
    string? PriceCurrency,
    decimal? CompareAtAmount);

/// <summary>کمپین فعال برای Landing.</summary>
public sealed record StoreLandingMerchandisingCampaign(
    Guid CampaignId,
    string? BadgeText,
    IReadOnlyList<StoreLandingMerchandisingMember> Members);

/// <summary>واجدشرایطی Amazing rail (همان قاعدهٔ Promotion storefront).</summary>
public static class StoreLandingMerchandisingEligibility
{
    /// <summary>عضو قابل‌فروش + موجود + قیمت کمپین معتبر اکیداً کمتر از قیمت عادی.</summary>
    public static bool IsAmazingRailEligible(StoreLandingMerchandisingMember member) =>
        member.IsMarketable
        && member.AvailableQuantity > 0
        && member.PriceAmount is decimal selling and > 0
        && member.CompareAtAmount is decimal compareAt and > 0
        && selling < compareAt;
}
