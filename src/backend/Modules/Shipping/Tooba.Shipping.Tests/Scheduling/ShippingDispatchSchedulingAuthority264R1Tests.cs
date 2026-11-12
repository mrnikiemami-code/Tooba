using Tooba.Shipping.Contracts.Scheduling;
using Xunit;

namespace Tooba.Shipping.Tests.Scheduling;

/// <summary>
/// TB-264-R1: the bounded corrections of the readiness/deadline authority.
/// </summary>
/// <remarks>
/// <para>
/// <b>A</b> — an explicit ZERO allowance is lawful arithmetic, not a fake one-second duration: the instants
/// are non-decreasing and equality appears exactly where the matching allowance is zero.
/// </para>
/// <para>
/// <b>B</b> — the customer's chosen WINDOW (date + open + close + zone) constrains the backward plan, so a
/// zero-day handoff can never be scheduled after the receiving window opens.
/// </para>
/// <para>
/// <b>C</b> — preparation is working time counted inside the explicitly configured processing week;
/// unpublished overnight hours are never consumed and 24/7 is only used when the operator chose it.
/// </para>
/// </remarks>
public sealed class ShippingDispatchSchedulingAuthority264R1Tests
{
    private const string Utc = "UTC";
    private const string Tehran = "Asia/Tehran";

    private static readonly TimeOnly Nine = new(9, 0);
    private static readonly TimeOnly Ten = new(10, 0);
    private static readonly TimeOnly Eleven = new(11, 0);
    private static readonly TimeOnly Fourteen = new(14, 0);
    private static readonly TimeOnly Seventeen = new(17, 0);

    // ------------------------------------------------------------------ A: explicit zero

    [Fact]
    public void A_zero_preparation_and_zero_transfer_collapse_all_three_instants()
    {
        var carrier = Week(Utc, [1, 2, 3, 4, 5], Nine, Seventeen);

        // The window opens at 10:00 while the carrier opens at 09:00, so a zero-day handoff is provable
        // BEFORE the appointment. (A window opening exactly at carrier open is refused by the same
        // half-open rule: the parcel cannot be handed over strictly before 09:00.)
        var deadlines = ShippingDispatchSchedulingAuthority.ResolvePreparationDeadlines(
            Commitment(new DateOnly(2026, 11, 12), Ten, Eleven, Utc),
            carrier,
            transportWorkingDays: 0,
            preparationMinutes: 0,
            handoffMinutes: 0);

        Assert.True(deadlines.IsSuccess, deadlines.ReasonCode);
        var value = deadlines.Value!;
        // Zero-day transport: the deadline is the customer window open, never the carrier close.
        Assert.Equal(new DateTimeOffset(2026, 11, 12, 10, 0, 0, TimeSpan.Zero), value.HandoffDeadlineUtc);
        Assert.Equal(value.HandoffDeadlineUtc, value.TransferDepartureUtc);
        Assert.Equal(value.TransferDepartureUtc, value.PreparationStartUtc);
        Assert.Equal(0, value.PreparationMinutes);
        Assert.Equal(0, value.HandoffMinutes);
    }

