using Tooba.BuildingBlocks;
using Tooba.Shipping.Contracts.Authoring;
using Tooba.Shipping.Contracts.Errors;
using Tooba.Shipping.Contracts.Packaging;
using Tooba.Shipping.Domain.Authoring;
using Xunit;

namespace Tooba.Shipping.Tests.Authoring;

/// <summary>
/// TB-SHIPPING-ADMIN-IMMUTABLE-REVISION-CAPTURE-214 — دامنه/رمزگذار capture بازبینی پیش‌نویس.
/// </summary>
/// <remarks>
/// این تست‌ها فقط codec و تغییرناپذیری را می‌سنجند: بدون EF، بدون دیتابیس و بدون تراکنش. اثبات
/// رابطه‌ای/هم‌زمانی در فایل storage است و بدون نمونهٔ دورریختنی PostgreSQL اجرا نمی‌شود.
/// </remarks>
public sealed class ShippingAuthoringRevisionSnapshotTests
{
    private static readonly Guid Tehran = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Isfahan = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid Persian = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid English = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid DeactivatedLanguage = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private const int Saturday = 6;
    private static readonly TimeOnly MorningStart = TimeOnly.FromTimeSpan(new TimeSpan(0, 9, 15, 30, 250));
    private static readonly TimeOnly MorningEnd = TimeOnly.FromTimeSpan(new TimeSpan(0, 12, 45, 59, 750));
    private static readonly DateTimeOffset Now = new(2026, 10, 25, 10, 0, 0, TimeSpan.Zero);

    /// <summary>پیش‌نویس با دو ردیف در یک منطقه که هزینهٔ بسته‌بندی متفاوت دارند و همهٔ کران‌ها پر است.</summary>
    private static ShippingAuthoringDraft RichDraft()
    {
        var draft = ShippingAuthoringDraft.Create(
            ShippingDraftOwner.ForStore("tb214-store"),
            "post",
            "express",
            [
                new ShippingAuthoringRegionGroupInput(null, [Node(Tehran)]),
                new ShippingAuthoringRegionGroupInput(null, [Node(Isfahan)]),
            ],
            Now);

        var groups = draft.RegionGroups.OrderBy(group => group.Order).ToArray();

        draft.ReplaceGroupRateRows(
            groups[0].GroupId,
            [
                Row(null, 0, 5000, 1, 10, 100_000.25m, 5_000_000.75m, 120_000.00m, 5_000.00m, 1, 3),
                Row(null, 5000, null, 11, null, null, null, 180_000.50m, 9_500.75m, 2, 5),
            ],
            "IRT",
            Now.AddMinutes(1));

        draft.ReplaceGroupRateRows(
            groups[1].GroupId,
            [Row(null, 0, 1000, 1, 2, null, null, 95_000.10m, 3_200.30m, 4, 7)],
            "IRT",
            Now.AddMinutes(2));

        draft.ApplyPresentation(
            draft.PreflightPresentation(
                [
                    new ShippingAuthoringDraft.MethodTranslationInput(Persian, "پست پیشتاز", "توضیح فارسی"),
                    new ShippingAuthoringDraft.MethodTranslationInput(English, "Express post", null),
                ],
                [
                    new ShippingAuthoringDraft.GroupTitleInput(groups[0].GroupId, Persian, "منطقهٔ تهران"),
                    new ShippingAuthoringDraft.GroupTitleInput(groups[0].GroupId, English, "Tehran area"),
                ],
                new HashSet<Guid> { Persian, English }),
            Now.AddMinutes(3));

        draft.ApplyWeeklySchedule(
            draft.PreflightWeeklySchedule(
                "Asia/Tehran",
                [
                    new ShippingAuthoringDraft.ScheduleDayInput(1, true, MorningStart, MorningEnd),
                    new ShippingAuthoringDraft.ScheduleDayInput(2, false, MorningStart, MorningEnd),
                    new ShippingAuthoringDraft.ScheduleDayInput(3, true, new TimeOnly(8, 0), new TimeOnly(16, 30)),
                    new ShippingAuthoringDraft.ScheduleDayInput(4, false, null, null),
                    new ShippingAuthoringDraft.ScheduleDayInput(5, true, new TimeOnly(10, 0), new TimeOnly(18, 0)),
                    new ShippingAuthoringDraft.ScheduleDayInput(6, true, new TimeOnly(9, 0), new TimeOnly(13, 0)),
                    new ShippingAuthoringDraft.ScheduleDayInput(7, false, new TimeOnly(9, 0), new TimeOnly(13, 0)),
                ]),
            Now.AddMinutes(4));

        draft.ApplyMethodSettings(
            draft.PreflightMethodSettings(
                ShippingFreightPayerMode.CollectOnDelivery,
                ShippingAuthoringCalculationMode.Manual),
            Now.AddMinutes(5));

        draft.ApplyTypeAssignment(
            draft.PreflightTypeAssignment(Guid.Parse("66666666-6666-6666-6666-666666666666")),
            Now.AddMinutes(6));
        return draft;
    }

