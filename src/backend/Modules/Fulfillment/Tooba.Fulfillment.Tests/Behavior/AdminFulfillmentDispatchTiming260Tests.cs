using Tooba.Fulfillment.Application.WorkQueue.Models;
using Tooba.Order.Contracts.Shipping;
using Xunit;

namespace Tooba.Fulfillment.Tests.Behavior;

/// <summary>
/// TB-260 case A — the ONE dispatch-timing authority against an EXPLICIT instant matrix. No ambient clock, no
/// database and no browser arithmetic: the accepted handoff window is half-open <c>[open, close)</c>, "today"
/// is the calendar day in the accepted carrier zone, and the customer receiving promise is never an input of
/// the dispatch deadline.
/// </summary>
public sealed class AdminFulfillmentDispatchTiming260Tests
{
    private static readonly Guid GroupA = Guid.Parse("01a1c260-0000-7000-9000-00000000000a");
    private static readonly Guid GroupB = Guid.Parse("01a1c260-0000-7000-9000-00000000000b");
    private static readonly DateTimeOffset Now = new(2026, 11, 10, 9, 30, 0, TimeSpan.Zero);

    // ---------------------------------------------------------------- boundary matrix

    /// <summary>
    /// The exact instants of the half-open window: before open is NOT due, exactly open IS due, inside is due,
    /// one tick before close is due, exactly close is already late, and a past handoff day is late.
    /// </summary>
    [Theory]
    [InlineData(8, 59, AdminFulfillmentDispatchBucket.Today, AdminFulfillmentHandoffWindowState.TodayNotOpen, false)]
    [InlineData(9, 0, AdminFulfillmentDispatchBucket.Today, AdminFulfillmentHandoffWindowState.Open, true)]
    [InlineData(9, 30, AdminFulfillmentDispatchBucket.Today, AdminFulfillmentHandoffWindowState.Open, true)]
    [InlineData(16, 59, AdminFulfillmentDispatchBucket.Today, AdminFulfillmentHandoffWindowState.Open, true)]
    [InlineData(17, 0, AdminFulfillmentDispatchBucket.Overdue, AdminFulfillmentHandoffWindowState.Closed, true)]
    [InlineData(20, 0, AdminFulfillmentDispatchBucket.Overdue, AdminFulfillmentHandoffWindowState.Closed, true)]
    public void Handoff_window_is_half_open_in_the_carrier_zone(
        int hour,
        int minute,
        AdminFulfillmentDispatchBucket expectedBucket,
        AdminFulfillmentHandoffWindowState expectedWindow,
        bool expectedDue)
    {
        var now = Tehran(hour, minute);
        var timing = AdminFulfillmentDispatchTimingAuthority.Classify(
            pending: true,
            inTransit: false,
            [Plan(new DateOnly(2026, 11, 10), 9, 0, 17, 0)],
            now);

        Assert.Equal(expectedBucket, timing.Bucket);
        Assert.Equal(expectedWindow, timing.WindowState);
        Assert.Equal(expectedDue, AdminFulfillmentDispatchTimingAuthority.Matches(
            timing, AdminFulfillmentDispatchTimingAuthority.FilterDue));
        // The accepted handoff DAY is today in every case of this matrix; only the window state moves.
        Assert.True(timing.IsToday);
        Assert.Equal(expectedBucket == AdminFulfillmentDispatchBucket.Overdue, timing.IsOverdue);
    }

    [Fact]
    public void Yesterday_is_overdue_and_tomorrow_is_upcoming_and_never_due()
    {
        var yesterday = AdminFulfillmentDispatchTimingAuthority.Classify(
            true, false, [Plan(new DateOnly(2026, 11, 9), 9, 0, 17, 0)], Now);
        Assert.Equal(AdminFulfillmentDispatchBucket.Overdue, yesterday.Bucket);
        Assert.Equal(AdminFulfillmentHandoffWindowState.Closed, yesterday.WindowState);

        var tomorrow = AdminFulfillmentDispatchTimingAuthority.Classify(
            true, false, [Plan(new DateOnly(2026, 11, 11), 9, 0, 17, 0)], Now);
        Assert.Equal(AdminFulfillmentDispatchBucket.Upcoming, tomorrow.Bucket);
        Assert.Equal(AdminFulfillmentHandoffWindowState.Future, tomorrow.WindowState);
        Assert.False(AdminFulfillmentDispatchTimingAuthority.Matches(
            tomorrow, AdminFulfillmentDispatchTimingAuthority.FilterDue));
        Assert.True(AdminFulfillmentDispatchTimingAuthority.Matches(
            tomorrow, AdminFulfillmentDispatchTimingAuthority.FilterUpcoming));
    }

