using Microsoft.EntityFrameworkCore;
using Tooba.Promotion.Application;
using Tooba.Promotion.Domain;
using Tooba.Promotion.Infrastructure.Persistence;

namespace Tooba.Promotion.Infrastructure;

/// <summary>
/// نگهبان باز نوشتن پروموشن. ماتریس SpiceDB اینجا قفل نمی‌شود.
/// </summary>
public sealed class OpenPromotionUseCaseGuard : IPromotionUseCaseGuard
{
    /// <inheritdoc />
    public Task EnsureCanMutateAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

/// <summary>
/// دفتر مصرف آینده. سقف هم‌زمان در این foundation قفل و شمرده نمی‌شود.
/// </summary>
public sealed class DeferredPromotionRedemptionLedger : IPromotionRedemptionLedger
{
    /// <inheritdoc />
    public Task<bool> CanRedeemAsync(Guid promotionId, Guid? customerPartyId, CancellationToken cancellationToken) =>
        Task.FromResult(true);
}

/// <summary>
/// مالک schema promotion: تعریف، فعال‌سازی و ارزیابی قطعی. Pricing را بازنویسی نمی‌کند.
/// </summary>
public sealed class PromotionDirectory : IPromotionDirectory
{
    private readonly PromotionDbContext _db;
    private readonly IPromotionUseCaseGuard _guard;
    private readonly IPromotionRedemptionLedger _ledger;

    /// <summary>
    /// دایرکتوری را به schema promotion وصل می‌کند.
    /// </summary>
    public PromotionDirectory(
        PromotionDbContext db,
        IPromotionUseCaseGuard guard,
        IPromotionRedemptionLedger ledger)
    {
        _db = db;
        _guard = guard;
        _ledger = ledger;
    }

    /// <inheritdoc />
    public async Task<PromotionReference> CreateAsync(
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
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var promotion = PromotionDefinition.Create(
            name,
            priority,
            effectiveFrom,
            effectiveTo,
            stackingPolicy,
            discountKind,
            percentageRate,
            fixedAmount,
            fixedAmountCurrency,
            couponCode,
            offerId,
            catalogVariantId,
            categoryId,
            sellerPartyId,
            market,
            salesChannel,
            currency,
            customerPartyId,
            organizationPartyId,
            minimumQuantity,
            minimumSubtotal,
            DateTimeOffset.UtcNow);
        _db.Promotions.Add(promotion);
        await _db.SaveChangesAsync(cancellationToken);
        return ToReference(promotion);
    }

