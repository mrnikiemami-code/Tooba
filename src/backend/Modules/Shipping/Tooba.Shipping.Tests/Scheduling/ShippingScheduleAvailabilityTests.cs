using Tooba.Shipping.Contracts.Operations;
using Tooba.Shipping.Domain.Aggregates;
using Tooba.Shipping.Domain.Scheduling;
using Xunit;

namespace Tooba.Shipping.Tests.Scheduling;

/// <summary>
/// TB-SHIPPING-FULL-OPERATIONS-076 — carrier pickup/dispatch availability. The evaluator is
/// deterministic over an EXPLICIT instant, fail-open for absent/inactive schedules (so legacy
/// methods are untouched), fail-closed for ambiguity and unresolvable time zones, and it never
/// claims enforced capacity.
/// </summary>
public sealed class ShippingScheduleAvailabilityTests
{
    private const string Tehran = "Asia/Tehran";

    private static readonly DateTimeOffset Window13To14Sunday = new(2026, 10, 18, 9, 30, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Window16To17Sunday = new(2026, 10, 18, 13, 0, 0, TimeSpan.Zero);

    // 2026-10-18 is a Sunday (ISO weekday 7). Iran is UTC+03:30 in October, so:
    //   09:30Z -> 13:00 local, 10:00Z -> 13:30 local, 10:30Z -> 14:00 local, 13:00Z -> 16:30 local.

    private static ShippingOperatingSchedule Schedule(
        IEnumerable<ShippingOperatingWindowDefinition> windows,
        string? option = null,
        string timeZone = Tehran)
        => ShippingOperatingSchedule.Create(
            "tipax",
            timeZone,
            windows,
            new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero),
            option);

    private static ShippingOperatingWindowDefinition Window(int weekday, int startHour, int startMinute, int endHour, int endMinute)
        => new(
            weekday,
            new TimeOnly(startHour, startMinute),
            new TimeOnly(endHour, endMinute));

    private static ShippingScheduleEvaluation Evaluate(
        IReadOnlyCollection<ShippingOperatingSchedule> schedules,
        DateTimeOffset? requested,
        string? option = null)
        => ShippingScheduleAvailabilityEvaluator.Evaluate(schedules, "tipax", option, requested);

    [Fact]
    public void A_requested_instant_inside_the_sunday_afternoon_window_is_available()
    {
        var schedule = Schedule([Window(7, 13, 0, 14, 0)]);

        var inside = Evaluate([schedule], new DateTimeOffset(2026, 10, 18, 10, 0, 0, TimeSpan.Zero));

        Assert.Equal(ShippingScheduleAvailability.Available, inside.Availability);
        Assert.Equal(ShippingScheduleEvaluationReasonCodes.Available, inside.ReasonCode);
        Assert.Equal(7, inside.LocalWeekday);
        Assert.Equal(new TimeOnly(13, 30), inside.LocalTime);
        Assert.Equal(Tehran, inside.TimeZoneId);
        Assert.True(inside.IsConstraintSatisfied);
    }

    [Fact]
    public void A_requested_instant_exactly_at_the_half_open_end_is_unavailable()
    {
        var schedule = Schedule([Window(7, 13, 0, 14, 0)]);

        // 10:30Z is 14:00 local: the window is [13:00, 14:00), so the boundary belongs to the NEXT window.
        var atEnd = Evaluate([schedule], new DateTimeOffset(2026, 10, 18, 10, 30, 0, TimeSpan.Zero));

        Assert.Equal(ShippingScheduleAvailability.Unavailable, atEnd.Availability);
        Assert.Equal(ShippingScheduleEvaluationReasonCodes.OutsideWindows, atEnd.ReasonCode);
        Assert.Null(atEnd.MatchedWindowId);
        Assert.False(atEnd.IsConstraintSatisfied);
    }

    [Fact]
    public void The_start_boundary_is_inclusive()
    {
        var schedule = Schedule([Window(7, 13, 0, 14, 0)]);

        var atStart = Evaluate([schedule], Window13To14Sunday);

        Assert.Equal(ShippingScheduleAvailability.Available, atStart.Availability);
        Assert.Equal(new TimeOnly(13, 0), atStart.LocalTime);
    }

    [Fact]
    public void A_second_window_on_the_same_day_is_matched_independently()
    {
        var schedule = Schedule([Window(7, 13, 0, 14, 0), Window(7, 16, 0, 17, 0)]);

        var second = Evaluate([schedule], Window16To17Sunday);

        Assert.Equal(ShippingScheduleAvailability.Available, second.Availability);
        Assert.Equal(new TimeOnly(16, 30), second.LocalTime);
        Assert.NotNull(second.MatchedWindowId);
    }

    [Fact]
    public void A_matching_local_time_on_the_wrong_weekday_is_unavailable()
    {
        var schedule = Schedule([Window(7, 13, 0, 14, 0)]);

        // Same local time, but a Monday in Tehran (2026-10-19 09:30Z -> 13:00 local Monday).
        var monday = Evaluate([schedule], new DateTimeOffset(2026, 10, 19, 9, 30, 0, TimeSpan.Zero));

        Assert.Equal(ShippingScheduleAvailability.Unavailable, monday.Availability);
        Assert.Equal(1, monday.LocalWeekday);
    }

