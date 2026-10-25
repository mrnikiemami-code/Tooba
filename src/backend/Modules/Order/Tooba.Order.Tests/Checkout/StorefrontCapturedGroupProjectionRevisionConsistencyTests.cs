using Tooba.BuildingBlocks;
using Tooba.Geography.Contracts.Places;
using Tooba.Order.Application.Storefront.Shipping.CapturedGroupProjection;
using Tooba.Shipping.Contracts.Authoring;
using Tooba.Shipping.Contracts.Packaging;
using Tooba.Shipping.Contracts.Rates;
using Xunit;

namespace Tooba.Order.Tests.Checkout;

/// <summary>
/// TB-218-R1: یک بازبینی capture‌شدهٔ انتخاب‌شده برای هر «مالک + سرویس/گزینه» در کل درخواست؛ دو گروه
/// نمی‌توانند دو بازبینی متفاوت از یک تعهد را هم‌زمان طلب کنند، ولی همان تعهد با همان بازبینی دقیق در
/// دو گروه مجاز است و هر گروه فکت/قصد خودش را نگه می‌دارد.
/// </summary>
[Collection("ShippingGroupGoodsFacts")]
public sealed class StorefrontCapturedGroupProjectionRevisionConsistencyTests
{
    private static readonly Guid LineA = Guid.Parse("0199f111-0000-7000-8000-00000000000a");
    private static readonly Guid LineB = Guid.Parse("0199f111-0000-7000-8000-00000000000b");
    private static readonly Guid OfferA = Guid.Parse("0199f222-0000-7000-8000-00000000000a");
    private static readonly Guid OfferB = Guid.Parse("0199f222-0000-7000-8000-00000000000b");
    private static readonly Guid DraftA = Guid.Parse("0199f333-0000-7000-8000-00000000000a");
    private static readonly Guid DraftB = Guid.Parse("0199f333-0000-7000-8000-00000000000b");
    private static readonly Guid CountryId = Guid.Parse("0199f444-0000-7000-8000-000000000001");
    private static readonly Guid CityD = Guid.Parse("0199f444-0000-7000-8000-00000000000d");
    private static readonly Guid CityE = Guid.Parse("0199f444-0000-7000-8000-00000000000e");

    /// <summary>
    /// همان مالک/سرویس/گزینه با بازبینی ۱ در گروه یک و بازبینی ۲ در گروه دو: پیش از هر خواندن سبد یا
    /// فراخوان Shipping رد می‌شود.
    /// </summary>
    [Fact]
    public async Task Same_owner_and_binding_with_two_revisions_across_groups_is_rejected_before_any_call()
    {
        using var harness = new TwoGroupHarness();
        var shipping = new RecordingCapturedQuoteLookup();
        var composer = Create(harness, shipping);

        var error = await Assert.ThrowsAsync<ContractOperationException>(() => composer.ComposeAsync(
            new StorefrontCapturedGroupProjectionRequest(
                harness.Command(),
                [
                    Plan("g1", [LineA], Candidate(DraftA, revision: 1, cityId: CityD)),
                    Plan("g2", [LineB], Candidate(DraftA, revision: 2, cityId: CityD)),
                ]),
            CancellationToken.None));

        Assert.Equal(StorefrontCapturedGroupProjectionReasons.PlanInvalid, error.Code);
        Assert.Equal(0, shipping.Calls);
        Assert.Equal(0, harness.Carts.Calls);
    }

    /// <summary>
    /// همان تعارض با تغییر بزرگی/کوچکی حروف سرویس/گزینه و مقصد متفاوت گروه‌ها هم بازبینی دوم را مجاز
    /// نمی‌کند: مقصد گروه و برچسب روش هویت انتشار نیستند.
    /// </summary>
    [Fact]
    public async Task Case_variation_and_different_group_destinations_do_not_authorize_a_second_revision()
    {
        using var harness = new TwoGroupHarness();
        var shipping = new RecordingCapturedQuoteLookup();
        var composer = Create(harness, shipping);

        var error = await Assert.ThrowsAsync<ContractOperationException>(() => composer.ComposeAsync(
            new StorefrontCapturedGroupProjectionRequest(
                harness.Command(),
                [
                    Plan("g1", [LineA],
                        Candidate(DraftA, revision: 1, cityId: CityD, methodCode: "express",
                            serviceCode: "Express", optionCode: "STD")),
                    Plan("g2", [LineB],
                        Candidate(DraftA, revision: 3, cityId: CityE, methodCode: "express-2",
                            serviceCode: " express ", optionCode: "std")),
                ]),
            CancellationToken.None));

        Assert.Equal(StorefrontCapturedGroupProjectionReasons.PlanInvalid, error.Code);
        Assert.Equal(0, shipping.Calls);
        Assert.Equal(0, harness.Carts.Calls);
    }

