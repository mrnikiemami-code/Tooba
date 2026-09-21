using Tooba.Offer.Contracts.Dtos;

namespace Tooba.Promotion.Application.Merchandising;

/// <summary>
/// سقف take اعضای کمپین؛ هم‌تراز Product Showcase / MaxTake=48.
/// </summary>
public static class MerchandisingCampaignRuntimeLimits
{
    /// <summary>حداکثر عضو قابل‌بازگشت در یک resolve.</summary>
    public const int MaxMemberTake = 48;

    /// <summary>بازار پیش‌فرض قیمت برای SingleStore dev / IR.</summary>
    public const string DefaultMarket = "IR";

    /// <summary>ارز پیش‌فرض قیمت.</summary>
    public const string DefaultCurrency = "IRR";
}

/// <summary>
/// خوانش زمان‌اجرای کمپین مرچندایزینگ؛ EF entity نیست.
/// </summary>
public sealed record MerchandisingCampaignRuntimeModel(
    Guid CampaignId,
    Guid StoreId,
    string PromotionTypeCode,
    string Title,
    string? Subtitle,
    string? BadgeText,
    DateTimeOffset StartAt,
    DateTimeOffset? EndAt,
    int Priority,
    bool IsTeasing,
    TimeSpan? RemainingDuration,
    IReadOnlyList<MerchandisingCampaignMemberRuntimeModel> Members);

/// <summary>
/// عضو زمان‌اجرا با حقیقت Offer/Price/Inventory؛ بدون ProductCard کامل.
/// PriceAmount = فروش مؤثر (کمپین در صورت اعمال، وگرنه پایه).
/// CompareAtAmount = قیمت پایه فقط وقتی اکیداً بیشتر از فروش مؤثر است.
/// </summary>
public sealed record MerchandisingCampaignMemberRuntimeModel(
    Guid SellerOfferId,
    Guid CatalogVariantId,
    int SortOrder,
    bool IsMarketable,
    decimal? PriceAmount,
    string? PriceCurrency,
    decimal AvailableQuantity,
    decimal? MinimumOrderQuantity,
    decimal? MaximumOrderQuantity,
    decimal? CompareAtAmount = null);

/// <summary>
/// واجدشرایطی نمایش در source فروشگاهی «پیشنهاد شگفت‌انگیز».
/// عضویت Campaign می‌تواند بدون promo بماند؛ storefront فقط تخفیف مؤثر را نشان می‌دهد.
/// </summary>
public static class MerchandisingCampaignStorefrontEligibility
{
    /// <summary>
    /// عضو قابل‌فروش + موجود + قیمت کمپین معتبر اکیداً کمتر از قیمت عادی.
    /// </summary>
    public static bool IsAmazingRailEligible(MerchandisingCampaignMemberRuntimeModel member) =>
        member.IsMarketable
        && member.AvailableQuantity > 0
        && member.PriceAmount is decimal selling and > 0
        && member.CompareAtAmount is decimal compareAt and > 0
        && selling < compareAt;
}

/// <summary>
/// پارامترهای حل قیمت برای projection کمپین.
/// </summary>
public sealed record MerchandisingPriceScope(
    string Market,
    SalesChannel Channel,
    string Currency)
{
    /// <summary>پیش‌فرض IR / Marketplace / IRR.</summary>
    public static MerchandisingPriceScope Default { get; } = new(
        MerchandisingCampaignRuntimeLimits.DefaultMarket,
        SalesChannel.Marketplace,
        MerchandisingCampaignRuntimeLimits.DefaultCurrency);
}

/// <summary>
/// کوئری خوانش زمان‌اجرای کمپین مرچندایزینگ برای مصرف بعدی Builder/Storefront.
/// </summary>
public interface IMerchandisingCampaignQuery
{
    /// <summary>
    /// برندهٔ runtime-active برای فروشگاه + کد گونه؛ اعضا با فیلتر موجودی/قابل‌فروش.
    /// ترتیب کمپین: Priority DESC، StartAt DESC، Id ASC.
    /// </summary>
    Task<MerchandisingCampaignRuntimeModel?> ResolveActiveByTypeAsync(
        Guid storeId,
        string typeCode,
        string locale,
        DateTimeOffset now,
        int take,
        MerchandisingPriceScope? priceScope,
        CancellationToken cancellationToken);

    /// <summary>
    /// نزدیک‌ترین کمپین Published آینده (StartAt &gt; now)؛ هرگز از active برنمی‌گردد.
    /// ترتیب: StartAt ASC، Priority DESC، Id ASC.
    /// </summary>
    Task<MerchandisingCampaignRuntimeModel?> ResolveFutureByTypeAsync(
        Guid storeId,
        string typeCode,
        string locale,
        DateTimeOffset now,
        int take,
        MerchandisingPriceScope? priceScope,
        CancellationToken cancellationToken);

    /// <summary>
    /// اعضای یک کمپین (مرتب، capped)؛ فقط اگر campaign.StoreId == storeId.
    /// </summary>
    Task<IReadOnlyList<MerchandisingCampaignMemberRuntimeModel>> ResolveCampaignMembersAsync(
        Guid campaignId,
        Guid storeId,
        string locale,
        DateTimeOffset now,
        int take,
        MerchandisingPriceScope? priceScope,
        CancellationToken cancellationToken);

    /// <summary>
    /// آیا کمپین در همین فروشگاه وجود دارد (برای اعتبارسنجی پیکربندی Builder).
    /// </summary>
    Task<bool> CampaignBelongsToStoreAsync(
        Guid campaignId,
        Guid storeId,
        CancellationToken cancellationToken);
}
