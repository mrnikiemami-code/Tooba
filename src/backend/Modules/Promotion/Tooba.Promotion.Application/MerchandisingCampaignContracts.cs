using Tooba.Promotion.Domain;

namespace Tooba.Promotion.Application;

/// <summary>
/// مرجع گونهٔ مرچندایزینگ.
/// </summary>
public sealed record MerchandisingPromotionTypeReference(
    Guid Id,
    string Code,
    bool IsSystem,
    bool IsActive,
    int SortOrder);

/// <summary>
/// مرجع کمپین مرچندایزینگ.
/// </summary>
public sealed record MerchandisingCampaignReference(
    Guid Id,
    Guid PromotionTypeId,
    Guid StoreId,
    MerchandisingCampaignLifecycleStatus LifecycleStatus,
    DateTimeOffset StartAt,
    DateTimeOffset? EndAt,
    int Priority,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>
/// ترجمهٔ کمپین.
/// </summary>
public sealed record MerchandisingCampaignTranslationReference(
    Guid CampaignId,
    string Locale,
    string Title,
    string? Subtitle,
    string? BadgeText);

/// <summary>
/// عضو مرتب‌شدهٔ کمپین.
/// </summary>
public sealed record MerchandisingCampaignMemberReference(
    Guid MembershipId,
    Guid CampaignId,
    Guid SellerOfferId,
    int SortOrder,
    DateTimeOffset CreatedAt);

/// <summary>
/// دایرکتوری کمپین مرچندایزینگ داخل schema promotion.
/// جدا از <see cref="IPromotionDirectory"/> تخفیف تسویه است.
/// </summary>
public interface IMerchandisingCampaignDirectory
{
    /// <summary>
    /// گونهٔ سیستمی AMAZING و ترجمه‌های FA/EN را به‌صورت idempotent تضمین می‌کند.
    /// </summary>
    Task<MerchandisingPromotionTypeReference> EnsureAmazingTypeSeededAsync(CancellationToken cancellationToken);

    /// <summary>
    /// کمپین پیش‌نویس می‌سازد.
    /// </summary>
    Task<MerchandisingCampaignReference> CreateCampaignAsync(
        Guid promotionTypeId,
        Guid storeId,
        DateTimeOffset startAt,
        DateTimeOffset? endAt,
        int priority,
        CancellationToken cancellationToken);

    /// <summary>
    /// پنجره و اولویت را به‌روز می‌کند.
    /// </summary>
    Task<MerchandisingCampaignReference> UpdateCampaignWindowAsync(
        Guid campaignId,
        DateTimeOffset startAt,
        DateTimeOffset? endAt,
        int priority,
        CancellationToken cancellationToken);

    /// <summary>
    /// کمپین را منتشر می‌کند.
    /// </summary>
    Task PublishCampaignAsync(Guid campaignId, CancellationToken cancellationToken);

    /// <summary>
    /// کمپین را بایگانی می‌کند.
    /// </summary>
    Task ArchiveCampaignAsync(Guid campaignId, CancellationToken cancellationToken);

    /// <summary>
    /// ترجمهٔ کمپین را درج یا به‌روز می‌کند.
    /// </summary>
    Task<MerchandisingCampaignTranslationReference> UpsertCampaignTranslationAsync(
        Guid campaignId,
        string locale,
        string title,
        string? subtitle,
        string? badgeText,
        CancellationToken cancellationToken);

    /// <summary>
    /// Offer را به کمپین اضافه می‌کند.
    /// <paramref name="expectedStoreId"/> باید با <c>campaign.StoreId</c> یکی باشد؛
    /// فراخواننده مالکیت Offer به همان فروشگاه را تضمین می‌کند (SellerOffer فیلد StoreId ندارد).
    /// </summary>
    Task<MerchandisingCampaignMemberReference> AddOfferAsync(
        Guid campaignId,
        Guid sellerOfferId,
        int sortOrder,
        Guid expectedStoreId,
        CancellationToken cancellationToken);

    /// <summary>
    /// عضویت را حذف می‌کند بدون حذف SellerOffer.
    /// </summary>
    Task RemoveOfferAsync(Guid campaignId, Guid sellerOfferId, CancellationToken cancellationToken);

    /// <summary>
    /// ترتیب اعضا را با لیست مرتب‌شدهٔ SellerOfferId عوض می‌کند.
    /// </summary>
    Task ReorderOffersAsync(
        Guid campaignId,
        IReadOnlyList<Guid> orderedSellerOfferIds,
        CancellationToken cancellationToken);

    /// <summary>
    /// کمپین فعال زمان‌اجرا را برای فروشگاه + کد گونه برمی‌گرداند
    /// (Priority DESC، StartAt DESC، Id ASC).
    /// </summary>
    Task<MerchandisingCampaignReference?> ResolveActiveCampaignAsync(
        Guid storeId,
        string typeCode,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    /// <summary>
    /// اعضای مرتب کمپین را برمی‌گرداند (SortOrder سپس Id).
    /// </summary>
    Task<IReadOnlyList<MerchandisingCampaignMemberReference>> ResolveOrderedMembersAsync(
        Guid campaignId,
        CancellationToken cancellationToken);

    /// <summary>
    /// کمپین دانهٔ Development را با شناسهٔ پایدار upsert می‌کند (پنجره/اولویت/lifecycle).
    /// فقط برای seed idempotent؛ API عمومی Admin نیست.
    /// </summary>
    Task<MerchandisingCampaignReference> UpsertSeedCampaignAsync(
        Guid campaignId,
        Guid promotionTypeId,
        Guid storeId,
        DateTimeOffset startAt,
        DateTimeOffset? endAt,
        int priority,
        MerchandisingCampaignLifecycleStatus lifecycle,
        CancellationToken cancellationToken);

    /// <summary>
    /// عضویت‌های کمپین را با لیست مرتب SellerOfferId همگام می‌کند (حذف اضافه، درج کمبود).
    /// </summary>
    Task SyncSeedMembersAsync(
        Guid campaignId,
        Guid expectedStoreId,
        IReadOnlyList<Guid> orderedSellerOfferIds,
        CancellationToken cancellationToken);

    /// <summary>
    /// فهرست Store-scoped کمپین‌ها با فیلتر و صفحه‌بندی برای Admin.
    /// </summary>
    Task<(IReadOnlyList<MerchandisingCampaignListRow> Items, int Total)> ListCampaignsAsync(
        Guid storeId,
        string? search,
        MerchandisingCampaignLifecycleStatus? lifecycle,
        Guid? promotionTypeId,
        string? runtimeWindow,
        DateTimeOffset now,
        string titleLocale,
        int skip,
        int take,
        CancellationToken cancellationToken);

    /// <summary>
    /// جزئیات یک کمپین Store-scoped را برمی‌گرداند یا null.
    /// </summary>
    Task<MerchandisingCampaignReference?> GetCampaignAsync(
        Guid campaignId,
        Guid storeId,
        CancellationToken cancellationToken);

    /// <summary>
    /// ترجمه‌های کمپین را برمی‌گرداند.
    /// </summary>
    Task<IReadOnlyList<MerchandisingCampaignTranslationReference>> ListCampaignTranslationsAsync(
        Guid campaignId,
        CancellationToken cancellationToken);

    /// <summary>
    /// گونه‌های فعال با نام نمایشی (بدون افشای کد خام در UI؛ Code فقط برای قرارداد داخلی).
    /// </summary>
    Task<IReadOnlyList<MerchandisingPromotionTypeOption>> ListPromotionTypeOptionsAsync(
        string locale,
        CancellationToken cancellationToken);
}

/// <summary>
/// ردیف فهرست Admin کمپین.
/// </summary>
public sealed record MerchandisingCampaignListRow(
    Guid Id,
    Guid PromotionTypeId,
    string PromotionTypeDisplayName,
    MerchandisingCampaignLifecycleStatus LifecycleStatus,
    DateTimeOffset StartAt,
    DateTimeOffset? EndAt,
    int Priority,
    int MemberCount,
    string? Title,
    DateTimeOffset UpdatedAt,
    string RuntimeLabel);

/// <summary>
/// گزینهٔ گونه برای انتخاب Admin (DisplayName محور).
/// </summary>
public sealed record MerchandisingPromotionTypeOption(
    Guid Id,
    string DisplayName,
    string Code,
    bool IsSystem);