    /// <summary>همان تعهد با بازبینی یکسان ولی پیش‌نویس دیگر هم پذیرفته نمی‌شود.</summary>
    [Fact]
    public async Task Same_binding_different_draft_at_same_revision_across_groups_is_rejected()
    {
        using var harness = new TwoGroupHarness();
        var shipping = new RecordingCapturedQuoteLookup();
        var composer = Create(harness, shipping);

        var error = await Assert.ThrowsAsync<ContractOperationException>(() => composer.ComposeAsync(
            new StorefrontCapturedGroupProjectionRequest(
                harness.Command(),
                [
                    Plan("g1", [LineA], Candidate(DraftA, revision: 2)),
                    Plan("g2", [LineB], Candidate(DraftB, revision: 2)),
                ]),
            CancellationToken.None));

        Assert.Equal(StorefrontCapturedGroupProjectionReasons.PlanInvalid, error.Code);
        Assert.Equal(0, shipping.Calls);
        Assert.Equal(0, harness.Carts.Calls);
    }

    /// <summary>
    /// کنترل مثبت: همان تعهد با همان پیش‌نویس و همان بازبینی در دو گروه مجاز است؛ هر گروه فکت واقعی
    /// خودش (۹۹ و ۱۹۸) را جدا می‌گیرد و هر نامزد یک فراخوان مستقل دارد — هیچ ادغامی رخ نمی‌دهد.
    /// </summary>
    [Fact]
    public async Task Same_binding_and_revision_across_groups_is_lawful_and_keeps_per_group_facts()
    {
        using var harness = new TwoGroupHarness();
        var shipping = new RecordingCapturedQuoteLookup();
        var composer = Create(harness, shipping);
        var week = Week("UTC", (1, 9, 0, 17, 0), (2, 9, 0, 17, 0));
        shipping.Respond = request => Quoted(request, freight: 1_000m, week: week);

        var result = await composer.ComposeAsync(
            new StorefrontCapturedGroupProjectionRequest(
                harness.Command(),
                [
                    Plan("g1", [LineA], Candidate(DraftA, revision: 4, cityId: CityD)),
                    Plan("g2", [LineB], Candidate(DraftA, revision: 4, cityId: CityD)),
                ]),
            CancellationToken.None);

        Assert.Equal(2, result.Groups.Count);
        Assert.Equal(99m, result.Groups[0].FinalGoodsAmountAfterDiscountAndTax);
        Assert.Equal(198m, result.Groups[1].FinalGoodsAmountAfterDiscountAndTax);
        Assert.Equal(99m, shipping.Requests[0].FinalGroupGoodsAmountAfterDiscountAndTax);
        Assert.Equal(198m, shipping.Requests[1].FinalGroupGoodsAmountAfterDiscountAndTax);
        Assert.Equal(2, shipping.Calls);
        Assert.All(
            result.Groups,
            group =>
            {
                var candidate = Assert.Single(group.Candidates);
                Assert.Equal(StorefrontCapturedCandidateOutcome.Quoted, candidate.Outcome);
                Assert.Equal(DraftA, candidate.OwnerResult!.DraftId);
                Assert.Equal(4, candidate.OwnerResult.SourceRevision);
                Assert.NotNull(candidate.Receiving);
            });
        Assert.Equal(1, harness.Carts.Calls);
    }

    /// <summary>
    /// مالک متفاوت یا گزینهٔ متفاوت، تعهد مستقل است و می‌تواند بازبینی مستقل داشته باشد.
    /// </summary>
    [Fact]
    public async Task Independent_owner_or_option_binding_keeps_its_own_revision()
    {
        using var harness = new TwoGroupHarness();
        var shipping = new RecordingCapturedQuoteLookup();
        var composer = Create(harness, shipping);
        var week = Week("UTC", (1, 9, 0, 17, 0));
        shipping.Respond = request => Quoted(request, freight: 1_000m, week: week);

        var differentOwner = await composer.ComposeAsync(
            new StorefrontCapturedGroupProjectionRequest(
                harness.Command(),
                [
                    Plan("g1", [LineA], Candidate(DraftA, revision: 1, tenantId: "tenant-a")),
                    Plan("g2", [LineB], Candidate(DraftA, revision: 9, tenantId: "tenant-b")),
                ]),
            CancellationToken.None);
        Assert.Equal(2, differentOwner.Groups.Count);
        Assert.All(
            differentOwner.Groups,
            group => Assert.Equal(StorefrontCapturedCandidateOutcome.Quoted, Assert.Single(group.Candidates).Outcome));

        // گزینهٔ دیگر با همان مالک: تعهد مستقل با بازبینی مستقل.
        shipping.Respond = request => request.DraftId == DraftA
            ? Quoted(request, freight: 1_000m, week: week, optionCode: "std")
            : Quoted(request, freight: 2_000m, week: week, optionCode: "fast");

        var differentOption = await composer.ComposeAsync(
            new StorefrontCapturedGroupProjectionRequest(
                harness.Command(),
                [
                    Plan("g1", [LineA], Candidate(DraftA, revision: 1, optionCode: "std")),
                    Plan("g2", [LineB], Candidate(DraftB, revision: 9, optionCode: "fast")),
                ]),
            CancellationToken.None);
        Assert.Equal(2, differentOption.Groups.Count);
        Assert.All(
            differentOption.Groups,
            group => Assert.Equal(StorefrontCapturedCandidateOutcome.Quoted, Assert.Single(group.Candidates).Outcome));
    }