    /// <summary>
    /// "Today" comes from the accepted carrier zone. At 00:30 on 2026-11-11 in Asia/Tehran the server UTC date
    /// still reads 2026-11-10 21:00, so a handoff dated 11-10 is late and a handoff dated 11-11 is today's work.
    /// </summary>
    [Fact]
    public void Today_is_measured_in_the_accepted_carrier_zone_not_the_server_date()
    {
        var now = Tehran(0, 30, day: 11);
        Assert.Equal(new DateTimeOffset(2026, 11, 10, 21, 0, 0, TimeSpan.Zero), now);
        Assert.Equal(new DateOnly(2026, 11, 10), DateOnly.FromDateTime(now.UtcDateTime));

        var previousCarrierDay = AdminFulfillmentDispatchTimingAuthority.Classify(
            true, false, [Plan(new DateOnly(2026, 11, 10), 9, 0, 17, 0)], now);
        Assert.Equal("Asia/Tehran", previousCarrierDay.HandoffTimeZone);
        Assert.Equal(AdminFulfillmentDispatchBucket.Overdue, previousCarrierDay.Bucket);

        var carrierToday = AdminFulfillmentDispatchTimingAuthority.Classify(
            true, false, [Plan(new DateOnly(2026, 11, 11), 9, 0, 17, 0)], now);
        Assert.Equal(AdminFulfillmentDispatchBucket.Today, carrierToday.Bucket);
        Assert.Equal(AdminFulfillmentHandoffWindowState.TodayNotOpen, carrierToday.WindowState);

        // 09:00 Tehran is 05:30 UTC, so 04:00 UTC (carrier 07:30) is not open yet and 06:00 UTC (carrier 09:30)
        // is inside the window — the instant decides, never the string.
        var beforeOpen = AdminFulfillmentDispatchTimingAuthority.Classify(
            true, false, [Plan(new DateOnly(2026, 11, 11), 9, 0, 17, 0)],
            new DateTimeOffset(2026, 11, 11, 4, 0, 0, TimeSpan.Zero));
        Assert.Equal(AdminFulfillmentHandoffWindowState.TodayNotOpen, beforeOpen.WindowState);
        Assert.Equal(AdminFulfillmentDispatchBucket.Today, beforeOpen.Bucket);

        var afterOpen = AdminFulfillmentDispatchTimingAuthority.Classify(
            true, false, [Plan(new DateOnly(2026, 11, 11), 9, 0, 17, 0)],
            new DateTimeOffset(2026, 11, 11, 6, 0, 0, TimeSpan.Zero));
        Assert.Equal(AdminFulfillmentHandoffWindowState.Open, afterOpen.WindowState);
        Assert.Equal(new DateTimeOffset(2026, 11, 11, 5, 30, 0, TimeSpan.Zero), afterOpen.OpensAtUtc);
        Assert.Equal(new DateTimeOffset(2026, 11, 11, 13, 30, 0, TimeSpan.Zero), afterOpen.DeadlineUtc);
    }

    /// <summary>One instant expressed in the accepted carrier zone (Asia/Tehran, +03:30, no DST since 2022).</summary>
    private static DateTimeOffset Tehran(int hour, int minute, int day = 10) =>
        new DateTimeOffset(new DateTime(2026, 11, day, hour, minute, 0, DateTimeKind.Unspecified), TimeSpan.FromHours(3.5))
            .ToUniversalTime();