    private static ShippingDestinationNode Node(Guid id)
        => new(ShippingDestinationNodeKind.City, id);

    private static ShippingAuthoringDraft.ShippingAuthoringRateRowInput Row(
        Guid? id,
        long? minWeight,
        long? maxWeight,
        int? minCount,
        int? maxCount,
        decimal? minAmount,
        decimal? maxAmount,
        decimal freight,
        decimal packaging,
        int minDays,
        int maxDays)
        => new(id, minWeight, maxWeight, minCount, maxCount, minAmount, maxAmount, freight, packaging, minDays, maxDays);

    [Fact]
    public void A_capture_roundtrips_every_stored_authored_fact_without_loss()
    {
        var draft = RichDraft();
        var snapshot = ShippingAuthoringRevisionSnapshot.Capture(draft, Now.AddHours(1));
        var payload = snapshot.ReadPayload();

        Assert.Equal(ShippingAuthoringRevisionSnapshot.CurrentFormatVersion, payload.FormatVersion);
        Assert.Equal(draft.Revision, payload.SourceRevision);
        Assert.Equal(draft.DraftId, payload.DraftId);
        Assert.Equal("post", payload.ServiceCode);
        Assert.Equal("express", payload.OptionCode);
        Assert.Equal(ShippingDraftOwnerKind.Store, payload.Owner.Kind);
        Assert.Equal("tb214-store", payload.Owner.TenantId);
        Assert.Null(payload.Owner.SellerPartyId);
        Assert.Equal(draft.CreatedAt, payload.DraftCreatedAt);
        Assert.Equal(draft.UpdatedAt, payload.DraftUpdatedAt);
        Assert.Equal(draft.ShippingTypeId, payload.ShippingTypeId);

        // Two rows in the SAME area with DIFFERENT packaging survive as two distinct facts.
        var firstGroup = payload.Groups.Single(group => group.GroupId == draft.RegionGroups[0].GroupId);
        Assert.Equal(0, firstGroup.Order);
        Assert.Equal("IRT", firstGroup.CurrencyCode);
        Assert.Single(firstGroup.Areas);
        Assert.Equal(Tehran, firstGroup.Areas[0].NodeId);
        Assert.Equal(ShippingDestinationNodeKind.City, firstGroup.Areas[0].NodeKind);
        Assert.Equal(2, firstGroup.RateRows.Count);
        Assert.Equal(5_000.00m, firstGroup.RateRows.Single(row => row.MinWeightGrams == 0).PackagingAmount);
        Assert.Equal(9_500.75m, firstGroup.RateRows.Single(row => row.MinWeightGrams == 5000).PackagingAmount);
        Assert.Equal(180_000.50m, firstGroup.RateRows.Single(row => row.MinWeightGrams == 5000).FreightAmount);
        Assert.Equal(5_000_000.75m, firstGroup.RateRows.Single(row => row.MinWeightGrams == 0).MaxMerchandiseAmount);
        Assert.Null(firstGroup.RateRows.Single(row => row.MinWeightGrams == 5000).MaxWeightGrams);

        // Language text: a null description stays null, and every stored language is kept (no active-only
        // projection, no fabrication of empty text).
        Assert.Equal(2, firstGroup.Titles.Count);
        Assert.Equal("Tehran area", firstGroup.Titles.Single(title => title.LanguageId == English).Title);
        Assert.Equal(2, payload.Translations.Count);
        Assert.Equal("توضیح فارسی", payload.Translations.Single(t => t.LanguageId == Persian).Description);
        Assert.Null(payload.Translations.Single(t => t.LanguageId == English).Description);
        Assert.Equal("Express post", payload.Translations.Single(t => t.LanguageId == English).Name);

        // Week: exact fractional ticks and remembered closed-day hours both survive.
        var week = Assert.IsType<ShippingAuthoringRevisionWeek>(payload.WeeklySchedule);
        Assert.Equal("Asia/Tehran", week.TimeZoneId);
        Assert.Equal(7, week.Days.Count);
        Assert.Equal(MorningStart, week.Days.Single(day => day.Weekday == 1).StartTime);
        Assert.Equal(MorningEnd, week.Days.Single(day => day.Weekday == 1).EndTime);
        Assert.False(week.Days.Single(day => day.Weekday == 2).IsWorking);
        Assert.Equal(MorningStart, week.Days.Single(day => day.Weekday == 2).StartTime);
        Assert.Null(week.Days.Single(day => day.Weekday == 4).StartTime);

        var settings = Assert.IsType<ShippingAuthoringRevisionSettings>(payload.MethodSettings);
        Assert.Equal(ShippingFreightPayerMode.CollectOnDelivery, settings.FreightPaymentMode);
        Assert.Equal(ShippingAuthoringCalculationMode.Manual, settings.CalculationMode);
    }

