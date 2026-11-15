using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Grid;
using Tooba.Fulfillment.Application.Fulfillments.Ports;
using Tooba.Fulfillment.Application.WorkQueue.Models;
using Tooba.Fulfillment.Application.WorkQueue.Queries;
using Tooba.Fulfillment.Domain.Aggregates;
using Tooba.Fulfillment.Domain.ValueObjects;
using Tooba.Fulfillment.Infrastructure.Persistence;
using Tooba.Fulfillment.Infrastructure.Queries;
using Tooba.Order.Contracts.Fulfillment;
using Tooba.Order.Contracts.Shipping;
using Tooba.Party.Contracts.Ports;
using Xunit;

namespace Tooba.Fulfillment.Tests.Behavior;

/// <summary>
/// TB-271-R1 — the four architect repairs of the employee queue, proven on the REAL owning pipeline
/// (policy → handler → query engine → projection) instead of on a hand-built row list.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item><b>A</b> «همه» is the five ACTIVE stages only; the archive is isolated, time-independent and its badge is
/// a constant function of the common constraints — so it cannot move when an active tab is selected, and a blocked
/// active unit keeps an honest stage instead of being dropped from SQL before classification.</item>
/// <item><b>B</b> one combined read (`QueryWithCountsAsync`) under ONE injected clock; the live handler never calls
/// the split page/count path.</item>
/// <item><b>C</b> an absent sort stays absent, so the documented job-priority order actually runs.</item>
/// <item><b>D</b> `jobStage`/`timeScope`/`queueFilter` are refused inside an advanced expression and a selection is
/// single-valued.</item>
/// <item><b>E</b> a missing/invalid accepted zone is explicitly undated, never guessed into UTC/Tehran/browser.</item>
/// </list>
/// </remarks>
public sealed class AdminFulfillmentWorkQueue271R1Tests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 15, 9, 0, 0, TimeSpan.Zero);
    private const string Tehran = "Asia/Tehran";

    private static readonly Guid SellerParty = Guid.Parse("01a1e271-0000-7000-8000-000000000001");
    private static readonly Guid SellerOrderA = Guid.Parse("01a1e271-0000-7000-8000-00000000000a");
    private static readonly Guid SellerOrderB = Guid.Parse("01a1e271-0000-7000-8000-00000000000b");
    private static readonly Guid SellerOrderC = Guid.Parse("01a1e271-0000-7000-8000-00000000000c");
    private static readonly Guid SellerOrderHistory = Guid.Parse("01a1e271-0000-7000-8000-00000000000d");

    private static readonly Guid GroupA = Guid.Parse("01a1e271-0000-7000-8000-000000000021");
    private static readonly Guid GroupB = Guid.Parse("01a1e271-0000-7000-8000-000000000022");
    private static readonly Guid GroupC = Guid.Parse("01a1e271-0000-7000-8000-000000000023");
    private static readonly Guid GroupHistory = Guid.Parse("01a1e271-0000-7000-8000-000000000024");

    // ---------------------------------------------------------------- C: sort

    [Fact]
    public void An_absent_sort_stays_absent_so_the_job_priority_order_can_run()
    {
        var normalized = AdminFulfillmentGridQueryPolicy.Instance.Normalize(
            new GridQueryRequest(1, 20, null, [], [], null));

        // Repair C: no synthetic `updatedAt desc`. Nothing was injected that the engine would have to obey.
        Assert.Empty(normalized.Sort);
    }

    [Fact]
    public void An_explicit_user_sort_is_preserved_verbatim_and_a_selection_token_is_never_sortable()
    {
        var explicitSort = AdminFulfillmentGridQueryPolicy.Instance.Normalize(
            new GridQueryRequest(
                1, 20, null,
                [new GridSortRequest("updatedAt", "asc"), new GridSortRequest("dispatchBucket", "desc")],
                [], null));
        Assert.Equal(
            [("updatedAt", "asc"), ("dispatchBucket", "desc")],
            explicitSort.Sort.Select(s => (s.Field, s.Direction)).ToArray());

        // A whitelisted FILTER field is not automatically a sortable column: the selection tokens are refused.
        var refused = AdminFulfillmentGridQueryPolicy.Instance.Normalize(
            new GridQueryRequest(
                1, 20, null,
                [new GridSortRequest("jobStage", "asc"), new GridSortRequest("timeScope", "asc")],
                [], null));
        Assert.Empty(refused.Sort);
    }

    // ---------------------------------------------------------------- D: advanced refusal

    [Fact]
    public void A_selection_token_inside_an_advanced_expression_is_refused_before_any_read()
    {
        foreach (var field in new[] { "jobStage", "timeScope", "queueFilter" })
        {
            var expression = new GridAdvancedFilterExpression(
                [new GridAdvancedFilterCondition("adv-0", field, "equals", "handoff", null, null)],
                []);

            var error = Assert.Throws<GridQueryValidationException>(() =>
                AdminFulfillmentGridQueryPolicy.Instance.Normalize(
                    new GridQueryRequest(1, 20, null, [], [], expression)));

            // The stable advanced-field error, not a silently ignored condition that would look applied.
            Assert.Equal("grid.advancedFilter.field.invalid", error.ErrorCode);
        }

        // An ordinary column condition is still accepted and normalized.
        var accepted = AdminFulfillmentGridQueryPolicy.Instance.Normalize(
            new GridQueryRequest(
                1, 20, null, [], [],
                new GridAdvancedFilterExpression(
                    [new GridAdvancedFilterCondition("adv-0", "cityName", "equals", "تهران", null, null)],
                    [])));
        Assert.Equal("cityName", Assert.Single(accepted.AdvancedFilter!.Conditions).Field);
    }

    [Fact]
    public void A_stage_or_scope_selection_must_be_exactly_one_value()
    {
        static GridQueryRequest Request(string field, string op, string? value, IReadOnlyList<string>? values) =>
            new(1, 20, null, [], [new GridFilterRequest(field, op, value, null, values)], null);

        // A negated/blank/multi-valued selection cannot mean "this stage", so it is refused instead of read as
        // its first token.
        foreach (var invalid in new[]
        {
            Request("jobStage", "notIn", null, ["handoff"]),
            Request("jobStage", "notEqual", "handoff", null),
            Request("jobStage", "equals", null, null),
            Request("jobStage", "in", null, ["handoff", "delivery"]),
            Request("timeScope", "in", null, ["today", "future"]),
        })
        {
            var error = Assert.Throws<GridQueryValidationException>(() =>
                AdminFulfillmentGridQueryPolicy.Instance.Normalize(invalid));
            Assert.Equal("grid.filter.operator.invalid", error.ErrorCode);
        }

        var accepted = AdminFulfillmentGridQueryPolicy.Instance.Normalize(
            Request("jobStage", "in", null, ["handoff"]));
        var filter = Assert.Single(accepted.Filters);
        Assert.Equal("jobStage", filter.Field);
        Assert.Equal(["handoff"], filter.Values!);
    }

    // ---------------------------------------------------------------- B: one read, one clock

    [Fact]
    public async Task The_live_handler_uses_the_one_combined_read_and_never_the_split_page_count_path()
    {
        var snapshot = new AdminFulfillmentWorkQueueSnapshot(
            [], 1, 20, 0, new AdminFulfillmentWorkQueueCounts(0, 0, 0, 0, 0, 0, 0));
        var query = new RecordingQueueQuery(snapshot);
        var handler = new QueryAdminFulfillmentWorkQueueHandler(query);

        var result = await handler.Handle(
            new QueryAdminFulfillmentWorkQueueQuery(new GridQueryRequest(1, 20, null, [], [], null)),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.IsFailure ? result.FirstError.Code : null);
        // The page and the badges are the SAME snapshot object, produced by one read.
        Assert.Same(snapshot.Counts, result.Value.DispatchCounts);
        Assert.Same(snapshot.Items, result.Value.Items);
        Assert.Equal(1, query.CombinedReads);
        Assert.Equal(0, query.PageReads);
        Assert.Equal(0, query.CountReads);
        // Repair C reached the engine: nothing was invented on the way.
        Assert.Empty(query.LastRequest!.Sort);
    }

    [Fact]
    public async Task A_refused_selection_returns_the_stable_field_error_and_reads_nothing()
    {
        var query = new RecordingQueueQuery(
            new AdminFulfillmentWorkQueueSnapshot([], 1, 20, 0, new AdminFulfillmentWorkQueueCounts(0, 0, 0, 0, 0, 0, 0)));
        var handler = new QueryAdminFulfillmentWorkQueueHandler(query);

        var result = await handler.Handle(
            new QueryAdminFulfillmentWorkQueueQuery(new GridQueryRequest(
                1, 20, null, [],
                [new GridFilterRequest("jobStage", "notIn", null, null, ["handoff"])],
                null)),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("grid.filter.operator.invalid", result.FirstError.Code);
        Assert.Equal(0, query.CombinedReads + query.PageReads + query.CountReads);
    }

    [Fact]
    public async Task One_combined_read_reads_the_server_clock_once_and_counts_the_same_classified_set_as_the_page()
    {
        await using var db = CreateDb();
        Seed(db);
        var clock = new CountingClock(Now);
        var engine = Engine(db, clock, [OverduePlan(), FuturePlan()]);

        var snapshot = await engine.QueryWithCountsAsync(
            Request("all", "all", page: 1, pageSize: 1), CancellationToken.None);

        // ONE clock read for this response: page and badges cannot disagree across a tick.
        Assert.Equal(1, clock.Reads);
        // Page is cut from the SAME sequence the counts describe.
        Assert.Equal(2, snapshot.TotalCount);
        Assert.Equal(2, snapshot.Counts.SelectedTotal);
        Assert.Single(snapshot.Items);
        // …and the badges describe the whole active set, not the one fetched row.
        Assert.Equal(2, snapshot.Counts.StageAll);
        Assert.Equal(2, snapshot.Counts.StageReadyToSend);
        Assert.Equal(1, snapshot.Counts.StageHistory);
        Assert.Equal(2, snapshot.Counts.TimeAll);
    }

    [Fact]
    public async Task Every_page_is_cut_from_the_same_total_beyond_page_size_and_never_repeats_a_row()
    {
        await using var db = CreateDb();
        Seed(db);
        var engine = Engine(db, new CountingClock(Now), [OverduePlan(), FuturePlan()]);

        var first = await engine.QueryWithCountsAsync(Request("all", "all", 1, 1), CancellationToken.None);
        var second = await engine.QueryWithCountsAsync(Request("all", "all", 2, 1), CancellationToken.None);
        var beyond = await engine.QueryWithCountsAsync(Request("all", "all", 3, 1), CancellationToken.None);

        // Repair A/B: two pages of ONE row are two different physical work units of a set that still totals 2, and
        // the badges describe that same set on every page instead of the fetched slice.
        Assert.Equal(2, first.TotalCount);
        Assert.Equal(2, second.TotalCount);
        Assert.Equal(first.Counts.SelectedTotal, second.Counts.SelectedTotal);
        Assert.Equal(first.Counts.StageAll, second.Counts.StageAll);
        Assert.NotEqual(first.Items[0].WorkUnitKey, second.Items[0].WorkUnitKey);
        // A page past the end is honestly empty; the total is not "the rows on this page".
        Assert.Empty(beyond.Items);
        Assert.Equal(2, beyond.Counts.SelectedTotal);
    }

    [Fact]
    public async Task The_default_order_is_overdue_then_future_with_no_client_sort()
    {
        await using var db = CreateDb();
        Seed(db);
        var engine = Engine(db, new CountingClock(Now), [OverduePlan(), FuturePlan()]);

        var snapshot = await engine.QueryWithCountsAsync(
            Request("all", "all", page: 1, pageSize: 25), CancellationToken.None);

        Assert.Equal(
            [
                AdminFulfillmentWorkUnitKinds.Key(AdminFulfillmentWorkUnitKinds.Group, GroupA),
                AdminFulfillmentWorkUnitKinds.Key(AdminFulfillmentWorkUnitKinds.Group, GroupB),
            ],
            snapshot.Items.Select(row => row.WorkUnitKey).ToArray());
        Assert.Equal(AdminFulfillmentJobTimeStates.PastDue, snapshot.Items[0].JobTimeState);
        Assert.Equal(AdminFulfillmentJobTimeStates.Future, snapshot.Items[1].JobTimeState);
    }

    // ---------------------------------------------------------------- A: active/archive parity

    [Fact]
    public async Task The_all_view_and_its_totals_never_carry_a_terminal_row()
    {
        await using var db = CreateDb();
        Seed(db);
        var engine = Engine(db, new CountingClock(Now), [OverduePlan(), FuturePlan()]);

        var snapshot = await engine.QueryWithCountsAsync(
            Request("all", "all", page: 1, pageSize: 100), CancellationToken.None);

        Assert.DoesNotContain(snapshot.Items, row => row.JobStage == AdminFulfillmentJobStageNames.History);
        Assert.Equal(2, snapshot.TotalCount);
        // The archive is counted, but never inside an active total.
        Assert.Equal(1, snapshot.Counts.StageHistory);
        Assert.Equal(snapshot.Counts.StageAll, snapshot.Counts.StagePreparation + snapshot.Counts.StageReadyToSend
            + snapshot.Counts.StageHandoff + snapshot.Counts.StageDelivery + snapshot.Counts.StageReturns);
    }

    [Fact]
    public async Task The_archive_badge_is_identical_under_every_active_tab()
    {
        await using var db = CreateDb();
        Seed(db);
        var engine = Engine(db, new CountingClock(Now), [OverduePlan(), FuturePlan()]);

        var all = await engine.QueryWithCountsAsync(Request("all", "all", 1, 25), CancellationToken.None);
        var preparation = await engine.QueryWithCountsAsync(Request("preparation", "all", 1, 25), CancellationToken.None);
        var handoff = await engine.QueryWithCountsAsync(Request("handoff", "all", 1, 25), CancellationToken.None);

        // Repair A: no stage-dependent SQL prefilter — the terminal unit is classified for every request, so its
        // badge (and the active total) cannot change with the selected active tab.
        Assert.Equal(all.Counts.StageHistory, preparation.Counts.StageHistory);
        Assert.Equal(all.Counts.StageHistory, handoff.Counts.StageHistory);
        Assert.Equal(all.Counts.StageAll, preparation.Counts.StageAll);
        Assert.Equal(all.Counts.StageAll, handoff.Counts.StageAll);
        // A tab with no row is honestly empty, never "all rows".
        Assert.Equal(0, preparation.Counts.SelectedTotal);
        Assert.Equal(0, handoff.Counts.SelectedTotal);
    }

    [Fact]
    public async Task The_archive_selection_is_time_independent_and_its_time_badges_are_honestly_zero()
    {
        await using var db = CreateDb();
        Seed(db);
        var engine = Engine(db, new CountingClock(Now), [OverduePlan(), FuturePlan()]);

        var history = await engine.QueryWithCountsAsync(Request("history", "today", 1, 25), CancellationToken.None);
        var echoedAll = await engine.QueryWithCountsAsync(Request("history", "all", 1, 25), CancellationToken.None);

        Assert.Equal(1, history.Counts.SelectedTotal);
        Assert.Single(history.Items);
        Assert.Equal(AdminFulfillmentJobStageNames.History, history.Items[0].JobStage);
        // The archive has no time dimension: borrowing the active time counts would be a lie.
        Assert.Equal(0, history.Counts.TimeToday);
        Assert.Equal(0, history.Counts.TimeFuture);
        Assert.Equal(0, history.Counts.TimeAll);
        // …and it is genuinely time-INDEPENDENT: an echoed scope cannot change its rows or its total.
        Assert.Equal(1, echoedAll.Counts.SelectedTotal);
        Assert.Equal(
            history.Items.Select(row => row.WorkUnitKey).ToArray(),
            echoedAll.Items.Select(row => row.WorkUnitKey).ToArray());
    }

    // ---------------------------------------------------------------- A: blocked active work

    [Fact]
    public void A_blocked_active_unit_keeps_its_honest_stage_because_terminality_is_a_positive_fact()
    {
        // No permitted capability at all — but the physical obligation is still open.
        Assert.Equal(
            AdminFulfillmentJobStageNames.ReadyToSend,
            AdminFulfillmentJobStageAuthority.Classify(
                AdminFulfillmentWorkUnitKinds.Parcel, "Created", [], hasOpenObligation: true,
                null, null, null, Tehran, Now).Stage);
        Assert.Equal(
            AdminFulfillmentJobStageNames.Preparation,
            AdminFulfillmentJobStageAuthority.Classify(
                AdminFulfillmentWorkUnitKinds.Group, "Packed", [], hasOpenObligation: true,
                null, null, null, Tehran, Now).Stage);
        Assert.Equal(
            AdminFulfillmentJobStageNames.Delivery,
            AdminFulfillmentJobStageAuthority.Classify(
                AdminFulfillmentWorkUnitKinds.Parcel, "InTransit", [], hasOpenObligation: true,
                null, null, null, Tehran, Now).Stage);

        // …while the SAME shapes without an obligation are the archive: the capability list proves nothing.
        Assert.Equal(
            AdminFulfillmentJobStageNames.History,
            AdminFulfillmentJobStageAuthority.Classify(
                AdminFulfillmentWorkUnitKinds.Parcel, "Created", [], hasOpenObligation: false,
                null, null, null, Tehran, Now).Stage);
    }

    [Fact]
    public async Task A_created_parcel_without_tracking_is_ready_for_send_and_never_archive()
    {
        await using var db = CreateDb();
        Seed(db);
        var engine = Engine(db, new CountingClock(Now), [OverduePlan(), FuturePlan()]);

        var snapshot = await engine.QueryWithCountsAsync(
            Request("ready_to_send", "all", 1, 25), CancellationToken.None);

        // Both group rows still owe a parcel; neither is terminal merely because no code was permitted.
        Assert.Equal(2, snapshot.TotalCount);
        Assert.All(snapshot.Items, row => Assert.Equal(AdminFulfillmentJobStageNames.ReadyToSend, row.JobStage));
        Assert.All(snapshot.Items, row => Assert.True(row.HasOpenObligation));
    }

    // ---------------------------------------------------------------- E: accepted zone

    [Fact]
    public async Task A_missing_accepted_zone_is_undated_with_an_explicit_reason_instead_of_a_guess()
    {
        await using var db = CreateDb();
        SeedWithZone(db);
        var engine = Engine(db, new CountingClock(Now), [NoZonePlan(string.Empty)]);

        var snapshot = await engine.QueryWithCountsAsync(
            Request("ready_to_send", "all", 1, 25), CancellationToken.None);

        var row = Assert.Single(snapshot.Items);
        Assert.Equal(AdminFulfillmentJobTimeMeanings.ZoneUnavailable, row.JobTimeMeaning);
        Assert.Equal("منطقهٔ زمانی مشخص نشده؛ زمان کار قابل تعیین نیست", row.JobTimeLabelFa);
        Assert.Equal(AdminFulfillmentJobTimeStates.Undated, row.JobTimeState);
        // The stored instant is kept for auditing — it is just not turned into an invented local day.
        Assert.NotNull(row.JobTimeUtc);
        // Undated CURRENT work stays in the default «امروز» and is never pretended to be future work.
        Assert.True(AdminFulfillmentJobStageAuthority.MatchesTimeScope(row.JobTimeState, "today"));
        Assert.False(AdminFulfillmentJobStageAuthority.MatchesTimeScope(row.JobTimeState, "future"));
    }

    [Fact]
    public async Task An_invalid_accepted_zone_is_named_as_invalid_and_never_replaced_by_utc_or_tehran()
    {
        await using var db = CreateDb();
        SeedWithZone(db);
        var engine = Engine(db, new CountingClock(Now), [NoZonePlan("Mars/Olympus")]);

        var snapshot = await engine.QueryWithCountsAsync(
            Request("ready_to_send", "all", 1, 25), CancellationToken.None);

        var row = Assert.Single(snapshot.Items);
        Assert.Equal(AdminFulfillmentJobTimeMeanings.ZoneUnavailable, row.JobTimeMeaning);
        Assert.Equal("منطقهٔ زمانی نامعتبر؛ زمان کار قابل تعیین نیست", row.JobTimeLabelFa);
        Assert.Equal(AdminFulfillmentJobTimeStates.Undated, row.JobTimeState);
    }

    [Fact]
    public void A_valid_accepted_zone_still_classifies_the_day_in_that_zone()
    {
        // now = 15 Nov 23:30 Tehran / 20:00 UTC; the deadline is 16 Nov 01:00 Tehran but still 15 Nov 21:30 UTC.
        var nowUtc = new DateTimeOffset(2026, 11, 15, 20, 0, 0, TimeSpan.Zero);
        var deadline = new DateTimeOffset(2026, 11, 15, 21, 30, 0, TimeSpan.Zero);
        var tehran = AdminFulfillmentJobStageAuthority.Classify(
            AdminFulfillmentWorkUnitKinds.Parcel, "Created", ["dispatch_shipment"], true,
            null, null, deadline, Tehran, nowUtc);
        var utc = AdminFulfillmentJobStageAuthority.Classify(
            AdminFulfillmentWorkUnitKinds.Parcel, "Created", ["dispatch_shipment"], true,
            null, null, deadline, "UTC", nowUtc);

        // A usable zone is reported truthfully with its own meaning and instant …
        Assert.Equal(AdminFulfillmentJobTimeMeanings.HandoffClose, tehran.JobTimeMeaning);
        Assert.Equal(deadline, tehran.JobTimeUtc);
        Assert.Equal(deadline, utc.JobTimeUtc);
        // … and the local DAY is answered in the accepted zone: tomorrow in Tehran, still today in UTC.
        Assert.Equal(AdminFulfillmentJobTimeStates.Future, tehran.JobTimeState);
        Assert.Equal(AdminFulfillmentJobTimeStates.DueToday, utc.JobTimeState);
    }

    // ---------------------------------------------------------------- fixtures

    private static GridQueryRequest Request(string stage, string scope, int page, int pageSize) =>
        new(
            page,
            pageSize,
            null,
            [],
            [
                new GridFilterRequest("jobStage", "equals", stage, null, null),
                new GridFilterRequest("timeScope", "equals", scope, null, null),
            ],
            null);

    private static FulfillmentDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<FulfillmentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(w => w.Ignore(
                Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new FulfillmentDbContext(options);
    }

    /// <summary>
    /// Two active packed groups (an overdue plan and a future plan) plus one fully delivered unit — the archive.
    /// Every physical fact is produced by the owning aggregate, never assembled by the test.
    /// </summary>
    private static void Seed(FulfillmentDbContext db)
    {
        Add(db, PackedUnit(Guid.NewGuid(), SellerOrderA, GroupA, Now));
        Add(db, PackedUnit(Guid.NewGuid(), SellerOrderB, GroupB, Now));
        Add(db, DeliveredUnit(Guid.NewGuid(), SellerOrderHistory, GroupHistory, Now));
        db.SaveChanges();
    }

    /// <summary>A packed unit with one Created parcel, so the parcel row is the active work unit.</summary>
    private static void SeedWithZone(FulfillmentDbContext db)
    {
        var unit = PackedUnit(Guid.NewGuid(), SellerOrderC, GroupC, Now);
        unit.CreateShipment(
            Guid.NewGuid(), () => Guid.NewGuid(), "پست", [(LineC, 1m)], Now,
            "post", "پست", shippingGroupId: GroupC);
        Add(db, unit);
        db.SaveChanges();
    }

    private static readonly Guid LineA = Guid.Parse("01a1e271-0000-7000-8000-000000000031");
    private static readonly Guid LineB = Guid.Parse("01a1e271-0000-7000-8000-000000000032");
    private static readonly Guid LineHistory = Guid.Parse("01a1e271-0000-7000-8000-000000000033");
    private static readonly Guid LineC = Guid.Parse("01a1e271-0000-7000-8000-000000000034");

    private static FulfillmentUnit PackedUnit(Guid fulfillmentId, Guid sellerOrderId, Guid groupId, DateTimeOffset now)
    {
        var lineId = groupId == GroupA ? LineA : groupId == GroupB ? LineB : LineC;
        var unit = FulfillmentUnit.CreateFromPaidOrder(
            fulfillmentId, () => Guid.NewGuid(), sellerOrderId, Guid.NewGuid(), SellerParty, Guid.NewGuid(),
            "گیرنده", "09120000000", "تهران", "تهران", "خیابان", "1234567890", "post", "پست",
            [(lineId, 1m, null)],
            now,
            [GroupPlan(groupId, lineId, DateOnly.FromDateTime(now.UtcDateTime))]);
        unit.MarkProcessing(now);
        unit.MarkPacked(now);
        return unit;
    }

    private static FulfillmentUnit DeliveredUnit(Guid fulfillmentId, Guid sellerOrderId, Guid groupId, DateTimeOffset now)
    {
        var unit = PackedUnitFor(fulfillmentId, sellerOrderId, groupId, LineHistory, now);
        var shipmentId = Guid.NewGuid();
        unit.CreateShipment(
            shipmentId, () => Guid.NewGuid(), "پست", [(LineHistory, 1m)], now,
            "post", "پست", shippingGroupId: groupId);
        unit.AssignTracking(shipmentId, "TRK-271-R1", now);
        unit.ApplyShipmentDispatched(shipmentId, now);
        unit.ApplyShipmentDelivered(shipmentId, now);
        return unit;
    }

    private static FulfillmentUnit PackedUnitFor(
        Guid fulfillmentId, Guid sellerOrderId, Guid groupId, Guid lineId, DateTimeOffset now)
    {
        var unit = FulfillmentUnit.CreateFromPaidOrder(
            fulfillmentId, () => Guid.NewGuid(), sellerOrderId, Guid.NewGuid(), SellerParty, Guid.NewGuid(),
            "گیرنده", "09120000000", "تهران", "تهران", "خیابان", "1234567890", "post", "پست",
            [(lineId, 1m, null)],
            now,
            [GroupPlan(groupId, lineId, DateOnly.FromDateTime(now.UtcDateTime))]);
        unit.MarkProcessing(now);
        unit.MarkPacked(now);
        return unit;
    }

    private static FulfillmentShippingGroupPlan GroupPlan(Guid groupId, Guid lineId, DateOnly minimum) =>
        new(groupId, SellerParty, "post", null, null, "پست", null, minimum, null, null, 0, [lineId]);

    private static void Add(FulfillmentDbContext db, FulfillmentUnit unit)
    {
        db.Fulfillments.Add(unit);
        db.Items.AddRange(unit.Items);
        db.ShippingGroups.AddRange(unit.ShippingGroups);
        db.Shipments.AddRange(unit.Shipments);
        db.ShipmentItems.AddRange(unit.Shipments.SelectMany(x => x.Items));
    }

    private static OrderAcceptedGroupDispatchPlanFact OverduePlan() =>
        Plan(SellerOrderA, GroupA, new DateOnly(2026, 11, 14), Tehran,
            preparationStartUtc: new DateTimeOffset(2026, 11, 14, 3, 0, 0, TimeSpan.Zero),
            transferDepartureUtc: new DateTimeOffset(2026, 11, 14, 8, 0, 0, TimeSpan.Zero));

    private static OrderAcceptedGroupDispatchPlanFact FuturePlan() =>
        Plan(SellerOrderB, GroupB, new DateOnly(2026, 11, 18), Tehran,
            preparationStartUtc: new DateTimeOffset(2026, 11, 17, 6, 0, 0, TimeSpan.Zero),
            transferDepartureUtc: new DateTimeOffset(2026, 11, 18, 4, 0, 0, TimeSpan.Zero));

    /// <summary>The same accepted plan shape, but its accepted zone is the caller's (missing/invalid) value.</summary>
    private static OrderAcceptedGroupDispatchPlanFact NoZonePlan(string zone) =>
        Plan(SellerOrderC, GroupC, new DateOnly(2026, 11, 18), zone,
            preparationStartUtc: null,
            transferDepartureUtc: new DateTimeOffset(2026, 11, 18, 4, 0, 0, TimeSpan.Zero));

    private static OrderAcceptedGroupDispatchPlanFact Plan(
        Guid sellerOrderId,
        Guid groupId,
        DateOnly handoffDate,
        string zone,
        DateTimeOffset? preparationStartUtc,
        DateTimeOffset? transferDepartureUtc) =>
        new(
            sellerOrderId,
            groupId,
            Guid.NewGuid(),
            SellerParty,
            "post",
            "پست",
            handoffDate,
            TimeSpan.FromHours(8).Ticks,
            TimeSpan.FromHours(20).Ticks,
            zone,
            2,
            3,
            new DateOnly(2026, 11, 20),
            "09:00-10:00",
            preparationStartUtc,
            transferDepartureUtc,
            transferDepartureUtc);

    private static AdminFulfillmentWorkQueueQueryEngine Engine(
        FulfillmentDbContext db,
        IClock clock,
        IReadOnlyList<OrderAcceptedGroupDispatchPlanFact> plans) =>
        new(db, new EmptyPartyLookup(), new EmptyOrders(), new Plans(plans), new NoOwnPlans(), clock);

    private sealed class CountingClock : IClock
    {
        private readonly DateTimeOffset _now;
        public CountingClock(DateTimeOffset now) => _now = now;
        public int Reads { get; private set; }
        public DateTimeOffset UtcNow
        {
            get
            {
                Reads++;
                return _now;
            }
        }
    }

    private sealed class EmptyPartyLookup : IPartyLookup
    {
        public Task<PartyLookupResult?> FindByIdAsync(Guid partyId, CancellationToken cancellationToken) =>
            Task.FromResult<PartyLookupResult?>(null);

        public Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesAsync(
            IReadOnlyList<Guid> partyIds, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, string>>(
                partyIds.Distinct().ToDictionary(id => id, _ => "فروشنده"));

        public Task<IReadOnlyList<Guid>> SearchIdsByDisplayNameAsync(
            string term, int take, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);

        public Task<IReadOnlyList<Guid>> FilterIdsByDisplayNameAsync(
            string? op, string? value, IReadOnlyList<string>? values, int take, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);
    }

    private sealed class EmptyOrders : IOrderGridEnrichmentReader
    {
        public Task<IReadOnlyDictionary<Guid, string>> GetOrderNumbersAsync(
            IReadOnlyList<Guid> sellerOrderIds, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, string>>(
                sellerOrderIds.Distinct().ToDictionary(id => id, _ => "TB-271-R1"));

        public Task<IReadOnlyList<Guid>> SearchSellerOrderIdsByOrderNumberAsync(
            string term, int take, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);

        public Task<IReadOnlyList<Guid>> FilterSellerOrderIdsByOrderNumberAsync(
            string? op, string? value, IReadOnlyList<string>? values, int take, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);

        public Task<IReadOnlyDictionary<Guid, string>> GetRecipientNamesAsync(
            IReadOnlyList<Guid> checkoutIds, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, string>>(
                checkoutIds.Distinct().ToDictionary(id => id, _ => "گیرنده"));

        public Task<IReadOnlyList<Guid>> SearchCheckoutIdsByRecipientAsync(
            string term, int take, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);

        public Task<IReadOnlyList<Guid>> FilterCheckoutIdsByRecipientAsync(
            string? op, string? value, IReadOnlyList<string>? values, int take, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);

        public Task<IReadOnlyDictionary<Guid, OrderGridLineSnapshot>> GetLinesAsync(
            IReadOnlyList<Guid> orderLineIds, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, OrderGridLineSnapshot>>(
                new Dictionary<Guid, OrderGridLineSnapshot>());
    }

    private sealed class Plans : IOrderDispatchPlanFactsReader
    {
        private readonly IReadOnlyList<OrderAcceptedGroupDispatchPlanFact> _plans;
        public Plans(IReadOnlyList<OrderAcceptedGroupDispatchPlanFact> plans) => _plans = plans;

        public Task<IReadOnlyList<OrderAcceptedGroupDispatchPlanFact>> ListPlannedAsync(
            CancellationToken cancellationToken) => Task.FromResult(_plans);

        public Task<IReadOnlyList<OrderAcceptedGroupDispatchPlanFact>> ListPlannedForSellerOrdersAsync(
            IReadOnlyList<Guid> sellerOrderIds, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<OrderAcceptedGroupDispatchPlanFact>>(
                _plans.Where(plan => sellerOrderIds.Contains(plan.SellerOrderId)).ToList());
    }

    private sealed class NoOwnPlans : IOrderShipmentDispatchPlanReader
    {
        public Task<IReadOnlyList<OrderShipmentDispatchPlanFact>> ListForShipmentsAsync(
            IReadOnlyList<Guid> shipmentIds, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<OrderShipmentDispatchPlanFact>>([]);

        public Task<OrderShipmentDispatchPlanFact?> GetForShipmentAsync(
            Guid shipmentId, CancellationToken cancellationToken) =>
            Task.FromResult<OrderShipmentDispatchPlanFact?>(null);
    }

    private sealed class RecordingQueueQuery : IAdminFulfillmentWorkQueueQuery
    {
        private readonly AdminFulfillmentWorkQueueSnapshot _snapshot;
        public RecordingQueueQuery(AdminFulfillmentWorkQueueSnapshot snapshot) => _snapshot = snapshot;

        public int CombinedReads { get; private set; }
        public int PageReads { get; private set; }
        public int CountReads { get; private set; }
        public GridQueryRequest? LastRequest { get; private set; }

        public Task<AdminFulfillmentWorkQueueSnapshot> QueryWithCountsAsync(
            GridQueryRequest request, CancellationToken cancellationToken)
        {
            CombinedReads++;
            LastRequest = request;
            return Task.FromResult(_snapshot);
        }

        public Task<GridPageResponse<AdminFulfillmentWorkQueueRow>> QueryAsync(
            GridQueryRequest request, CancellationToken cancellationToken)
        {
            PageReads++;
            LastRequest = request;
            return Task.FromResult(new GridPageResponse<AdminFulfillmentWorkQueueRow>(
                _snapshot.Items, _snapshot.Page, _snapshot.PageSize, _snapshot.TotalCount));
        }

        public Task<AdminFulfillmentWorkQueueCounts> CountAsync(
            GridQueryRequest request, CancellationToken cancellationToken)
        {
            CountReads++;
            LastRequest = request;
            return Task.FromResult(_snapshot.Counts);
        }
    }
}
