using Tooba.BuildingBlocks;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Order.Domain.Checkout;
using Xunit;

namespace Tooba.Order.Tests.Checkout;

/// <summary>
/// TB-SHIPPING-COMPACT-GROUP-ORDER-EVIDENCE-231 — شاهد فشردهٔ هر گروه ارسال روی مسیر موجود
/// پیش‌نویس ← پلن ← مادی‌سازی قطعی. این حقایق بدون پایگاه‌داده اجرا می‌شوند؛ اثبات رابطه‌ای در
/// <c>ShippingGroupAcceptedEvidenceStorageTests</c> است.
/// </summary>
public sealed class ShippingGroupAcceptedEvidenceTests
{
    internal static readonly DateTimeOffset Now = new(2026, 10, 27, 9, 0, 0, TimeSpan.Zero);
    internal static readonly Guid Snapshot = Guid.Parse("01a17d0e-0419-7000-b575-f507d2f89abc");
    internal static readonly Guid Draft = Guid.Parse("01a17d0d-e033-7000-af43-49a1daa28822");
    internal static readonly Guid Cart = Guid.Parse("01a17cfd-c1b1-7000-b10d-8cf21258a1cd");
    internal static readonly Guid SellerA = Guid.Parse("01a030d1-40cb-7000-8abe-6d31739956c5");
    internal static readonly Guid SellerB = Guid.Parse("01a030d1-40cb-7000-8abe-6d31739956c6");
    internal static readonly Guid CartLineA = Guid.Parse("01a17cfe-0000-7000-9000-0000000000a1");
    internal static readonly Guid CartLineB = Guid.Parse("01a17cfe-0000-7000-9000-0000000000b2");
    internal static readonly Guid AreaA = Guid.Parse("01a17900-0000-7000-9000-0000000000a1");
    internal static readonly Guid AreaB = Guid.Parse("01a17900-0000-7000-9000-0000000000b2");
    internal static readonly Guid RowA = Guid.Parse("01a17901-0000-7000-9000-0000000000a1");
    internal static readonly Guid RowB = Guid.Parse("01a17901-0000-7000-9000-0000000000b2");
    internal static readonly Guid TypeId = Guid.Parse("01a1785e-01f3-7000-b50d-d6372e756f89");

    [Fact]
    public void Two_groups_sharing_one_snapshot_keep_distinct_accepted_facts_through_materialization()
    {
        var groups = PlannerGroups();
        var evidenceA = Evidence(
            AreaA, RowA, freight: 120_000m, packaging: 5_000m,
            start: new DateOnly(2026, 10, 29), end: new DateOnly(2026, 11, 5),
            payable: groups[0].ShippingAmount, selection: 3);
        var evidenceB = Evidence(
            AreaB, RowB, freight: 80_000m, packaging: 10_000m,
            start: new DateOnly(2026, 10, 31), end: new DateOnly(2026, 11, 7),
            payable: groups[1].ShippingAmount, selection: 4);

        groups[0].ReplaceAcceptedEvidence(evidenceA, Now);
        groups[1].ReplaceAcceptedEvidence(evidenceB, Now);

        var materialized = Materialize(groups);

        Assert.Equal(2, materialized.Count);
        Assert.All(materialized, group => Assert.Equal(Snapshot, group.AcceptedEvidence!.SnapshotId));
        Assert.All(materialized, group => Assert.Equal(Draft, group.AcceptedEvidence!.DraftId));
        Assert.All(materialized, group => Assert.Equal(2, group.AcceptedEvidence!.SourceRevision));
        Assert.All(materialized, group => Assert.Equal("IRR", group.AcceptedEvidence!.Currency));

        var first = materialized.Single(x => x.SellerPartyId == SellerA);
        var second = materialized.Single(x => x.SellerPartyId == SellerB);
        Assert.Equal(AreaA, first.AcceptedEvidence!.AreaGroupId);
        Assert.Equal(RowA, first.AcceptedEvidence!.RateRowId);
        Assert.Equal(120_000m, first.AcceptedEvidence!.FreightAmount);
        Assert.Equal(5_000m, first.AcceptedEvidence!.PackagingAmount);
        Assert.Equal(new DateOnly(2026, 10, 29), first.AcceptedEvidence!.ReceivingStartDate);
        Assert.Equal(new DateOnly(2026, 11, 5), first.AcceptedEvidence!.ReceivingEndDate);
        Assert.Equal(3, first.AcceptedEvidence!.SelectionVersion);

        Assert.Equal(AreaB, second.AcceptedEvidence!.AreaGroupId);
        Assert.Equal(RowB, second.AcceptedEvidence!.RateRowId);
        Assert.Equal(80_000m, second.AcceptedEvidence!.FreightAmount);
        Assert.Equal(10_000m, second.AcceptedEvidence!.PackagingAmount);
        Assert.Equal(new DateOnly(2026, 10, 31), second.AcceptedEvidence!.ReceivingStartDate);
        Assert.Equal(4, second.AcceptedEvidence!.SelectionVersion);

        // عضویت خطوط دقیقاً همان هویت پیش‌نویس است و با افزودن شاهد تغییر نمی‌کند.
        Assert.Equal(CartLineA, groups[0].Lines.Single().CartLineId);
        Assert.Equal(CartLineB, groups[1].Lines.Single().CartLineId);
        Assert.Equal(groups[0].ShippingGroupId, first.ShippingGroupId);
        Assert.Equal(groups[1].ShippingGroupId, second.ShippingGroupId);
    }

