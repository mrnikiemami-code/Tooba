using Tooba.Promotion.Domain.ValueObjects;

namespace Tooba.Promotion.Application.Ports;

/// <summary>
/// نوشتن تعریف پروموشن و ارزیابی. Pricing/Order را مالک نیست.
/// </summary>
public interface IPromotionDirectory : IPromotionEvaluator
{
    /// <summary>
    /// پروموشن پیش‌نویس می‌سازد.
    /// </summary>
    Task<PromotionReference> CreateAsync(
        string name,
        int priority,
        DateTimeOffset effectiveFrom,
        DateTimeOffset? effectiveTo,
        PromotionStackingPolicy stackingPolicy,
        PromotionDiscountKind discountKind,
        decimal percentageRate,
        decimal fixedAmount,
        string? fixedAmountCurrency,
        string? couponCode,
        Guid? offerId,
        Guid? catalogVariantId,
        Guid? categoryId,
        Guid? sellerPartyId,
        string? market,
        string? salesChannel,
        string? currency,
        Guid? customerPartyId,
        Guid? organizationPartyId,
        int? minimumQuantity,
        decimal? minimumSubtotal,
        CancellationToken cancellationToken);

    /// <summary>
    /// پروموشن را فعال می‌کند.
    /// </summary>
    Task ActivateAsync(Guid promotionId, CancellationToken cancellationToken);

    /// <summary>
    /// نام/اولویت را عوض می‌کند بدون دست زدن به تصویر سفارش.
    /// </summary>
    Task ChangeAsync(Guid promotionId, string name, int priority, CancellationToken cancellationToken);

    /// <summary>
    /// پروموشن را منقضی می‌کند.
    /// </summary>
    Task ExpireAsync(Guid promotionId, CancellationToken cancellationToken);

    /// <summary>
    /// فهرست پروموشن‌های متعلق به یک فروشنده.
    /// </summary>
    Task<IReadOnlyList<PromotionReference>> ListBySellerAsync(
        Guid? tenantId,
        Guid sellerPartyId,
        CancellationToken cancellationToken);

    /// <summary>
    /// پروموشن متعلق به فروشنده را برمی‌گرداند؛ در غیر این صورت تهی.
    /// </summary>
    Task<PromotionReference?> GetForSellerAsync(
        Guid? tenantId,
        Guid sellerPartyId,
        Guid promotionId,
        CancellationToken cancellationToken);

    /// <summary>
    /// پروموشن پیش‌نویس فروشنده می‌سازد و SellerPartyId را اجبار می‌کند.
    /// </summary>
    Task<PromotionReference> CreateForSellerAsync(
        Guid? tenantId,
        Guid sellerPartyId,
        string name,
        DateTimeOffset effectiveFrom,
        DateTimeOffset? effectiveTo,
        PromotionDiscountKind discountKind,
        decimal percentageRate,
        decimal fixedAmount,
        string? fixedAmountCurrency,
        string? couponCode,
        decimal? minimumSubtotal,
        CancellationToken cancellationToken);

    /// <summary>
    /// فیلدهای پیش‌نویس/غیرفعال متعلق به فروشنده را به‌روز می‌کند.
    /// </summary>
    Task<PromotionReference> UpdateForSellerAsync(
        Guid? tenantId,
        Guid sellerPartyId,
        Guid promotionId,
        string name,
        DateTimeOffset effectiveFrom,
        DateTimeOffset? effectiveTo,
        PromotionDiscountKind discountKind,
        decimal percentageRate,
        decimal fixedAmount,
        string? fixedAmountCurrency,
        string? couponCode,
        decimal? minimumSubtotal,
        CancellationToken cancellationToken);

    /// <summary>
    /// پروموشن متعلق به فروشنده را فعال می‌کند.
    /// </summary>
    Task ActivateForSellerAsync(
        Guid? tenantId,
        Guid sellerPartyId,
        Guid promotionId,
        CancellationToken cancellationToken);

    /// <summary>
    /// پروموشن متعلق به فروشنده را منقضی/غیرفعال می‌کند.
    /// </summary>
    Task DeactivateForSellerAsync(
        Guid? tenantId,
        Guid sellerPartyId,
        Guid promotionId,
        CancellationToken cancellationToken);

    /// <summary>
    /// فهرست نظارتی ادمین؛ فیلتر اختیاری فروشنده.
    /// </summary>
    Task<IReadOnlyList<PromotionReference>> ListForAdminAsync(
        Guid? tenantId,
        Guid? sellerPartyId,
        CancellationToken cancellationToken);

    /// <summary>
    /// جزئیات نظارتی ادمین.
    /// </summary>
    Task<PromotionReference?> GetForAdminAsync(
        Guid? tenantId,
        Guid promotionId,
        CancellationToken cancellationToken);

    /// <summary>
    /// غیرفعال‌سازی نظارتی ادمین بدون مالکیت فروشنده.
    /// </summary>
    Task DeactivateForAdminAsync(
        Guid? tenantId,
        Guid promotionId,
        CancellationToken cancellationToken);
}
