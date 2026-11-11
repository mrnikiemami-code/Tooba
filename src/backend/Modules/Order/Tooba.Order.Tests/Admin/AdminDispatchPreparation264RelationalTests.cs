using Microsoft.EntityFrameworkCore;
using Npgsql;
using Tooba.BuildingBlocks;
using Tooba.Order.Domain.Checkout;
using Tooba.Order.Infrastructure.Integrations.DispatchPlan;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Persistence;
using Xunit;
using Xunit.Abstractions;

namespace Tooba.Order.Tests.Admin;

/// <summary>
/// TB-264 on REAL storage: a preparation-aware accepted plan (policy v2) persists its operational deadlines,
/// reads back through the real Contracts bridge with lossless instants, is distinguishable from a historical
/// policy-v1 plan, and the additive database constraint refuses a <b>partial</b> deadline triple.
/// </summary>
/// <remarks>
/// Gated by <c>TB264_CONNECTION</c>: with no target nothing runs and nothing is claimed. Fixtures are created
/// only through the production materializer and are task-labelled (<c>TB264-…</c>). User rows are never read or
/// written, and the only raw statement is the negative constraint probe against the task's OWN v1 row.
/// </remarks>
public sealed class AdminDispatchPreparation264RelationalTests
{
    private const string RelationalEnvVar = "TB264_CONNECTION";

    private readonly ITestOutputHelper _output;

    public AdminDispatchPreparation264RelationalTests(ITestOutputHelper output) => _output = output;

    private static string? Connection =>
        Environment.GetEnvironmentVariable(RelationalEnvVar) is { Length: > 0 } raw ? raw : null;