    /// <inheritdoc />
    public async Task ActivateAsync(Guid promotionId, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var promotion = await _db.Promotions.SingleAsync(x => x.PromotionId == promotionId, cancellationToken);
        promotion.Activate(DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task ChangeAsync(Guid promotionId, string name, int priority, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var promotion = await _db.Promotions.SingleAsync(x => x.PromotionId == promotionId, cancellationToken);
        promotion.Change(name, priority, DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task ExpireAsync(Guid promotionId, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var promotion = await _db.Promotions.SingleAsync(x => x.PromotionId == promotionId, cancellationToken);
        promotion.Expire(DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PromotionReference>> ListBySellerAsync(
        Guid? tenantId,
        Guid sellerPartyId,
        CancellationToken cancellationToken)
    {
        _ = tenantId;
        var rows = await _db.Promotions.AsNoTracking()
            .Where(x => x.SellerPartyId == sellerPartyId)
            .OrderByDescending(x => x.UpdatedAt)
            .ThenBy(x => x.PromotionId)
            .ToListAsync(cancellationToken);
        return rows.Select(ToReference).ToList();
    }

    /// <inheritdoc />
    public async Task<PromotionReference?> GetForSellerAsync(
        Guid? tenantId,
        Guid sellerPartyId,
        Guid promotionId,
        CancellationToken cancellationToken)
    {
        _ = tenantId;
        var promotion = await _db.Promotions.AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.PromotionId == promotionId && x.SellerPartyId == sellerPartyId,
                cancellationToken);
        return promotion is null ? null : ToReference(promotion);
    }

    /// <inheritdoc />
    public async Task<PromotionReference> CreateForSellerAsync(
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
        CancellationToken cancellationToken)
    {
        _ = tenantId;
        if (sellerPartyId == Guid.Empty)
        {
            throw new InvalidOperationException("شناسهٔ فروشنده برای پروموشن لازم است.");
        }

        return await CreateAsync(
            name,
            priority: 100,
            effectiveFrom,
            effectiveTo,
            PromotionStackingPolicy.Exclusive,
            discountKind,
            percentageRate,
            fixedAmount,
            fixedAmountCurrency,
            couponCode,
            offerId: null,
            catalogVariantId: null,
            categoryId: null,
            sellerPartyId,
            market: null,
            salesChannel: null,
            currency: discountKind == PromotionDiscountKind.FixedAmountOff
                ? (fixedAmountCurrency ?? "IRR")
                : null,
            customerPartyId: null,
            organizationPartyId: null,
            minimumQuantity: null,
            minimumSubtotal,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PromotionReference> UpdateForSellerAsync(
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
        CancellationToken cancellationToken)
    {
        _ = tenantId;
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var promotion = await RequireOwnedAsync(sellerPartyId, promotionId, cancellationToken);
        promotion.UpdateEditableFields(
            name,
            effectiveFrom,
            effectiveTo,
            discountKind,
            percentageRate,
            fixedAmount,
            fixedAmountCurrency,
            couponCode,
            minimumSubtotal,
            DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        return ToReference(promotion);
    }

    /// <inheritdoc />
    public async Task ActivateForSellerAsync(
        Guid? tenantId,
        Guid sellerPartyId,
        Guid promotionId,
        CancellationToken cancellationToken)
    {
        _ = tenantId;
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var promotion = await RequireOwnedAsync(sellerPartyId, promotionId, cancellationToken);
        promotion.Activate(DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeactivateForSellerAsync(
        Guid? tenantId,
        Guid sellerPartyId,
        Guid promotionId,
        CancellationToken cancellationToken)
    {
        _ = tenantId;
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var promotion = await RequireOwnedAsync(sellerPartyId, promotionId, cancellationToken);
        promotion.Expire(DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PromotionReference>> ListForAdminAsync(
        Guid? tenantId,
        Guid? sellerPartyId,
        CancellationToken cancellationToken)
    {
        _ = tenantId;
        var query = _db.Promotions.AsNoTracking().AsQueryable();
        if (sellerPartyId is Guid seller && seller != Guid.Empty)
        {
            query = query.Where(x => x.SellerPartyId == seller);
        }

        var rows = await query
            .OrderByDescending(x => x.UpdatedAt)
            .ThenBy(x => x.PromotionId)
            .ToListAsync(cancellationToken);
        return rows.Select(ToReference).ToList();
    }

    /// <inheritdoc />
    public async Task<PromotionReference?> GetForAdminAsync(
        Guid? tenantId,
        Guid promotionId,
        CancellationToken cancellationToken)
    {
        _ = tenantId;
        var promotion = await _db.Promotions.AsNoTracking()
            .SingleOrDefaultAsync(x => x.PromotionId == promotionId, cancellationToken);
        return promotion is null ? null : ToReference(promotion);
    }

    /// <inheritdoc />
    public async Task DeactivateForAdminAsync(
        Guid? tenantId,
        Guid promotionId,
        CancellationToken cancellationToken)
    {
        _ = tenantId;
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var promotion = await _db.Promotions.SingleOrDefaultAsync(
            x => x.PromotionId == promotionId,
            cancellationToken)
            ?? throw new InvalidOperationException("پروموشن یافت نشد.");
        promotion.Expire(DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PromotionEvaluationResult> EvaluateAsync(
        PromotionEvaluationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var rejections = new List<string>();
        if (request.Quantity <= 0 || request.BaseTaxExclusiveAmount < 0 || string.IsNullOrWhiteSpace(request.Currency))
        {
            return new PromotionEvaluationResult(0m, request.BaseTaxExclusiveAmount, [], ["INPUT_INVALID"]);
        }

        var candidates = await _db.Promotions.AsNoTracking().ToListAsync(cancellationToken);
        var facts = new PromotionEligibilityFacts(
            request.OfferId,
            request.CatalogVariantId,
            request.CategoryId,
            request.SellerPartyId,
            request.Market,
            request.SalesChannel,
            request.Currency,
            request.CustomerPartyId,
            request.OrganizationPartyId,
            request.Quantity,
            request.BaseTaxExclusiveAmount,
            request.CouponCode);

        var matching = candidates
            .Where(x => x.IsEffectiveAt(request.At) && x.IsEligible(facts))
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.PromotionId)
            .ToList();

        if (matching.Count == 0)
        {
            if (!string.IsNullOrWhiteSpace(request.CouponCode))
            {
                rejections.Add("COUPON_NOT_APPLICABLE");
            }

            return new PromotionEvaluationResult(0m, request.BaseTaxExclusiveAmount, [], rejections);
        }

        IReadOnlyList<PromotionDefinition> selected;
        var exclusive = matching.Where(x => x.StackingPolicy == PromotionStackingPolicy.Exclusive).ToList();
        if (exclusive.Count > 0)
        {
            selected = [exclusive[0]];
        }
        else
        {
            selected = matching.Where(x => x.StackingPolicy == PromotionStackingPolicy.Stackable).ToList();
        }

        var remaining = request.BaseTaxExclusiveAmount;
        var applied = new List<AppliedPromotion>();
        foreach (var promotion in selected)
        {
            if (!await _ledger.CanRedeemAsync(promotion.PromotionId, request.CustomerPartyId, cancellationToken))
            {
                rejections.Add("REDEMPTION_BLOCKED");
                continue;
            }

            var discount = promotion.ComputeDiscount(remaining, request.Currency);
            if (discount <= 0)
            {
                if (promotion.DiscountKind == PromotionDiscountKind.FixedAmountOff)
                {
                    rejections.Add("CURRENCY_MISMATCH");
                }

                continue;
            }

            remaining -= discount;
            applied.Add(new AppliedPromotion(
                promotion.PromotionId,
                promotion.Name,
                promotion.CouponCode,
                promotion.DiscountKind,
                discount));
        }

        var totalDiscount = PromotionRounding.Round(request.BaseTaxExclusiveAmount - remaining, request.Currency);
        var post = PromotionRounding.Round(remaining, request.Currency);
        return new PromotionEvaluationResult(totalDiscount, post, applied, rejections);
    }

    private async Task<PromotionDefinition> RequireOwnedAsync(
        Guid sellerPartyId,
        Guid promotionId,
        CancellationToken cancellationToken)
    {
        var promotion = await _db.Promotions.SingleOrDefaultAsync(
            x => x.PromotionId == promotionId && x.SellerPartyId == sellerPartyId,
            cancellationToken);
        if (promotion is null)
        {
            throw new InvalidOperationException("پروموشن متعلق به این فروشنده نیست یا یافت نشد.");
        }

        return promotion;
    }

    private static PromotionReference ToReference(PromotionDefinition promotion) =>
        new(
            promotion.PromotionId,
            promotion.Name,
            promotion.Status,
            promotion.Priority,
            promotion.EffectiveFrom,
            promotion.EffectiveTo,
            promotion.StackingPolicy,
            promotion.DiscountKind,
            promotion.PercentageRate,
            promotion.FixedAmount,
            promotion.FixedAmountCurrency,
            promotion.CouponCode,
            promotion.SellerPartyId,
            promotion.MinimumSubtotal);
}
