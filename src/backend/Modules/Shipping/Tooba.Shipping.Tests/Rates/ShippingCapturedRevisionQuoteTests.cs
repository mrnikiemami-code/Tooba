using Tooba.Geography.Contracts.Places;
using Tooba.Shipping.Contracts.Authoring;
using Tooba.Shipping.Contracts.Packaging;
using Tooba.Shipping.Contracts.Rates;
using Tooba.Shipping.Domain.Aggregates;
using Tooba.Shipping.Domain.Authoring;
using Tooba.Shipping.Domain.Rates;
using Xunit;

namespace Tooba.Shipping.Tests.Rates;

/// <summary>
/// TB-SHIPPING-CAPTURED-MANUAL-QUOTE-215 — قیمت‌گذاری خالص و قطعی یک بازبینی capture‌شده با ارزیابی
/// سه‌محوری (وزن/تعداد/مبلغ کالا) و دو مؤلفهٔ هزینه از یک ردیف.
/// </summary>
/// <remarks>
/// ورودی‌ها از payload واقعیِ <see cref="ShippingAuthoringRevisionSnapshot"/> (کارخانهٔ دامنه + codec)
/// ساخته می‌شوند؛ هیچ DB/EF/HTTP/ساعت در این تست‌ها دخالت ندارد. این تست‌ها ادعای فعال‌سازی/صلاحیت
/// مشتری یا آمادگی عملیاتی نمی‌سازند.
/// </remarks>
public sealed class ShippingCapturedRevisionQuoteTests
{
    private static readonly Guid Country = Guid.Parse("77777777-7777-7777-7777-777777777777");
    private static readonly Guid Tehran = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Isfahan = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid CheapestRow = Guid.Parse("aaaaaaaa-0000-4000-8000-000000000001");
    private static readonly Guid HeaviestRow = Guid.Parse("aaaaaaaa-0000-4000-8000-000000000002");
    private static readonly Guid TypeId = Guid.Parse("66666666-6666-6666-6666-666666666666");
    private const string Service = "post";
    private const string Option = "express";
    private static readonly DateTimeOffset Now = new(2026, 10, 25, 10, 0, 0, TimeSpan.Zero);

    // ---------------------------------------------------------------- the three-AND-axis selection

    [Fact]
    public void Two_rows_in_the_same_area_take_freight_and_packaging_from_the_SAME_three_axis_match()
    {
        var payload = Payload(
            [[Tehran]],
            [
                [
                    Row(null, 0, 5000, 1, 10, 100_000.25m, 5_000_000.75m, 120_000.00m, 5_000.00m, 1, 3),
                    Row(null, 5000, null, 11, null, null, null, 180_000.50m, 9_500.75m, 2, 5),
                ],
            ]);

        var light = ShippingRatePolicyEngine.QuoteCapturedRevision(
            Request(payload, weightGrams: 3_000, itemCount: 3, goodsAmount: 200_000.00m));

        Assert.True(light.IsQuoted, light.ReasonCode);
        Assert.Equal(ShippingCapturedQuoteOutcome.Quoted, light.Outcome);
        Assert.Equal(RowId(payload, 120_000.00m), light.Selection!.RateRowId);
        Assert.Equal(120_000.00m, light.FreightAmount);
        Assert.Equal(5_000.00m, light.PackagingAmount);
        Assert.Equal(125_000.00m, light.TotalExpenseAmount);
        Assert.Equal(100_000.25m, light.Selection.Conditions.Merchandise!.Min);
        Assert.Equal(5_000_000.75m, light.Selection.Conditions.Merchandise!.Max);
        Assert.Equal(1, light.Selection.Conditions.Count!.Min);
        Assert.Equal(10, light.Selection.Conditions.Count!.Max);

        // The OTHER row is selected by its own axes, and its packaging comes with it — never a mix.
        var heavy = ShippingRatePolicyEngine.QuoteCapturedRevision(
            Request(payload, weightGrams: 6_000, itemCount: 12));

        Assert.True(heavy.IsQuoted, heavy.ReasonCode);
        Assert.Equal(RowId(payload, 180_000.50m), heavy.Selection!.RateRowId);
        Assert.Equal(180_000.50m, heavy.FreightAmount);
        Assert.Equal(9_500.75m, heavy.PackagingAmount);
        Assert.Equal(189_501.25m, heavy.TotalExpenseAmount);
    }