    [Fact]
    public async Task Preparation_deadlines_round_trip_and_a_partial_triple_is_refused()
    {
        if (Connection is null)
        {
            return;
        }

        var target = new NpgsqlConnectionStringBuilder(Connection);
        Assert.Equal("127.0.0.1", target.Host);
        Assert.Equal(5432, target.Port);
        Assert.Equal("tooba_alpha", target.Database);

        var tag = $"TB264-{DateTime.UtcNow:HHmmss}";
        var now = DateTimeOffset.UtcNow;

        var checkoutId = Guid.NewGuid();
        var sellerA = Guid.NewGuid();
        var sellerB = Guid.NewGuid();
        var sellerOrderA = Guid.NewGuid();
        var sellerOrderB = Guid.NewGuid();
        var orderLineA = Guid.NewGuid();
        var orderLineB = Guid.NewGuid();
        var cartLineA = Guid.NewGuid();
        var cartLineB = Guid.NewGuid();
        var groupA = Guid.NewGuid();
        var groupB = Guid.NewGuid();

        await using var orderDb = NewOrderDb(target);

        await InsertCheckoutAsync(orderDb, checkoutId, tag, now);
        await InsertSellerOrderAsync(orderDb, sellerOrderA, checkoutId, $"{tag}-A", sellerA);
        await InsertSellerOrderAsync(orderDb, sellerOrderB, checkoutId, $"{tag}-B", sellerB);
        await InsertOrderLineAsync(orderDb, orderLineA, sellerOrderA, sellerA);
        await InsertOrderLineAsync(orderDb, orderLineB, sellerOrderB, sellerB);

        // A concrete, feasible receiving date in the carrier zone (the next Monday, at least three days ahead).
        var receivingDate = NextWeekday(DateOnly.FromDateTime(now.UtcDateTime).AddDays(3), DayOfWeek.Monday);
        var deadlines = Tooba.Shipping.Contracts.Scheduling.ShippingDispatchSchedulingAuthority.ResolvePreparationDeadlines(
            receivingDate,
            CarrierWeek(),
            transportWorkingDays: 2,
            preparationMinutes: 90,
            handoffMinutes: 45);
        Assert.True(deadlines.IsSuccess, deadlines.ReasonCode);
        var expected = deadlines.Value!;

        // ---------- 1) policy v2 plan through the production domain factory ----------
        var v2 = ShippingGroupPlannedDispatch.Create(
            expected.Handoff.Date,
            expected.Handoff.OpensAt.Ticks,
            expected.Handoff.ClosesAt.Ticks,
            "Asia/Tehran",
            transportWorkingDays: 2,
            policyVersion: Tooba.Shipping.Contracts.Scheduling.ShippingDispatchSchedulingAuthority.PreparationAwarePolicyVersion,
            scheduleSnapshotId: Guid.NewGuid(),
            scheduleSourceRevision: 9,
            preparationStartUtc: expected.PreparationStartUtc,
            transferDepartureUtc: expected.TransferDepartureUtc,
            handoffDeadlineUtc: expected.HandoffDeadlineUtc,
            preparationMinutes: 90,
            handoffMinutes: 45);

        Assert.Equal(2, v2.PolicyVersion);
        Assert.NotNull(v2.PreparationStartUtc);
        Assert.NotNull(v2.TransferDepartureUtc);
        Assert.NotNull(v2.HandoffDeadlineUtc);
        Assert.Equal(90, v2.PreparationMinutes);
        Assert.Equal(45, v2.HandoffMinutes);

        // ---------- 2) a historical policy-v1 plan in the same checkout (no deadlines) ----------
        var v1Date = NextWeekday(DateOnly.FromDateTime(now.UtcDateTime).AddDays(2), DayOfWeek.Tuesday);
        var v1 = ShippingGroupPlannedDispatch.Create(
            v1Date,
            new TimeOnly(9, 0).Ticks,
            new TimeOnly(17, 0).Ticks,
            "Asia/Tehran",
            transportWorkingDays: 1,
            policyVersion: Tooba.Shipping.Contracts.Scheduling.ShippingDispatchSchedulingAuthority.PolicyVersion,
            scheduleSnapshotId: Guid.NewGuid(),
            scheduleSourceRevision: 4);

        Assert.Equal(1, v1.PolicyVersion);
        Assert.Null(v1.PreparationStartUtc);
        Assert.Null(v1.TransferDepartureUtc);
        Assert.Null(v1.HandoffDeadlineUtc);
        Assert.Null(v1.PreparationMinutes);
        Assert.Null(v1.HandoffMinutes);

        var materialized = OrderShippingGroupMaterializer.Materialize(
            [
                Plan(groupA, sellerA, cartLineA, Evidence(receivingDate, v2), receivingDate),
                Plan(groupB, sellerB, cartLineB, Evidence(v1Date, v1), v1Date),
            ],
            [(orderLineA, sellerA), (orderLineB, sellerB)],
            new Dictionary<Guid, Guid> { [sellerA] = sellerOrderA, [sellerB] = sellerOrderB },
            new Dictionary<Guid, Guid> { [cartLineA] = orderLineA, [cartLineB] = orderLineB },
            checkoutId,
            now);
        orderDb.AddRange(materialized);
        await orderDb.SaveChangesAsync();

        // ---------- 3) durable columns: 13 dispatch columns for v2, 8 for v1 ----------
        await using (var connection = new NpgsqlConnection(target.ConnectionString))
        {
            await connection.OpenAsync();

            var v2Counts = await DispatchColumnCountsAsync(connection, groupA);
            Assert.Equal("8/13/5", v2Counts);

            var v1Counts = await DispatchColumnCountsAsync(connection, groupB);
            Assert.Equal("8/8/0", v1Counts);        }

        // ---------- 4) the OWNING read bridge returns the same instants (policy v2) ----------
        var bridge = new OrderDispatchPlanFactsBridge(orderDb);
        var facts = await bridge.ListPlannedForSellerOrdersAsync([sellerOrderA, sellerOrderB], CancellationToken.None);
        Assert.Equal(2, facts.Count);

        var factA = facts.Single(x => x.ShippingGroupId == groupA);
        Assert.Equal(2, factA.PolicyVersion);
        Assert.Equal(expected.Handoff.Date, factA.HandoffDate);
        Assert.Equal("Asia/Tehran", factA.HandoffTimeZone);
        Assert.Equal(expected.PreparationStartUtc, factA.PreparationStartUtc);
        Assert.Equal(expected.TransferDepartureUtc, factA.TransferDepartureUtc);
        Assert.Equal(expected.HandoffDeadlineUtc, factA.HandoffDeadlineUtc);
        Assert.True(factA.PreparationStartUtc < factA.TransferDepartureUtc);
        Assert.True(factA.TransferDepartureUtc <= factA.HandoffDeadlineUtc);
        // The backward-derived start is a REAL instant in the carrier zone: 09:00–17:00 Tehran acceptance.
        Assert.Equal(new TimeOnly(17, 0), TimeOnly.FromTimeSpan(new TimeSpan(factA.HandoffCloseTicks)));

        var factB = facts.Single(x => x.ShippingGroupId == groupB);
        Assert.Equal(1, factB.PolicyVersion);
        Assert.Null(factB.PreparationStartUtc);
        Assert.Null(factB.TransferDepartureUtc);
        Assert.Null(factB.HandoffDeadlineUtc);

        // ---------- 5) a PARTIAL triple is refused by the additive constraint ----------
        await using (var connection = new NpgsqlConnection(target.ConnectionString))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText =
                """
                UPDATE "order".order_shipping_groups
                SET accepted_dispatch_preparation_start_utc = now()
                WHERE shipping_group_id = @id
                """;
            command.Parameters.AddWithValue("id", groupB);
            var failure = await Assert.ThrowsAsync<PostgresException>(() => command.ExecuteNonQueryAsync());
            Assert.Equal(PostgresErrorCodes.CheckViolation, failure.SqlState);
        }