    [Fact]
    public void No_schedule_at_all_is_not_configured_and_never_a_rejection()
    {
        var none = Evaluate([], Window13To14Sunday);

        Assert.Equal(ShippingScheduleAvailability.NotConfigured, none.Availability);
        Assert.Equal(ShippingScheduleEvaluationReasonCodes.NotConfigured, none.ReasonCode);
        Assert.True(none.IsConstraintSatisfied);
        Assert.Null(none.TimeZoneId);
    }

    [Fact]
    public void An_inactive_schedule_has_the_documented_safe_meaning_of_not_enforced()
    {
        var schedule = Schedule([Window(7, 13, 0, 14, 0)]);
        schedule.Deactivate(new DateTimeOffset(2026, 10, 2, 0, 0, 0, TimeSpan.Zero));

        var inactive = Evaluate([schedule], Window13To14Sunday);

        Assert.Equal(ShippingScheduleAvailability.InactiveNotEnforced, inactive.Availability);
        Assert.Equal(ShippingScheduleEvaluationReasonCodes.InactiveNotEnforced, inactive.ReasonCode);
        Assert.True(inactive.IsConstraintSatisfied);
    }

    [Fact]
    public void An_active_schedule_without_a_requested_instant_is_not_evaluated()
    {
        var schedule = Schedule([Window(7, 13, 0, 14, 0)]);

        var notEvaluated = Evaluate([schedule], null);

        Assert.Equal(ShippingScheduleAvailability.NotEvaluated, notEvaluated.Availability);
        Assert.Equal(ShippingScheduleEvaluationReasonCodes.NotEvaluated, notEvaluated.ReasonCode);
        Assert.Equal(Tehran, notEvaluated.TimeZoneId);
        // "Not evaluated" is never "available", so it cannot be treated as a satisfied constraint.
        Assert.False(notEvaluated.IsConstraintSatisfied);
    }

    [Fact]
    public void An_option_specific_schedule_does_not_apply_to_another_option()
    {
        var express = Schedule([Window(7, 13, 0, 14, 0)], option: "express");

        var standard = Evaluate([express], Window13To14Sunday, option: "standard");
        Assert.Equal(ShippingScheduleAvailability.NotConfigured, standard.Availability);

        var matching = Evaluate([express], Window13To14Sunday, option: "express");
        Assert.Equal(ShippingScheduleAvailability.Available, matching.Availability);
    }

    [Fact]
    public void A_service_level_schedule_is_the_documented_fallback_for_any_option()
    {
        var serviceLevel = Schedule([Window(7, 13, 0, 14, 0)]);

        var withOption = Evaluate([serviceLevel], Window13To14Sunday, option: "express");

        Assert.Equal(ShippingScheduleAvailability.Available, withOption.Availability);
    }

    [Fact]
    public void Two_active_schedules_for_the_same_identity_are_reported_as_ambiguous()
    {
        var first = Schedule([Window(7, 13, 0, 14, 0)]);
        var second = Schedule([Window(7, 16, 0, 17, 0)]);

        var ambiguous = Evaluate([first, second], Window13To14Sunday);

        Assert.Equal(ShippingScheduleAvailability.Ambiguous, ambiguous.Availability);
        Assert.Equal(ShippingScheduleEvaluationReasonCodes.Ambiguous, ambiguous.ReasonCode);
        Assert.False(ambiguous.IsConstraintSatisfied);
    }

    [Fact]
    public void Configured_capacity_is_reported_but_never_enforced()
    {
        var schedule = Schedule(
        [
            new ShippingOperatingWindowDefinition(7, new TimeOnly(13, 0), new TimeOnly(14, 0), 25),
        ]);

        var available = Evaluate([schedule], Window13To14Sunday);

        Assert.Equal(25, available.ConfiguredCapacityTotal);
        Assert.Equal(ShippingCapacityEnforcement.NotEnforced, available.CapacityEnforcement);
        Assert.Equal(ShippingScheduleAvailability.Available, available.Availability);
    }

    [Fact]
    public void A_window_without_capacity_reports_no_configured_capacity()
    {
        var schedule = Schedule([Window(7, 13, 0, 14, 0)]);

        var available = Evaluate([schedule], Window13To14Sunday);

        Assert.Null(available.ConfiguredCapacityTotal);
        Assert.Equal(ShippingCapacityEnforcement.NotEnforced, available.CapacityEnforcement);
    }

    [Fact]
    public void The_schedule_verdict_is_not_a_customer_delivery_promise()
    {
        // The evaluator may only describe carrier availability; a promised/estimated delivery date is
        // Order's asset and must never appear on the Shipping schedule surface.
        var scheduleProperties = typeof(ShippingOperatingSchedule).GetProperties().Select(x => x.Name).ToArray();
        var evaluationProperties = typeof(ShippingScheduleEvaluation).GetProperties().Select(x => x.Name).ToArray();

        foreach (var promiseConcept in new[]
                 {
                     "PromisedDeliveryDate", "DeliveryDate", "EstimatedArrival", "Promise", "PromisedSlot",
                 })
        {
            Assert.DoesNotContain(promiseConcept, scheduleProperties);
            Assert.DoesNotContain(promiseConcept, evaluationProperties);
        }

        // The explicit carrier-availability wording IS present, so the boundary is stated, not implied.
        Assert.Contains("Availability", evaluationProperties);
        Assert.Contains("LocalWeekday", evaluationProperties);
    }
}