    [Fact]
    public void Tiers_are_half_open_so_a_boundary_prices_once_and_touching_ranges_never_overlap()
    {
        var payload = Payload(
            [[Tehran]],
            [
                [
                    Row(null, 0, 2000, 1, 5, null, null, 10_000.00m, 1_000.00m, 1, 2),
                    Row(null, 2000, 4000, 6, null, null, null, 20_000.00m, 2_000.00m, 2, 3),
                ],
            ]);

        // min is inclusive: 2000 belongs to the SECOND tier only.
        var atLowerBound = ShippingRatePolicyEngine.QuoteCapturedRevision(
            Request(payload, weightGrams: 2000, itemCount: 6));
        Assert.True(atLowerBound.IsQuoted, atLowerBound.ReasonCode);
        Assert.Equal(RowId(payload, 20_000.00m), atLowerBound.Selection!.RateRowId);

        // max is exclusive: 4000 is in no tier at all.
        var atUpperBound = ShippingRatePolicyEngine.QuoteCapturedRevision(
            Request(payload, weightGrams: 4000));
        Assert.Equal(ShippingCapturedQuoteOutcome.ConditionNotMatched, atUpperBound.Outcome);
        AssertNoMoney(atUpperBound);

        // Explicit 0 is a PRESENT measurement, distinct from missing: 0 falls in [0,2000).
        var explicitZero = ShippingRatePolicyEngine.QuoteCapturedRevision(
            Request(payload, weightGrams: 0, itemCount: 1));
        Assert.True(explicitZero.IsQuoted, explicitZero.ReasonCode);
        Assert.Equal(RowId(payload, 10_000.00m), explicitZero.Selection!.RateRowId);
    }

    [Fact]
    public void A_missing_measurement_for_a_known_axis_is_not_quoted_and_is_never_a_zero_rate()
    {
        var payload = Payload(
            [[Tehran]],
            [[Row(null, 0, 5000, 1, 10, 100_000.25m, null, 120_000.00m, 5_000.00m, 1, 3)]]);

        // Weight axis exists, weight not declared.
        var missingWeight = ShippingRatePolicyEngine.QuoteCapturedRevision(
            Request(payload, weightGrams: null, itemCount: 3, goodsAmount: 200_000.00m));
        Assert.Equal(ShippingCapturedQuoteOutcome.MeasurementRequired, missingWeight.Outcome);
        Assert.Equal(ShippingCapturedQuoteReasonCodes.MeasurementRequired, missingWeight.ReasonCode);
        AssertNoMoney(missingWeight);

        // Merchandise axis exists, the final after-discount-and-tax amount is not declared.
        var missingGoods = ShippingRatePolicyEngine.QuoteCapturedRevision(
            Request(payload, weightGrams: 1_000, itemCount: 3, goodsAmount: null));
        Assert.Equal(ShippingCapturedQuoteOutcome.MeasurementRequired, missingGoods.Outcome);
        AssertNoMoney(missingGoods);
    }

