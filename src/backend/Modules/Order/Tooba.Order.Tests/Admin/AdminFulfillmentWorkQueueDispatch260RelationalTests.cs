using Microsoft.EntityFrameworkCore;
using Npgsql;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Grid;
using Tooba.Fulfillment.Application.Fulfillments.Ports;
using Tooba.Fulfillment.Application.WorkQueue.Models;
using Tooba.Fulfillment.Domain.Aggregates;
using Tooba.Fulfillment.Infrastructure.Persistence;
using Tooba.Fulfillment.Infrastructure.Queries;
using Tooba.Order.Domain.Checkout;
using Tooba.Order.Infrastructure.Integrations.DispatchPlan;
using Tooba.Order.Infrastructure.Integrations.GridEnrichment;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Party.Contracts.Ports;
using Tooba.Persistence;
using Xunit;
using Xunit.Abstractions;

namespace Tooba.Order.Tests.Admin;

/// <summary>
/// TB-260 case B on REAL storage: the dispatch-aware admin work queue is executed through its production engine
/// over the ONE canonical PostgreSQL database — the durable Order plan comes through the real Contracts bridge
/// (<see cref="OrderDispatchPlanFactsBridge"/>) and the Fulfillment physical facts come from the real DbContext.
/// </summary>
/// <remarks>
/// Gated by <c>TB260_CONNECTION</c>: with no target nothing runs and nothing is claimed. Every row is created with
/// the production domain paths only (accepted-evidence materializer for Order, paid-order unit for Fulfillment) and
/// is task-labelled (<c>TB260-…</c>). User orders, parcels, payments and service rows are never read or written.
/// </remarks>
public sealed class AdminFulfillmentWorkQueueDispatch260RelationalTests
{
    private const string RelationalEnvVar = "TB260_CONNECTION";

    private readonly ITestOutputHelper _output;

    public AdminFulfillmentWorkQueueDispatch260RelationalTests(ITestOutputHelper output) => _output = output;

    private static string? Connection =>
        Environment.GetEnvironmentVariable(RelationalEnvVar) is { Length: > 0 } raw ? raw : null;

