using Tooba.Order.Application.Storefront;
using Tooba.Order.Application.Storefront.Models;
using Tooba.Order.Application.Storefront.Services;
using Xunit;

namespace Tooba.Order.Tests.Storefront;

/// <summary>
/// TB-SHIPPING-STOREFRONT-CUSTOMER-RECEIVING-CALENDAR-213 — تقویم <b>دریافت مشتری</b>.
///
/// تصمیم مالک (بند ۲۹ سند قواعد): «۱ تا ۳ روز تحویل» وعدهٔ دریافت مشتری است، مرجعْ تاریخ سفارش،
/// آماده‌سازی و حمل داخل همان وعده، روز تعطیل شمرده نمی‌شود و ساعت‌ها از همان روزِ تقویم روش می‌آیند.
/// این تست‌ها فقط محاسبهٔ خالص را می‌سنجند: بدون Host، بدون دیتابیس، بدون HTTP و بدون مرورگر.
/// </summary>
public sealed class StorefrontShippingReceivingCalendarTests
{
    // 2026-10-24 شنبه است؛ 2026-10-25 یکشنبه، 2026-10-26 دوشنبه و 2026-10-27 سه‌شنبه.
    private static readonly DateOnly Saturday = new(2026, 10, 24);
    private static readonly DateOnly Sunday = new(2026, 10, 25);
    private static readonly DateOnly Monday = new(2026, 10, 26);
    private static readonly DateOnly Tuesday = new(2026, 10, 27);
    private static readonly DateOnly Wednesday = new(2026, 10, 28);
    private static readonly DateOnly Thursday = new(2026, 10, 29);

    private static readonly TimeOnly Nine = new(9, 0);
    private static readonly TimeOnly Twelve = new(12, 0);

    private static StorefrontShippingReceivingWeek AllWorking(TimeOnly? opensAt = null, TimeOnly? closesAt = null)
        => new(Enum.GetValues<DayOfWeek>()
            .Select(day => new StorefrontShippingReceivingDay(
                day,
                true,
                opensAt ?? Nine,
                closesAt ?? new TimeOnly(17, 0)))
            .ToArray());

    private static StorefrontShippingReceivingWeek Closed(params DayOfWeek[] closedDays)
    {
        var closed = new HashSet<DayOfWeek>(closedDays);
        return new(Enum.GetValues<DayOfWeek>()
            .Select(day => new StorefrontShippingReceivingDay(
                day,
                !closed.Contains(day),
                Nine,
                Twelve))
            .ToArray());
    }

    private static StorefrontShippingReceivingWeek WithHours(
        DayOfWeek day,
        TimeOnly opensAt,
        TimeOnly closesAt)
    {
        var week = AllWorking();
        return new(week.Days
            .Select(x => x.DayOfWeek == day
                ? new StorefrontShippingReceivingDay(day, true, opensAt, closesAt)
                : x)
            .ToArray());
    }

    [Fact]
    public void Two_working_days_after_the_order_date_become_the_earliest_receiving_date()
    {
        var calendar = StorefrontShippingCalculator.BuildCustomerReceivingCalendar(
            Saturday, 2, AllWorking(), deliveryHorizonDays: 3);

        Assert.Equal(Monday, calendar.EarliestEligibleDate);
        Assert.Equal(
            new[] { Monday, Tuesday, Wednesday },
            calendar.AvailableDates.Select(x => x.Date).ToArray());
    }

    [Fact]
    public void Closed_monday_between_the_two_working_days_is_skipped()
    {
        // مثال صریح مالک: سفارش شنبه؛ یکشنبه روز کاری #۱؛ دوشنبه تعطیل؛ سه‌شنبه روز کاری #۲.
        var week = Closed(DayOfWeek.Monday);

        var calendar = StorefrontShippingCalculator.BuildCustomerReceivingCalendar(
            Saturday, 2, week, deliveryHorizonDays: 2);

        Assert.Equal(Tuesday, calendar.EarliestEligibleDate);
        Assert.DoesNotContain(Monday, calendar.AvailableDates.Select(x => x.Date));
        Assert.Equal(
            new[] { Tuesday, Wednesday },
            calendar.AvailableDates.Select(x => x.Date).ToArray());
    }

    [Fact]
    public void Reference_date_on_a_closed_day_never_becomes_the_promise()
    {
        var week = Closed(DayOfWeek.Sunday);

        var zeroOffset = StorefrontShippingCalculator.BuildCustomerReceivingCalendar(
            Sunday, 0, week, deliveryHorizonDays: 2);
        var oneWorkingDay = StorefrontShippingCalculator.BuildCustomerReceivingCalendar(
            Sunday, 1, week, deliveryHorizonDays: 1);

        Assert.Equal(Monday, zeroOffset.EarliestEligibleDate);
        Assert.Equal(Monday, zeroOffset.AvailableDates[0].Date);
        // «اولین روز کاری بعد از تاریخ سفارش» = دوشنبه؛ روز تعطیل هرگز شمرده یا پیشنهاد نمی‌شود.
        Assert.Equal(Monday, oneWorkingDay.EarliestEligibleDate);
    }

