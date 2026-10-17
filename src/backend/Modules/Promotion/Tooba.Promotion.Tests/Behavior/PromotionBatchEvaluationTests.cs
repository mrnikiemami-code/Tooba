using Tooba.Promotion.Application.Promotions;
using Tooba.Promotion.Application.Promotions.Ports;
using Tooba.Promotion.Domain.Aggregates;
using Tooba.Promotion.Domain.ValueObjects;
using Xunit;

namespace Tooba.Promotion.Tests.Behavior;

/// <summary>
/// TB-STOREFRONT-PERF-PROMOTION-BATCH-184 — behavior lock for the owner-side batch read.
/// The Home storefront used to trigger one <c>promotion.promotions</c> read per composed card. The owner now
/// reads the candidate set once per request scope and evaluates every card against that same set, so these
/// tests pin (a) the decision table is unchanged, (b) one load serves many evaluations, and (c) a mutation in
/// the same scope invalidates the snapshot instead of serving stale definitions.
/// </summary>
public sealed class PromotionBatchEvaluationTests
{
    private static readonly Guid Seller = Guid.Parse("01a030d1-40cb-7000-8abe-6d31739956c5");
    private static readonly Guid OtherSeller = Guid.Parse("01a030d1-40cb-7000-9999-6d31739956c5");
    private static readonly DateTimeOffset Now = new(2026, 10, 17, 12, 0, 0, TimeSpan.Zero);

    private static PromotionDefinition Promotion(
        Guid? id = null,
        int priority = 100,
        PromotionStackingPolicy stacking = PromotionStackingPolicy.Exclusive,
        PromotionDiscountKind kind = PromotionDiscountKind.PercentageOff,
        decimal percentageRate = 0.1m,
        decimal fixedAmount = 0m,
        Guid? sellerPartyId = null,
        DateTimeOffset? effectiveFrom = null,
        DateTimeOffset? effectiveTo = null,
        string? currency = null,
        bool active = true)
    {
        var promotion = PromotionDefinition.Create(
            id ?? Guid.NewGuid(),
            "promo",
            priority,
            effectiveFrom ?? Now.AddDays(-1),
            effectiveTo,
            stacking,
            kind,
            percentageRate,
            fixedAmount,
            kind == PromotionDiscountKind.FixedAmountOff ? currency ?? "IRR" : null,
            couponCode: null,
            offerId: null,
            catalogVariantId: null,
            categoryId: null,
            sellerPartyId ?? Seller,
            market: null,
            salesChannel: null,
            currency: null,
            customerPartyId: null,
            organizationPartyId: null,
            minimumQuantity: null,
            minimumSubtotal: null,
            Now.AddDays(-2));
        if (active)
        {
            promotion.Activate(Now.AddDays(-1));
        }

        return promotion;
    }

