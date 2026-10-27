using Tooba.BuildingBlocks;
using Tooba.Order.Domain.Checkout;
using Xunit;

namespace Tooba.Order.Tests.Checkout;

/// <summary>
/// TB-SHIPPING-COMPACT-GROUP-ORDER-EVIDENCE-231-R1 — شاهد فشردهٔ هر گروه ارسال روی مسیر موجود
/// پیش‌نویس ← پلن ← مادی‌سازی قطعی، با دقت کامل مالک/نسخه/ساعت/مبلغ.
/// </summary>
/// <remarks>
/// این حقایق بدون پایگاه‌داده اجرا می‌شوند؛ اثبات رابطه‌ای در
/// <c>ShippingGroupAcceptedEvidenceStorageTests</c> است.
/// </remarks>
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

    /// <summary>tenant دقیق فروشگاه؛ مقایسهٔ آن حساس به بزرگی حروف است.</summary>
    internal const string TenantKey = "store-alpha";

    /// <summary>بازبینی/نسخهٔ انتخاب بالاتر از int.MaxValue (دقت long منبع).</summary>
    internal const long LargeSourceRevision = 3_000_000_000L;

    internal const long LargeSelectionVersion = 4_294_967_296L;

    /// <summary>09:00:00.1234567 — هفت رقم اعشار که time/timestamp پستگرس آن را بی‌افت نگه نمی‌دارد.</summary>
    internal static readonly long OpenTicks = (9 * TimeSpan.TicksPerHour) + 1_234_567L;

    /// <summary>17:00:00.7654321</summary>
    internal static readonly long CloseTicks = (17 * TimeSpan.TicksPerHour) + 7_654_321L;

    [Fact]
    public void Two_groups_sharing_one_snapshot_keep_distinct_owner_money_and_hours_through_materialization()
    {
        var groups = PlannerGroups();
        var evidenceA = Evidence(
            AreaA, RowA, freight: 120_000m, packaging: 5_000m,
            owner: ShippingGroupOwner.Store(TenantKey),
            payable: groups[0].ShippingAmount, selection: LargeSelectionVersion);
        var evidenceB = Evidence(
            AreaB, RowB, freight: 80_000m, packaging: 10_000m,
            owner: ShippingGroupOwner.Seller(SellerB),
            date: new DateOnly(2026, 10, 31),
            openTicks: OpenTicks + 1, closeTicks: CloseTicks + 1,
            payable: groups[1].ShippingAmount, selection: LargeSelectionVersion + 1);

        groups[0].ReplaceAcceptedEvidence(evidenceA, Now);
        groups[1].ReplaceAcceptedEvidence(evidenceB, Now);

        var materialized = Materialize(groups);

        Assert.Equal(2, materialized.Count);
        Assert.All(materialized, group => Assert.Equal(Snapshot, group.AcceptedEvidence!.SnapshotId));
        Assert.All(materialized, group => Assert.Equal(Draft, group.AcceptedEvidence!.DraftId));
        Assert.All(materialized, group => Assert.Equal(LargeSourceRevision, group.AcceptedEvidence!.SourceRevision));
        Assert.All(materialized, group => Assert.Equal("IRR", group.AcceptedEvidence!.Currency));

        var first = materialized.Single(x => x.SellerPartyId == SellerA);
        var second = materialized.Single(x => x.SellerPartyId == SellerB);

        Assert.Equal(ShippingGroupOwnerKind.Store, first.AcceptedEvidence!.Owner.Kind);
        Assert.Equal(TenantKey, first.AcceptedEvidence!.Owner.TenantKey);
        Assert.Null(first.AcceptedEvidence!.Owner.SellerPartyId);
        Assert.Equal(AreaA, first.AcceptedEvidence!.AreaGroupId);
        Assert.Equal(RowA, first.AcceptedEvidence!.RateRowId);
        Assert.Equal(120_000m, first.AcceptedEvidence!.FreightAmount);
        Assert.Equal(5_000m, first.AcceptedEvidence!.PackagingAmount);
        Assert.Equal(125_000m, first.AcceptedEvidence!.PayableAtCheckoutAmount);
        Assert.Equal(0m, first.AcceptedEvidence!.CarrierCollectAmount);
        Assert.Equal(new DateOnly(2026, 10, 29), first.AcceptedEvidence!.ReceivingDate);
        Assert.Equal(OpenTicks, first.AcceptedEvidence!.ReceivingOpenTicks);
        Assert.Equal(CloseTicks, first.AcceptedEvidence!.ReceivingCloseTicks);
        Assert.Equal(9, new TimeOnly(OpenTicks).Hour);
        Assert.Equal(17, new TimeOnly(CloseTicks).Hour);
        Assert.Equal(LargeSelectionVersion, first.AcceptedEvidence!.SelectionVersion);

        Assert.Equal(ShippingGroupOwnerKind.Seller, second.AcceptedEvidence!.Owner.Kind);
        Assert.Equal(SellerB, second.AcceptedEvidence!.Owner.SellerPartyId);
        Assert.Null(second.AcceptedEvidence!.Owner.TenantKey);
        Assert.Equal(AreaB, second.AcceptedEvidence!.AreaGroupId);
        Assert.Equal(RowB, second.AcceptedEvidence!.RateRowId);
        Assert.Equal(80_000m, second.AcceptedEvidence!.FreightAmount);
        Assert.Equal(10_000m, second.AcceptedEvidence!.PackagingAmount);
        Assert.Equal(new DateOnly(2026, 10, 31), second.AcceptedEvidence!.ReceivingDate);
        Assert.Equal(OpenTicks + 1, second.AcceptedEvidence!.ReceivingOpenTicks);
        Assert.Equal(LargeSelectionVersion + 1, second.AcceptedEvidence!.SelectionVersion);

        // عضویت خطوط دقیقاً همان هویت پیش‌نویس است و با افزودن شاهد تغییر نمی‌کند.
        Assert.Equal(CartLineA, groups[0].Lines.Single().CartLineId);
        Assert.Equal(CartLineB, groups[1].Lines.Single().CartLineId);
        Assert.Equal(groups[0].ShippingGroupId, first.ShippingGroupId);
        Assert.Equal(groups[1].ShippingGroupId, second.ShippingGroupId);
    }

    [Fact]
    public void Marketplace_owner_is_preserved_and_never_rewritten_as_the_group_seller()
    {
        var groups = PlannerGroups();
        groups[0].ReplaceAcceptedEvidence(
            Evidence(AreaA, RowA, 120_000m, 5_000m, owner: ShippingGroupOwner.Marketplace()),
            Now);

        var materialized = Materialize([groups[0]]).Single();

        Assert.Equal(ShippingGroupOwnerKind.Marketplace, materialized.AcceptedEvidence!.Owner.Kind);
        Assert.Null(materialized.AcceptedEvidence!.Owner.TenantKey);
        Assert.Null(materialized.AcceptedEvidence!.Owner.SellerPartyId);
        Assert.Equal(SellerA, materialized.SellerPartyId);
        Assert.Equal(120_000m, materialized.AcceptedEvidence!.FreightAmount);
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
        AssertFault(ShippingGroupEvidenceFault.SourceRevisionInvalid, () =>
            Evidence(AreaA, RowA, 120_000m, 5_000m, sourceRevision: -1));
        AssertFault(ShippingGroupEvidenceFault.FormatUnsupported, () =>
            Evidence(AreaA, RowA, 120_000m, 5_000m, formatVersion: 2));
        AssertFault(ShippingGroupEvidenceFault.SelectionVersionInvalid, () =>
            Evidence(AreaA, RowA, 120_000m, 5_000m, selection: 0));
        AssertFault(ShippingGroupEvidenceFault.OwnerRequired, () =>
            ShippingGroupAcceptedEvidence.Create(
                Snapshot, Draft, 1L, ShippingGroupAcceptedEvidence.SupportedFormatVersion, 1L,
                null!, AreaA, RowA, TypeId,
                120_000m, 5_000m, "IRR", 125_000m, 0m,
                new DateOnly(2026, 10, 29), OpenTicks, CloseTicks, "Asia/Tehran", null, null));
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
        AssertFault(ShippingGroupEvidenceFault.CurrencyInvalid, () =>
            Evidence(AreaA, RowA, 120_000m, 5_000m, currency: "IR1"));
        AssertFault(ShippingGroupEvidenceFault.TimeZoneInvalid, () =>
            Evidence(AreaA, RowA, 120_000m, 5_000m, timeZone: "  "));
        AssertFault(ShippingGroupEvidenceFault.DeliveryDaysInvalid, () =>
            Evidence(AreaA, RowA, 120_000m, 5_000m, minDays: 3, maxDays: 2));
        AssertFault(ShippingGroupEvidenceFault.DeliveryDaysInvalid, () =>
            Evidence(AreaA, RowA, 120_000m, 5_000m, minDays: 2, maxDays: null));
        AssertFault(ShippingGroupEvidenceFault.DeliveryDaysInvalid, () =>
            Evidence(AreaA, RowA, 120_000m, 5_000m, minDays: 2, maxDays: 400));
    }

    [Fact]
    public void Owner_identity_must_be_one_of_three_exact_shapes()
    {
        AssertFault(ShippingGroupEvidenceFault.OwnerInvalid, () =>
            ShippingGroupOwner.Store("   "));
        AssertFault(ShippingGroupEvidenceFault.OwnerInvalid, () =>
            ShippingGroupOwner.Store(new string('t', ShippingGroupOwner.MaxTenantKeyLength + 1)));
        AssertFault(ShippingGroupEvidenceFault.OwnerInvalid, () =>
            ShippingGroupOwner.Seller(Guid.Empty));
        AssertFault(ShippingGroupEvidenceFault.OwnerInvalid, () =>
            ShippingGroupOwner.Restore(ShippingGroupOwnerKind.Seller, null, null));
        AssertFault(ShippingGroupEvidenceFault.OwnerInvalid, () =>
            ShippingGroupOwner.Restore(ShippingGroupOwnerKind.Store, null, null));
        AssertFault(ShippingGroupEvidenceFault.OwnerInvalid, () =>
            ShippingGroupOwner.Restore(ShippingGroupOwnerKind.Marketplace, TenantKey, null));
        AssertFault(ShippingGroupEvidenceFault.OwnerInvalid, () =>
            ShippingGroupOwner.Restore(ShippingGroupOwnerKind.Marketplace, null, SellerA));
        AssertFault(ShippingGroupEvidenceFault.OwnerInvalid, () =>
            ShippingGroupOwner.Restore(ShippingGroupOwnerKind.Seller, TenantKey, SellerA));
        AssertFault(ShippingGroupEvidenceFault.OwnerInvalid, () =>
            ShippingGroupOwner.Restore((ShippingGroupOwnerKind)99, TenantKey, null));

        // tenant هرگز lower/نرمال نمی‌شود.
        var restored = ShippingGroupOwner.Restore(ShippingGroupOwnerKind.Store, " Store-Alpha ", null);
        Assert.Equal("Store-Alpha", restored.TenantKey);
        Assert.NotEqual("store-alpha", restored.TenantKey);
    }

    [Fact]
    public void Owner_that_does_not_match_the_group_seller_is_refused()
    {
        var groups = PlannerGroups();

        AssertFault(ShippingGroupEvidenceFault.OwnerMismatch, () =>
            groups[0].ReplaceAcceptedEvidence(
                Evidence(AreaA, RowA, 120_000m, 5_000m,
                    owner: ShippingGroupOwner.Seller(SellerB),
                    payable: groups[0].ShippingAmount),
                Now));
        Assert.Null(groups[0].AcceptedEvidence);
        Assert.Equal(Now, groups[0].UpdatedAt);

        // Store/Marketplace هرگز به‌جای فروشندهٔ گروه بازنویسی نمی‌شوند.
        groups[0].ReplaceAcceptedEvidence(
            Evidence(AreaA, RowA, 120_000m, 5_000m, payable: groups[0].ShippingAmount),
            Now);
        Assert.Equal(ShippingGroupOwnerKind.Store, groups[0].AcceptedEvidence!.Owner.Kind);
        Assert.Equal(SellerA, groups[0].SellerPartyId);
    }

    [Fact]
    public void Accepted_receiving_interval_requires_one_positive_same_day_window()
    {
        AssertFault(ShippingGroupEvidenceFault.ReceivingIntervalInvalid, () =>
            Evidence(AreaA, RowA, 120_000m, 5_000m, openTicks: CloseTicks, closeTicks: CloseTicks));
        AssertFault(ShippingGroupEvidenceFault.ReceivingIntervalInvalid, () =>
            Evidence(AreaA, RowA, 120_000m, 5_000m, openTicks: CloseTicks, closeTicks: OpenTicks));
        AssertFault(ShippingGroupEvidenceFault.ReceivingIntervalInvalid, () =>
            Evidence(AreaA, RowA, 120_000m, 5_000m, openTicks: -1L));
        AssertFault(ShippingGroupEvidenceFault.ReceivingIntervalInvalid, () =>
            Evidence(AreaA, RowA, 120_000m, 5_000m,
                closeTicks: ShippingGroupAcceptedEvidence.MaxTimeOfDayTicks + 1));
        AssertFault(ShippingGroupEvidenceFault.ReceivingDateInvalid, () =>
            Evidence(AreaA, RowA, 120_000m, 5_000m, date: DateOnly.MinValue));

        // مرزهای مجاز روز کامل پذیرفته می‌شوند (00:00 تا 23:59:59.9999999).
        var edge = Evidence(AreaA, RowA, 120_000m, 5_000m,
            openTicks: 0L, closeTicks: ShippingGroupAcceptedEvidence.MaxTimeOfDayTicks);
        Assert.Equal(0L, edge.ReceivingOpenTicks);
        Assert.Equal(ShippingGroupAcceptedEvidence.MaxTimeOfDayTicks, edge.ReceivingCloseTicks);
    }

    [Fact]
    public void Money_must_be_exactly_representable_and_conserved()
    {
        // اعشار اضافه/ظرفیت numeric(18,2): رد، نه گرد کردن/بریدن.
        AssertFault(ShippingGroupEvidenceFault.AmountUnrepresentable, () =>
            Evidence(AreaA, RowA, 120_000.001m, 5_000m, payable: 125_000.001m));
        AssertFault(ShippingGroupEvidenceFault.AmountUnrepresentable, () =>
            Evidence(AreaA, RowA, 10_000_000_000_000_000m, 0m, payable: 10_000_000_000_000_000m));
        AssertFault(ShippingGroupEvidenceFault.AmountUnrepresentable, () =>
            Evidence(AreaA, RowA, 120_000m, 5_000m, payable: 125_000.999m, collect: 0m));

        // عدم توازن کرایه+بسته‌بندی با تسویه+دریافت حامل: رد، بدون بازمحاسبهٔ هیچ طرف.
        AssertFault(ShippingGroupEvidenceFault.AmountConservationInvalid, () =>
            Evidence(AreaA, RowA, 150_000m, 7_000m, payable: 125_000m, collect: 0m));

        // صفر صریح مجاز است و در جمع حفظ می‌شود.
        var zero = Evidence(AreaA, RowA, 0m, 0m, payable: 0m, collect: 0m);
        Assert.Equal(0m, zero.FreightAmount);
        Assert.Equal(0m, zero.PackagingAmount);

        // بیشترین مقدار قابل‌نمایش دقیقاً پذیرفته می‌شود.
        var max = Evidence(AreaA, RowA,
            ShippingGroupAcceptedEvidence.MaxStorableAmount, 0m,
            payable: ShippingGroupAcceptedEvidence.MaxStorableAmount, collect: 0m);
        Assert.Equal(ShippingGroupAcceptedEvidence.MaxStorableAmount, max.FreightAmount);

        // تقسیم پرداخت بین تسویه و دریافت حامل، وقتی جمع برابر است، مجاز است.
        var split = Evidence(AreaA, RowA, 100_000m, 20_000m, payable: 90_000m, collect: 30_000m);
        Assert.Equal(90_000m, split.PayableAtCheckoutAmount);
        Assert.Equal(30_000m, split.CarrierCollectAmount);
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

        // مبلغ قابل‌پرداخت شاهد با هزینهٔ پذیرفته‌شدهٔ گروه نمی‌خواند (شاهد خودش متوازن است: 120,000 + 6,000).
        AssertFault(ShippingGroupEvidenceFault.PayableMismatch, () =>
            groups[0].ReplaceAcceptedEvidence(
                Evidence(AreaA, RowA, 120_000m, 6_000m), Now));
        Assert.Null(groups[0].AcceptedEvidence);

        // تاریخ پذیرفته‌شده باید همان تاریخ انتخاب‌شدهٔ گروه باشد.
        AssertFault(ShippingGroupEvidenceFault.ScheduleMismatch, () =>
            groups[0].ReplaceAcceptedEvidence(
                Evidence(AreaA, RowA, 120_000m, 5_000m,
                    date: new DateOnly(2026, 10, 31),
                    payable: groups[0].ShippingAmount),
                Now));
        Assert.Null(groups[0].AcceptedEvidence);

        // پلن سفارش هم همان ناسازگاری‌ها را پیش از مادی‌سازی رد می‌کند.
        groups[0].ReplaceAcceptedEvidence(
            Evidence(AreaA, RowA, 120_000m, 5_000m, payable: groups[0].ShippingAmount), Now);
        var mismatched = Plan(groups[0]) with
        {
            AcceptedEvidence = Evidence(AreaA, RowA, 120_000m, 6_000m),
        };
        AssertFault(ShippingGroupEvidenceFault.PayableMismatch, () => Materialize([mismatched], groups));

        var foreignCurrency = Plan(groups[0]) with
        {
            AcceptedEvidence = Evidence(AreaA, RowA, 120_000m, 5_000m, currency: "USD", payable: groups[0].ShippingAmount),
        };
        AssertFault(ShippingGroupEvidenceFault.CurrencyMismatch, () => Materialize([foreignCurrency], groups));

        var wrongOwner = Plan(groups[0]) with
        {
            AcceptedEvidence = Evidence(AreaA, RowA, 120_000m, 5_000m,
                owner: ShippingGroupOwner.Seller(SellerB),
                payable: groups[0].ShippingAmount),
        };
        AssertFault(ShippingGroupEvidenceFault.OwnerMismatch, () => Materialize([wrongOwner], groups));
    }

    [Fact]
    public void Changed_selected_schedule_cannot_coexist_with_stale_evidence()
    {
        var groups = PlannerGroups();
        groups[0].ReplaceAcceptedEvidence(
            Evidence(AreaA, RowA, 120_000m, 5_000m, payable: groups[0].ShippingAmount), Now);

        var chosenDate = groups[0].SelectedDeliveryDate;
        var chosenWindow = groups[0].SelectedDeliveryTimeWindow;

        // تاریخ دیگر: رد پیش از جهش.
        AssertFault(ShippingGroupEvidenceFault.ScheduleMismatch, () =>
            groups[0].ApplySchedule(new DateOnly(2026, 10, 31), chosenWindow));
        // پنجرهٔ دیگر: رد پیش از جهش.
        AssertFault(ShippingGroupEvidenceFault.ScheduleMismatch, () =>
            groups[0].ApplySchedule(chosenDate, "10:00-18:00"));

        Assert.Equal(chosenDate, groups[0].SelectedDeliveryDate);
        Assert.Equal(chosenWindow, groups[0].SelectedDeliveryTimeWindow);
        Assert.NotNull(groups[0].AcceptedEvidence);

        // همان تاریخ/پنجره دوباره مجاز است و شاهد را خراب نمی‌کند.
        groups[0].ApplySchedule(chosenDate, chosenWindow);
        Assert.Equal(AreaA, groups[0].AcceptedEvidence!.AreaGroupId);

        // پس از پاک‌کردن صریح شاهد، رفتار قدیمی زمان‌بندی برمی‌گردد.
        groups[0].ReplaceAcceptedEvidence(null, Now);
        groups[0].ApplySchedule(new DateOnly(2026, 11, 2), "10:00-18:00");
        Assert.Equal(new DateOnly(2026, 11, 2), groups[0].SelectedDeliveryDate);
        Assert.Null(groups[0].AcceptedEvidence);
    }

    [Fact]
    public void Replacing_one_groups_evidence_leaves_the_other_group_untouched()
    {
        var groups = PlannerGroups();
        groups[0].ReplaceAcceptedEvidence(
            Evidence(AreaA, RowA, 120_000m, 5_000m, payable: groups[0].ShippingAmount), Now);
        var other = Evidence(AreaB, RowB, 80_000m, 10_000m,
            date: new DateOnly(2026, 10, 31), payable: groups[1].ShippingAmount);
        groups[1].ReplaceAcceptedEvidence(other, Now);

        var replacement = Evidence(
            AreaB, RowB, 85_000m, 5_000m,
            date: new DateOnly(2026, 10, 31),
            snapshot: Guid.Parse("01a17d0e-0419-7000-b575-f507d2f89abd"),
            payable: groups[1].ShippingAmount, selection: 5);
        groups[1].ReplaceAcceptedEvidence(replacement, Now);

        Assert.Equal(AreaA, groups[0].AcceptedEvidence!.AreaGroupId);
        Assert.Equal(Snapshot, groups[0].AcceptedEvidence!.SnapshotId);
        Assert.Equal(replacement.SnapshotId, groups[1].AcceptedEvidence!.SnapshotId);
        Assert.Equal(85_000m, groups[1].AcceptedEvidence!.FreightAmount);
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
        long sourceRevision = LargeSourceRevision,
        int formatVersion = ShippingGroupAcceptedEvidence.SupportedFormatVersion,
        long selection = LargeSelectionVersion,
        ShippingGroupOwner? owner = null,
        Guid? type = null,
        decimal? payable = null,
        decimal? collect = null,
        string currency = "IRR",
        DateOnly? date = null,
        long? openTicks = null,
        long? closeTicks = null,
        string timeZone = "Asia/Tehran",
        int? minDays = 2,
        int? maxDays = 3) =>
        ShippingGroupAcceptedEvidence.Create(
            snapshot ?? Snapshot,
            draftId ?? Draft,
            sourceRevision,
            formatVersion,
            selection,
            owner ?? ShippingGroupOwner.Store(TenantKey),
            areaGroupId,
            rateRowId,
            type ?? TypeId,
            freight,
            packaging,
            currency,
            payable ?? freight + packaging,
            collect ?? 0m,
            date ?? new DateOnly(2026, 10, 29),
            openTicks ?? OpenTicks,
            closeTicks ?? CloseTicks,
            timeZone,
            minDays,
            maxDays);

    internal static IReadOnlyList<CartShippingDraftGroup> PlannerGroups(
        decimal standardAmount = 125_000m,
        decimal expressAmount = 90_000m)
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
                "post:standard", null, null, "پست عادی", standardAmount, "IRR", 2,
                new DateOnly(2026, 10, 29), SellerA),
            new CartShippingGroupEligibleMethod(
                "post:express", null, null, "پست سریع", expressAmount, "IRR", 1,
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

    internal static IReadOnlyList<OrderShippingGroup> Materialize(IReadOnlyList<CartShippingDraftGroup> groups) =>
        Materialize(groups.Select(Plan).ToArray(), groups);

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