    [Fact]
    public void Consecutive_closed_days_and_the_week_boundary_are_skipped()
    {
        var week = Closed(DayOfWeek.Thursday, DayOfWeek.Friday);

        // از سه‌شنبه: چهارشنبه #۱، پنجشنبه/جمعه تعطیل، شنبه #۲ (عبور از مرز هفته).
        var acrossBoundary = StorefrontShippingCalculator.BuildCustomerReceivingCalendar(
            Tuesday, 2, week, deliveryHorizonDays: 2);
        // سفارش روی یک روز تعطیل: اول شنبه بعدی روز کاری #۱ است.
        var fromClosedReference = StorefrontShippingCalculator.BuildCustomerReceivingCalendar(
            new DateOnly(2026, 10, 23), 1, week, deliveryHorizonDays: 1);

        Assert.Equal(new DateOnly(2026, 10, 31), acrossBoundary.EarliestEligibleDate);
        Assert.Equal(Saturday, fromClosedReference.EarliestEligibleDate);
    }

    [Fact]
    public void Closed_date_after_the_earliest_date_is_never_offered()
    {
        var week = Closed(DayOfWeek.Wednesday);

        var calendar = StorefrontShippingCalculator.BuildCustomerReceivingCalendar(
            Saturday, 0, week, deliveryHorizonDays: 5);

        Assert.Equal(Saturday, calendar.EarliestEligibleDate);
        Assert.Equal(
            new[] { Saturday, Sunday, Monday, Tuesday, Thursday },
            calendar.AvailableDates.Select(x => x.Date).ToArray());
        Assert.DoesNotContain(Wednesday, calendar.AvailableDates.Select(x => x.Date));
    }

    [Fact]
    public void Each_date_keeps_its_own_interval_and_fractional_time_is_preserved()
    {
        var sundayOpen = TimeOnly.FromTimeSpan(TimeSpan.FromSeconds(10 * 3600 + 30 * 60 + 15.5));
        var sundayClose = TimeOnly.FromTimeSpan(TimeSpan.FromSeconds(14 * 3600 + 45 * 60 + 45.25));
        var week = WithHours(DayOfWeek.Sunday, sundayOpen, sundayClose);

        var calendar = StorefrontShippingCalculator.BuildCustomerReceivingCalendar(
            Saturday, 0, week, deliveryHorizonDays: 2);

        // هیچ بازهٔ ثابت ۹ تا ۱۲ و هیچ ساعت مرورگر: هر تاریخ بازهٔ همان روز را دارد.
        Assert.Equal(Nine, calendar.AvailableDates[0].OpensAt);
        Assert.Equal(new TimeOnly(17, 0), calendar.AvailableDates[0].ClosesAt);
        Assert.Equal(sundayOpen, calendar.AvailableDates[1].OpensAt);
        Assert.Equal(sundayClose, calendar.AvailableDates[1].ClosesAt);
        Assert.NotEqual(calendar.AvailableDates[0].OpensAt, calendar.AvailableDates[1].OpensAt);
    }

    [Fact]
    public void A_fully_closed_week_reports_explicit_no_availability()
    {
        var calendar = StorefrontShippingCalculator.BuildCustomerReceivingCalendar(
            Saturday, 2, Closed(Enum.GetValues<DayOfWeek>()), deliveryHorizonDays: 5);

        Assert.Null(calendar.EarliestEligibleDate);
        Assert.Empty(calendar.AvailableDates);
    }

    [Fact]
    public void Incomplete_duplicate_and_out_of_range_weeks_are_rejected_without_fallback()
    {
        var incomplete = new StorefrontShippingReceivingWeek(
            AllWorking().Days.Take(6).ToArray());
        var duplicate = new StorefrontShippingReceivingWeek(
            AllWorking().Days.Select(x => x.DayOfWeek == DayOfWeek.Friday
                ? new StorefrontShippingReceivingDay(DayOfWeek.Monday, true, Nine, Twelve)
                : x).ToArray());
        var outOfRange = new StorefrontShippingReceivingWeek(
            AllWorking().Days.Select(x => x.DayOfWeek == DayOfWeek.Friday
                ? new StorefrontShippingReceivingDay((DayOfWeek)7, true, Nine, Twelve)
                : x).ToArray());

        foreach (var week in new[] { incomplete, duplicate, outOfRange })
        {
            var exception = Assert.Throws<StorefrontOrderException>(() =>
                StorefrontShippingCalculator.BuildCustomerReceivingCalendar(Saturday, 1, week, 3));
            Assert.Equal(StorefrontOrderErrors.ShippingDeliveryInvalid, exception.Code);
        }
    }