    [Fact]
    public void The_earliest_accepted_handoff_decides_while_extra_plans_are_counted_never_averaged()
    {
        var timing = AdminFulfillmentDispatchTimingAuthority.Classify(
            true,
            false,
            [
                Plan(new DateOnly(2026, 11, 12), 9, 0, 17, 0, GroupB),
                Plan(new DateOnly(2026, 11, 10), 9, 0, 17, 0, GroupA),
                Plan(new DateOnly(2026, 11, 14), 9, 0, 17, 0, Guid.Parse("01a1c260-0000-7000-9000-00000000000c")),
            ],
            Now);

        Assert.Equal(AdminFulfillmentDispatchBucket.Today, timing.Bucket);
        Assert.Equal(new DateOnly(2026, 11, 10), timing.HandoffDate);
        Assert.Equal(3, timing.DistinctPlanVariants);
        Assert.Equal("09:00–17:00", timing.HandoffTimeWindow);
    }

    /// <summary>A pending row with no accepted plan is «موعد ارسال مشخص نشده», never an invented date.</summary>
    [Fact]
    public void Pending_without_an_accepted_plan_is_unspecified()
    {
        var timing = AdminFulfillmentDispatchTimingAuthority.Classify(true, false, [], Now);

        Assert.Equal(AdminFulfillmentDispatchBucket.Unspecified, timing.Bucket);
        Assert.Equal(AdminFulfillmentHandoffWindowState.None, timing.WindowState);
        Assert.False(timing.HasPlan);
        Assert.Null(timing.DeadlineUtc);
        Assert.Null(timing.HandoffDate);
        Assert.True(AdminFulfillmentDispatchTimingAuthority.Matches(
            timing, AdminFulfillmentDispatchTimingAuthority.FilterUnspecified));
        Assert.False(AdminFulfillmentDispatchTimingAuthority.Matches(
            timing, AdminFulfillmentDispatchTimingAuthority.FilterDue));
    }

    [Fact]
    public void Nothing_pending_with_an_open_parcel_is_in_transit_and_never_due_work()
    {
        var inTransit = AdminFulfillmentDispatchTimingAuthority.Classify(
            pending: false, inTransit: true, [Plan(new DateOnly(2026, 11, 9), 9, 0, 17, 0)], Now);
        Assert.Equal(AdminFulfillmentDispatchBucket.InTransit, inTransit.Bucket);
        Assert.Equal(AdminFulfillmentHandoffWindowState.None, inTransit.WindowState);
        Assert.False(AdminFulfillmentDispatchTimingAuthority.Matches(
            inTransit, AdminFulfillmentDispatchTimingAuthority.FilterDue));
        Assert.False(AdminFulfillmentDispatchTimingAuthority.Matches(
            inTransit, AdminFulfillmentDispatchTimingAuthority.FilterOverdue));

        var finished = AdminFulfillmentDispatchTimingAuthority.Classify(false, false, [], Now);
        Assert.Equal(AdminFulfillmentDispatchBucket.NotQueued, finished.Bucket);
    }

    /// <summary>
    /// Buckets are mutually exclusive so the counts can never double count: an overdue row is not today, and
    /// «سررسیدشده» is the OVERLAPPING subset (today-open + overdue), not a fourth bucket.
    /// </summary>
    [Fact]
    public void Buckets_are_mutually_exclusive_while_the_due_filter_overlaps_them()
    {
        var cases = new (bool Pending, bool Transit, OrderAcceptedGroupDispatchPlanFact[] Plans, AdminFulfillmentDispatchBucket Expected)[]
        {
            (true, false, [Plan(new DateOnly(2026, 11, 10), 9, 0, 17, 0)], AdminFulfillmentDispatchBucket.Today),
            (true, false, [Plan(new DateOnly(2026, 11, 10), 9, 0, 10, 0)], AdminFulfillmentDispatchBucket.Overdue),
            (true, false, [Plan(new DateOnly(2026, 11, 13), 9, 0, 17, 0)], AdminFulfillmentDispatchBucket.Upcoming),
            (true, false, [], AdminFulfillmentDispatchBucket.Unspecified),
            (false, true, [], AdminFulfillmentDispatchBucket.InTransit),
            (false, false, [], AdminFulfillmentDispatchBucket.NotQueued),
        };

        var buckets = cases
            .Select(c => AdminFulfillmentDispatchTimingAuthority.Classify(c.Pending, c.Transit, c.Plans, Now))
            .ToList();
        Assert.Equal(cases.Select(c => c.Expected).ToList(), buckets.Select(t => t.Bucket).ToList());
        Assert.Equal(buckets.Count, buckets.Select(t => t.Bucket).Distinct().Count());

        var due = buckets.Count(t => AdminFulfillmentDispatchTimingAuthority.Matches(
            t, AdminFulfillmentDispatchTimingAuthority.FilterDue));
        var today = buckets.Count(t => t.Bucket == AdminFulfillmentDispatchBucket.Today);
        var overdue = buckets.Count(t => t.Bucket == AdminFulfillmentDispatchBucket.Overdue);
        Assert.Equal(today + overdue, due);
    }

