using Tooba.Promotion.Domain;

namespace Tooba.Promotion.Application;

/// <summary>
/// مرجع پروموشن برای ادمین/آزمون. قیمت تألیف‌شده نیست.
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
    string? CouponCode);

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
}
