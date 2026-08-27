using Tooba.Promotion.Application;
using Tooba.Promotion.Domain;

namespace Tooba.Host.Promotion;

/// <summary>
/// ترکیب نمایشی پنل فروشنده/ادمین روی قرارداد Promotion. مالک دامنه نیست.
/// </summary>
public sealed class PromotionPanelComposer
{
    private readonly IPromotionDirectory _promotions;

    /// <summary>
    /// سازندهٔ ترکیب پروموشن پنل.
    /// </summary>
    public PromotionPanelComposer(IPromotionDirectory promotions) => _promotions = promotions;

    /// <summary>فهرست فروشنده.</summary>
    public Task<IReadOnlyList<PromotionReference>> SellerListAsync(
        Guid sellerPartyId,
        CancellationToken cancellationToken) =>
        _promotions.ListBySellerAsync(null, sellerPartyId, cancellationToken);

    /// <summary>جزئیات فروشنده.</summary>
    public Task<PromotionReference?> SellerGetAsync(
        Guid sellerPartyId,
        Guid promotionId,
        CancellationToken cancellationToken) =>
        _promotions.GetForSellerAsync(null, sellerPartyId, promotionId, cancellationToken);

    /// <summary>ایجاد فروشنده.</summary>
    public Task<PromotionReference> SellerCreateAsync(
        Guid sellerPartyId,
        UpsertSellerPromotionBody body,
        CancellationToken cancellationToken)
    {
        var parsed = ParseBody(body);
        return _promotions.CreateForSellerAsync(
            null,
            sellerPartyId,
            parsed.Name,
            parsed.EffectiveFrom,
            parsed.EffectiveTo,
            parsed.DiscountKind,
            parsed.PercentageRate,
            parsed.FixedAmount,
            parsed.FixedAmountCurrency,
            parsed.CouponCode,
            parsed.MinimumSubtotal,
            cancellationToken);
    }

    /// <summary>به‌روزرسانی پیش‌نویس/منقضی فروشنده.</summary>
    public Task<PromotionReference> SellerUpdateAsync(
        Guid sellerPartyId,
        Guid promotionId,
        UpsertSellerPromotionBody body,
        CancellationToken cancellationToken)
    {
        var parsed = ParseBody(body);
        return _promotions.UpdateForSellerAsync(
            null,
            sellerPartyId,
            promotionId,
            parsed.Name,
            parsed.EffectiveFrom,
            parsed.EffectiveTo,
            parsed.DiscountKind,
            parsed.PercentageRate,
            parsed.FixedAmount,
            parsed.FixedAmountCurrency,
            parsed.CouponCode,
            parsed.MinimumSubtotal,
            cancellationToken);
    }

    /// <summary>فعال‌سازی فروشنده.</summary>
    public Task SellerActivateAsync(
        Guid sellerPartyId,
        Guid promotionId,
        CancellationToken cancellationToken) =>
        _promotions.ActivateForSellerAsync(null, sellerPartyId, promotionId, cancellationToken);

    /// <summary>غیرفعال‌سازی فروشنده.</summary>
    public Task SellerDeactivateAsync(
        Guid sellerPartyId,
        Guid promotionId,
        CancellationToken cancellationToken) =>
        _promotions.DeactivateForSellerAsync(null, sellerPartyId, promotionId, cancellationToken);

    /// <summary>فهرست نظارتی ادمین.</summary>
    public Task<IReadOnlyList<PromotionReference>> AdminListAsync(
        Guid? sellerPartyId,
        CancellationToken cancellationToken) =>
        _promotions.ListForAdminAsync(null, sellerPartyId, cancellationToken);

    /// <summary>جزئیات نظارتی ادمین.</summary>
    public Task<PromotionReference?> AdminGetAsync(
        Guid promotionId,
        CancellationToken cancellationToken) =>
        _promotions.GetForAdminAsync(null, promotionId, cancellationToken);

    /// <summary>غیرفعال‌سازی نظارتی ادمین.</summary>
    public Task AdminDeactivateAsync(
        Guid promotionId,
        CancellationToken cancellationToken) =>
        _promotions.DeactivateForAdminAsync(null, promotionId, cancellationToken);

    private static ParsedPromotionBody ParseBody(UpsertSellerPromotionBody body)
    {
        ArgumentNullException.ThrowIfNull(body);
        if (string.IsNullOrWhiteSpace(body.Name))
        {
            throw new InvalidOperationException("نام پروموشن خالی نیست.");
        }

        if (string.IsNullOrWhiteSpace(body.CouponCode))
        {
            throw new InvalidOperationException("کد کوپن برای پروموشن فروشنده لازم است.");
        }

        var kind = string.Equals(body.DiscountKind, "FixedAmountOff", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(body.DiscountKind, "fixed", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(body.DiscountKind, "تومان", StringComparison.Ordinal)
            ? PromotionDiscountKind.FixedAmountOff
            : PromotionDiscountKind.PercentageOff;

        decimal percentageRate;
        decimal fixedAmount;
        string? fixedCurrency;
        if (kind == PromotionDiscountKind.PercentageOff)
        {
            // UI may send 20 for 20% or 0.20 as fraction.
            var raw = body.DiscountValue;
            percentageRate = raw > 1m ? raw / 100m : raw;
            fixedAmount = 0m;
            fixedCurrency = null;
        }
        else
        {
            percentageRate = 0m;
            fixedAmount = body.DiscountValue;
            fixedCurrency = string.IsNullOrWhiteSpace(body.Currency) ? "IRR" : body.Currency.Trim().ToUpperInvariant();
        }

        var from = body.EffectiveFrom ?? DateTimeOffset.UtcNow;
        return new ParsedPromotionBody(
            body.Name.Trim(),
            from,
            body.EffectiveTo,
            kind,
            percentageRate,
            fixedAmount,
            fixedCurrency,
            body.CouponCode.Trim(),
            body.MinimumSubtotal);
    }

    private sealed record ParsedPromotionBody(
        string Name,
        DateTimeOffset EffectiveFrom,
        DateTimeOffset? EffectiveTo,
        PromotionDiscountKind DiscountKind,
        decimal PercentageRate,
        decimal FixedAmount,
        string? FixedAmountCurrency,
        string CouponCode,
        decimal? MinimumSubtotal);
}

/// <summary>بدنهٔ ایجاد/ویرایش پروموشن فروشنده.</summary>
public sealed record UpsertSellerPromotionBody(
    string Name,
    string CouponCode,
    string DiscountKind,
    decimal DiscountValue,
    DateTimeOffset? EffectiveFrom,
    DateTimeOffset? EffectiveTo,
    string? Currency = null,
    decimal? MinimumSubtotal = null);
