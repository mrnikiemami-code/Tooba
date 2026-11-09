using Tooba.Shipping.Contracts.Scheduling;
using Xunit;

namespace Tooba.Shipping.Tests.Scheduling;

/// <summary>
/// TB-256: مرجع یکتای زمان‌بندی ارسال — پذیرش حامل مستقل از تحویل مشتری، برش نیم‌باز، شمارش
/// روزهای کاری حمل پس از پذیرش و MAX به‌جای MIN.
/// </summary>
public sealed class ShippingDispatchSchedulingAuthority256Tests
{
    private const string Utc = "UTC";

    private static readonly TimeOnly Nine = new(9, 0);
    private static readonly TimeOnly Seventeen = new(17, 0);

    [Fact]
    public void A_maximum_delivery_days_drive_the_first_customer_date_never_minimum()
    {
        var carrier = Week(Utc, workingDays: [1, 2, 3, 4, 5], Nine, Seventeen);
        var customer = Week(Utc, workingDays: [1, 2, 3, 4, 5], Nine, Seventeen);

        // Monday 2026-11-09 08:00 UTC — before opening, so the handoff is today.
        var plan = ShippingDispatchSchedulingAuthority.BuildReceivingCalendar(
            Utc_(2026, 11, 9, 8, 0), carrier, customer, transportWorkingDays: 3, horizonDates: 1, customerSlotMinutes: null);

        Assert.True(plan.IsSuccess);
        var earliest = plan.Value!.EarliestEligibleDate;
        Assert.Equal(new DateOnly(2026, 11, 12), earliest); // Monday +3 working days
        Assert.NotEqual(new DateOnly(2026, 11, 11), earliest); // never MIN (2)
        Assert.Equal(new DateOnly(2026, 11, 12), Assert.Single(plan.Value.Dates).Date);
    }

    [Fact]
    public void B_cutoff_is_half_open_before_opening_just_before_close_exactly_close_and_after_close()
    {
        var carrier = Week(Utc, workingDays: [1, 2, 3, 4, 5], Nine, Seventeen);

        Assert.Equal(new DateOnly(2026, 11, 9),
            ShippingDispatchSchedulingAuthority.ResolveFirstHandoff(Utc_(2026, 11, 9, 8, 0), carrier).Value!.Date);

        Assert.Equal(new DateOnly(2026, 11, 9),
            ShippingDispatchSchedulingAuthority.ResolveFirstHandoff(Utc_(2026, 11, 9, 16, 59), carrier).Value!.Date);

        // Exactly closing time is too late: the interval is [open, close).
        Assert.Equal(new DateOnly(2026, 11, 10),
            ShippingDispatchSchedulingAuthority.ResolveFirstHandoff(Utc_(2026, 11, 9, 17, 0), carrier).Value!.Date);

        Assert.Equal(new DateOnly(2026, 11, 10),
            ShippingDispatchSchedulingAuthority.ResolveFirstHandoff(Utc_(2026, 11, 9, 18, 0), carrier).Value!.Date);

        // Before opening targets opening of the same day; the whole window is still offered.
        var early = ShippingDispatchSchedulingAuthority.ResolveFirstHandoff(Utc_(2026, 11, 9, 6, 0), carrier).Value!;
        Assert.Equal(Nine, early.OpensAt);
        Assert.Equal(Seventeen, early.ClosesAt);
    }

    [Fact]
    public void C_wednesday_carrier_close_at_noon_is_independent_of_customer_0900_1700_and_friday_closed_rolls_forward()
    {
        // West Asia style: Monday/Tuesday/Thursday 09–17, Wednesday 09–12, Friday closed, weekend closed.
        var carrier = new ShippingSchedulingWeekView(
            Utc,
            [
                new(1, true, Nine, Seventeen),
                new(2, true, Nine, Seventeen),
                new(3, true, Nine, new TimeOnly(12, 0)),
                new(4, true, Nine, Seventeen),
                new(5, false, null, null),
                new(6, false, null, null),
                new(7, false, null, null),
            ]);
        var customer = Week(Utc, [1, 2, 3, 4, 5, 6, 7], Nine, Seventeen);

        // Wednesday 11:59 — still inside the carrier window (customer hours are irrelevant here).
        var beforeNoon = ShippingDispatchSchedulingAuthority.BuildReceivingCalendar(
            Utc_(2026, 11, 11, 11, 59), carrier, customer, transportWorkingDays: 1, horizonDates: 1, customerSlotMinutes: null);
        Assert.True(beforeNoon.IsSuccess);
        Assert.Equal(new DateOnly(2026, 11, 12), beforeNoon.Value!.EarliestEligibleDate); // Wed +1 = Thu
        Assert.Equal(Nine, Assert.Single(beforeNoon.Value.Dates).Slots[0].OpensAt);
        Assert.Equal(Seventeen, Assert.Single(beforeNoon.Value.Dates).Slots[0].ClosesAt);

        // Wednesday 12:00 — the carrier window is closed even though the customer still works to 17:00.
        var atNoon = ShippingDispatchSchedulingAuthority.ResolveFirstHandoff(Utc_(2026, 11, 11, 12, 0), carrier);
        Assert.Equal(new DateOnly(2026, 11, 12), atNoon.Value!.Date);

        // Friday is a nonworking carrier day: Thursday after close rolls to the next working acceptance day.
        var friday = ShippingDispatchSchedulingAuthority.ResolveFirstHandoff(Utc_(2026, 11, 13, 10, 0), carrier);
        Assert.Equal(new DateOnly(2026, 11, 16), friday.Value!.Date); // Monday
    }