    [Fact]
    public async Task Dispatch_buckets_counts_filters_and_paging_come_from_the_durable_plan_on_real_storage()
    {
        if (Connection is null)
        {
            return;
        }

        var target = new NpgsqlConnectionStringBuilder(Connection);
        Assert.Equal("127.0.0.1", target.Host);
        Assert.Equal(5432, target.Port);
        Assert.Equal("tooba_alpha", target.Database);

        var tag = $"TB260-{DateTime.UtcNow:HHmmss}";
        var now = DateTimeOffset.UtcNow;
        var carrierToday = Tooba.Fulfillment.Application.WorkQueue.Models.AdminFulfillmentDispatchTimingAuthority
            .LocalDate(now, "Asia/Tehran");

        var checkoutId = Guid.NewGuid();
        var sellerA = Guid.NewGuid();
        var sellerB = Guid.NewGuid();
        var sellerC = Guid.NewGuid();
        var sellerOrderA = Guid.NewGuid();
        var sellerOrderB = Guid.NewGuid();
        var sellerOrderC = Guid.NewGuid();
        var orderLineA = Guid.NewGuid();
        var orderLineB = Guid.NewGuid();
        var orderLineC = Guid.NewGuid();
        var cartLineA = Guid.NewGuid();
        var cartLineB = Guid.NewGuid();
        var cartLineC = Guid.NewGuid();
        var groupA = Guid.NewGuid();
        var groupB = Guid.NewGuid();
        var groupC = Guid.NewGuid();

        await using var orderDb = NewOrderDb(target);
        await using var fulfillmentDb = NewFulfillmentDb(target);

        // ---------- 1) durable Order commitment: one group WITH an accepted plan, two without ----------
        await InsertCheckoutAsync(orderDb, checkoutId, tag, now);
        await InsertSellerOrderAsync(orderDb, sellerOrderA, checkoutId, $"{tag}-A", sellerA);
        await InsertSellerOrderAsync(orderDb, sellerOrderB, checkoutId, $"{tag}-B", sellerB);
        await InsertSellerOrderAsync(orderDb, sellerOrderC, checkoutId, $"{tag}-C", sellerC);
        await InsertOrderLineAsync(orderDb, orderLineA, sellerOrderA, sellerA);
        await InsertOrderLineAsync(orderDb, orderLineB, sellerOrderB, sellerB);
        await InsertOrderLineAsync(orderDb, orderLineC, sellerOrderC, sellerC);

        var planned = ShippingGroupPlannedDispatch.Create(
            carrierToday,
            new TimeOnly(0, 0).Ticks,
            new TimeOnly(23, 59, 59).Ticks,
            "Asia/Tehran",
            transportWorkingDays: 1,
            policyVersion: 1,
            scheduleSnapshotId: Guid.NewGuid(),
            scheduleSourceRevision: 7);

        var evidenceWithPlan = Evidence(carrierToday, planned);
        var plans = new[]
        {
            Plan(groupA, sellerA, cartLineA, evidenceWithPlan, carrierToday),
            Plan(groupB, sellerB, cartLineB, acceptedEvidence: null, receivingDate: carrierToday),
            Plan(groupC, sellerC, cartLineC, acceptedEvidence: null, receivingDate: carrierToday),
        };
        var materialized = OrderShippingGroupMaterializer.Materialize(
            plans,
            [(orderLineA, sellerA), (orderLineB, sellerB), (orderLineC, sellerC)],
            new Dictionary<Guid, Guid> { [sellerA] = sellerOrderA, [sellerB] = sellerOrderB, [sellerC] = sellerOrderC },
            new Dictionary<Guid, Guid>
            {
                [cartLineA] = orderLineA,
                [cartLineB] = orderLineB,
                [cartLineC] = orderLineC,
            },
            checkoutId,
            now);
        orderDb.AddRange(materialized);
        await orderDb.SaveChangesAsync();

        // ---------- 2) physical Fulfillment facts: two pending rows and one finished row ----------
        AddUnit(fulfillmentDb, Guid.NewGuid(), checkoutId, sellerA, sellerOrderA, $"{tag} A", [(orderLineA, 2m)], now, groupA, orderLineA);
        AddUnit(fulfillmentDb, Guid.NewGuid(), checkoutId, sellerB, sellerOrderB, $"{tag} B", [(orderLineB, 1m)], now, groupB, orderLineB);
        var finishedId = AddUnit(
            fulfillmentDb, Guid.NewGuid(), checkoutId, sellerC, sellerOrderC, $"{tag} C", [(orderLineC, 1m)], now, groupC, orderLineC);
        await fulfillmentDb.SaveChangesAsync();

        var finished = await fulfillmentDb.Fulfillments.SingleAsync(x => x.FulfillmentId == finishedId);
        finished.AbortForOrderCancel(now);
        await fulfillmentDb.SaveChangesAsync();
        fulfillmentDb.ChangeTracker.Clear();

        var engine = new AdminFulfillmentWorkQueueQueryEngine(
            fulfillmentDb,
            new EmptyPartyLookup(),
            new OrderGridEnrichmentBridge(orderDb),
            new OrderDispatchPlanFactsBridge(orderDb),
            new FixedClock(now));

        var beforeCounts = await PhysicalCountsAsync(target);

        // ---------- 3) the whole tagged set: buckets, plan facts and the exact view locator ----------
        // TB-260: the DEFAULT queue is live work. A cancelled row is history and must not appear here.
        var all = await engine.QueryAsync(Request(page: 1, pageSize: 25, search: tag), CancellationToken.None);
        Assert.Equal(2, all.TotalCount);
        Assert.Equal(2, all.Items.Count);
        Assert.DoesNotContain(all.Items, x => x.SellerPartyId == sellerC);

        var rowA = all.Items.Single(x => x.SellerPartyId == sellerA);
        Assert.Equal(nameof(AdminFulfillmentDispatchBucket.Today), rowA.DispatchBucket);
        Assert.Equal(AdminFulfillmentDispatchBucketNames.WindowOpen, rowA.HandoffWindowState);
        Assert.Equal(carrierToday, rowA.PlannedHandoffDate);
        Assert.Equal("00:00–23:59", rowA.PlannedHandoffWindow);
        Assert.Equal("Asia/Tehran", rowA.PlannedHandoffTimeZone);
        Assert.Equal(1, rowA.HandoffPlanVariants);
        Assert.NotNull(rowA.DispatchDeadlineUtc);
        Assert.Equal(groupA, rowA.TargetShippingGroupId);
        Assert.Equal(carrierToday, rowA.CustomerReceivingDate);
        Assert.Equal("09:00-17:00", rowA.CustomerReceivingWindow);
        Assert.True(rowA.PendingDispatch);
        Assert.Equal("mark_processing", rowA.NextStepCode);
        Assert.Equal("شروع پردازش", rowA.NextStepLabelFa);
        Assert.Null(rowA.NextStepBlockerFa);
        Assert.Equal($"{tag}-A", rowA.OrderReference);

        var rowB = all.Items.Single(x => x.SellerPartyId == sellerB);
        Assert.Equal(nameof(AdminFulfillmentDispatchBucket.Unspecified), rowB.DispatchBucket);
        Assert.Equal(AdminFulfillmentDispatchBucketNames.WindowNone, rowB.HandoffWindowState);
        Assert.Null(rowB.PlannedHandoffDate);
        Assert.Null(rowB.DispatchDeadlineUtc);
        Assert.Null(rowB.TargetShippingGroupId);
        Assert.Equal(0, rowB.HandoffPlanVariants);
        Assert.True(rowB.PendingDispatch);
        Assert.Equal("mark_processing", rowB.NextStepCode);

        // Default order is dispatch priority: today first, then the plan-less pending row.
        Assert.Equal([sellerA, sellerB], all.Items.Select(x => x.SellerPartyId).ToArray());

        // ---------- 4) counts describe the WHOLE filtered set, never one page ----------
        var counts = await engine.CountAsync(Request(page: 1, pageSize: 1, search: tag), CancellationToken.None);
        Assert.Equal(2, counts.TotalActive);
        Assert.Equal(1, counts.Today);
        Assert.Equal(0, counts.Overdue);
        Assert.Equal(0, counts.Upcoming);
        Assert.Equal(1, counts.Unspecified);
        Assert.Equal(0, counts.InTransit);
        Assert.Equal(1, counts.Due);
        Assert.Equal(counts.Today + counts.Overdue + counts.Upcoming + counts.Unspecified, counts.TotalActive);

        // ---------- 5) each timing filter pages the exact subset it names ----------
        var due = await engine.QueryAsync(Request(1, 25, tag, AdminFulfillmentDispatchTimingAuthority.FilterDue), CancellationToken.None);
        Assert.Equal([sellerA], due.Items.Select(x => x.SellerPartyId).ToArray());

        var today = await engine.QueryAsync(Request(1, 25, tag, AdminFulfillmentDispatchTimingAuthority.FilterToday), CancellationToken.None);
        Assert.Equal([sellerA], today.Items.Select(x => x.SellerPartyId).ToArray());

        var overdue = await engine.QueryAsync(Request(1, 25, tag, AdminFulfillmentDispatchTimingAuthority.FilterOverdue), CancellationToken.None);
        Assert.Empty(overdue.Items);

        var upcoming = await engine.QueryAsync(Request(1, 25, tag, AdminFulfillmentDispatchTimingAuthority.FilterUpcoming), CancellationToken.None);
        Assert.Empty(upcoming.Items);

        var unspecified = await engine.QueryAsync(Request(1, 25, tag, AdminFulfillmentDispatchTimingAuthority.FilterUnspecified), CancellationToken.None);
        Assert.Equal([sellerB], unspecified.Items.Select(x => x.SellerPartyId).ToArray());

        // The unnamed `all` filter is not a timing filter and must not drop the finished row: history stays
        // reachable through an explicit filter, it is only absent from the default live queue.
        var unnamed = await engine.QueryAsync(Request(1, 25, tag, "all"), CancellationToken.None);
        Assert.Equal(3, unnamed.TotalCount);
        var rowC = unnamed.Items.Single(x => x.SellerPartyId == sellerC);
        Assert.Equal(nameof(AdminFulfillmentDispatchBucket.NotQueued), rowC.DispatchBucket);
        Assert.False(rowC.PendingDispatch);
        Assert.Equal([sellerA, sellerB], unnamed.Items.Take(2).Select(x => x.SellerPartyId).ToArray());

        // ---------- 6) server pagination is stable and complete ----------
        var page1 = await engine.QueryAsync(Request(1, 1, tag, "all"), CancellationToken.None);
        var page2 = await engine.QueryAsync(Request(2, 1, tag, "all"), CancellationToken.None);
        var page3 = await engine.QueryAsync(Request(3, 1, tag, "all"), CancellationToken.None);
        Assert.Equal(3, page1.TotalCount);
        Assert.Equal(1, page1.Items.Count);
        Assert.Equal(1, page2.Items.Count);
        Assert.Equal(1, page3.Items.Count);
        var paged = page1.Items.Concat(page2.Items).Concat(page3.Items).Select(x => x.FulfillmentId).ToArray();
        Assert.Equal(3, paged.Distinct().Count());
        Assert.Equal(unnamed.Items.Select(x => x.FulfillmentId).ToArray(), paged);

        // ---------- 7) the query itself wrote nothing ----------
        var afterCounts = await PhysicalCountsAsync(target);
        Assert.Equal(beforeCounts, afterCounts);

        // Still exactly ONE durable plan in this checkout: no read path manufactured a second one.
        var plannedGroups = await orderDb.OrderShippingGroups.AsNoTracking()
            .CountAsync(x => x.CheckoutId == checkoutId
                && x.AcceptedEvidence != null
                && x.AcceptedEvidence.PlannedDispatch != null);
        Assert.Equal(1, plannedGroups);
        Assert.Equal(
            carrierToday,
            (await orderDb.OrderShippingGroups.AsNoTracking()
                .SingleAsync(x => x.ShippingGroupId == groupA))
            .AcceptedEvidence!.PlannedDispatch!.HandoffDate);

        var evidenceDirectory = Environment.GetEnvironmentVariable("TB260_EVIDENCE_DIR");
        var line =
            $"{tag} planGroup={groupA} handoff={carrierToday:yyyy-MM-dd} window=00:00-23:59 zone=Asia/Tehran "
            + $"bucketA=Today/open rowB=Unspecified rowC=NotQueued counts=active2/today1/overdue0/upcoming0/unspecified1/transit0/due1 "
            + $"filters=dueA/todayA/overdue0/upcoming0/unspecifiedB pages=3x1 distinct=3 writes=0";
        _output.WriteLine(line);
        if (!string.IsNullOrWhiteSpace(evidenceDirectory))
        {
            Directory.CreateDirectory(evidenceDirectory);
            File.AppendAllText(Path.Combine(evidenceDirectory, "tb260-relational.txt"), line + Environment.NewLine);
        }
    }