    [Fact]
    public void A_known_axis_that_does_not_match_excludes_its_row_before_any_missing_axis_is_considered()
    {
        var payload = Payload(
            [[Tehran]],
            [
                [
                    // Excluded by weight; it also has a merchandise axis whose amount is NOT supplied.
                    Row(null, 0, 2000, null, null, 1_000m, 2_000m, 10_000.00m, 1_000.00m, 1, 2),
                    Row(null, 2000, null, null, null, null, null, 20_000.00m, 2_000.00m, 2, 3),
                ],
            ]);

        var quote = ShippingRatePolicyEngine.QuoteCapturedRevision(
            Request(payload, weightGrams: 5000, goodsAmount: null));

        // The irrelevant row must not turn a valid unconditional row into "measurement required".
        Assert.True(quote.IsQuoted, quote.ReasonCode);
        Assert.Equal(RowId(payload, 20_000.00m), quote.Selection!.RateRowId);
        Assert.Null(quote.Selection.Conditions.Merchandise);
    }

    [Fact]
    public void Two_matching_rows_are_ambiguous_even_when_their_amounts_are_identical()
    {
        // An AMBIGUOUS pair cannot be authored today (the authoring guard refuses overlapping rows), but it
        // can exist in an already captured legacy/corrupt revision. The evaluator must fail closed instead of
        // resolving it by row order or by an equal amount.
        var basePayload = Payload(
            [[Tehran]],
            [[Row(null, null, null, null, null, null, null, 50_000.00m, 4_000.00m, 1, 3)]]);
        var payload = ReplaceRows(
            basePayload,
            [
                CapturedRow(null, null, null, null, null, null, null, 50_000.00m, 4_000.00m, 1, 3),
                CapturedRow(null, null, null, null, null, null, null, 50_000.00m, 4_000.00m, 1, 3),
            ]);

        var quote = ShippingRatePolicyEngine.QuoteCapturedRevision(Request(payload));

        Assert.Equal(ShippingCapturedQuoteOutcome.ConditionAmbiguous, quote.Outcome);
        Assert.Equal(ShippingCapturedQuoteReasonCodes.ConditionAmbiguous, quote.ReasonCode);
        Assert.Null(quote.Selection);
        AssertNoMoney(quote);
    }

    // ---------------------------------------------------------------- fail-closed rejects

