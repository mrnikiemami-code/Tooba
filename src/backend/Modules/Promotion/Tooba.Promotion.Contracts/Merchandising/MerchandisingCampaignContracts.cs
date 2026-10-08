namespace Tooba.Promotion.Contracts.Merchandising;

/// <summary>
/// کانال فروش Promotion-owned برای projection کمپین. Promotion عمداً enum فروشگاه Offer را در
/// امضای عمومی خود مصرف نمی‌کند تا مرز Contracts این ماژول بدون ارجاع به ماژول دیگر قابل‌مصرف باشد؛
/// نگاشت به کانال Pricing/Offer در Infrastructure انجام می‌شود.
/// </summary>
public enum MerchandisingSalesChannel
{
    /// <summary>فروش مستقیم فروشگاه.</summary>
    Direct = 0,

    /// <summary>مارکت‌پلیس چندفروشنده‌ای.</summary>
    Marketplace = 1,

    /// <summary>فروش نمایندگی.</summary>
    Agency = 2,

    /// <summary>فروش سازمانی.</summary>
    Corporate = 3,

    /// <summary>فروش وابسته.</summary>
    Affiliate = 4,

    /// <summary>فروش از طریق API یکپارچه.</summary>
    Api = 5,
}

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

/// <summary>کدهای پایدار گونهٔ مرچندایزینگ (قرارداد؛ نه Domain entity).</summary>
public static class MerchandisingPromotionTypeCodes
{
    /// <summary>کد سیستمی پیشنهاد شگفت‌انگیز.</summary>
    public const string Amazing = "AMAZING";
}

/// <summary>Stable Development merchandising campaign / store identifiers.</summary>
public static class MerchandisingDevelopmentIds
{
    /// <summary>شناسهٔ فروشگاه توسعه store-alpha.</summary>
    public static readonly Guid StoreAlphaId = Guid.Parse("aaaaaaaa-aaaa-7aaa-8aaa-aaaaaaaaaaa1");

    /// <summary>کمپین فعال برنده.</summary>
    public static readonly Guid ActivePrimaryId = Guid.Parse("019a16a0-0001-7000-8000-000000000001");

    /// <summary>کمپین فعال بازنده.</summary>
    public static readonly Guid ActiveLoserId = Guid.Parse("019a16a0-0002-7000-8000-000000000002");

    /// <summary>کمپین آینده.</summary>
    public static readonly Guid FutureId = Guid.Parse("019a16a0-0003-7000-8000-000000000003");

    /// <summary>کمپین منقضی.</summary>
    public static readonly Guid ExpiredId = Guid.Parse("019a16a0-0004-7000-8000-000000000004");

    /// <summary>کمپین پیش‌نویس.</summary>
    public static readonly Guid DraftId = Guid.Parse("019a16a0-0005-7000-8000-000000000005");
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
/// پارامترهای حل قیمت برای projection کمپین. کانال Promotion-owned است و در Infrastructure به
/// کانال Pricing/Offer نگاشت می‌شود.
/// </summary>
public sealed record MerchandisingPriceScope(
    string Market,
    MerchandisingSalesChannel Channel,
    string Currency)
{
    /// <summary>پیش‌فرض IR / Marketplace / IRR.</summary>
    public static MerchandisingPriceScope Default { get; } = new(
        MerchandisingCampaignRuntimeLimits.DefaultMarket,
        MerchandisingSalesChannel.Marketplace,
        MerchandisingCampaignRuntimeLimits.DefaultCurrency);
}

/// <summary>
/// کوئری خوانش زمان‌اجرای کمپین مرچندایزینگ برای Catalog Landing و سایر مصرف‌کنندگان Contracts.
/// </summary>
public interface IMerchandisingCampaignQuery
{
    /// <summary>
    /// برندهٔ runtime-active برای فروشگاه + کد گونه؛ اعضا با فیلتر موجودی/قابل‌فروش.
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