    [Fact]
    public void A_later_draft_edit_never_rewrites_an_existing_capture()
    {
        var draft = RichDraft();
        var captured = ShippingAuthoringRevisionSnapshot.Capture(draft, Now.AddHours(1));
        var originalPayload = captured.Payload;
        var originalRevision = captured.SourceRevision;
        var originalCapturedAt = captured.CapturedAt;

        var group = draft.RegionGroups.OrderBy(candidate => candidate.Order).First();
        draft.ReplaceGroupRateRows(
            group.GroupId,
            [Row(null, 0, 900, 1, 2, null, null, 1_000.00m, 10.00m, 1, 2)],
            "IRT",
            Now.AddHours(2));

        Assert.Equal(originalPayload, captured.Payload);
        Assert.Equal(originalRevision, captured.SourceRevision);
        Assert.Equal(originalCapturedAt, captured.CapturedAt);
        Assert.NotEqual(draft.Revision, captured.SourceRevision);

        // A new revision yields a DIFFERENT capture with different content, never an overwrite.
        var next = ShippingAuthoringRevisionSnapshot.Capture(draft, Now.AddHours(3));
        Assert.NotEqual(captured.SnapshotId, next.SnapshotId);
        Assert.NotEqual(captured.SourceRevision, next.SourceRevision);
        Assert.NotEqual(captured.Payload, next.Payload);
        Assert.Single(next.ReadPayload().Groups.Single(g => g.GroupId == group.GroupId).RateRows);
    }

    [Fact]
    public void Capturing_the_same_revision_twice_reuses_identity_time_and_content_contract()
    {
        var draft = RichDraft();
        var first = ShippingAuthoringRevisionSnapshot.Capture(draft, Now.AddHours(1));
        // The same aggregate read again at the SAME revision must describe the same capture identity key.
        var second = ShippingAuthoringRevisionSnapshot.Capture(draft, Now.AddHours(5));

        Assert.Equal(first.DraftId, second.DraftId);
        Assert.Equal(first.SourceRevision, second.SourceRevision);
        Assert.Equal(first.OwnerKind, second.OwnerKind);
        Assert.Equal(first.OwnerTenantId, second.OwnerTenantId);
        Assert.Equal(first.OwnerSellerPartyId, second.OwnerSellerPartyId);
        Assert.Equal(first.Payload, second.Payload);
    }

    [Fact]
    public void An_unknown_format_version_or_unreadable_payload_is_rejected_never_decoded_as_empty()
    {
        var draft = RichDraft();
        var snapshot = ShippingAuthoringRevisionSnapshot.Capture(draft, Now);
        var json = snapshot.Payload;

        var forged = json.Replace("\"FormatVersion\":1", "\"FormatVersion\":99", StringComparison.Ordinal);
        Assert.NotEqual(json, forged);

        AssertRejected(forged);
        AssertRejected(string.Empty);
        AssertRejected("{");
        AssertRejected("null");
    }

    [Fact]
    public void Every_owner_kind_is_preserved_in_the_captured_identity()
    {
        var marketplace = ShippingAuthoringRevisionSnapshot.Capture(
            ShippingAuthoringDraft.Create(
                ShippingDraftOwner.ForMarketplace(),
                "post",
                null,
                [new ShippingAuthoringRegionGroupInput(null, [Node(Tehran)])],
                Now),
            Now);

        var sellerId = Guid.Parse("77777777-7777-7777-7777-777777777777");
        var seller = ShippingAuthoringRevisionSnapshot.Capture(
            ShippingAuthoringDraft.Create(
                ShippingDraftOwner.ForSeller(sellerId),
                "post",
                null,
                [],
                Now),
            Now);

        Assert.Equal(ShippingDraftOwnerKind.Marketplace, marketplace.ReadPayload().Owner.Kind);
        Assert.Null(marketplace.ReadPayload().Owner.TenantId);
        Assert.Null(marketplace.ReadPayload().Owner.SellerPartyId);
        Assert.Equal(ShippingDraftOwnerKind.Seller, seller.ReadPayload().Owner.Kind);
        Assert.Equal(sellerId, seller.ReadPayload().Owner.SellerPartyId);
        Assert.Null(seller.ReadPayload().Owner.TenantId);
        // An empty draft is an explicitly incomplete configuration, never unlimited/free coverage.
        Assert.Empty(seller.ReadPayload().Groups);
    }

    [Fact]
    public void Capture_never_bumps_the_draft_revision_or_updated_at()
    {
        var draft = RichDraft();
        var revision = draft.Revision;
        var updatedAt = draft.UpdatedAt;

        _ = ShippingAuthoringRevisionSnapshot.Capture(draft, Now.AddDays(1));

        Assert.Equal(revision, draft.Revision);
        Assert.Equal(updatedAt, draft.UpdatedAt);
    }

    private static void AssertRejected(string payload)
    {
        var exception = Assert.Throws<SemanticException>(
            () => ShippingAuthoringRevisionSnapshot.DecodeStoredPayload(payload));
        Assert.Equal(ShippingErrorCodes.AuthoringRevisionSnapshotInvalid, exception.Error.Code);
    }
}