    [Fact]
    public void Missing_coverage_multiple_groups_currency_and_identity_are_all_rejected_without_money()
    {
        var single = Payload(
            [[Tehran]],
            [[Row(null, null, null, null, null, null, null, 10_000.00m, 1_000.00m, 1, 2)]]);

        // Another city is simply not covered.
        var uncovered = ShippingRatePolicyEngine.QuoteCapturedRevision(
            new ShippingCapturedRevisionQuoteRequest(
                Identity(single),
                single,
                ShippingDestinationScope.ForCity(Country, null, Isfahan),
                CurrencyCode: "IRT"));
        Assert.Equal(ShippingCapturedQuoteOutcome.CoverageMissing, uncovered.Outcome);
        AssertNoMoney(uncovered);

        // Two groups both covering Tehran: no first/deepest/cheapest decision is allowed.
        var twoGroups = Payload(
            [[Tehran], [Tehran]],
            [
                [Row(null, null, null, null, null, null, null, 10_000.00m, 1_000.00m, 1, 2)],
                [Row(null, null, null, null, null, null, null, 20_000.00m, 2_000.00m, 1, 2)],
            ]);
        var ambiguousGroup = ShippingRatePolicyEngine.QuoteCapturedRevision(
            new ShippingCapturedRevisionQuoteRequest(
                Identity(twoGroups),
                twoGroups,
                ShippingDestinationScope.ForCity(Country, null, Tehran),
                CurrencyCode: "IRT"));
        Assert.Equal(ShippingCapturedQuoteOutcome.GroupAmbiguous, ambiguousGroup.Outcome);
        AssertNoMoney(ambiguousGroup);

        // Currency: missing on either side is its own answer; a different currency is never converted.
        var missingCurrency = ShippingRatePolicyEngine.QuoteCapturedRevision(
            new ShippingCapturedRevisionQuoteRequest(
                Identity(single),
                single,
                ShippingDestinationScope.ForCity(Country, null, Tehran)));
        Assert.Equal(ShippingCapturedQuoteOutcome.CurrencyMissing, missingCurrency.Outcome);
        AssertNoMoney(missingCurrency);

        var mismatchedCurrency = ShippingRatePolicyEngine.QuoteCapturedRevision(
            Request(single, currency: "USD"));
        Assert.Equal(ShippingCapturedQuoteOutcome.CurrencyMismatch, mismatchedCurrency.Outcome);
        AssertNoMoney(mismatchedCurrency);

        // Identity: another draft, another revision or another owner never prices this payload.
        var otherDraft = ShippingRatePolicyEngine.QuoteCapturedRevision(
            new ShippingCapturedRevisionQuoteRequest(
                Identity(single) with { DraftId = Guid.NewGuid() },
                single,
                ShippingDestinationScope.ForCity(Country, null, Tehran),
                CurrencyCode: "IRT"));
        Assert.Equal(ShippingCapturedQuoteOutcome.IdentityMismatch, otherDraft.Outcome);
        AssertNoMoney(otherDraft);

        var otherRevision = ShippingRatePolicyEngine.QuoteCapturedRevision(
            new ShippingCapturedRevisionQuoteRequest(
                Identity(single) with { SourceRevision = single.SourceRevision + 1 },
                single,
                ShippingDestinationScope.ForCity(Country, null, Tehran),
                CurrencyCode: "IRT"));
        Assert.Equal(ShippingCapturedQuoteOutcome.IdentityMismatch, otherRevision.Outcome);
        AssertNoMoney(otherRevision);

        var otherOwner = ShippingRatePolicyEngine.QuoteCapturedRevision(
            new ShippingCapturedRevisionQuoteRequest(
                Identity(single) with { OwnerTenantId = "someone-else" },
                single,
                ShippingDestinationScope.ForCity(Country, null, Tehran),
                CurrencyCode: "IRT"));
        Assert.Equal(ShippingCapturedQuoteOutcome.IdentityMismatch, otherOwner.Outcome);
        AssertNoMoney(otherOwner);
    }

    [Fact]
    public void An_incomplete_destination_path_or_incoherent_stored_bounds_fails_closed()
    {
        var payload = Payload(
            [[Tehran]],
            [[Row(null, null, null, null, null, null, null, 10_000.00m, 1_000.00m, 1, 2)]]);

        var emptyCountry = ShippingRatePolicyEngine.QuoteCapturedRevision(
            new ShippingCapturedRevisionQuoteRequest(
                Identity(payload),
                payload,
                new ShippingDestinationScope(Guid.Empty, null, Tehran),
                CurrencyCode: "IRT"));
        Assert.Equal(ShippingCapturedQuoteOutcome.DestinationPathInvalid, emptyCountry.Outcome);
        AssertNoMoney(emptyCountry);

        var serviceAreaWithoutCity = ShippingRatePolicyEngine.QuoteCapturedRevision(
            new ShippingCapturedRevisionQuoteRequest(
                Identity(payload),
                payload,
                new ShippingDestinationScope(Country, null, null, Guid.NewGuid()),
                CurrencyCode: "IRT"));
        Assert.Equal(ShippingCapturedQuoteOutcome.DestinationPathInvalid, serviceAreaWithoutCity.Outcome);
        AssertNoMoney(serviceAreaWithoutCity);

        // A stored upper bound that is not greater than its lower bound is a configuration defect, not a
        // tier to normalise. The authoring guard would refuse it today, but a corrupt/legacy captured row
        // must still fail closed instead of being reinterpreted.
        var invalidBounds = ReplaceRows(
            payload,
            [CapturedRow(null, 2000, 2000, null, null, null, null, 10_000.00m, 1_000.00m, 1, 2)]);
        var invalid = ShippingRatePolicyEngine.QuoteCapturedRevision(
            new ShippingCapturedRevisionQuoteRequest(
                Identity(invalidBounds),
                invalidBounds,
                ShippingDestinationScope.ForCity(Country, null, Tehran),
                CurrencyCode: "IRT"));
        Assert.Equal(ShippingCapturedQuoteOutcome.ConfigurationInvalid, invalid.Outcome);
        AssertNoMoney(invalid);

        // A negative stored amount is also a defect: it is never treated as "free".
        var negative = ReplaceRows(
            payload,
            [CapturedRow(null, null, null, null, null, null, null, -1m, 1_000.00m, 1, 2)]);
        var negativeAmount = ShippingRatePolicyEngine.QuoteCapturedRevision(
            new ShippingCapturedRevisionQuoteRequest(
                Identity(negative),
                negative,
                ShippingDestinationScope.ForCity(Country, null, Tehran),
                CurrencyCode: "IRT"));
        Assert.Equal(ShippingCapturedQuoteOutcome.ConfigurationInvalid, negativeAmount.Outcome);
        AssertNoMoney(negativeAmount);
    }