    [Fact]
    public void Legacy_plan_without_evidence_materializes_with_no_evidence()
    {
        var materialized = Materialize(PlannerGroups());

        Assert.Equal(2, materialized.Count);
        Assert.All(materialized, group => Assert.Null(group.AcceptedEvidence));
        Assert.All(materialized, group => Assert.NotEmpty(group.Lines));
    }

    [Fact]
    public void Partial_or_invalid_evidence_is_refused_before_any_mutation()
    {
        AssertFault(ShippingGroupEvidenceFault.SnapshotRequired, () =>
            Evidence(AreaA, RowA, 120_000m, 5_000m, snapshot: Guid.Empty));
        AssertFault(ShippingGroupEvidenceFault.DraftRequired, () =>
            Evidence(AreaA, RowA, 120_000m, 5_000m, draftId: Guid.Empty));
        AssertFault(ShippingGroupEvidenceFault.SourceRevisionInvalid, () =>
            Evidence(AreaA, RowA, 120_000m, 5_000m, sourceRevision: 0));
        AssertFault(ShippingGroupEvidenceFault.FormatUnsupported, () =>
            Evidence(AreaA, RowA, 120_000m, 5_000m, formatVersion: 2));
        AssertFault(ShippingGroupEvidenceFault.SelectionVersionInvalid, () =>
            Evidence(AreaA, RowA, 120_000m, 5_000m, selection: 0));
        AssertFault(ShippingGroupEvidenceFault.ResponsibilityInvalid, () =>
            Evidence(AreaA, RowA, 120_000m, 5_000m, responsibility: (OfferFulfillmentResponsibility)99));
        AssertFault(ShippingGroupEvidenceFault.AreaGroupRequired, () =>
            Evidence(Guid.Empty, RowA, 120_000m, 5_000m));
        AssertFault(ShippingGroupEvidenceFault.RateRowRequired, () =>
            Evidence(AreaA, Guid.Empty, 120_000m, 5_000m));
        AssertFault(ShippingGroupEvidenceFault.ShippingTypeInvalid, () =>
            Evidence(AreaA, RowA, 120_000m, 5_000m, type: Guid.Empty));
        AssertFault(ShippingGroupEvidenceFault.AmountInvalid, () =>
            Evidence(AreaA, RowA, -1m, 5_000m, payable: 0m));
        AssertFault(ShippingGroupEvidenceFault.AmountInvalid, () =>
            Evidence(AreaA, RowA, 120_000m, 5_000m, collect: -1m));
        AssertFault(ShippingGroupEvidenceFault.CurrencyInvalid, () =>
            Evidence(AreaA, RowA, 120_000m, 5_000m, currency: "IR"));
        AssertFault(ShippingGroupEvidenceFault.TimeZoneInvalid, () =>
            Evidence(AreaA, RowA, 120_000m, 5_000m, timeZone: "  "));
        AssertFault(ShippingGroupEvidenceFault.ReceivingIntervalInvalid, () =>
            Evidence(AreaA, RowA, 120_000m, 5_000m,
                start: new DateOnly(2026, 11, 5), end: new DateOnly(2026, 10, 29)));
        AssertFault(ShippingGroupEvidenceFault.DeliveryDaysInvalid, () =>
            Evidence(AreaA, RowA, 120_000m, 5_000m, minDays: 3, maxDays: 2));
        AssertFault(ShippingGroupEvidenceFault.DeliveryDaysInvalid, () =>
            Evidence(AreaA, RowA, 120_000m, 5_000m, minDays: 2, maxDays: null));
    }