    private static StorefrontCapturedGroupProjectionComposer Create(
        TwoGroupHarness harness,
        RecordingCapturedQuoteLookup shipping) =>
        new(harness.Directory, shipping, new MutableClock(new DateTimeOffset(2026, 10, 24, 8, 0, 0, TimeSpan.Zero)));

    private static StorefrontCapturedGroupPlan Plan(
        string groupKey,
        IReadOnlyList<Guid> cartLineIds,
        params StorefrontCapturedCandidatePlan[] candidates) =>
        new(groupKey, cartLineIds, candidates);

    private static StorefrontCapturedCandidatePlan Candidate(
        Guid draftId,
        long revision,
        Guid? cityId = null,
        string methodCode = "express",
        string serviceCode = "express",
        string? optionCode = "std",
        string tenantId = "tenant-a") =>
        new(
            methodCode,
            ShippingCapturedQuoteOwnerKind.Store,
            tenantId,
            null,
            draftId,
            revision,
            new DestinationIdentifiers(CountryId, AdministrativeAreaId: null, CityId: cityId ?? CityD),
            serviceCode,
            optionCode,
            AvailableDateCount: 2);

    private static ShippingCapturedQuoteResult Quoted(
        ShippingCapturedQuoteLookupRequest request,
        decimal freight,
        ShippingCapturedReceivingWeek week,
        string serviceCode = "express",
        string? optionCode = "std")
    {
        var total = freight;
        return new ShippingCapturedQuoteResult(
            ShippingCapturedQuoteVerdict.Quoted,
            ShippingCapturedQuoteVerdictCodes.Quoted,
            serviceCode,
            optionCode,
            request.CurrencyCode,
            freight,
            0m,
            total,
            total,
            0m,
            new ShippingCapturedQuoteSettings(
                ShippingFreightPayerMode.Prepaid,
                ShippingAuthoringCalculationMode.Manual),
            SnapshotId: Guid.NewGuid(),
            DraftId: request.DraftId,
            SourceRevision: request.SourceRevision,
            FormatVersion: 1,
            ShippingTypeId: Guid.Parse("0199f777-0000-7000-8000-000000000001"),
            GroupId: Guid.Parse("0199f777-0000-7000-8000-000000000002"),
            RateRowId: Guid.Parse("0199f777-0000-7000-8000-000000000003"),
            MinDeliveryDays: 1,
            MaxDeliveryDays: 3,
            ReceivingWeek: week);
    }

    private static ShippingCapturedReceivingWeek Week(
        string timeZoneId,
        params (int Weekday, int OpenHour, int OpenMinute, int CloseHour, int CloseMinute)[] working)
    {
        var open = working.ToDictionary(x => x.Weekday);
        var days = Enumerable.Range(1, 7)
            .Select(iso => open.TryGetValue(iso, out var span)
                ? new ShippingCapturedReceivingWeekDay(
                    iso,
                    true,
                    new TimeOnly(span.OpenHour, span.OpenMinute),
                    new TimeOnly(span.CloseHour, span.CloseMinute))
                : new ShippingCapturedReceivingWeekDay(iso, false, null, null))
            .ToArray();
        return new ShippingCapturedReceivingWeek(timeZoneId, days);
    }

    /// <summary>سبد دو خطی با تخفیف ۱۰ و ۲۰ که به ۹۹ و ۱۹۸ پس از مالیات می‌رسد.</summary>
    private sealed class TwoGroupHarness : IDisposable
    {
        internal readonly ShippingGroupGoodsFactsHarness Inner = new();

        internal FakeCartGateway Carts => Inner.Carts;

        internal Tooba.Order.Infrastructure.Checkout.Persistence.CheckoutDirectory Directory => Inner.Directory;

        internal TwoGroupHarness()
        {
            Inner.Carts.Cart = Inner.Cart(
                Inner.Line(LineA, OfferA, 100m),
                Inner.Line(LineB, OfferB, 200m));
            Inner.Promotions.Discounts[OfferA] = 10m;
            Inner.Promotions.Discounts[OfferB] = 20m;
        }

        internal Tooba.Order.Application.Checkout.Contracts.SubmitCheckoutCommand Command() => Inner.Command();

        public void Dispose() => Inner.Dispose();
    }
}