    [Fact]
    public void Invalid_working_interval_and_out_of_range_inputs_are_rejected_not_repaired()
    {
        var reversed = WithHours(DayOfWeek.Sunday, Twelve, Nine);
        var zeroLength = WithHours(DayOfWeek.Sunday, Nine, Nine);
        var closedWithOddInterval = new StorefrontShippingReceivingWeek(
            AllWorking().Days.Select(x => x.DayOfWeek == DayOfWeek.Friday
                ? new StorefrontShippingReceivingDay(DayOfWeek.Friday, false, Twelve, Nine)
                : x).ToArray());

        foreach (var week in new[] { reversed, zeroLength })
        {
            var exception = Assert.Throws<StorefrontOrderException>(() =>
                StorefrontShippingCalculator.BuildCustomerReceivingCalendar(Saturday, 1, week, 3));
            Assert.Equal(StorefrontOrderErrors.ShippingDeliveryInvalid, exception.Code);
        }

        var negativeOffset = Assert.Throws<StorefrontOrderException>(() =>
            StorefrontShippingCalculator.BuildCustomerReceivingCalendar(Saturday, -1, AllWorking(), 3));
        Assert.Equal(StorefrontOrderErrors.ShippingDeliveryInvalid, negativeOffset.Code);

        var negativeHorizon = Assert.Throws<StorefrontOrderException>(() =>
            StorefrontShippingCalculator.BuildCustomerReceivingCalendar(Saturday, 1, AllWorking(), -1));
        Assert.Equal(StorefrontOrderErrors.ShippingDeliveryInvalid, negativeHorizon.Code);

        // روز تعطیل بازهٔ دریافت ندارد؛ مقدار نامرتب آن نه اصلاح می‌شود و نه رد.
        var closedDay = StorefrontShippingCalculator.BuildCustomerReceivingCalendar(
            Saturday, 0, closedWithOddInterval, deliveryHorizonDays: 1);
        Assert.Equal(Saturday, closedDay.EarliestEligibleDate);
        Assert.Equal(Nine, closedDay.AvailableDates[0].OpensAt);
    }

    [Fact]
    public void Two_groups_keep_their_own_minimum_and_own_hours_without_shared_mutation()
    {
        var firstWeek = WithHours(DayOfWeek.Saturday, Nine, Twelve);
        var secondWeek = WithHours(DayOfWeek.Saturday, new TimeOnly(16, 0), new TimeOnly(21, 0));

        var first = StorefrontShippingCalculator.BuildCustomerReceivingCalendar(
            Saturday, 1, firstWeek, deliveryHorizonDays: 1);
        var second = StorefrontShippingCalculator.BuildCustomerReceivingCalendar(
            Saturday, 3, secondWeek, deliveryHorizonDays: 5);

        Assert.Equal(Sunday, first.EarliestEligibleDate);
        Assert.Equal(Tuesday, second.EarliestEligibleDate);
        Assert.Equal(Nine, first.AvailableDates[0].OpensAt);
        Assert.Equal(new TimeOnly(17, 0), first.AvailableDates[0].ClosesAt);
        // گروه دوم سه روز کاری بعد شروع می‌شود، پس شنبه در فهرستش دیرتر می‌آید و بازهٔ خودش را دارد.
        Assert.Equal(Nine, second.AvailableDates[0].OpensAt);
        Assert.Equal(new DateOnly(2026, 10, 31), second.AvailableDates[^1].Date);
        Assert.Equal(new TimeOnly(16, 0), second.AvailableDates[^1].OpensAt);
        Assert.Equal(new TimeOnly(21, 0), second.AvailableDates[^1].ClosesAt);
        Assert.Equal(7, firstWeek.Days.Count);
        Assert.Equal(7, secondWeek.Days.Count);
        Assert.Equal(9, firstWeek.Days.Single(x => x.DayOfWeek == DayOfWeek.Saturday).OpensAt.Hour);
        Assert.Equal(16, secondWeek.Days.Single(x => x.DayOfWeek == DayOfWeek.Saturday).OpensAt.Hour);
    }

    [Fact]
    public void Legacy_delivery_calculator_facts_remain_unchanged()
    {
        var minimum = StorefrontShippingCalculator.ComputeMinimumDeliveryDate(Saturday, 2, 3);

        Assert.Equal(Thursday, minimum);
        Assert.Equal(5, StorefrontShippingCalculator.BuildDeliveryDates(minimum, 5).Count);
        Assert.Equal(minimum, StorefrontShippingCalculator.BuildDeliveryDates(minimum, 5)[0]);
        Assert.Equal(4, StorefrontShippingCalculator.ValidTimeWindows(minimum, minimum).Count);
        Assert.Empty(StorefrontShippingCalculator.ValidTimeWindows(Saturday, Monday));
        Assert.Equal("9-12", StorefrontShippingCalculator.DayTimeWindows[0].Value);
    }
}