    [Fact]
    public void A_zero_preparation_is_equality_and_zero_transfer_is_equality_independently()
    {
        var carrier = Week(Utc, [1, 2, 3, 4, 5], Nine, Seventeen);
        var commitment = Commitment(new DateOnly(2026, 11, 12), Ten, Eleven, Utc);

        // 0 preparation + 30 transfer: preparation starts exactly when the parcel departs.
        var zeroPrep = ShippingDispatchSchedulingAuthority.ResolvePreparationDeadlines(
            commitment, carrier, 0, preparationMinutes: 0, handoffMinutes: 30);
        Assert.True(zeroPrep.IsSuccess, zeroPrep.ReasonCode);
        Assert.Equal(new DateTimeOffset(2026, 11, 12, 9, 30, 0, TimeSpan.Zero), zeroPrep.Value!.TransferDepartureUtc);
        Assert.Equal(zeroPrep.Value!.TransferDepartureUtc, zeroPrep.Value!.PreparationStartUtc);
        Assert.Equal(new DateTimeOffset(2026, 11, 12, 10, 0, 0, TimeSpan.Zero), zeroPrep.Value!.HandoffDeadlineUtc);

        // 30 preparation + 0 transfer: departure is exactly the acceptance deadline. Continuous operation is
        // chosen explicitly here so this case isolates the ZERO transfer, not the processing calendar.
        var zeroTransfer = ShippingDispatchSchedulingAuthority.ResolvePreparationDeadlines(
            commitment, carrier, 0, preparationMinutes: 30, handoffMinutes: 0, processingContinuous: true);
        Assert.True(zeroTransfer.IsSuccess, zeroTransfer.ReasonCode);
        Assert.Equal(zeroTransfer.Value!.HandoffDeadlineUtc, zeroTransfer.Value!.TransferDepartureUtc);
        Assert.Equal(
            new DateTimeOffset(2026, 11, 12, 9, 30, 0, TimeSpan.Zero),
            zeroTransfer.Value!.PreparationStartUtc);

        // A positive pair keeps the strict separation between preparation and departure.
        var positive = ShippingDispatchSchedulingAuthority.ResolvePreparationDeadlines(
            commitment, carrier, 0, preparationMinutes: 60, handoffMinutes: 45, processingContinuous: true);
        Assert.True(positive.IsSuccess, positive.ReasonCode);
        Assert.True(positive.Value!.PreparationStartUtc < positive.Value!.TransferDepartureUtc);
        Assert.True(positive.Value!.TransferDepartureUtc < positive.Value!.HandoffDeadlineUtc);
    }

    [Fact]
    public void A_partial_and_out_of_range_allowances_are_still_refused()
    {
        var carrier = Week(Utc, [1, 2, 3, 4, 5], Nine, Seventeen);
        var commitment = Commitment(new DateOnly(2026, 11, 12), Nine, Eleven, Utc);

        var negativePrep = ShippingDispatchSchedulingAuthority.ResolvePreparationDeadlines(
            commitment, carrier, 0, -1, 0);
        Assert.False(negativePrep.IsSuccess);
        Assert.Equal(ShippingDispatchSchedulingReasons.PreparationMinutesInvalid, negativePrep.ReasonCode);

        var negativeHandoff = ShippingDispatchSchedulingAuthority.ResolvePreparationDeadlines(
            commitment, carrier, 0, 0, -1);
        Assert.False(negativeHandoff.IsSuccess);
        Assert.Equal(ShippingDispatchSchedulingReasons.HandoffMinutesInvalid, negativeHandoff.ReasonCode);

        var hugePrep = ShippingDispatchSchedulingAuthority.ResolvePreparationDeadlines(
            commitment,
            carrier,
            0,
            ShippingDispatchSchedulingAuthority.MaxPreparationMinutes + 1,
            0);
        Assert.False(hugePrep.IsSuccess);
        Assert.Equal(ShippingDispatchSchedulingReasons.PreparationMinutesInvalid, hugePrep.ReasonCode);
    }

    // ------------------------------------------------------------------ B: the chosen window