    // ---------------------------------------------------------------- payment split + configuration

    [Fact]
    public void Prepaid_pays_everything_at_checkout_and_collect_on_delivery_moves_only_the_freight()
    {
        var prepaid = Payload(
            [[Tehran]],
            [[Row(null, null, null, null, null, null, null, 180_000.50m, 9_500.75m, 2, 5)]],
            payerMode: ShippingFreightPayerMode.Prepaid);

        var prepaidQuote = ShippingRatePolicyEngine.QuoteCapturedRevision(Request(prepaid));
        Assert.True(prepaidQuote.IsQuoted, prepaidQuote.ReasonCode);
        Assert.Equal(180_000.50m, prepaidQuote.FreightAmount);
        Assert.Equal(9_500.75m, prepaidQuote.PackagingAmount);
        Assert.Equal(189_501.25m, prepaidQuote.TotalExpenseAmount);
        Assert.Equal(189_501.25m, prepaidQuote.PayableAtCheckoutAmount);
        Assert.Equal(0m, prepaidQuote.CarrierCollectAmount);
        Assert.Equal(ShippingFreightPayerMode.Prepaid, prepaidQuote.FreightPayerMode);

        var collect = Payload(
            [[Tehran]],
            [[Row(null, null, null, null, null, null, null, 180_000.50m, 9_500.75m, 2, 5)]],
            payerMode: ShippingFreightPayerMode.CollectOnDelivery);

        var collectQuote = ShippingRatePolicyEngine.QuoteCapturedRevision(Request(collect));
        Assert.True(collectQuote.IsQuoted, collectQuote.ReasonCode);
        Assert.Equal(189_501.25m, collectQuote.TotalExpenseAmount);
        Assert.Equal(9_500.75m, collectQuote.PayableAtCheckoutAmount);
        Assert.Equal(180_000.50m, collectQuote.CarrierCollectAmount);
    }

    [Fact]
    public void An_unconfigured_or_non_manual_method_is_not_quoted_and_never_a_zero_price()
    {
        var rows = new[]
        {
            Row(null, null, null, null, null, null, null, 10_000.00m, 1_000.00m, 1, 2),
        };

        var unconfigured = Payload([[Tehran]], [rows], withSettings: false);
        var noSettings = ShippingRatePolicyEngine.QuoteCapturedRevision(Request(unconfigured));
        Assert.Equal(ShippingCapturedQuoteOutcome.NotConfigured, noSettings.Outcome);
        AssertNoMoney(noSettings);

        var free = Payload([[Tehran]], [rows], calculationMode: ShippingAuthoringCalculationMode.Free);
        var freeQuote = ShippingRatePolicyEngine.QuoteCapturedRevision(Request(free));
        Assert.Equal(ShippingCapturedQuoteOutcome.CalculationModeUnavailable, freeQuote.Outcome);
        Assert.Equal(ShippingCapturedQuoteReasonCodes.FreeNotImplemented, freeQuote.ReasonCode);
        AssertNoMoney(freeQuote);

        var carrier = Payload([[Tehran]], [rows], calculationMode: ShippingAuthoringCalculationMode.CarrierApi);
        var carrierQuote = ShippingRatePolicyEngine.QuoteCapturedRevision(Request(carrier));
        Assert.Equal(ShippingCapturedQuoteOutcome.CalculationModeUnavailable, carrierQuote.Outcome);
        Assert.Equal(ShippingCapturedQuoteReasonCodes.CarrierModeUnavailable, carrierQuote.ReasonCode);
        AssertNoMoney(carrierQuote);
    }