    // ------------------------------------------------------------------ helpers

    private static GridQueryRequest Request(int page, int pageSize, string search, string? queueFilter = null) =>
        new(
            page,
            pageSize,
            search,
            [],
            queueFilter is null
                ? []
                : [new GridFilterRequest("queueFilter", "equals", queueFilter, null, null)]);

    private static OrderShippingGroupPlan Plan(
        Guid groupId,
        Guid sellerPartyId,
        Guid cartLineId,
        ShippingGroupAcceptedEvidence? acceptedEvidence,
        DateOnly receivingDate) =>
        new(
            groupId,
            sellerPartyId,
            "post",
            null,
            null,
            "پیشتاز",
            null,
            105_000m,
            "IRR",
            DateOnly.FromDateTime(DateTime.UtcNow),
            acceptedEvidence is null ? null : receivingDate,
            acceptedEvidence is null ? null : "09:00-17:00",
            1,
            [cartLineId],
            acceptedEvidence);

    private static ShippingGroupAcceptedEvidence Evidence(
        DateOnly receivingDate,
        ShippingGroupPlannedDispatch planned) =>
        ShippingGroupAcceptedEvidence.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            42L,
            ShippingGroupAcceptedEvidence.CarrierScheduleFormatVersion,
            1L,
            ShippingGroupOwner.Store("store-alpha"),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            100_000m,
            5_000m,
            "IRR",
            105_000m,
            0m,
            receivingDate,
            new TimeOnly(9, 0).Ticks,
            new TimeOnly(17, 0).Ticks,
            "Asia/Tehran",
            1,
            3,
            null,
            planned);

    private static Guid AddUnit(
        FulfillmentDbContext db,
        Guid fulfillmentId,
        Guid checkoutId,
        Guid sellerPartyId,
        Guid sellerOrderId,
        string recipient,
        IReadOnlyList<(Guid LineId, decimal Quantity)> lines,
        DateTimeOffset now,
        Guid shippingGroupId,
        Guid lineId)
    {
        var unit = FulfillmentUnit.CreateFromPaidOrder(
            fulfillmentId,
            Guid.NewGuid,
            sellerOrderId,
            checkoutId,
            sellerPartyId,
            Guid.NewGuid(),
            recipient,
            "09120000000",
            "تهران",
            "تهران",
            recipient,
            "1234567890",
            "post",
            "پیشتاز",
            lines.Select(x => (x.LineId, x.Quantity, (Guid?)null)).ToList(),
            now,
            [
                new FulfillmentShippingGroupPlan(
                    shippingGroupId,
                    sellerPartyId,
                    "post",
                    null,
                    null,
                    "پیشتاز",
                    null,
                    DateOnly.FromDateTime(now.UtcDateTime),
                    null,
                    null,
                    1,
                    [lineId]),
            ]);
        db.Fulfillments.Add(unit);
        db.Items.AddRange(unit.Items);
        db.ShippingGroups.AddRange(unit.ShippingGroups);
        return unit.FulfillmentId;
    }

    private static OrderDbContext NewOrderDb(NpgsqlConnectionStringBuilder target)
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, target.ConnectionString, OrderDbContext.Schema, typeof(OrderDbContext));
        return new OrderDbContext(options.Options);
    }

    private static FulfillmentDbContext NewFulfillmentDb(NpgsqlConnectionStringBuilder target)
    {
        var options = new DbContextOptionsBuilder<FulfillmentDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            options, target.ConnectionString, FulfillmentDbContext.Schema, typeof(FulfillmentDbContext));
        return new FulfillmentDbContext(options.Options);
    }

    private static async Task InsertCheckoutAsync(OrderDbContext db, Guid checkoutId, string tag, DateTimeOffset now) =>
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO "order".checkouts
                 (checkout_id, idempotency_key, cart_id, mode, buyer_party_id, placed_by_user_id, market, currency, channel, created_at, submitted_at)
             VALUES
                 ({checkoutId}, {tag}, {Guid.NewGuid()}, {"OnlinePurchase"}, {Guid.NewGuid()},
                  {Guid.NewGuid()}, {"IR"}, {"IRR"}, {"Marketplace"}, {now}, {now})
             """);

    private static async Task InsertSellerOrderAsync(
        OrderDbContext db, Guid sellerOrderId, Guid checkoutId, string orderNumber, Guid sellerPartyId) =>
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO "order".seller_orders
                 (seller_order_id, checkout_id, order_number, seller_party_id, status, subtotal_snapshot, tax_snapshot, discount_snapshot, grand_total_snapshot, currency)
             VALUES
                 ({sellerOrderId}, {checkoutId}, {orderNumber}, {sellerPartyId}, {"Paid"}, {0m}, {0m}, {0m}, {0m}, {"IRR"})
             """);

    private static async Task InsertOrderLineAsync(
        OrderDbContext db, Guid orderLineId, Guid sellerOrderId, Guid sellerPartyId) =>
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO "order".order_lines
                 (line_id, seller_order_id, offer_id, catalog_variant_id, seller_party_id, quantity, unit_price_snapshot, line_total_snapshot, currency, tax_exclusive, price_id)
             VALUES
                 ({orderLineId}, {sellerOrderId}, {Guid.NewGuid()}, {Guid.NewGuid()}, {sellerPartyId}, {1m}, {100_000m}, {100_000m}, {"IRR"}, {true}, {Guid.NewGuid()})
             """);

    /// <summary>Row counts of the tables this test could have touched — proof the read path wrote nothing.</summary>
    private static async Task<string> PhysicalCountsAsync(NpgsqlConnectionStringBuilder target)
    {
        await using var connection = new NpgsqlConnection(target.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            select
                (select count(*) from fulfillment.fulfillments) || '/'
              || (select count(*) from fulfillment.items) || '/'
              || (select count(*) from fulfillment.shipments) || '/'
              || (select count(*) from fulfillment.shipping_groups) || '/'
              || (select count(*) from "order".order_shipping_groups) || '/'
              || (select count(*) from "order".seller_orders)
            """;
        return (string)(await command.ExecuteScalarAsync())!;
    }

    private sealed class EmptyPartyLookup : IPartyLookup
    {
        public Task<PartyLookupResult?> FindByIdAsync(Guid partyId, CancellationToken cancellationToken) =>
            Task.FromResult<PartyLookupResult?>(null);

        public Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesAsync(
            IReadOnlyList<Guid> partyIds, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, string>>(new Dictionary<Guid, string>());

        public Task<IReadOnlyList<Guid>> SearchIdsByDisplayNameAsync(
            string term, int take, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);

        public Task<IReadOnlyList<Guid>> FilterIdsByDisplayNameAsync(
            string? op, string? value, IReadOnlyList<string>? values, int take, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);
    }
}