    [Fact]
    public void Incoherent_evidence_never_reaches_a_draft_or_order_group()
    {
        var groups = PlannerGroups();
        var before = groups[0].UpdatedAt;

        // ارز شاهد با ارز گروه نمی‌خواند: رد و بدون هیچ جهشی.
        AssertFault(ShippingGroupEvidenceFault.CurrencyMismatch, () =>
            groups[0].ReplaceAcceptedEvidence(
                Evidence(AreaA, RowA, 120_000m, 5_000m, currency: "USD", payable: groups[0].ShippingAmount),
                Now));
        Assert.Null(groups[0].AcceptedEvidence);
        Assert.Equal(before, groups[0].UpdatedAt);

        // مبلغ قابل‌پرداخت شاهد با هزینهٔ پذیرفته‌شدهٔ گروه نمی‌خواند.
        AssertFault(ShippingGroupEvidenceFault.PayableMismatch, () =>
            groups[0].ReplaceAcceptedEvidence(
                Evidence(AreaA, RowA, 120_000m, 5_000m, payable: groups[0].ShippingAmount + 1m),
                Now));
        Assert.Null(groups[0].AcceptedEvidence);

        // پلن سفارش هم همان ناسازگاری‌ها را پیش از مادی‌سازی رد می‌کند.
        groups[0].ReplaceAcceptedEvidence(
            Evidence(AreaA, RowA, 120_000m, 5_000m, payable: groups[0].ShippingAmount), Now);
        var mismatched = Plan(groups[0]) with
        {
            AcceptedEvidence = Evidence(AreaA, RowA, 120_000m, 5_000m, payable: groups[0].ShippingAmount + 5m),
        };
        AssertFault(ShippingGroupEvidenceFault.PayableMismatch, () => Materialize([mismatched], groups));

        var foreignCurrency = Plan(groups[0]) with
        {
            AcceptedEvidence = Evidence(AreaA, RowA, 120_000m, 5_000m, currency: "USD", payable: groups[0].ShippingAmount),
        };
        AssertFault(ShippingGroupEvidenceFault.CurrencyMismatch, () => Materialize([foreignCurrency], groups));
    }

    [Fact]
    public void Replacing_one_groups_evidence_leaves_the_other_group_untouched()
    {
        var groups = PlannerGroups();
        groups[0].ReplaceAcceptedEvidence(
            Evidence(AreaA, RowA, 120_000m, 5_000m, payable: groups[0].ShippingAmount), Now);
        var other = Evidence(AreaB, RowB, 80_000m, 10_000m, payable: groups[1].ShippingAmount);
        groups[1].ReplaceAcceptedEvidence(other, Now);

        var replacement = Evidence(
            AreaB, RowB, 95_000m, 12_000m,
            start: new DateOnly(2026, 11, 2), end: new DateOnly(2026, 11, 9),
            snapshot: Guid.Parse("01a17d0e-0419-7000-b575-f507d2f89abd"),
            payable: groups[1].ShippingAmount, selection: 5);
        groups[1].ReplaceAcceptedEvidence(replacement, Now);

        Assert.Equal(AreaA, groups[0].AcceptedEvidence!.AreaGroupId);
        Assert.Equal(Snapshot, groups[0].AcceptedEvidence!.SnapshotId);
        Assert.Equal(replacement.SnapshotId, groups[1].AcceptedEvidence!.SnapshotId);
        Assert.Equal(95_000m, groups[1].AcceptedEvidence!.FreightAmount);
        Assert.Equal(5, groups[1].AcceptedEvidence!.SelectionVersion);

        // پاک‌کردن شاهد فقط همان گروه را پاک می‌کند.
        groups[1].ReplaceAcceptedEvidence(null, Now);
        Assert.Null(groups[1].AcceptedEvidence);
        Assert.NotNull(groups[0].AcceptedEvidence);
    }

    private static void AssertFault(string expected, Action action)
    {
        var fault = Assert.Throws<ContractOperationException>(action);
        Assert.Equal(expected, fault.Code);
    }

    internal static ShippingGroupAcceptedEvidence Evidence(
        Guid areaGroupId,
        Guid rateRowId,
        decimal freight,
        decimal packaging,
        Guid? snapshot = null,
        Guid? draftId = null,
        int sourceRevision = 2,
        int formatVersion = ShippingGroupAcceptedEvidence.SupportedFormatVersion,
        int selection = 3,
        OfferFulfillmentResponsibility? responsibility = OfferFulfillmentResponsibility.Platform,
        Guid? type = null,
        decimal? payable = null,
        decimal? collect = null,
        string currency = "IRR",
        DateOnly? start = null,
        DateOnly? end = null,
        string timeZone = "Asia/Tehran",
        int? minDays = 2,
        int? maxDays = 3) =>
        ShippingGroupAcceptedEvidence.Create(
            snapshot ?? Snapshot,
            draftId ?? Draft,
            sourceRevision,
            formatVersion,
            selection,
            responsibility,
            areaGroupId,
            rateRowId,
            type ?? TypeId,
            freight,
            packaging,
            currency,
            payable ?? freight + packaging,
            collect ?? 0m,
            start ?? new DateOnly(2026, 10, 29),
            end ?? new DateOnly(2026, 11, 5),
            timeZone,
            minDays,
            maxDays);