    // ---------------------------------------------------------------- metadata / immutability

    [Fact]
    public void The_quote_carries_full_provenance_and_the_captured_payload_survives_a_later_edit()
    {
        var draft = Draft(
            [[Tehran]],
            [[Row(null, null, null, null, null, null, null, 120_000.00m, 5_000.00m, 1, 3)]],
            payerMode: ShippingFreightPayerMode.CollectOnDelivery,
            withSettings: true,
            withSchedule: true);
        var snapshot = ShippingAuthoringRevisionSnapshot.Capture(draft, Now.AddHours(1));
        var payload = snapshot.ReadPayload();

        var quote = ShippingRatePolicyEngine.QuoteCapturedRevision(Request(payload));

        Assert.True(quote.IsQuoted, quote.ReasonCode);
        Assert.Equal(Service, quote.ServiceCode);
        Assert.Equal(Option, quote.OptionCode);
        Assert.Equal("IRT", quote.CurrencyCode);
        Assert.Equal(ShippingCapturedQuoteEligibility.NotEvaluated, quote.Eligibility);

        var selection = quote.Selection!;
        Assert.Equal(payload.Groups[0].GroupId, selection.GroupId);
        Assert.Equal(payload.DraftId, selection.DraftId);
        Assert.Equal(payload.SourceRevision, selection.SourceRevision);
        Assert.Equal(payload.FormatVersion, selection.FormatVersion);
        Assert.Equal(TypeId, selection.ShippingTypeId);
        Assert.Equal(1, selection.MinDeliveryDays);
        Assert.Equal(3, selection.MaxDeliveryDays);

        // The stored week travels as metadata only: no conversion into preparation/lead days or windows.
        Assert.NotNull(selection.WeeklySchedule);
        Assert.Equal("Asia/Tehran", selection.WeeklySchedule!.TimeZoneId);
        Assert.Equal(7, selection.WeeklySchedule.Days.Count);
        Assert.Equal(ShippingFreightPayerMode.CollectOnDelivery, selection.Settings.FreightPaymentMode);
        Assert.Equal(ShippingAuthoringCalculationMode.Manual, selection.Settings.CalculationMode);

        // A later edit of the SAME aggregate cannot rewrite the already captured revision.
        draft.ReplaceGroupRateRows(
            draft.RegionGroups[0].GroupId,
            [Row(null, null, null, null, null, null, null, 999_000.00m, 99_000.00m, 4, 9)],
            "IRT",
            Now.AddHours(2));

        var afterEdit = snapshot.ReadPayload();
        var storedRow = afterEdit.Groups.Single(group => group.GroupId == selection.GroupId)
            .RateRows.Single(row => row.RateRowId == selection.RateRowId);
        Assert.Equal(120_000.00m, storedRow.FreightAmount);
        Assert.Equal(5_000.00m, storedRow.PackagingAmount);
        Assert.Equal(1, storedRow.MinDeliveryDays);
        Assert.Equal(3, storedRow.MaxDeliveryDays);
    }

    // ---------------------------------------------------------------- legacy behaviour untouched