    [Fact]
    public void D_timezone_midnight_skips_days_while_utc_is_still_the_previous_date()
    {
        // 2026-11-09 06:00 UTC is 2026-11-08 22:00 in Pacific Standard Time (Sunday).
        var carrier = new ShippingSchedulingWeekView(
            "Pacific Standard Time",
            [
                new(1, true, Nine, Seventeen),
                new(2, true, Nine, Seventeen),
                new(3, true, Nine, Seventeen),
                new(4, true, Nine, Seventeen),
                new(5, true, Nine, Seventeen),
                new(6, false, null, null),
                new(7, false, null, null),
            ]);

        var handoff = ShippingDispatchSchedulingAuthority.ResolveFirstHandoff(Utc_(2026, 11, 9, 6, 0), carrier);
        Assert.True(handoff.IsSuccess);
        Assert.Equal(new DateOnly(2026, 11, 9), handoff.Value!.Date);
        Assert.Equal(9, handoff.Value.OpensAt.Hour);
    }

    [Fact]
    public void D_zero_transport_days_is_explicit_and_never_offers_an_elapsed_slot()
    {
        var carrier = Week(Utc, [1, 2, 3, 4, 5], Nine, Seventeen);
        var customer = Week(Utc, [1, 2, 3, 4, 5], Nine, Seventeen);

        var plan = ShippingDispatchSchedulingAuthority.BuildReceivingCalendar(
            Utc_(2026, 11, 9, 10, 30), carrier, customer, transportWorkingDays: 0, horizonDates: 1, customerSlotMinutes: 60);

        Assert.True(plan.IsSuccess);
        var date = Assert.Single(plan.Value!.Dates);
        Assert.Equal(new DateOnly(2026, 11, 9), date.Date);
        // 09:00 and 10:00 already started: the first offered slot opens at 11:00.
        Assert.Equal(new TimeOnly(11, 0), date.Slots[0].OpensAt);
        Assert.DoesNotContain(date.Slots, slot => slot.OpensAt < new TimeOnly(10, 30));
    }

    [Fact]
    public void D_invalid_or_absent_calendars_fail_closed_with_stable_reasons()
    {
        var carrier = Week(Utc, [1, 2, 3, 4, 5], Nine, Seventeen);
        var customer = Week(Utc, [1, 2, 3, 4, 5], Nine, Seventeen);
        var now = Utc_(2026, 11, 9, 8, 0);

        Assert.Equal(ShippingDispatchSchedulingReasons.NoCarrierWorkingDay,
            ShippingDispatchSchedulingAuthority.ResolveFirstHandoff(now, AllClosed(Utc)).ReasonCode);

        var fiveDays = new ShippingSchedulingWeekView(Utc, carrier.Days.Take(5).ToArray());
        Assert.Equal(ShippingDispatchSchedulingReasons.CarrierWeekInvalid,
            ShippingDispatchSchedulingAuthority.ResolveFirstHandoff(now, fiveDays).ReasonCode);

        Assert.Equal(ShippingDispatchSchedulingReasons.CarrierTimeZoneUnresolvable,
            ShippingDispatchSchedulingAuthority.ResolveFirstHandoff(
                now,
                new ShippingSchedulingWeekView("Not/AZone", carrier.Days)).ReasonCode);

        Assert.Equal(ShippingDispatchSchedulingReasons.TransportDaysInvalid,
            ShippingDispatchSchedulingAuthority.BuildReceivingCalendar(
                now, carrier, customer, transportWorkingDays: -1, horizonDates: 1, customerSlotMinutes: null).ReasonCode);
        Assert.Equal(ShippingDispatchSchedulingReasons.TransportDaysInvalid,
            ShippingDispatchSchedulingAuthority.BuildReceivingCalendar(
                now, carrier, customer, transportWorkingDays: 366, horizonDates: 1, customerSlotMinutes: null).ReasonCode);
        Assert.Equal(ShippingDispatchSchedulingReasons.HorizonInvalid,
            ShippingDispatchSchedulingAuthority.BuildReceivingCalendar(
                now, carrier, customer, transportWorkingDays: 1, horizonDates: 91, customerSlotMinutes: null).ReasonCode);
        Assert.Equal(ShippingDispatchSchedulingReasons.SlotDurationInvalid,
            ShippingDispatchSchedulingAuthority.BuildReceivingCalendar(
                now, carrier, customer, transportWorkingDays: 1, horizonDates: 1, customerSlotMinutes: 0).ReasonCode);
        Assert.Equal(ShippingDispatchSchedulingReasons.SlotDurationUnfit,
            ShippingDispatchSchedulingAuthority.BuildReceivingCalendar(
                now, carrier, customer, transportWorkingDays: 1, horizonDates: 1, customerSlotMinutes: 600).ReasonCode);
        Assert.Equal(ShippingDispatchSchedulingReasons.NoCustomerWorkingDay,
            ShippingDispatchSchedulingAuthority.BuildReceivingCalendar(
                now, carrier, AllClosed(Utc), transportWorkingDays: 1, horizonDates: 1, customerSlotMinutes: null).ReasonCode);

        // Horizon 0 is explicit: the earliest date is known, no date list is fabricated.
        var noList = ShippingDispatchSchedulingAuthority.BuildReceivingCalendar(
            now, carrier, customer, transportWorkingDays: 1, horizonDates: 0, customerSlotMinutes: null);
        Assert.True(noList.IsSuccess);
        Assert.Equal(new DateOnly(2026, 11, 10), noList.Value!.EarliestEligibleDate);
        Assert.Empty(noList.Value.Dates);
    }

