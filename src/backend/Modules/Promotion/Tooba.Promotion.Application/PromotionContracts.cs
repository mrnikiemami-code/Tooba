using Tooba.Promotion.Domain;

namespace Tooba.Promotion.Application;

/// <summary>
/// مرجع پروموشن برای ادمین/فروشنده/آزمون. قیمت تألیف‌شده نیست.
/// </summary>
public sealed record PromotionReference(
    Guid PromotionId,
    string Name,
    PromotionStatus Status,
    int Priority,
    DateTimeOffset EffectiveFrom,
    DateTimeOffset? EffectiveTo,
    PromotionStackingPolicy StackingPolicy,
    PromotionDiscountKind DiscountKind,
    decimal PercentageRate,
    decimal FixedAmount,
    string? FixedAmountCurrency,
    string? CouponCode,
    Guid? SellerPartyId = null,
    decimal? MinimumSubtotal = null);

/// <summary>
/// ورودی ارزیابی. مبلغ پایه از Pricing می‌آید نه از سبد به‌عنوان حقیقت.
/// </summary>
public sealed record PromotionEvaluationRequest(
    Guid OfferId,
    Guid CatalogVariantId,
    Guid? CategoryId,
    Guid SellerPartyId,
    string Market,
    string SalesChannel,
    string Currency,
    int Quantity,
    decimal BaseTaxExclusiveAmount,
    Guid? CustomerPartyId,
    Guid? OrganizationPartyId,
    string? CouponCode,
    DateTimeOffset At);

/// <summary>
/// یک پروموشن اعمال‌شده در نتیجهٔ ارزیابی.
/// </summary>
public sealed record AppliedPromotion(
    Guid PromotionId,
    string Name,
    string? CouponCode,
    PromotionDiscountKind DiscountKind,
    decimal DiscountAmount);

/// <summary>
/// خروجی ارزیابی. Pricing را mutate نمی‌کند و مالیات حساب نمی‌کند.
/// </summary>
public sealed record PromotionEvaluationResult(
    decimal DiscountAmount,
    decimal PostDiscountTaxExclusiveAmount,
    IReadOnlyList<AppliedPromotion> Applied,
    IReadOnlyList<string> RejectionReasons);

/// <summary>
/// درز نگهبان نوشتن پروموشن. ماتریس نهایی ادمین اینجا نیست.
/// </summary>
public interface IPromotionUseCaseGuard
{
    /// <summary>
    /// اجازهٔ تعریف/فعال‌سازی را بررسی می‌کند.
    /// </summary>
    Task EnsureCanMutateAsync(CancellationToken cancellationToken);
}

/// <summary>
/// ارزیابی قطعی پروموشن روی واقعیت‌های ورودی قرارداد.
/// </summary>
public interface IPromotionEvaluator
{
    /// <summary>
    /// تخفیف را روی مبلغ بدون مالیات خط حساب می‌کند. ارز نامطابق مبلغ ثابت را اعمال نمی‌کند.
    /// </summary>
    Task<PromotionEvaluationResult> EvaluateAsync(PromotionEvaluationRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// درز آیندهٔ سقف مصرف. الان ارزیابی‌only است و سهمیه را قفل نمی‌کند.
/// </summary>
public interface IPromotionRedemptionLedger
{
    /// <summary>
    /// برای foundation فقط موفق بودن مسیر را اعلام می‌کند؛ شمارش همزمان به Task بعدی موکول است.
    /// </summary>
    Task<bool> CanRedeemAsync(Guid promotionId, Guid? customerPartyId, CancellationToken cancellationToken);
}

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