    [Fact]
    public void B_same_day_handoff_is_capped_by_the_customer_window_open_not_the_carrier_close()
    {
        var carrier = Week(Utc, [1, 2, 3, 4, 5], Nine, Seventeen);

        // The exact case the review measured: carrier 09:00-17:00, customer tomorrow 10:00-11:00,
        // transport 0, preparation 60, transfer 30. The old plan said carrier 17:00 — AFTER the appointment.
        var deadlines = ShippingDispatchSchedulingAuthority.ResolvePreparationDeadlines(
            Commitment(new DateOnly(2026, 11, 12), Ten, Eleven, Utc),
            carrier,
            transportWorkingDays: 0,
            preparationMinutes: 60,
            handoffMinutes: 30,
            processingContinuous: true);

        Assert.True(deadlines.IsSuccess, deadlines.ReasonCode);
        var value = deadlines.Value!;
        Assert.Equal(new DateOnly(2026, 11, 12), value.Handoff.Date);
        Assert.Equal(new DateTimeOffset(2026, 11, 12, 10, 0, 0, TimeSpan.Zero), value.HandoffDeadlineUtc);
        Assert.Equal(new DateTimeOffset(2026, 11, 12, 9, 30, 0, TimeSpan.Zero), value.TransferDepartureUtc);
        Assert.Equal(new DateTimeOffset(2026, 11, 12, 8, 30, 0, TimeSpan.Zero), value.PreparationStartUtc);
        // The plan never claims the close instant itself is the appointment.
        Assert.True(value.HandoffDeadlineUtc <= new DateTimeOffset(2026, 11, 12, 10, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void B_cross_zone_window_is_compared_in_real_instants_of_the_commitment_zone()
    {
        var carrier = Week(Utc, [1, 2, 3, 4, 5], Nine, Seventeen);

        // Tehran 14:00-15:00 on the same date = 10:30-11:30 UTC (Tehran is UTC+03:30 in November 2026).
        var deadlines = ShippingDispatchSchedulingAuthority.ResolvePreparationDeadlines(
            Commitment(new DateOnly(2026, 11, 12), Fourteen, new TimeOnly(15, 0), Tehran),
            carrier,
            transportWorkingDays: 0,
            preparationMinutes: 60,
            handoffMinutes: 30,
            processingContinuous: true);

        Assert.True(deadlines.IsSuccess, deadlines.ReasonCode);
        var value = deadlines.Value!;
        Assert.Equal(new DateOnly(2026, 11, 12), value.Handoff.Date);
        Assert.Equal(new DateTimeOffset(2026, 11, 12, 10, 30, 0, TimeSpan.Zero), value.HandoffDeadlineUtc);
        Assert.Equal(new DateTimeOffset(2026, 11, 12, 10, 0, 0, TimeSpan.Zero), value.TransferDepartureUtc);
        Assert.Equal(new DateTimeOffset(2026, 11, 12, 9, 0, 0, TimeSpan.Zero), value.PreparationStartUtc);
    }

    [Fact]
    public void B_a_window_before_the_carrier_opens_cannot_be_served_on_that_same_day()
    {
        var carrier = Week(Utc, [1, 2, 3, 4, 5], Nine, Seventeen);

        // Tehran 08:00-09:00 = 04:30-05:30 UTC, entirely before the carrier opens at 09:00 UTC. A zero-day
        // handoff on that date is impossible, so the plan must move to an EARLIER acceptance day instead of
        // pretending the window is served afterwards.
        var deadlines = ShippingDispatchSchedulingAuthority.ResolvePreparationDeadlines(
            Commitment(new DateOnly(2026, 11, 12), new TimeOnly(8, 0), Nine, Tehran),
            carrier,
            transportWorkingDays: 0,
            preparationMinutes: 0,
            handoffMinutes: 0);

        Assert.True(deadlines.IsSuccess, deadlines.ReasonCode);
        Assert.True(deadlines.Value!.HandoffDeadlineUtc < new DateTimeOffset(2026, 11, 12, 4, 30, 0, TimeSpan.Zero));
        Assert.Equal(new DateOnly(2026, 11, 11), deadlines.Value!.Handoff.Date);
    }

    [Fact]
    public void B_positive_transit_keeps_date_granularity_and_never_claims_an_extra_day()
    {
        var carrier = Week(Utc, [1, 2, 3, 4, 5], Nine, Seventeen);

        // MAX 2 working days back from Thursday 2026-11-12 => Tuesday 2026-11-10, and its close stays the
        // deadline because a multi-day transit makes only the DATE provable.
        var deadlines = ShippingDispatchSchedulingAuthority.ResolvePreparationDeadlines(
            Commitment(new DateOnly(2026, 11, 12), Ten, Eleven, Utc),
            carrier,
            transportWorkingDays: 2,
            preparationMinutes: 60,
            handoffMinutes: 30,
            processingContinuous: true);

        Assert.True(deadlines.IsSuccess, deadlines.ReasonCode);
        Assert.Equal(new DateOnly(2026, 11, 10), deadlines.Value!.Handoff.Date);
        Assert.Equal(new DateTimeOffset(2026, 11, 10, 17, 0, 0, TimeSpan.Zero), deadlines.Value!.HandoffDeadlineUtc);

        // Tuesday + 2 working days = Thursday: the same day is NOT offered a day earlier than required.
        Assert.Equal(
            new DateOnly(2026, 11, 12),
            ShippingDispatchSchedulingAuthority.AdvanceTransportDays(carrier, new DateOnly(2026, 11, 10), 2));
        Assert.Equal(
            new DateOnly(2026, 11, 13),
            ShippingDispatchSchedulingAuthority.AdvanceTransportDays(carrier, new DateOnly(2026, 11, 11), 2));
    }

    // ------------------------------------------------------------------ C: processing week

    [Fact]
    public void C_preparation_is_counted_inside_the_processing_week_and_never_overnight()
    {
        var carrier = Week(Utc, [1, 2, 3, 4, 5], Nine, Seventeen);
        var processing = Week(Utc, [1, 2, 3, 4, 5], Nine, Seventeen);

        // Thursday window 10:00-11:00, transport 0 => deadline 10:00 Thursday, and 120 working preparation
        // minutes can only be consumed Thu 09:00-10:00 + Wed 16:00-17:00 (overnight closed hours are NOT
        // counted).
        var deadlines = ShippingDispatchSchedulingAuthority.ResolvePreparationDeadlines(
            Commitment(new DateOnly(2026, 11, 12), Ten, Eleven, Utc),
            carrier,
            transportWorkingDays: 0,
            preparationMinutes: 120,
            handoffMinutes: 0,
            processingWeek: processing,
            processingContinuous: false);

        Assert.True(deadlines.IsSuccess, deadlines.ReasonCode);
        Assert.Equal(new DateTimeOffset(2026, 11, 12, 10, 0, 0, TimeSpan.Zero), deadlines.Value!.TransferDepartureUtc);
        Assert.Equal(new DateTimeOffset(2026, 11, 11, 16, 0, 0, TimeSpan.Zero), deadlines.Value!.PreparationStartUtc);
    }

    [Fact]
    public void C_a_closed_processing_day_rolls_the_start_back_to_the_previous_working_day()
    {
        var carrier = Week(Utc, [1, 2, 3, 4, 5], Nine, Seventeen);
        // Thursday is CLOSED for the warehouse: nothing can be prepared that day.
        var processing = Week(Utc, [1, 2, 3, 5], Nine, Seventeen);

        var deadlines = ShippingDispatchSchedulingAuthority.ResolvePreparationDeadlines(
            Commitment(new DateOnly(2026, 11, 12), Ten, Eleven, Utc),
            carrier,
            transportWorkingDays: 0,
            preparationMinutes: 60,
            handoffMinutes: 0,
            processingWeek: processing,
            processingContinuous: false);

        Assert.True(deadlines.IsSuccess, deadlines.ReasonCode);
        Assert.Equal(new DateTimeOffset(2026, 11, 11, 16, 0, 0, TimeSpan.Zero), deadlines.Value!.PreparationStartUtc);
    }

    [Fact]
    public void C_explicit_continuous_operation_is_the_only_24_7_arithmetic()
    {
        var carrier = Week(Utc, [1, 2, 3, 4, 5], Nine, Seventeen);
        var processing = Week(Utc, [1, 2, 3, 4, 5], Nine, Seventeen);

        var continuous = ShippingDispatchSchedulingAuthority.ResolvePreparationDeadlines(
            Commitment(new DateOnly(2026, 11, 12), Ten, Eleven, Utc),
            carrier,
            transportWorkingDays: 0,
            preparationMinutes: 120,
            handoffMinutes: 0,
            processingWeek: processing,
            processingContinuous: true);

        Assert.True(continuous.IsSuccess, continuous.ReasonCode);
        Assert.Equal(new DateTimeOffset(2026, 11, 12, 8, 0, 0, TimeSpan.Zero), continuous.Value!.PreparationStartUtc);

        var workingWeek = ShippingDispatchSchedulingAuthority.ResolvePreparationDeadlines(
            Commitment(new DateOnly(2026, 11, 12), Ten, Eleven, Utc),
            carrier,
            transportWorkingDays: 0,
            preparationMinutes: 120,
            handoffMinutes: 0,
            processingWeek: processing,
            processingContinuous: false);

        Assert.True(workingWeek.IsSuccess, workingWeek.ReasonCode);
        Assert.Equal(new DateTimeOffset(2026, 11, 11, 16, 0, 0, TimeSpan.Zero), workingWeek.Value!.PreparationStartUtc);
        Assert.NotEqual(continuous.Value!.PreparationStartUtc, workingWeek.Value!.PreparationStartUtc);
    }

    [Fact]
    public void C_a_working_week_without_a_processing_calendar_and_an_all_closed_week_are_refused()
    {
        var carrier = Week(Utc, [1, 2, 3, 4, 5], Nine, Seventeen);
        var commitment = Commitment(new DateOnly(2026, 11, 12), Nine, Eleven, Utc);

        var missingWeek = ShippingDispatchSchedulingAuthority.ResolvePreparationDeadlines(
            commitment, carrier, 0, 120, 0, processingWeek: null, processingContinuous: false);
        Assert.False(missingWeek.IsSuccess);
        Assert.Equal(ShippingDispatchSchedulingReasons.TimingPolicyUnconfigured, missingWeek.ReasonCode);

        var allClosed = ShippingDispatchSchedulingAuthority.ResolvePreparationDeadlines(
            commitment, carrier, 0, 120, 0, processingWeek: Week(Utc, [], Nine, Seventeen), processingContinuous: false);
        Assert.False(allClosed.IsSuccess);
        Assert.Equal(ShippingDispatchSchedulingReasons.NoProcessingWorkingDay, allClosed.ReasonCode);
    }

    [Fact]
    public void C_no_processing_hour_is_invented_when_the_instant_is_already_inside_a_closed_night()
    {
        var carrier = Week(Utc, [1, 2, 3, 4, 5], Nine, Seventeen);
        var customer = Week(Utc, [1, 2, 3, 4, 5], Nine, Seventeen);
        var processing = Week(Utc, [1, 2, 3, 4, 5], Nine, Seventeen);

        // Eligible at 23:00 Thursday with 120 preparation minutes: continuous arithmetic would say 01:00,
        // but the warehouse is closed until 09:00 Friday, so the first handoff-ready instant is 11:00.
        var continuous = ShippingDispatchSchedulingAuthority.BuildReceivingCalendar(
            Utc_(2026, 11, 12, 23, 0), carrier, customer, 0, 1, null,
            preparationMinutes: 120, handoffMinutes: 0, processingWeek: processing, processingContinuous: true);
        Assert.True(continuous.IsSuccess, continuous.ReasonCode);
        Assert.Equal(new DateOnly(2026, 11, 13), continuous.Value!.EarliestEligibleDate);

        var working = ShippingDispatchSchedulingAuthority.BuildReceivingCalendar(
            Utc_(2026, 11, 12, 23, 0), carrier, customer, 0, 1, null,
            preparationMinutes: 120, handoffMinutes: 0, processingWeek: processing, processingContinuous: false);
        Assert.True(working.IsSuccess, working.ReasonCode);
        // Ready 11:00 Friday; the whole-day Friday customer slot already started, so the first offered date
        // is the next customer working date rather than a slot that begins in the past.
        Assert.Equal(new DateOnly(2026, 11, 16), working.Value!.EarliestEligibleDate);
    }

    // ------------------------------------------------------------------ helpers

    private static ShippingReceivingCommitment Commitment(
        DateOnly date,
        TimeOnly opens,
        TimeOnly closes,
        string zone) => new(date, opens, closes, zone);

    private static ShippingSchedulingWeekView Week(
        string zone,
        int[] workingDays,
        TimeOnly opens,
        TimeOnly closes)
    {
        var days = new ShippingSchedulingDayView[7];
        for (var weekday = 1; weekday <= 7; weekday++)
        {
            var working = workingDays.Contains(weekday);
            days[weekday - 1] = new ShippingSchedulingDayView(
                weekday,
                working,
                working ? opens : null,
                working ? closes : null);
        }

        return new ShippingSchedulingWeekView(zone, days);
    }

    private static DateTimeOffset Utc_(int year, int month, int day, int hour, int minute)
        => new(year, month, day, hour, minute, 0, TimeSpan.Zero);

}