    [Fact]
    public void Filters_are_owned_names_and_priority_orders_the_work()
    {
        Assert.True(AdminFulfillmentDispatchTimingAuthority.IsTimingFilter("dispatch_due"));
        Assert.True(AdminFulfillmentDispatchTimingAuthority.IsTimingFilter("DISPATCH_OVERDUE"));
        Assert.False(AdminFulfillmentDispatchTimingAuthority.IsTimingFilter("needs_action"));
        Assert.False(AdminFulfillmentDispatchTimingAuthority.IsTimingFilter(null));

        // An unknown/absent filter must never filter silently.
        var timing = AdminFulfillmentDispatchTimingAuthority.Classify(true, false, [], Now);
        Assert.True(AdminFulfillmentDispatchTimingAuthority.Matches(timing, "all"));
        Assert.True(AdminFulfillmentDispatchTimingAuthority.Matches(timing, null));

        var ranks = new[]
        {
            AdminFulfillmentDispatchBucket.NotQueued,
            AdminFulfillmentDispatchBucket.InTransit,
            AdminFulfillmentDispatchBucket.Unspecified,
            AdminFulfillmentDispatchBucket.Upcoming,
            AdminFulfillmentDispatchBucket.Today,
            AdminFulfillmentDispatchBucket.Overdue,
        }.Select(AdminFulfillmentDispatchTimingAuthority.PriorityRank).ToArray();
        Assert.Equal(ranks.OrderByDescending(x => x).ToArray(), ranks);
        Assert.Equal(ranks.Length, ranks.Distinct().Count());
    }

    /// <summary>
    /// The queue's own pending/in-transit predicates: a Created parcel is still dispatch work (it needs a
    /// tracking code and a handoff), a dispatched parcel is in transit, and delivered/cancelled are finished.
    /// </summary>
    [Fact]
    public void Pending_dispatch_work_and_open_transit_are_read_from_physical_facts()
    {
        var line = Guid.NewGuid();
        var ready = Customer(
            Tooba.Fulfillment.Domain.ValueObjects.FulfillmentStatus.ReadyToFulfill,
            [new Tooba.Fulfillment.Application.Fulfillments.Models.FulfillmentItemSnapshot(Guid.NewGuid(), line, 1m, 0m, null, 0m, 0m)],
            []);
        Assert.True(AdminFulfillmentQueueFilters.HasPendingDispatchWork(ready.Status, ready.Items, ready.Shipments));
        Assert.False(AdminFulfillmentQueueFilters.HasOpenTransitParcels(ready.Shipments));

        var created = Customer(
            Tooba.Fulfillment.Domain.ValueObjects.FulfillmentStatus.Packed,
            [new Tooba.Fulfillment.Application.Fulfillments.Models.FulfillmentItemSnapshot(Guid.NewGuid(), line, 1m, 0m, null, 1m, 1m)],
            [Parcel(Tooba.Fulfillment.Domain.ValueObjects.ShipmentStatus.Created, line)]);
        Assert.True(AdminFulfillmentQueueFilters.HasPendingDispatchWork(created.Status, created.Items, created.Shipments));
        Assert.False(AdminFulfillmentQueueFilters.HasOpenTransitParcels(created.Shipments));

        var dispatched = Customer(
            Tooba.Fulfillment.Domain.ValueObjects.FulfillmentStatus.Dispatched,
            [new Tooba.Fulfillment.Application.Fulfillments.Models.FulfillmentItemSnapshot(Guid.NewGuid(), line, 1m, 1m, null, 1m, 1m)],
            [Parcel(Tooba.Fulfillment.Domain.ValueObjects.ShipmentStatus.Dispatched, line)]);
        Assert.False(AdminFulfillmentQueueFilters.HasPendingDispatchWork(dispatched.Status, dispatched.Items, dispatched.Shipments));
        Assert.True(AdminFulfillmentQueueFilters.HasOpenTransitParcels(dispatched.Shipments));

        var delivered = Customer(
            Tooba.Fulfillment.Domain.ValueObjects.FulfillmentStatus.Delivered,
            [new Tooba.Fulfillment.Application.Fulfillments.Models.FulfillmentItemSnapshot(Guid.NewGuid(), line, 1m, 1m, null, 1m, 1m)],
            []);
        Assert.False(AdminFulfillmentQueueFilters.HasPendingDispatchWork(delivered.Status, delivered.Items, delivered.Shipments));
    }