    internal static IReadOnlyList<CartShippingDraftGroup> PlannerGroups()
    {
        var lines = new[]
        {
            new CartShippingGroupCartLine(CartLineA, SellerA, 1m),
            new CartShippingGroupCartLine(CartLineB, SellerB, 1m),
        };
        var requests = new[]
        {
            new CartShippingGroupRequest(
                "post:standard", null, null, null, [CartLineA],
                new DateOnly(2026, 10, 29), "09:00-17:00"),
            new CartShippingGroupRequest(
                "post:express", null, null, null, [CartLineB],
                new DateOnly(2026, 10, 31), "09:00-17:00"),
        };
        var eligible = new[]
        {
            new CartShippingGroupEligibleMethod(
                "post:standard", null, null, "پست عادی", 125_000m, "IRR", 2,
                new DateOnly(2026, 10, 29), SellerA),
            new CartShippingGroupEligibleMethod(
                "post:express", null, null, "پست سریع", 90_000m, "IRR", 1,
                new DateOnly(2026, 10, 31), SellerB),
        };

        var next = 0;
        Guid NewId() => Guid.Parse($"01a17d0a-0000-7000-9000-0000000000{++next:D2}");

        return CartShippingGroupPlanner.Plan(
            Cart, lines, requests, eligible, null, null, Now, NewId);
    }

    internal static OrderShippingGroupPlan Plan(CartShippingDraftGroup group) =>
        new(
            group.ShippingGroupId,
            group.SellerPartyId,
            group.MethodCode,
            group.ServiceCode,
            group.OptionCode,
            group.MethodLabel,
            group.OriginContext,
            group.ShippingAmount,
            group.Currency,
            group.MinimumDeliveryDate,
            group.SelectedDeliveryDate,
            group.SelectedDeliveryTimeWindow,
            group.LeadDays,
            group.Lines.Select(x => x.CartLineId).ToArray(),
            group.AcceptedEvidence);

    internal static IReadOnlyList<OrderShippingGroup> Materialize(IReadOnlyList<CartShippingDraftGroup> groups)
    {
        var orderLines = groups
            .Select(group => (OrderLineId: OrderLineId(CartLineIndex(group.Lines[0].CartLineId)), SellerPartyId: group.SellerPartyId))
            .ToArray();
        var sellerOrder = groups
            .Select(group => group.SellerPartyId)
            .Distinct()
            .ToDictionary(seller => seller, _ => Guid.NewGuid());
        var lineMap = groups
            .SelectMany(group => group.Lines)
            .ToDictionary(line => line.CartLineId, line => OrderLineId(CartLineIndex(line.CartLineId)));

        return Materialize(groups.Select(Plan).ToArray(), orderLines, sellerOrder, lineMap);
    }

    internal static IReadOnlyList<OrderShippingGroup> Materialize(
        IReadOnlyList<OrderShippingGroupPlan> plans,
        IReadOnlyList<CartShippingDraftGroup> groups)
    {
        var orderLines = groups
            .Select(group => (OrderLineId: OrderLineId(CartLineIndex(group.Lines[0].CartLineId)), SellerPartyId: group.SellerPartyId))
            .ToArray();
        var sellerOrder = groups
            .Select(group => group.SellerPartyId)
            .Distinct()
            .ToDictionary(seller => seller, _ => Guid.NewGuid());
        var lineMap = groups
            .SelectMany(group => group.Lines)
            .ToDictionary(line => line.CartLineId, line => OrderLineId(CartLineIndex(line.CartLineId)));

        return Materialize(plans, orderLines, sellerOrder, lineMap);
    }

    private static IReadOnlyList<OrderShippingGroup> Materialize(
        IReadOnlyList<OrderShippingGroupPlan> plans,
        IReadOnlyList<(Guid OrderLineId, Guid SellerPartyId)> orderLines,
        IReadOnlyDictionary<Guid, Guid> sellerOrder,
        IReadOnlyDictionary<Guid, Guid> lineMap) =>
        OrderShippingGroupMaterializer.Materialize(
            plans,
            orderLines,
            sellerOrder,
            lineMap,
            Guid.Parse("01a17d0b-0000-7000-9000-000000000001"),
            Now);

    internal static int CartLineIndex(Guid cartLineId) => cartLineId == CartLineA ? 1 : 2;

    internal static Guid OrderLineId(int index) =>
        Guid.Parse($"01a17d0c-0000-7000-9000-0000000000{index:D2}");
}


