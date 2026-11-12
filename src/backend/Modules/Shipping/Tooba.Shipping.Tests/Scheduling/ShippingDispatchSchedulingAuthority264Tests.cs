using Tooba.Shipping.Contracts.Scheduling;
using Xunit;

namespace Tooba.Shipping.Tests.Scheduling;

/// <summary>
/// TB-264: readiness-aware forward calendar and backward operational deadlines.
/// </summary>
/// <remarks>
/// Every case uses an explicit injected instant and an explicit zone; the system clock is never read.
/// The point of the cases is that <c>nowUtc</c> is <b>not</b> goods readiness: physical preparation and the
/// transfer to the carrier happen first, once, before the first possible carrier acceptance.
/// </remarks>
public sealed class ShippingDispatchSchedulingAuthority264Tests
{
    private const string Utc = "UTC";
    private const string Tehran = "Asia/Tehran";

    private static readonly TimeOnly Nine = new(9, 0);
    private static readonly TimeOnly Noon = new(12, 0);
    private static readonly TimeOnly Seventeen = new(17, 0);

    // ---------------------------------------------------------------- A: forward

    [Fact]
    public void A_preparation_and_transfer_shift_the_first_possible_acceptance()
    {
        var carrier = Week(Utc, [1, 2, 3, 4, 5], Nine, Seventeen);
        var customer = Week(Utc, [1, 2, 3, 4, 5], Nine, Seventeen);

        // eligible 14:00 + 2h -> 16:00 -> still inside today's [09:00, 17:00) acceptance window.
        Assert.Equal(new DateOnly(2026, 11, 9), FirstHandoffDay(Utc_(2026, 11, 9, 14, 0), carrier));

        // eligible 16:00 + 2h -> 18:00 -> after close, so the first feasible day is tomorrow.
        Assert.Equal(new DateOnly(2026, 11, 10), FirstHandoffDay(Utc_(2026, 11, 9, 16, 0), carrier));

        // 16:59 + 2h -> 18:59 -> also misses today.
        Assert.Equal(new DateOnly(2026, 11, 10), FirstHandoffDay(Utc_(2026, 11, 9, 16, 59), carrier));

        // Exactly at close is too late (half-open interval).
        Assert.Equal(new DateOnly(2026, 11, 10), FirstHandoffDay(Utc_(2026, 11, 9, 15, 0), carrier));

        // The same facts through the public calendar entry point. The carrier handoff is still today, but
        // a whole-day customer slot that already started before readiness is not offered again, so the
        // earliest customer DATE is tomorrow even though acceptance itself is feasible today.
        var today = ShippingDispatchSchedulingAuthority.BuildReceivingCalendar(
            Utc_(2026, 11, 9, 14, 0), carrier, customer, transportWorkingDays: 0, horizonDates: 1, customerSlotMinutes: null,
            preparationMinutes: 60, handoffMinutes: 60, processingContinuous: true);
        Assert.True(today.IsSuccess);
        Assert.Equal(new DateOnly(2026, 11, 9), FirstHandoffDay(Utc_(2026, 11, 9, 14, 0), carrier));
        Assert.Equal(new DateOnly(2026, 11, 10), today.Value!.EarliestEligibleDate);

        var tomorrow = ShippingDispatchSchedulingAuthority.BuildReceivingCalendar(
            Utc_(2026, 11, 9, 16, 0), carrier, customer, transportWorkingDays: 0, horizonDates: 1, customerSlotMinutes: null,
            preparationMinutes: 60, handoffMinutes: 60, processingContinuous: true);
        Assert.True(tomorrow.IsSuccess);
        Assert.Equal(new DateOnly(2026, 11, 10), tomorrow.Value!.EarliestEligibleDate);
    }

    [Fact]
    public void A_carrier_closes_at_noon_while_customer_hours_reach_1700_and_closures_roll_forward()
    {
        // Wednesday acceptance ends at 12:00 while customer receiving still runs to 17:00 — the two
        // calendars are independent and the customer window never authorizes a carrier handoff.
        var carrier = Week(Utc, [1, 2, 3, 4, 5], Nine, Seventeen, wednesdayClose: Noon);
        var customer = Week(Utc, [1, 2, 3, 4, 5], Nine, Seventeen);

        var calendared = ShippingDispatchSchedulingAuthority.BuildReceivingCalendar(
            Utc_(2026, 11, 11, 10, 30), carrier, customer, transportWorkingDays: 0, horizonDates: 1,
            customerSlotMinutes: null, preparationMinutes: 60, handoffMinutes: 60, processingContinuous: true);

        // 10:30 + 2h = 12:30 is past Wednesday close, so Wednesday is not offered even though the
        // customer calendar is open to 17:00.
        Assert.True(calendared.IsSuccess);
        Assert.Equal(new DateOnly(2026, 11, 12), calendared.Value!.EarliestEligibleDate);
        Assert.DoesNotContain(calendared.Value!.Dates, date => date.Date == new DateOnly(2026, 11, 11));
    }

