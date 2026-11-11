using Tooba.Fulfillment.Application.WorkQueue.Models;
using Tooba.Order.Contracts.Shipping;
using Xunit;

namespace Tooba.Fulfillment.Tests.Behavior;

/// <summary>
/// TB-264: the operational preparation state and the separate preparation filter, derived from the SAME
/// accepted plan and the SAME injected instant as the handoff classification.
/// </summary>
/// <remarks>
/// The point of the matrix is that preparation and handoff are two different facts: a row can be comfortably
/// «آینده» for acceptance while its preparation must already have started, and a legacy policy-v1 plan is never
/// claimed as preparation-due because it carries no preparation deadline at all.
/// </remarks>
public sealed class AdminFulfillmentPreparationTiming264Tests
{
    private static readonly Guid GroupA = Guid.Parse("01a1c264-0000-7000-9000-00000000000a");

    // Thursday 2026-11-12, handoff window 09:00–17:00 Asia/Tehran (05:30–13:30 UTC).
    private static readonly DateOnly HandoffDate = new(2026, 11, 12);
    private static readonly DateTimeOffset OpenUtc = new(2026, 11, 12, 5, 30, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset CloseUtc = new(2026, 11, 12, 13, 30, 0, TimeSpan.Zero);

    [Fact]
    public void A_legacy_plan_has_no_preparation_state_and_is_never_preparation_due()
    {
        var timing = AdminFulfillmentDispatchTimingAuthority.Classify(
            pending: true,
            inTransit: false,
            [Plan(policyVersion: 1, preparationStart: null, transferDeparture: null, handoffDeadline: null)],
            CloseUtc.AddDays(-2));

        Assert.Equal(AdminFulfillmentPreparationState.None, timing.PreparationState);
        Assert.Null(timing.PreparationStartUtc);
        Assert.Null(timing.TransferDepartureUtc);
        // The acceptance deadline is still provable from the stored window itself; only the two
        // backward-derived preparation/transfer instants are absent for a legacy plan.
        Assert.Equal(CloseUtc, timing.HandoffDeadlineUtc);
        Assert.Equal(1, timing.PolicyVersion);
        Assert.False(AdminFulfillmentDispatchTimingAuthority.IsPreparationDue(timing));
        Assert.False(AdminFulfillmentDispatchTimingAuthority.Matches(
            timing, AdminFulfillmentDispatchTimingAuthority.FilterPreparationDue));

        // The historical window semantics of «سررسیدشده» are unchanged for a plan-less-prep row: before the
        // window opens it is not due, exactly open it is due.
        Assert.False(AdminFulfillmentDispatchTimingAuthority.Matches(
            timing, AdminFulfillmentDispatchTimingAuthority.FilterDue));
    }

    [Fact]
    public void B_preparation_state_follows_its_own_deadline_not_the_handoff_window()
    {
        var plan = Plan(policyVersion: 2, preparationStart: CloseUtc.AddHours(-6), transferDeparture: CloseUtc.AddHours(-1), handoffDeadline: CloseUtc);

        var upcoming = AdminFulfillmentDispatchTimingAuthority.Classify(
            true, false, [plan], CloseUtc.AddHours(-7));
        Assert.Equal(AdminFulfillmentPreparationState.Today, upcoming.PreparationState); // prep date is the handoff day, still ahead
        Assert.Equal(new DateTimeOffset(2026, 11, 12, 7, 30, 0, TimeSpan.Zero), upcoming.PreparationStartUtc);
        Assert.Equal(new DateTimeOffset(2026, 11, 12, 12, 30, 0, TimeSpan.Zero), upcoming.TransferDepartureUtc);
        Assert.Equal(2, upcoming.PolicyVersion);
        Assert.False(AdminFulfillmentDispatchTimingAuthority.IsPreparationDue(upcoming));

        // Exactly at preparation start is already "should be underway" — the deadline is a start instant.
        var due = AdminFulfillmentDispatchTimingAuthority.Classify(
            true, false, [plan], upcoming.PreparationStartUtc!.Value);
        Assert.Equal(AdminFulfillmentPreparationState.Overdue, due.PreparationState);
        Assert.True(AdminFulfillmentDispatchTimingAuthority.IsPreparationDue(due));
        Assert.True(AdminFulfillmentDispatchTimingAuthority.Matches(
            due, AdminFulfillmentDispatchTimingAuthority.FilterPreparationDue));
    }

    [Fact]
    public void B_preparation_due_is_surfaced_while_the_carrier_window_is_still_closed()
    {
        var plan = Plan(policyVersion: 2, preparationStart: OpenUtc.AddHours(-3), transferDeparture: OpenUtc.AddMinutes(-30), handoffDeadline: CloseUtc);

        // 04:00 UTC = 07:30 Tehran: the window has not opened yet, but preparation should have started at 02:30 UTC.
        var timing = AdminFulfillmentDispatchTimingAuthority.Classify(
            true, false, [plan], new DateTimeOffset(2026, 11, 12, 4, 0, 0, TimeSpan.Zero));

        Assert.Equal(AdminFulfillmentDispatchBucket.Today, timing.Bucket);
        Assert.Equal(AdminFulfillmentHandoffWindowState.TodayNotOpen, timing.WindowState);
        Assert.Equal(AdminFulfillmentPreparationState.Overdue, timing.PreparationState);

        // Preparation-due is its own filter and «سررسیدشده» is prep-aware: a row that must already be in
        // preparation can no longer look comfortably upcoming.
        Assert.True(AdminFulfillmentDispatchTimingAuthority.Matches(
            timing, AdminFulfillmentDispatchTimingAuthority.FilterPreparationDue));
        Assert.True(AdminFulfillmentDispatchTimingAuthority.Matches(
            timing, AdminFulfillmentDispatchTimingAuthority.FilterDue));
        Assert.True(AdminFulfillmentDispatchTimingAuthority.Matches(
            timing, AdminFulfillmentDispatchTimingAuthority.FilterToday));
    }

    [Fact]
    public void B_a_passed_handoff_deadline_is_handoff_missed_and_an_upcoming_preparation_is_upcoming()
    {
        var plan = Plan(policyVersion: 2, preparationStart: CloseUtc.AddHours(-6), transferDeparture: CloseUtc.AddHours(-1), handoffDeadline: CloseUtc);

        var missed = AdminFulfillmentDispatchTimingAuthority.Classify(true, false, [plan], CloseUtc);
        Assert.Equal(AdminFulfillmentPreparationState.HandoffMissed, missed.PreparationState);
        Assert.True(AdminFulfillmentDispatchTimingAuthority.IsPreparationDue(missed));
        Assert.Equal(AdminFulfillmentDispatchBucket.Overdue, missed.Bucket);

        // A preparation start tomorrow (relative to now) is Upcoming even though the handoff is near.
        var far = Plan(
            handoffDate: new DateOnly(2026, 11, 16),
            policyVersion: 2,
            preparationStart: new DateTimeOffset(2026, 11, 15, 6, 0, 0, TimeSpan.Zero),
            transferDeparture: new DateTimeOffset(2026, 11, 15, 12, 0, 0, TimeSpan.Zero),
            handoffDeadline: new DateTimeOffset(2026, 11, 16, 13, 30, 0, TimeSpan.Zero));
        var upcoming = AdminFulfillmentDispatchTimingAuthority.Classify(true, false, [far], CloseUtc);
        Assert.Equal(AdminFulfillmentPreparationState.Upcoming, upcoming.PreparationState);
        Assert.False(AdminFulfillmentDispatchTimingAuthority.IsPreparationDue(upcoming));
        Assert.False(AdminFulfillmentDispatchTimingAuthority.Matches(
            upcoming, AdminFulfillmentDispatchTimingAuthority.FilterPreparationDue));
    }

    [Fact]
    public void C_finished_physical_work_is_never_asked_to_prepare_again()
    {
        var plan = Plan(policyVersion: 2, preparationStart: CloseUtc.AddHours(-6), transferDeparture: CloseUtc.AddHours(-1), handoffDeadline: CloseUtc);

        // A dispatched/in-transit parcel: the plan is context only.
        var transit = AdminFulfillmentDispatchTimingAuthority.Classify(
            pending: false, inTransit: true, [plan], CloseUtc.AddHours(-8));
        Assert.Equal(AdminFulfillmentDispatchBucket.InTransit, transit.Bucket);
        Assert.Equal(AdminFulfillmentPreparationState.NotApplicable, transit.PreparationState);
        Assert.Equal(2, transit.PolicyVersion);
        Assert.False(AdminFulfillmentDispatchTimingAuthority.IsPreparationDue(transit));

        // Delivered/cancelled: not queued at all.
        var notQueued = AdminFulfillmentDispatchTimingAuthority.Classify(false, false, [plan], CloseUtc.AddHours(-8));
        Assert.Equal(AdminFulfillmentDispatchBucket.NotQueued, notQueued.Bucket);
        Assert.Equal(AdminFulfillmentPreparationState.None, notQueued.PreparationState);
    }

    [Fact]
    public void C_the_two_filters_are_distinct_and_the_physical_priority_is_unchanged()
    {
        Assert.Contains(AdminFulfillmentDispatchTimingAuthority.FilterPreparationDue, AdminFulfillmentDispatchTimingAuthority.TimingFilters);
        Assert.NotEqual(
            AdminFulfillmentDispatchTimingAuthority.FilterPreparationDue,
            AdminFulfillmentDispatchTimingAuthority.FilterDue);

        Assert.Equal(
            AdminFulfillmentPreparationStateNames.Overdue,
            AdminFulfillmentPreparationStateNames.Of(AdminFulfillmentPreparationState.Overdue));
        Assert.Equal(0, AdminFulfillmentDispatchTimingAuthority.PriorityRank(AdminFulfillmentDispatchBucket.Overdue));
        Assert.Equal(1, AdminFulfillmentDispatchTimingAuthority.PriorityRank(AdminFulfillmentDispatchBucket.Today));
    }

    // ---------------------------------------------------------------- helpers

    private static OrderAcceptedGroupDispatchPlanFact Plan(
        DateOnly? handoffDate = null,
        int policyVersion = 2,
        DateTimeOffset? preparationStart = null,
        DateTimeOffset? transferDeparture = null,
        DateTimeOffset? handoffDeadline = null) =>
        new(
            Guid.Parse("01a1c264-0000-7000-9000-0000000000f1"),
            GroupA,
            Guid.Parse("01a1c264-0000-7000-9000-0000000000f2"),
            Guid.Parse("01a1c264-0000-7000-9000-0000000000f3"),
            "post",
            "پیشتاز",
            handoffDate ?? HandoffDate,
            new TimeOnly(9, 0).Ticks,
            new TimeOnly(17, 0).Ticks,
            "Asia/Tehran",
            1,
            policyVersion,
            handoffDate ?? HandoffDate,
            "09:00-17:00",
            preparationStart,
            transferDeparture,
            handoffDeadline);
}