    private static PromotionEvaluationRequest Request(
        decimal amount = 1_000_000m,
        string currency = "IRR",
        Guid? seller = null,
        DateTimeOffset? at = null) =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            seller ?? Seller,
            "IR",
            "Marketplace",
            currency,
            Quantity: 1m,
            amount,
            CustomerPartyId: null,
            OrganizationPartyId: null,
            CouponCode: null,
            at ?? Now);

    private static Task<bool> Redeemable(Guid _, CancellationToken __) => Task.FromResult(true);

    private static Task<bool> Blocked(Guid _, CancellationToken __) => Task.FromResult(false);

    [Fact]
    public async Task Empty_promotion_set_yields_no_discount_and_keeps_the_base_amount()
    {
        var result = await PromotionDecisionEngine.EvaluateAsync([], Request(), Redeemable, default);

        Assert.Equal(0m, result.DiscountAmount);
        Assert.Equal(1_000_000m, result.PostDiscountTaxExclusiveAmount);
        Assert.Empty(result.Applied);
        Assert.Empty(result.RejectionReasons);
    }

    [Fact]
    public async Task Applicable_percentage_promotion_discounts_once()
    {
        var result = await PromotionDecisionEngine.EvaluateAsync([Promotion()], Request(), Redeemable, default);

        Assert.Equal(100_000m, result.DiscountAmount);
        Assert.Equal(900_000m, result.PostDiscountTaxExclusiveAmount);
        Assert.Single(result.Applied);
    }

    [Fact]
    public async Task Promotion_scoped_to_another_seller_is_not_applied()
    {
        var result = await PromotionDecisionEngine.EvaluateAsync(
            [Promotion(sellerPartyId: OtherSeller)], Request(), Redeemable, default);

        Assert.Equal(0m, result.DiscountAmount);
        Assert.Empty(result.Applied);
    }

    [Fact]
    public async Task Expired_and_not_yet_effective_promotions_are_not_applied()
    {
        var expired = Promotion(effectiveFrom: Now.AddDays(-5), effectiveTo: Now.AddDays(-1));
        var future = Promotion(effectiveFrom: Now.AddDays(1));

        var result = await PromotionDecisionEngine.EvaluateAsync([expired, future], Request(), Redeemable, default);

        Assert.Equal(0m, result.DiscountAmount);
        Assert.Empty(result.Applied);
    }

    [Fact]
    public async Task Effective_window_is_inclusive_from_and_exclusive_to()
    {
        var atStart = Promotion(effectiveFrom: Now, effectiveTo: Now.AddDays(1));
        var atEnd = Promotion(effectiveFrom: Now.AddDays(-1), effectiveTo: Now);

        var startResult = await PromotionDecisionEngine.EvaluateAsync([atStart], Request(at: Now), Redeemable, default);
        var endResult = await PromotionDecisionEngine.EvaluateAsync([atEnd], Request(at: Now), Redeemable, default);

        Assert.Equal(100_000m, startResult.DiscountAmount);
        Assert.Equal(0m, endResult.DiscountAmount);
    }

    [Fact]
    public async Task Highest_priority_exclusive_promotion_wins_over_a_lower_priority_one()
    {
        var low = Promotion(priority: 10, percentageRate: 0.5m);
        var high = Promotion(priority: 500, percentageRate: 0.1m);

        var result = await PromotionDecisionEngine.EvaluateAsync([low, high], Request(), Redeemable, default);

        Assert.Single(result.Applied);
        Assert.Equal(100_000m, result.DiscountAmount);
    }

    [Fact]
    public async Task Stackable_promotions_apply_in_priority_order_on_the_remaining_amount()
    {
        var first = Promotion(priority: 500, stacking: PromotionStackingPolicy.Stackable, percentageRate: 0.1m);
        var second = Promotion(priority: 100, stacking: PromotionStackingPolicy.Stackable, percentageRate: 0.1m);

        var result = await PromotionDecisionEngine.EvaluateAsync([second, first], Request(), Redeemable, default);

        Assert.Equal(2, result.Applied.Count);
        Assert.Equal(190_000m, result.DiscountAmount);
        Assert.Equal(810_000m, result.PostDiscountTaxExclusiveAmount);
    }

    [Fact]
    public async Task Fixed_amount_promotion_in_a_foreign_currency_is_rejected_without_zeroing_a_discount()
    {
        var foreign = Promotion(
            kind: PromotionDiscountKind.FixedAmountOff,
            percentageRate: 0m,
            fixedAmount: 50_000m,
            currency: "USD");

        var result = await PromotionDecisionEngine.EvaluateAsync([foreign], Request(), Redeemable, default);

        Assert.Equal(0m, result.DiscountAmount);
        Assert.Contains("CURRENCY_MISMATCH", result.RejectionReasons);
    }

    [Fact]
    public async Task Blocked_redemption_is_reported_and_no_discount_is_applied()
    {
        var result = await PromotionDecisionEngine.EvaluateAsync([Promotion()], Request(), Blocked, default);

        Assert.Equal(0m, result.DiscountAmount);
        Assert.Contains("REDEMPTION_BLOCKED", result.RejectionReasons);
    }

    [Theory]
    [InlineData(0, 1_000_000)]
    [InlineData(-1, 1_000_000)]
    [InlineData(1, -1)]
    public async Task Invalid_input_fails_closed(decimal quantity, decimal amount)
    {
        var request = Request(amount: amount) with { Quantity = quantity };

        var result = await PromotionDecisionEngine.EvaluateAsync([Promotion()], request, Redeemable, default);

        Assert.Equal(0m, result.DiscountAmount);
        Assert.Contains("INPUT_INVALID", result.RejectionReasons);
    }

    [Fact]
    public async Task One_snapshot_load_serves_many_card_evaluations_in_the_same_scope()
    {
        var loads = 0;
        var snapshot = new PromotionCandidateSnapshot(_ =>
        {
            loads++;
            return Task.FromResult<IReadOnlyList<PromotionDefinition>>([Promotion()]);
        });

        for (var card = 0; card < 12; card++)
        {
            var candidates = await snapshot.GetAsync(default);
            var result = await PromotionDecisionEngine.EvaluateAsync(candidates, Request(), Redeemable, default);
            Assert.Equal(100_000m, result.DiscountAmount);
        }

        Assert.Equal(1, loads);
        Assert.Equal(1, snapshot.LoadCount);
    }

    [Fact]
    public async Task Mutation_in_the_same_scope_invalidates_the_snapshot()
    {
        var loads = 0;
        var rows = new List<PromotionDefinition>([Promotion()]);
        var snapshot = new PromotionCandidateSnapshot(_ =>
        {
            loads++;
            return Task.FromResult<IReadOnlyList<PromotionDefinition>>(rows.ToList());
        });

        await snapshot.GetAsync(default);
        Assert.Equal(1, loads);

        rows.Clear();
        snapshot.Invalidate();

        var refreshed = await snapshot.GetAsync(default);
        Assert.Equal(2, loads);
        Assert.Empty(refreshed);

        var result = await PromotionDecisionEngine.EvaluateAsync(refreshed, Request(), Redeemable, default);
        Assert.Equal(0m, result.DiscountAmount);
    }
}