    [Fact]
    public void A_zero_preparation_is_explicit_and_keeps_the_historical_reference_instant()
    {
        var carrier = Week(Utc, [1, 2, 3, 4, 5], Nine, Seventeen);
        var customer = Week(Utc, [1, 2, 3, 4, 5], Nine, Seventeen);

        var legacy = ShippingDispatchSchedulingAuthority.BuildReceivingCalendar(
            Utc_(2026, 11, 9, 8, 0), carrier, customer, 0, 1, null);
        var explicitZero = ShippingDispatchSchedulingAuthority.BuildReceivingCalendar(
            Utc_(2026, 11, 9, 8, 0), carrier, customer, 0, 1, null, preparationMinutes: 0, handoffMinutes: 0);

        Assert.True(legacy.IsSuccess);
        Assert.True(explicitZero.IsSuccess);
        Assert.Equal(legacy.Value!.EarliestEligibleDate, explicitZero.Value!.EarliestEligibleDate);
        Assert.Equal(Assert.Single(legacy.Value.Dates).Slots, Assert.Single(explicitZero.Value.Dates).Slots);
    }

    // ---------------------------------------------------------------- B: backward

    [Fact]
    public void B_latest_deadlines_are_derived_backwards_from_the_same_transport_rule()
    {
        var carrier = Week(Utc, [1, 2, 3, 4, 5], Nine, Seventeen);

        // MAX 3 working days after a Thursday 2026-11-12 delivery => latest handoff Monday 2026-11-09.
        var deadlines = ShippingDispatchSchedulingAuthority.ResolvePreparationDeadlines(
            new DateOnly(2026, 11, 12), carrier, transportWorkingDays: 3, preparationMinutes: 60, handoffMinutes: 45);

        Assert.True(deadlines.IsSuccess);
        var value = deadlines.Value!;
        Assert.Equal(new DateOnly(2026, 11, 9), value.Handoff.Date);
        Assert.Equal(new DateTimeOffset(2026, 11, 9, 17, 0, 0, TimeSpan.Zero), value.HandoffDeadlineUtc);
        Assert.Equal(new DateTimeOffset(2026, 11, 9, 16, 15, 0, TimeSpan.Zero), value.TransferDepartureUtc);
        Assert.Equal(new DateTimeOffset(2026, 11, 9, 15, 15, 0, TimeSpan.Zero), value.PreparationStartUtc);
        Assert.Equal(60, value.PreparationMinutes);
        Assert.Equal(45, value.HandoffMinutes);

        // One later handoff day (Tuesday) would arrive on Friday — after the chosen Thursday — so Monday
        // really is the LAST feasible acceptance day, not merely a feasible one.
        var arrivalFromTuesday = ShippingDispatchSchedulingAuthority.AdvanceTransportDays(
            carrier, new DateOnly(2026, 11, 10), 3);
        Assert.Equal(new DateOnly(2026, 11, 13), arrivalFromTuesday);
        Assert.True(arrivalFromTuesday > new DateOnly(2026, 11, 12));
    }

    [Fact]
    public void B_zero_transit_and_cross_zone_deadlines_compare_real_instants()
    {
        var tehranCarrier = Week(Tehran, [1, 2, 3, 4, 5], Nine, Seventeen);

        // Thursday 2026-11-12, transport 0: the handoff is the same local Thursday and close is 17:00
        // Tehran = 13:30 UTC (the carrier zone, not the operator's zone).
        var deadlines = ShippingDispatchSchedulingAuthority.ResolvePreparationDeadlines(
            new DateOnly(2026, 11, 12), tehranCarrier, transportWorkingDays: 0, preparationMinutes: 60, handoffMinutes: 30);

        Assert.True(deadlines.IsSuccess);
        var value = deadlines.Value!;
        Assert.Equal(new DateOnly(2026, 11, 12), value.Handoff.Date);
        Assert.Equal(new DateTimeOffset(2026, 11, 12, 13, 30, 0, TimeSpan.Zero), value.HandoffDeadlineUtc);
        Assert.Equal(new DateTimeOffset(2026, 11, 12, 13, 0, 0, TimeSpan.Zero), value.TransferDepartureUtc);
        Assert.Equal(new DateTimeOffset(2026, 11, 12, 12, 0, 0, TimeSpan.Zero), value.PreparationStartUtc);

        // The deadline is strictly before acceptance close: "inside the window" never means "at close".
        Assert.True(value.PreparationStartUtc < value.TransferDepartureUtc);
        Assert.True(value.TransferDepartureUtc <= value.HandoffDeadlineUtc);
    }