    [Fact]
    public void E_backward_latest_handoff_is_validated_by_the_same_forward_rule()
    {
        var carrier = Week(Utc, [1, 2, 3, 4, 5], Nine, Seventeen);

        var latest = ShippingDispatchSchedulingAuthority.ResolveLatestHandoffForReceivingDate(
            new DateOnly(2026, 11, 12), carrier, transportWorkingDays: 3);
        Assert.True(latest.IsSuccess);
        Assert.Equal(new DateOnly(2026, 11, 9), latest.Value!.Date);
        Assert.Equal(new DateOnly(2026, 11, 12),
            ShippingDispatchSchedulingAuthority.AdvanceTransportDays(carrier, latest.Value.Date, 3));

        // A date that transport working days can never land on (Saturday is closed) is refused, not subtracted.
        var impossible = ShippingDispatchSchedulingAuthority.ResolveLatestHandoffForReceivingDate(
            new DateOnly(2026, 11, 14), carrier, transportWorkingDays: 1);
        Assert.False(impossible.IsSuccess);
        Assert.Equal(ShippingDispatchSchedulingReasons.NoFeasibleHandoff, impossible.ReasonCode);
    }

    [Fact]
    public void F_readiness_is_counted_once_and_slots_belong_to_the_separate_customer_calendar()
    {
        // Carrier works 08:00–20:00; customer delivers 09:00–13:00 with 60-minute slots.
        var carrier = Week(Utc, [1, 2, 3, 4, 5], new TimeOnly(8, 0), new TimeOnly(20, 0));
        var customer = Week(Utc, [1, 2, 3, 4, 5], Nine, new TimeOnly(13, 0));

        var plan = ShippingDispatchSchedulingAuthority.BuildReceivingCalendar(
            Utc_(2026, 11, 9, 8, 30), carrier, customer, transportWorkingDays: 1, horizonDates: 1, customerSlotMinutes: 60);

        Assert.True(plan.IsSuccess);
        var date = Assert.Single(plan.Value!.Dates);
        Assert.Equal(new DateOnly(2026, 11, 10), date.Date); // readiness (Mon) + one working day, no extra preparation
        Assert.Equal(
            [new TimeOnly(9, 0), new TimeOnly(10, 0), new TimeOnly(11, 0), new TimeOnly(12, 0)],
            date.Slots.Select(slot => slot.OpensAt).ToArray());
        Assert.All(date.Slots, slot => Assert.Equal(TimeSpan.FromMinutes(60), slot.ClosesAt - slot.OpensAt));
        Assert.Equal(new TimeOnly(13, 0), date.Slots[^1].ClosesAt);
    }

    private static ShippingSchedulingWeekView Week(
        string timeZone,
        int[] workingDays,
        TimeOnly opens,
        TimeOnly closes) =>
        new(
            timeZone,
            Enumerable.Range(1, 7)
                .Select(weekday => workingDays.Contains(weekday)
                    ? new ShippingSchedulingDayView(weekday, true, opens, closes)
                    : new ShippingSchedulingDayView(weekday, false, null, null))
                .ToArray());

    private static ShippingSchedulingWeekView AllClosed(string timeZone) =>
        new(timeZone, Enumerable.Range(1, 7).Select(weekday => new ShippingSchedulingDayView(weekday, false, null, null)).ToArray());

    private static DateTimeOffset Utc_(int year, int month, int day, int hour, int minute) =>
        new(year, month, day, hour, minute, 0, TimeSpan.Zero);
}
