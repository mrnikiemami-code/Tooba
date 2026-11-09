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

        // TB-256-R1: "on or before" — a customer date the carrier is CLOSED on is reachable from an earlier
        // handoff whose conservative arrival is before the chosen date. It is not rejected for that reason.
        var saturday = ShippingDispatchSchedulingAuthority.ResolveLatestHandoffForReceivingDate(
            new DateOnly(2026, 11, 14), carrier, transportWorkingDays: 1);
        Assert.True(saturday.IsSuccess);
        Assert.Equal(new DateOnly(2026, 11, 12), saturday.Value!.Date); // Thu handoff, Fri arrival, Sat delivery
        Assert.True(ShippingDispatchSchedulingAuthority.AdvanceTransportDays(carrier, saturday.Value.Date, 1)
            <= new DateOnly(2026, 11, 14));

        // A receiving date that is earlier than the first possible arrival is filtered by the calendar
        // (it never becomes an offered date), not by the pure backward rule inventing a past handoff.
        var plan = ShippingDispatchSchedulingAuthority.BuildReceivingCalendar(
            Utc_(2026, 11, 9, 8, 0), carrier, carrier, transportWorkingDays: 3, horizonDates: 5, customerSlotMinutes: null);
        Assert.True(plan.IsSuccess);
        Assert.All(plan.Value!.Dates, date => Assert.True(date.Date >= new DateOnly(2026, 11, 12)));

        // No carrier working day at all is the one genuine no-handoff case, with its own stable reason.
        Assert.Equal(ShippingDispatchSchedulingReasons.NoCarrierWorkingDay,
            ShippingDispatchSchedulingAuthority.ResolveLatestHandoffForReceivingDate(
                new DateOnly(2026, 11, 14), AllClosed(Utc), transportWorkingDays: 1).ReasonCode);
    }

    [Fact]
    public void E2_latest_handoff_is_the_latest_one_whose_arrival_is_on_or_before_the_date()
    {
        var carrier = Week(Utc, [1, 2, 3, 4, 5], Nine, Seventeen);
        var from = new DateOnly(2026, 11, 9);
        for (var offset = 0; offset < 40; offset++)
        {
            var sameDay = ShippingDispatchSchedulingAuthority.ResolveLatestHandoffForReceivingDate(
                from.AddDays(offset), carrier, transportWorkingDays: 0);
            Assert.True(sameDay.IsSuccess);

            var three = ShippingDispatchSchedulingAuthority.ResolveLatestHandoffForReceivingDate(
                from.AddDays(offset), carrier, transportWorkingDays: 3);
            Assert.True(three.IsSuccess);

            foreach (var resolved in new[] { sameDay.Value!, three.Value! })
            {
                var days = resolved == sameDay.Value ? 0 : 3;
                var arrival = ShippingDispatchSchedulingAuthority.AdvanceTransportDays(carrier, resolved.Date, days);

                // Forward arrival never overshoots the chosen customer date…
                Assert.True(arrival <= from.AddDays(offset));

                // …and the NEXT working handoff would overshoot it: this is the latest feasible one.
                var nextWorking = AdvanceToNextWorkingDay(carrier, resolved.Date);
                if (nextWorking <= from.AddDays(offset))
                {
                    Assert.True(
                        ShippingDispatchSchedulingAuthority.AdvanceTransportDays(carrier, nextWorking, days) > from.AddDays(offset));
                }
            }
        }
    }

    [Fact]
    public void E3_zero_day_elapsed_slots_compare_real_instants_across_different_time_zones()
    {
        // Carrier 09:00–17:00 Asia/Tehran = 05:30–13:30 UTC. Customer 06:00–12:00 UTC, 60-minute slots.
        var carrier = Week("Asia/Tehran", [1, 2, 3, 4, 5], Nine, Seventeen);
        var customer = Week(Utc, [1, 2, 3, 4, 5], new TimeOnly(6, 0), new TimeOnly(12, 0));

        // 2026-11-09 05:00 UTC = 08:30 Tehran (before opening). Handoff opens 09:00 Tehran = 05:30 UTC.
        var plan = ShippingDispatchSchedulingAuthority.BuildReceivingCalendar(
            Utc_(2026, 11, 9, 5, 0), carrier, customer, transportWorkingDays: 0, horizonDates: 1, customerSlotMinutes: 60);

        Assert.True(plan.IsSuccess);
        var date = Assert.Single(plan.Value!.Dates);
        Assert.Equal(new DateOnly(2026, 11, 9), date.Date);
        // The 06:00 UTC slot is after the 05:30 UTC handoff opening and after readiness: it stays offered.
        // Comparing carrier clock values (09:00) with customer clock values would have dropped it.
        Assert.Equal(new TimeOnly(6, 0), date.Slots[0].OpensAt);
        Assert.Contains(date.Slots, slot => slot.OpensAt == new TimeOnly(6, 0));
    }

    [Fact]
    public void E4_sparse_carrier_week_and_maximum_duration_stay_bounded_and_fail_closed_without_throwing()
    {
        // One carrier working day per week (Monday) — the worst supported shape.
        var carrier = Week(Utc, [1], Nine, Seventeen);
        var customer = Week(Utc, [1, 2, 3, 4, 5], Nine, Seventeen);

        // MAX supported duration: the forward count and the backward search both stay bounded.
        var maxDays = ShippingDispatchSchedulingAuthority.MaxTransportWorkingDays;
        var maxDate = new DateOnly(2026, 11, 9).AddDays(maxDays * 7);
        var latest = ShippingDispatchSchedulingAuthority.ResolveLatestHandoffForReceivingDate(maxDate, carrier, maxDays);
        Assert.True(latest.IsSuccess);
        Assert.Equal(maxDate.DayOfWeek, latest.Value!.Date.DayOfWeek);
        Assert.Equal(maxDate, ShippingDispatchSchedulingAuthority.AdvanceTransportDays(carrier, latest.Value.Date, maxDays));

        // The calendar itself never throws for the same bounds; it either produces dates or fails closed.
        var plan = ShippingDispatchSchedulingAuthority.BuildReceivingCalendar(
            Utc_(2026, 11, 9, 8, 0),
            carrier,
            customer,
            maxDays,
            ShippingDispatchSchedulingAuthority.MaxHorizonDates,
            customerSlotMinutes: null);
        Assert.True(plan.IsSuccess);
        Assert.NotEmpty(plan.Value!.Dates);
    }

    private static DateOnly AdvanceToNextWorkingDay(ShippingSchedulingWeekView carrier, DateOnly date)
    {
        for (var offset = 1; offset <= 7; offset++)
        {
            var candidate = date.AddDays(offset);
            var iso = candidate.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)candidate.DayOfWeek;
            if (carrier.Days.Single(day => day.Weekday == iso).IsWorking)
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("week_without_working_day");
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