    // ---------------------------------------------------------------- C: policy validation

    [Fact]
    public void C_a_partial_timing_policy_and_out_of_range_values_are_refused()
    {
        var carrier = Week(Utc, [1, 2, 3, 4, 5], Nine, Seventeen);
        var customer = Week(Utc, [1, 2, 3, 4, 5], Nine, Seventeen);

        var partial = ShippingDispatchSchedulingAuthority.BuildReceivingCalendar(
            Utc_(2026, 11, 9, 8, 0), carrier, customer, 0, 1, null, preparationMinutes: 60);
        Assert.False(partial.IsSuccess);
        Assert.Equal(ShippingDispatchSchedulingReasons.TimingPolicyIncomplete, partial.ReasonCode);

        var tooMuchPreparation = ShippingDispatchSchedulingAuthority.BuildReceivingCalendar(
            Utc_(2026, 11, 9, 8, 0), carrier, customer, 0, 1, null,
            preparationMinutes: ShippingDispatchSchedulingAuthority.MaxPreparationMinutes + 1, handoffMinutes: 0);
        Assert.False(tooMuchPreparation.IsSuccess);
        Assert.Equal(ShippingDispatchSchedulingReasons.PreparationMinutesInvalid, tooMuchPreparation.ReasonCode);

        var tooMuchHandoff = ShippingDispatchSchedulingAuthority.BuildReceivingCalendar(
            Utc_(2026, 11, 9, 8, 0), carrier, customer, 0, 1, null,
            preparationMinutes: 0, handoffMinutes: ShippingDispatchSchedulingAuthority.MaxHandoffMinutes + 1);
        Assert.False(tooMuchHandoff.IsSuccess);
        Assert.Equal(ShippingDispatchSchedulingReasons.HandoffMinutesInvalid, tooMuchHandoff.ReasonCode);

        var negative = ShippingDispatchSchedulingAuthority.ResolvePreparationDeadlines(
            new DateOnly(2026, 11, 12), carrier, 0, preparationMinutes: -1, handoffMinutes: 0);
        Assert.False(negative.IsSuccess);
        Assert.Equal(ShippingDispatchSchedulingReasons.PreparationMinutesInvalid, negative.ReasonCode);

        var negativeHandoff = ShippingDispatchSchedulingAuthority.ResolvePreparationDeadlines(
            new DateOnly(2026, 11, 12), carrier, 0, preparationMinutes: 0, handoffMinutes: -1);
        Assert.False(negativeHandoff.IsSuccess);
        Assert.Equal(ShippingDispatchSchedulingReasons.HandoffMinutesInvalid, negativeHandoff.ReasonCode);
    }

    [Fact]
    public void C_the_policy_versions_are_distinct_and_the_new_one_is_additive()
    {
        Assert.Equal(1, ShippingDispatchSchedulingAuthority.PolicyVersion);
        Assert.Equal(2, ShippingDispatchSchedulingAuthority.PreparationAwarePolicyVersion);
        Assert.True(ShippingDispatchSchedulingAuthority.PreparationAwarePolicyVersion > ShippingDispatchSchedulingAuthority.PolicyVersion);
    }

    // ---------------------------------------------------------------- helpers

    private static DateOnly FirstHandoffDay(DateTimeOffset eligibleUtc, ShippingSchedulingWeekView carrier)
    {
        var ready = eligibleUtc.AddMinutes(120);
        var handoff = ShippingDispatchSchedulingAuthority.ResolveFirstHandoff(ready, carrier);
        Assert.True(handoff.IsSuccess, handoff.ReasonCode);
        return handoff.Value!.Date;
    }

    private static ShippingSchedulingWeekView Week(
        string zone,
        int[] workingDays,
        TimeOnly opens,
        TimeOnly closes,
        TimeOnly? wednesdayClose = null)
    {
        var days = new ShippingSchedulingDayView[7];
        for (var weekday = 1; weekday <= 7; weekday++)
        {
            var working = workingDays.Contains(weekday);
            var dayClose = weekday == 3 && wednesdayClose is { } wednesday ? wednesday : closes;
            days[weekday - 1] = new ShippingSchedulingDayView(weekday, working, working ? opens : null, working ? dayClose : null);
        }

        return new ShippingSchedulingWeekView(zone, days);
    }

    private static DateTimeOffset Utc_(int year, int month, int day, int hour, int minute)
        => new(year, month, day, hour, minute, 0, TimeSpan.Zero);
}