    [Fact]
    public void The_existing_legacy_quote_paths_are_unchanged()
    {
        var destination = new DestinationIdentifiers(Country, null);
        var policy = ShippingRatePolicy.Create(
            Service,
            ShippingDestinationScope.ForCountry(Country),
            120_000m,
            "IRR",
            1,
            2,
            null,
            Now);
        var grant = ShippingCoverageGrant.Create(Service, ShippingDestinationScope.ForCountry(Country), Now);

        var quoted = ShippingRatePolicyEngine.Quote(
            new ShippingQuoteRequest(Service, destination, 500_000m, "IRR"),
            [policy],
            [grant]);

        Assert.True(quoted.IsQuoted);
        Assert.Equal(120_000m, quoted.ChargeAmount);
        Assert.Equal(1, quoted.PreparationDays);
        Assert.Equal(2, quoted.LeadDays);

        var unpriced = ShippingRatePolicyEngine.Quote(
            new ShippingQuoteRequest(Service, destination, 500_000m, "IRR"),
            [],
            [grant]);

        Assert.False(unpriced.IsQuoted);
        Assert.Equal(ShippingServiceabilityOutcome.UnknownService, unpriced.Outcome);
        Assert.Equal(ShippingReasonCodes.ServiceNotPriced, unpriced.ReasonCode);
    }

    // ---------------------------------------------------------------- fixtures

    private static ShippingAuthoringDraft Draft(
        IReadOnlyList<Guid[]> areaGroups,
        IReadOnlyList<ShippingAuthoringDraft.ShippingAuthoringRateRowInput[]>? rowsPerGroup = null,
        ShippingFreightPayerMode payerMode = ShippingFreightPayerMode.Prepaid,
        ShippingAuthoringCalculationMode calculationMode = ShippingAuthoringCalculationMode.Manual,
        bool withSettings = true,
        bool withSchedule = false,
        string currency = "IRT")
    {
        var draft = ShippingAuthoringDraft.Create(
            ShippingDraftOwner.ForStore("tb215-store"),
            Service,
            Option,
            areaGroups
                .Select(areas => new ShippingAuthoringRegionGroupInput(
                    null,
                    areas.Select(id => new ShippingDestinationNode(ShippingDestinationNodeKind.City, id)).ToArray()))
                .ToArray(),
            Now);

        var groups = draft.RegionGroups.OrderBy(group => group.Order).ToArray();
        if (rowsPerGroup is not null)
        {
            for (var index = 0; index < rowsPerGroup.Count && index < groups.Length; index++)
            {
                draft.ReplaceGroupRateRows(
                    groups[index].GroupId,
                    rowsPerGroup[index],
                    currency,
                    Now.AddMinutes(1 + index));
            }
        }

        if (withSchedule)
        {
            draft.ApplyWeeklySchedule(
                draft.PreflightWeeklySchedule(
                    "Asia/Tehran",
                    [
                        new ShippingAuthoringDraft.ScheduleDayInput(1, true, new TimeOnly(9, 0), new TimeOnly(17, 0)),
                        new ShippingAuthoringDraft.ScheduleDayInput(2, false, new TimeOnly(9, 0), new TimeOnly(17, 0)),
                        new ShippingAuthoringDraft.ScheduleDayInput(3, true, new TimeOnly(9, 0), new TimeOnly(17, 0)),
                        new ShippingAuthoringDraft.ScheduleDayInput(4, true, new TimeOnly(9, 0), new TimeOnly(17, 0)),
                        new ShippingAuthoringDraft.ScheduleDayInput(5, true, new TimeOnly(9, 0), new TimeOnly(17, 0)),
                        new ShippingAuthoringDraft.ScheduleDayInput(6, true, new TimeOnly(9, 0), new TimeOnly(13, 0)),
                        new ShippingAuthoringDraft.ScheduleDayInput(7, false, null, null),
                    ]),
                Now.AddMinutes(8));
        }

        if (withSettings)
        {
            draft.ApplyMethodSettings(
                draft.PreflightMethodSettings(payerMode, calculationMode),
                Now.AddMinutes(9));
        }

        draft.ApplyTypeAssignment(draft.PreflightTypeAssignment(TypeId), Now.AddMinutes(10));
        return draft;
    }