    [Fact]
    public void Next_step_is_the_first_permitted_operation_of_the_same_authority()
    {
        Assert.Equal("mark_processing", AdminFulfillmentNextStep.Resolve(["mark_processing", "mark_packed"]).Code);
        Assert.Equal("assign_tracking", AdminFulfillmentNextStep.Resolve(["assign_tracking", "create_shipment"]).Code);
        Assert.Equal("dispatch_shipment", AdminFulfillmentNextStep.Resolve(["dispatch_shipment", "deliver_shipment"]).Code);
        Assert.Equal("deliver_shipment", AdminFulfillmentNextStep.Resolve(["record_non_delivery", "deliver_shipment"]).Code);
        Assert.Equal("ارسال به حامل", AdminFulfillmentNextStep.Resolve(["dispatch_shipment"]).LabelFa);
        Assert.Equal(string.Empty, AdminFulfillmentNextStep.Resolve([]).Code);
        // An unknown code never becomes a step.
        Assert.Equal(string.Empty, AdminFulfillmentNextStep.Resolve(["teleport"]).Code);
    }

    // ---------------------------------------------------------------- helpers

    private static OrderAcceptedGroupDispatchPlanFact Plan(
        DateOnly handoffDate,
        int openHour,
        int openMinute,
        int closeHour,
        int closeMinute,
        Guid? groupId = null) =>
        new(
            Guid.Parse("01a1b92d-e97e-7000-999a-143350d0edaf"),
            groupId ?? GroupA,
            Guid.Parse("01a1c260-0000-7000-9000-0000000000f1"),
            Guid.Parse("01a1c260-0000-7000-9000-0000000000f2"),
            "post",
            "پیشتاز",
            handoffDate,
            new TimeOnly(openHour, openMinute).Ticks,
            new TimeOnly(closeHour, closeMinute).Ticks,
            "Asia/Tehran",
            1,
            1,
            handoffDate,
            "09:00-17:00");

    private static Tooba.Fulfillment.Application.Fulfillments.Models.FulfillmentSnapshot Customer(
        Tooba.Fulfillment.Domain.ValueObjects.FulfillmentStatus status,
        IReadOnlyList<Tooba.Fulfillment.Application.Fulfillments.Models.FulfillmentItemSnapshot> items,
        IReadOnlyList<Tooba.Fulfillment.Application.Fulfillments.Models.ShipmentSnapshot> shipments) =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            status,
            "TB260 گیرنده",
            "09120000000",
            "تهران",
            "تهران",
            "نشانی آزمون تسک 260",
            "1234567890",
            "post",
            "پیشتاز",
            items,
            shipments,
            Now,
            Now);

    private static Tooba.Fulfillment.Application.Fulfillments.Models.ShipmentSnapshot Parcel(
        Tooba.Fulfillment.Domain.ValueObjects.ShipmentStatus status,
        Guid lineId) =>
        new(
            Guid.NewGuid(),
            status,
            "پست",
            "TRK-TB260",
            status == Tooba.Fulfillment.Domain.ValueObjects.ShipmentStatus.Created ? null : Now.AddHours(-1),
            null,
            [new Tooba.Fulfillment.Application.Fulfillments.Models.ShipmentLineSnapshot(lineId, 1m)],
            Now.AddHours(-2),
            "post",
            "پیشتاز");
}
