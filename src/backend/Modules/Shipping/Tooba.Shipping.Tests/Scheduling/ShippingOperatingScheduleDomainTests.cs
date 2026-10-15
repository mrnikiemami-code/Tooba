using Tooba.Shipping.Contracts.Errors;
using Tooba.Shipping.Domain.Scheduling;
using Xunit;

namespace Tooba.Shipping.Tests.Scheduling;

/// <summary>
/// TB-SHIPPING-FULL-OPERATIONS-071 — domain invariants of the weekly operating schedule:
/// ISO weekday range, half-open <c>[start,end)</c> windows, overlap refusal, timezone validation,
/// and the "schedule is not a promise" boundary.
/// </summary>
public sealed class ShippingOperatingScheduleDomainTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-10-15T00:00:00Z");

    private static ShippingOperatingSchedule Schedule(
        params ShippingOperatingWindowDefinition[] windows)
        => ShippingOperatingSchedule.Create("tipax", "Asia/Tehran", windows, Now, optionCode: "standard");

    [Fact]
    public void Sunday_13_00_to_14_00_plus_a_second_distinct_window_is_accepted()
    {
        var schedule = Schedule(
            new ShippingOperatingWindowDefinition(7, new TimeOnly(13, 0), new TimeOnly(14, 0)),
            new ShippingOperatingWindowDefinition(7, new TimeOnly(16, 30), new TimeOnly(18, 0), 25));

        Assert.True(schedule.IsActive);
        Assert.Equal("tipax", schedule.ServiceCode);
        Assert.Equal("standard", schedule.OptionCode);
        Assert.Equal("Asia/Tehran", schedule.TimeZoneId);
        Assert.Equal(2, schedule.Windows.Count);

        // Sorted by weekday then start, so the admin view is deterministic.
        Assert.Equal(new TimeOnly(13, 0), schedule.Windows[0].StartTime);
        Assert.Equal(new TimeOnly(16, 30), schedule.Windows[1].StartTime);
        Assert.Equal(25, schedule.Windows[1].Capacity);
        Assert.Equal(25, schedule.ConfiguredCapacityTotal);
    }

    [Fact]
    public void Adjacent_windows_do_not_overlap_because_the_range_is_half_open()
    {
        // 13:00-14:00 and 14:00-15:00 share only the instant 14:00, which the half-open semantics
        // assigns to the second window exactly once. A closed range would reject this as an overlap.
        var schedule = Schedule(
            new ShippingOperatingWindowDefinition(7, new TimeOnly(13, 0), new TimeOnly(14, 0)),
            new ShippingOperatingWindowDefinition(7, new TimeOnly(14, 0), new TimeOnly(15, 0)));

        Assert.Equal(2, schedule.Windows.Count);
        Assert.True(schedule.Windows[0].ContainsLocal(new TimeOnly(13, 59, 59)));
        Assert.False(schedule.Windows[0].ContainsLocal(new TimeOnly(14, 0)));
        Assert.True(schedule.Windows[1].ContainsLocal(new TimeOnly(14, 0)));
    }

    [Theory]
    [InlineData(13, 0, 13, 0)]   // zero length
    [InlineData(14, 0, 13, 0)]   // reversed
    [InlineData(22, 0, 2, 0)]    // overnight, not supported
    public void Reversed_zero_length_and_overnight_windows_are_refused(int startHour, int startMinute, int endHour, int endMinute)
    {
        var thrown = Record.Exception(() => Schedule(
            new ShippingOperatingWindowDefinition(
                7,
                new TimeOnly(startHour, startMinute),
                new TimeOnly(endHour, endMinute))));

        Assert.NotNull(thrown);
        Assert.Equal(ShippingErrorCodes.ScheduleWindowRangeInvalid, thrown!.Message);
    }

    [Fact]
    public void Overlapping_windows_on_the_same_weekday_are_refused_with_a_stable_code()
    {
        var thrown = Record.Exception(() => Schedule(
            new ShippingOperatingWindowDefinition(7, new TimeOnly(13, 0), new TimeOnly(15, 0)),
            new ShippingOperatingWindowDefinition(7, new TimeOnly(14, 0), new TimeOnly(16, 0))));

        Assert.NotNull(thrown);
        Assert.Equal(ShippingErrorCodes.ScheduleWindowOverlap, thrown!.Message);
    }

    [Fact]
    public void The_same_time_range_on_two_different_weekdays_is_not_an_overlap()
    {
        var schedule = Schedule(
            new ShippingOperatingWindowDefinition(6, new TimeOnly(13, 0), new TimeOnly(14, 0)),
            new ShippingOperatingWindowDefinition(7, new TimeOnly(13, 0), new TimeOnly(14, 0)));

        Assert.Equal(2, schedule.Windows.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(8)]
    [InlineData(-1)]
    public void Weekday_outside_the_iso_1_to_7_range_is_refused(int weekday)
    {
        var thrown = Record.Exception(() => Schedule(
            new ShippingOperatingWindowDefinition(weekday, new TimeOnly(13, 0), new TimeOnly(14, 0))));

        Assert.NotNull(thrown);
        Assert.Equal(ShippingErrorCodes.ScheduleWeekdayInvalid, thrown!.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    [InlineData(1_000_001)]
    public void Configured_capacity_must_be_positive_and_bounded(int capacity)
    {
        var thrown = Record.Exception(() => Schedule(
            new ShippingOperatingWindowDefinition(7, new TimeOnly(13, 0), new TimeOnly(14, 0), capacity)));

        Assert.NotNull(thrown);
        Assert.Equal(ShippingErrorCodes.ScheduleWindowCapacityInvalid, thrown!.Message);
    }

    [Fact]
    public void An_absent_capacity_stays_null_and_totals_stay_null()
    {
        var schedule = Schedule(
            new ShippingOperatingWindowDefinition(7, new TimeOnly(13, 0), new TimeOnly(14, 0)));

        Assert.Null(schedule.Windows[0].Capacity);

        // No window declares capacity, so there is no configured total to report - never a fabricated 0.
        Assert.Null(schedule.ConfiguredCapacityTotal);
    }

    [Fact]
    public void An_unresolvable_or_missing_timezone_is_refused()
    {
        var missing = Record.Exception(() => ShippingOperatingSchedule.Create(
            "tipax",
            "  ",
            [new ShippingOperatingWindowDefinition(7, new TimeOnly(13, 0), new TimeOnly(14, 0))],
            Now));
        Assert.Equal(ShippingErrorCodes.ScheduleTimeZoneRequired, missing!.Message);

        var unknown = Record.Exception(() => ShippingOperatingSchedule.Create(
            "tipax",
            "Mars/Olympus_Mons",
            [new ShippingOperatingWindowDefinition(7, new TimeOnly(13, 0), new TimeOnly(14, 0))],
            Now));
        Assert.Equal(ShippingErrorCodes.ScheduleTimeZoneInvalid, unknown!.Message);
    }

    [Fact]
    public void Windows_can_be_replaced_completely_and_repeat_the_same_body_idempotently()
    {
        var schedule = Schedule(
            new ShippingOperatingWindowDefinition(7, new TimeOnly(13, 0), new TimeOnly(14, 0)));

        var replacement = new[]
        {
            new ShippingOperatingWindowDefinition(1, new TimeOnly(9, 0), new TimeOnly(11, 0)),
            new ShippingOperatingWindowDefinition(7, new TimeOnly(13, 0), new TimeOnly(14, 0)),
        };

        schedule.ReplaceWindows(replacement, Now);
        Assert.Equal(2, schedule.Windows.Count);

        // Replaying the same body must not accumulate windows or change the set.
        schedule.ReplaceWindows(replacement, Now);
        Assert.Equal(2, schedule.Windows.Count);
        Assert.Equal(1, schedule.Windows[0].Weekday);
    }

    [Fact]
    public void Too_many_windows_are_refused()
    {
        var windows = Enumerable.Range(0, ShippingOperatingSchedule.MaxWindows + 1)
            .Select(i => new ShippingOperatingWindowDefinition(
                1,
                new TimeOnly(0, 0).Add(TimeSpan.FromMinutes(i * 2)),
                new TimeOnly(0, 0).Add(TimeSpan.FromMinutes(i * 2 + 1))))
            .ToArray();

        var thrown = Record.Exception(() => Schedule(windows));
        Assert.Equal(ShippingErrorCodes.ScheduleWindowsTooMany, thrown!.Message);
    }

    [Fact]
    public void Deactivate_and_activate_keep_the_stable_identity()
    {
        var schedule = Schedule(
            new ShippingOperatingWindowDefinition(7, new TimeOnly(13, 0), new TimeOnly(14, 0)));
        var id = schedule.ScheduleId;

        schedule.Deactivate(Now);
        Assert.False(schedule.IsActive);
        Assert.Equal(id, schedule.ScheduleId);

        schedule.Activate(Now);
        Assert.True(schedule.IsActive);
        Assert.Equal(id, schedule.ScheduleId);
    }

    [Fact]
    public void Option_identity_is_never_split_out_of_the_service_code()
    {
        var schedule = Schedule(
            new ShippingOperatingWindowDefinition(7, new TimeOnly(13, 0), new TimeOnly(14, 0)));

        Assert.True(schedule.BelongsTo("tipax", "standard"));
        Assert.True(schedule.BelongsTo("TIPAX", "STANDARD"));
        Assert.False(schedule.BelongsTo("tipax", null));
        Assert.False(schedule.BelongsTo("tipax", "express"));
        Assert.False(schedule.BelongsTo("post", "standard"));

        // A combined token is a different service code, never service + option.
        Assert.False(schedule.BelongsTo("tipax:standard", null));
    }

    [Fact]
    public void A_service_level_schedule_is_distinct_from_an_option_schedule()
    {
        var serviceLevel = ShippingOperatingSchedule.Create(
            "tipax",
            "Asia/Tehran",
            [new ShippingOperatingWindowDefinition(7, new TimeOnly(13, 0), new TimeOnly(14, 0))],
            Now);

        Assert.False(serviceLevel.IsOptionSpecific);
        Assert.Null(serviceLevel.OptionCode);
        Assert.True(serviceLevel.BelongsTo("tipax", null));
        Assert.False(serviceLevel.BelongsTo("tipax", "standard"));
    }
}