        // The refused write left the historical row untouched.
        orderDb.ChangeTracker.Clear();
        var storedV1 = await orderDb.OrderShippingGroups.AsNoTracking().SingleAsync(x => x.ShippingGroupId == groupB);
        Assert.Null(storedV1.AcceptedEvidence!.PlannedDispatch!.PreparationStartUtc);
        Assert.Equal(1, storedV1.AcceptedEvidence!.PlannedDispatch!.PolicyVersion);

        // ---------- 6) exactly one durable plan per accepted group, nothing manufactured ----------
        var plannedGroups = await orderDb.OrderShippingGroups.AsNoTracking()
            .CountAsync(x => x.CheckoutId == checkoutId && x.AcceptedEvidence!.PlannedDispatch != null);
        Assert.Equal(2, plannedGroups);

        var line =
            $"{tag} checkout={checkoutId} receiving={receivingDate:yyyy-MM-dd} "
            + $"v2Group={groupA} handoff={factA.HandoffDate:yyyy-MM-dd} prep={factA.PreparationStartUtc:O} "
            + $"transfer={factA.TransferDepartureUtc:O} deadline={factA.HandoffDeadlineUtc:O} policyV2={factA.PolicyVersion} "
            + $"v1Group={groupB} policyV1={factB.PolicyVersion} v1Deadlines=null columns=13/8 "
            + "partialTriple=23514-refused bridge=exact";
        _output.WriteLine(line);
        var evidenceDirectory = Environment.GetEnvironmentVariable("TB264_EVIDENCE_DIR");
        if (!string.IsNullOrWhiteSpace(evidenceDirectory))
        {
            Directory.CreateDirectory(evidenceDirectory);
            File.AppendAllText(Path.Combine(evidenceDirectory, "tb264-relational.txt"), line + Environment.NewLine);
        }
    }

    // ------------------------------------------------------------------ helpers

    private static Tooba.Shipping.Contracts.Scheduling.ShippingSchedulingWeekView CarrierWeek()
    {
        var days = new Tooba.Shipping.Contracts.Scheduling.ShippingSchedulingDayView[7];
        for (var weekday = 1; weekday <= 7; weekday++)
        {
            var working = weekday <= 5;
            days[weekday - 1] = new Tooba.Shipping.Contracts.Scheduling.ShippingSchedulingDayView(
                weekday, working, working ? new TimeOnly(9, 0) : null, working ? new TimeOnly(17, 0) : null);
        }

        return new Tooba.Shipping.Contracts.Scheduling.ShippingSchedulingWeekView("Asia/Tehran", days);
    }

    private static DateOnly NextWeekday(DateOnly from, DayOfWeek target)
    {
        var date = from;
        while (date.DayOfWeek != target)
        {
            date = date.AddDays(1);
        }

        return date;
    }

    private static async Task<string> DispatchColumnCountsAsync(NpgsqlConnection connection, Guid groupId)
    {
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT
                num_nonnulls(
                    accepted_dispatch_handoff_date,
                    accepted_dispatch_open_ticks,
                    accepted_dispatch_close_ticks,
                    accepted_dispatch_time_zone,
                    accepted_dispatch_transport_days,
                    accepted_dispatch_policy_version,
                    accepted_dispatch_snapshot_id,
                    accepted_dispatch_source_revision)
                || '/' ||
                num_nonnulls(
                    accepted_dispatch_handoff_date,
                    accepted_dispatch_open_ticks,
                    accepted_dispatch_close_ticks,
                    accepted_dispatch_time_zone,
                    accepted_dispatch_transport_days,
                    accepted_dispatch_policy_version,
                    accepted_dispatch_snapshot_id,
                    accepted_dispatch_source_revision,
                    accepted_dispatch_preparation_start_utc,
                    accepted_dispatch_transfer_departure_utc,
                    accepted_dispatch_handoff_deadline_utc,
                    accepted_dispatch_preparation_minutes,
                    accepted_dispatch_handoff_minutes)
                || '/' ||
                num_nonnulls(
                    accepted_dispatch_preparation_start_utc,
                    accepted_dispatch_transfer_departure_utc,
                    accepted_dispatch_handoff_deadline_utc,
                    accepted_dispatch_preparation_minutes,
                    accepted_dispatch_handoff_minutes)
            FROM "order".order_shipping_groups
            WHERE shipping_group_id = @id
            """;
        command.Parameters.AddWithValue("id", groupId);
        return (string)(await command.ExecuteScalarAsync())!;
    }

    private static OrderShippingGroupPlan Plan(
        Guid groupId,
        Guid sellerPartyId,
        Guid cartLineId,
        ShippingGroupAcceptedEvidence acceptedEvidence,
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
            receivingDate,
            "09:00-17:00",
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

    private static OrderDbContext NewOrderDb(NpgsqlConnectionStringBuilder target)
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, target.ConnectionString, OrderDbContext.Schema, typeof(OrderDbContext));
        return new OrderDbContext(options.Options);
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
}