    private static ShippingAuthoringRevisionPayload Payload(
        IReadOnlyList<Guid[]> areaGroups,
        IReadOnlyList<ShippingAuthoringDraft.ShippingAuthoringRateRowInput[]>? rowsPerGroup = null,
        ShippingFreightPayerMode payerMode = ShippingFreightPayerMode.Prepaid,
        ShippingAuthoringCalculationMode calculationMode = ShippingAuthoringCalculationMode.Manual,
        bool withSettings = true,
        bool withSchedule = false,
        string currency = "IRT")
        => ShippingAuthoringRevisionSnapshot
            .Capture(
                Draft(areaGroups, rowsPerGroup, payerMode, calculationMode, withSettings, withSchedule, currency),
                Now.AddHours(1))
            .ReadPayload();

    /// <summary>
    /// یک payload با همان هویت/بازبینی اما ردیف‌های داده‌شده می‌سازد تا «کران/مبلغ ناسازگار» واقعاً از
    /// مسیر capture عبور کند، نه از یک بدنهٔ دست‌ساز.
    /// </summary>
    private static ShippingAuthoringRevisionPayload ReplaceRows(
        ShippingAuthoringRevisionPayload original,
        ShippingAuthoringRevisionRateRow[] rows)
    {
        var firstGroupId = original.Groups[0].GroupId;
        var groups = original.Groups
            .Select(group => group.GroupId == firstGroupId
                ? group with { RateRows = rows }
                : group)
            .ToArray();

        return original with { Groups = groups };
    }

    /// <summary>یک ردیف در همان شکل تایپ‌شدهٔ payload (برای بازتولید دادهٔ کهنه/ناسازگار).</summary>
    private static ShippingAuthoringRevisionRateRow CapturedRow(
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
        => new(
            id ?? Guid.NewGuid(),
            minWeight,
            maxWeight,
            minCount,
            maxCount,
            minAmount,
            maxAmount,
            freight,
            packaging,
            minDays,
            maxDays);

    /// <summary>شناسهٔ ردیف واقعیِ ذخیره‌شده را از کرایهٔ یکتای همان ردیف پیدا می‌کند.</summary>
    private static Guid RowId(ShippingAuthoringRevisionPayload payload, decimal freightAmount)
        => payload.Groups
            .SelectMany(group => group.RateRows)
            .Single(row => row.FreightAmount == freightAmount)
            .RateRowId;

    private static ShippingCapturedRevisionIdentity Identity(ShippingAuthoringRevisionPayload payload)
        => new(
            payload.Owner.Kind,
            payload.Owner.TenantId,
            payload.Owner.SellerPartyId,
            payload.DraftId,
            payload.SourceRevision);

    private static ShippingCapturedRevisionQuoteRequest Request(
        ShippingAuthoringRevisionPayload payload,
        long? weightGrams = null,
        int? itemCount = null,
        decimal? goodsAmount = null,
        string? currency = "IRT")
        => new(
            Identity(payload),
            payload,
            ShippingDestinationScope.ForCity(Country, null, Tehran),
            weightGrams,
            itemCount,
            goodsAmount,
            currency);

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

    private static void AssertNoMoney(ShippingCapturedRevisionQuote quote)
    {
        Assert.False(quote.IsQuoted);
        Assert.Null(quote.CurrencyCode);
        Assert.Null(quote.FreightAmount);
        Assert.Null(quote.PackagingAmount);
        Assert.Null(quote.TotalExpenseAmount);
        Assert.Null(quote.PayableAtCheckoutAmount);
        Assert.Null(quote.CarrierCollectAmount);
        Assert.Null(quote.FreightPayerMode);
    }
}
