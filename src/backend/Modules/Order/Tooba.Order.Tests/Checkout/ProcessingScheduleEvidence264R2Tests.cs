using Tooba.BuildingBlocks;
using Tooba.Order.Application.Storefront.Shipping;
using Tooba.Order.Domain.Checkout;
using Tooba.Shipping.Contracts.Authoring;
using Tooba.Shipping.Contracts.Packaging;
using Tooba.Shipping.Contracts.Rates;
using Tooba.Shipping.Contracts.Scheduling;
using Xunit;

namespace Tooba.Order.Tests.Checkout;

/// <summary>
/// TB-264-R2 (delivery closure): a capture that carries the operator's explicit <b>processing week</b> is
/// written as format <b>4</b> by the same release. The order-side evidence contract must accept that format,
/// otherwise every real customer selection of such a method is refused as «not priced yet» and the whole
/// authoring → publish → customer calendar → accepted-plan chain can never be completed.
/// </summary>
/// <remarks>
/// The schedule itself stays in Shipping; the evidence remains compact. Format versions 1/2/3 keep their
/// exact historical meaning and nothing is backfilled or reinterpreted.
/// </remarks>
public sealed class ProcessingScheduleEvidence264R2Tests
{
    private static readonly Guid Snapshot = Guid.Parse("01a1cd87-711c-7000-b2c5-c60cd630ffdb");
    private static readonly Guid Draft = Guid.Parse("01a1cd43-b98d-7000-b2a9-a35ab39d8058");
    private static readonly Guid AreaGroup = Guid.Parse("01a1cd44-6c1f-7000-8d2a-3650a23cc6a0");
    private static readonly Guid RateRow = Guid.Parse("01a1cd44-6c21-7000-870e-0efc6efc584a");
    private static readonly Guid ShippingType = Guid.Parse("01a16f43-6b4a-7000-8670-4c16e14c6345");

    [Fact]
    public void A_format_four_capture_with_a_processing_week_yields_an_accepted_policy_three_plan()
    {
        var quote = Quote();
        var date = new DateOnly(2026, 11, 14);
        var opens = new TimeOnly(9, 0).Ticks;
        var closes = new TimeOnly(10, 0).Ticks;

        var resolved = StorefrontPlannedDispatchResolver.TryResolve(
            quote,
            date,
            opens,
            closes,
            StorefrontPlannedDispatchResolver.CurrentPolicyVersion,
            out var plan,
            out var reason);

        Assert.True(resolved, reason);
        Assert.NotNull(plan);
        Assert.Equal(ShippingDispatchSchedulingAuthority.WindowAwarePolicyVersion, plan!.PolicyVersion);
        Assert.Equal(300, plan.PreparationMinutes);
        Assert.Equal(60, plan.HandoffMinutes);
        Assert.NotNull(plan.PreparationStartUtc);
        Assert.NotNull(plan.TransferDepartureUtc);
        Assert.NotNull(plan.HandoffDeadlineUtc);
        Assert.True(plan.PreparationStartUtc <= plan.TransferDepartureUtc);
        Assert.True(plan.TransferDepartureUtc <= plan.HandoffDeadlineUtc);
        Assert.Equal(plan.HandoffDeadlineUtc!.Value.AddMinutes(-60), plan.TransferDepartureUtc!.Value);
        Assert.True(plan.HandoffDate <= date);

        // The evidence contract is the gate that used to refuse this format before any write happened.
        var evidence = ShippingGroupAcceptedEvidence.Create(
            Snapshot,
            Draft,
            6,
            quote.FormatVersion!.Value,
            3,
            ShippingGroupOwner.Store("store-alpha"),
            AreaGroup,
            RateRow,
            ShippingType,
            150_000m,
            8_000m,
            "IRR",
            158_000m,
            0m,
            date,
            opens,
            closes,
            "Asia/Tehran",
            1,
            2,
            carrierQuote: null,
            plannedDispatch: plan);

        Assert.Equal(ShippingGroupAcceptedEvidence.ProcessingScheduleFormatVersion, evidence.FormatVersion);
        Assert.NotNull(evidence.PlannedDispatch);
        Assert.Equal(3, evidence.PlannedDispatch!.PolicyVersion);
    }

    private static ShippingCapturedQuoteResult Quote()
    {
        var carrier = new ShippingCapturedCarrierWeek(
            "Asia/Tehran",
            Enumerable.Range(1, 7)
                .Select(day => new ShippingCapturedCarrierWeekDay(day, true, new TimeOnly(8, 0), new TimeOnly(20, 0)))
                .ToArray());
        var processing = new ShippingCapturedProcessingWeek(
            "Asia/Tehran",
            Enumerable.Range(1, 7)
                .Select(day => new ShippingCapturedProcessingWeekDay(day, day != 5, new TimeOnly(9, 0), new TimeOnly(18, 0)))
                .ToArray());
        var receiving = new ShippingCapturedReceivingWeek(
            "Asia/Tehran",
            Enumerable.Range(1, 7)
                .Select(day => new ShippingCapturedReceivingWeekDay(day, true, new TimeOnly(9, 0), new TimeOnly(18, 0)))
                .ToArray(),
            60);

        return new ShippingCapturedQuoteResult(
            ShippingCapturedQuoteVerdict.Quoted,
            "quoted",
            "tipax",
            "express",
            "IRR",
            150_000m,
            8_000m,
            158_000m,
            158_000m,
            0m,
            new ShippingCapturedQuoteSettings(
                ShippingFreightPayerMode.Prepaid,
                ShippingAuthoringCalculationMode.Manual,
                300,
                60,
                ShippingAuthoringPreparationProcessingMode.WorkingWeek),
            Snapshot,
            Draft,
            6,
            ShippingGroupAcceptedEvidence.ProcessingScheduleFormatVersion,
            ShippingType,
            AreaGroup,
            RateRow,
            1,
            2,
            receiving,
            null,
            carrier,
            processing);
    }
}
