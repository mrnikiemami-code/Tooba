using Tooba.Order.Application.Storefront.Shipping;
using Tooba.Shipping.Contracts.Rates;
using Tooba.Shipping.Contracts.Scheduling;
using Xunit;

namespace Tooba.Order.Tests.Checkout;

/// <summary>
/// TB-256-R1 (gap A): یک تعهد <b>جدید</b> بدون تقویم پذیرش حامل پذیرفته نمی‌شود، در حالی که شاهد
/// تاریخیِ ذخیره‌شدهٔ بدون تقویم فقط خوانده می‌شود و هیچ برنامهٔ حدسی نمی‌سازد.
/// </summary>
public sealed class PlannedDispatchCarrierRequirement256R1Tests
{
    private static readonly DateOnly ReceivingDate = new(2026, 11, 12);
    private static readonly Guid Snapshot = Guid.Parse("01a17d0e-0419-7000-b575-f507d2f89abc");

    /// <summary>۱۱:۰۰ تا ۱۳:۰۰ محلی؛ بازهٔ انتخاب‌شدهٔ مشتری برای همان تاریخ.</summary>
    private static readonly long WindowOpensTicks = new TimeOnly(11, 0).Ticks;

    /// <summary>۱۳:۰۰ محلی؛ پایان بازهٔ انتخاب‌شده.</summary>
    private static readonly long WindowClosesTicks = new TimeOnly(13, 0).Ticks;

    [Fact]
    public void A_new_commitment_without_a_carrier_calendar_is_refused_with_its_own_reason()
    {
        var resolved = StorefrontPlannedDispatchResolver.TryResolve(
            Quote(carrierWeek: null),
            ReceivingDate,
            WindowOpensTicks,
            WindowClosesTicks,
            StorefrontPlannedDispatchResolver.CurrentPolicyVersion,
            out var plan,
            out var reason);

        Assert.False(resolved);
        Assert.Null(plan);
        Assert.Equal(StorefrontPlannedDispatchResolver.CarrierScheduleMissing, reason);
    }

    [Fact]
    public void Historical_evidence_without_a_carrier_calendar_is_readable_and_never_invents_a_plan()
    {
        var resolved = StorefrontPlannedDispatchResolver.TryResolve(
            Quote(carrierWeek: null),
            ReceivingDate,
            WindowOpensTicks,
            WindowClosesTicks,
            StorefrontPlannedDispatchResolver.CurrentPolicyVersion,
            out var plan,
            out var reason,
            allowAbsentCarrierSchedule: true);

        Assert.True(resolved);
        Assert.Null(plan);
        Assert.Equal(string.Empty, reason);
    }

    [Fact]
    public void A_configured_carrier_calendar_resolves_the_latest_handoff_that_arrives_on_or_before_the_date()
    {
        var resolved = StorefrontPlannedDispatchResolver.TryResolve(
            Quote(carrierWeek: CarrierWeek("UTC", (1, 9, 0, 17), (2, 9, 0, 17), (3, 9, 0, 17), (4, 9, 0, 17), (5, 9, 0, 17))),
            ReceivingDate,
            WindowOpensTicks,
            WindowClosesTicks,
            ShippingDispatchSchedulingAuthority.PolicyVersion,
            out var plan,
            out var reason);

        Assert.True(resolved);
        Assert.Equal(string.Empty, reason);
        Assert.NotNull(plan);
        // Thursday 2026-11-12 with MAX 3 transport working days: Monday 2026-11-09 is the latest handoff
        // whose conservative arrival is not after the customer's chosen date.
        Assert.Equal(new DateOnly(2026, 11, 9), plan!.HandoffDate);
        Assert.Equal(3, plan.TransportWorkingDays);
        Assert.Equal("UTC", plan.HandoffTimeZone);
        Assert.True(plan.HandoffDate <= ReceivingDate);
    }

    private static ShippingCapturedQuoteResult Quote(ShippingCapturedCarrierWeek? carrierWeek) =>
        new(
            ShippingCapturedQuoteVerdict.Quoted,
            "quoted",
            "post",
            "standard",
            "IRR",
            120_000m,
            5_000m,
            125_000m,
            125_000m,
            0m,
            null,
            Snapshot,
            Guid.Parse("01a17d0e-0419-7000-b575-f507d2f89abd"),
            2_000_000_001L,
            2,
            null,
            Guid.Parse("01a1b92d-e97e-7000-999a-143350d0edaf"),
            null,
            2,
            3,
            null,
            null,
            carrierWeek);

    private static ShippingCapturedCarrierWeek CarrierWeek(
        string timeZone,
        params (int Weekday, int OpensHour, int OpensMinute, int ClosesHour)[] days) =>
        new(
            timeZone,
            Enumerable.Range(1, 7)
                .Select(weekday =>
                {
                    var match = days.FirstOrDefault(day => day.Weekday == weekday);
                    return match.Weekday == weekday
                        ? new ShippingCapturedCarrierWeekDay(
                            weekday,
                            IsWorking: true,
                            new TimeOnly(match.OpensHour, match.OpensMinute),
                            new TimeOnly(match.ClosesHour, 0))
                        : new ShippingCapturedCarrierWeekDay(weekday, IsWorking: false, null, null);
                })
                .ToArray());
}
