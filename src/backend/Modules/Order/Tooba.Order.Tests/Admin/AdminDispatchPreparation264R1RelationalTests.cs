using Microsoft.EntityFrameworkCore;
using Npgsql;
using Tooba.BuildingBlocks;
using Tooba.Order.Domain.Checkout;
using Tooba.Order.Infrastructure.Integrations.DispatchPlan;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Persistence;
using Tooba.Shipping.Contracts.Scheduling;
using Xunit;
using Xunit.Abstractions;

namespace Tooba.Order.Tests.Admin;

/// <summary>
/// TB-264-R1 (gap A) on REAL storage: the explicit ZERO allowances that the authority and the domain accept
/// are also accepted by the durable constraint, the non-decreasing triple round-trips losslessly through the
/// owning read bridge, and the constraint still refuses a decreasing triple or a zero allowance that does not
/// collapse its own boundary.
/// </summary>
/// <remarks>
/// Gated by <c>TB264_CONNECTION</c>: with no target nothing runs and nothing is claimed. Fixtures are created
/// only through the production domain factory + materializer and are task-labelled (<c>TB264R1-…</c>). Every
/// negative probe updates only this task's OWN rows.
/// </remarks>
public sealed class AdminDispatchPreparation264R1RelationalTests
{
    private const string RelationalEnvVar = "TB264_CONNECTION";
    private const string Zone = "UTC";

    private static readonly DateOnly Day = new(2026, 11, 12); // Thursday 2026-11-12.

    private readonly ITestOutputHelper _output;

    public AdminDispatchPreparation264R1RelationalTests(ITestOutputHelper output) => _output = output;

    private static string? Connection =>
        Environment.GetEnvironmentVariable(RelationalEnvVar) is { Length: > 0 } raw ? raw : null;

    [Fact]
    public async Task Zero_allowances_are_durable_and_a_decreasing_triple_is_still_refused()
    {
        if (Connection is null)
        {
            return;
        }

        var target = new NpgsqlConnectionStringBuilder(Connection);
        Assert.Equal("127.0.0.1", target.Host);
        Assert.Equal(5432, target.Port);
        Assert.Equal("tooba_alpha", target.Database);

        var tag = $"TB264R1-{DateTime.UtcNow:HHmmss}";
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

        // ---------- 1) the authority produces both explicit-zero shapes ----------
        var zeroPair = ShippingDispatchSchedulingAuthority.ResolvePreparationDeadlines(
            new ShippingReceivingCommitment(Day, new TimeOnly(10, 0), new TimeOnly(11, 0), Zone),
            CarrierWeek(),
            transportWorkingDays: 0,
            preparationMinutes: 0,
            handoffMinutes: 0);
        Assert.True(zeroPair.IsSuccess, zeroPair.ReasonCode);
        Assert.Equal(zeroPair.Value!.HandoffDeadlineUtc, zeroPair.Value.TransferDepartureUtc);
        Assert.Equal(zeroPair.Value.TransferDepartureUtc, zeroPair.Value.PreparationStartUtc);
        Assert.Equal(new DateTimeOffset(2026, 11, 12, 10, 0, 0, TimeSpan.Zero), zeroPair.Value.HandoffDeadlineUtc);

        var zeroPrep = ShippingDispatchSchedulingAuthority.ResolvePreparationDeadlines(
            new ShippingReceivingCommitment(Day, new TimeOnly(10, 0), new TimeOnly(11, 0), Zone),
            CarrierWeek(),
            transportWorkingDays: 0,
            preparationMinutes: 0,
            handoffMinutes: 30);
        Assert.True(zeroPrep.IsSuccess, zeroPrep.ReasonCode);
        Assert.Equal(zeroPrep.Value!.TransferDepartureUtc, zeroPrep.Value.PreparationStartUtc);
        Assert.Equal(zeroPrep.Value.HandoffDeadlineUtc.AddMinutes(-30), zeroPrep.Value.TransferDepartureUtc);

        // ---------- 2) both persist through the production domain factory + materializer ----------
        var planA = ShippingGroupPlannedDispatch.Create(
            zeroPair.Value.Handoff.Date,
            zeroPair.Value.Handoff.OpensAt.Ticks,
            zeroPair.Value.Handoff.ClosesAt.Ticks,
            Zone,
            transportWorkingDays: 0,
            policyVersion: ShippingDispatchSchedulingAuthority.WindowAwarePolicyVersion,
            scheduleSnapshotId: Guid.NewGuid(),
            scheduleSourceRevision: 11,
            preparationStartUtc: zeroPair.Value.PreparationStartUtc,
            transferDepartureUtc: zeroPair.Value.TransferDepartureUtc,
            handoffDeadlineUtc: zeroPair.Value.HandoffDeadlineUtc,
            preparationMinutes: 0,
            handoffMinutes: 0);

        var planB = ShippingGroupPlannedDispatch.Create(
            zeroPrep.Value.Handoff.Date,
            zeroPrep.Value.Handoff.OpensAt.Ticks,
            zeroPrep.Value.Handoff.ClosesAt.Ticks,
            Zone,
            transportWorkingDays: 0,
            policyVersion: ShippingDispatchSchedulingAuthority.WindowAwarePolicyVersion,
            scheduleSnapshotId: Guid.NewGuid(),
            scheduleSourceRevision: 12,
            preparationStartUtc: zeroPrep.Value.PreparationStartUtc,
            transferDepartureUtc: zeroPrep.Value.TransferDepartureUtc,
            handoffDeadlineUtc: zeroPrep.Value.HandoffDeadlineUtc,
            preparationMinutes: 0,
            handoffMinutes: 30);

        await using var orderDb = NewOrderDb(target);
        await InsertCheckoutAsync(orderDb, checkoutId, tag, now);
        await InsertSellerOrderAsync(orderDb, sellerOrderA, checkoutId, $"{tag}-A", sellerA);
        await InsertSellerOrderAsync(orderDb, sellerOrderB, checkoutId, $"{tag}-B", sellerB);
        await InsertOrderLineAsync(orderDb, orderLineA, sellerOrderA, sellerA);
        await InsertOrderLineAsync(orderDb, orderLineB, sellerOrderB, sellerB);

        var materialized = OrderShippingGroupMaterializer.Materialize(
            [
                Plan(groupA, sellerA, cartLineA, Evidence(Day, planA), Day),
                Plan(groupB, sellerB, cartLineB, Evidence(Day, planB), Day),
            ],
            [(orderLineA, sellerA), (orderLineB, sellerB)],
            new Dictionary<Guid, Guid> { [sellerA] = sellerOrderA, [sellerB] = sellerOrderB },
            new Dictionary<Guid, Guid> { [cartLineA] = orderLineA, [cartLineB] = orderLineB },
            checkoutId,
            now);
        orderDb.AddRange(materialized);
        await orderDb.SaveChangesAsync();

        // ---------- 3) the OWNING read bridge returns the exact instants (equality preserved) ----------
        var bridge = new OrderDispatchPlanFactsBridge(orderDb);
        var facts = await bridge.ListPlannedForSellerOrdersAsync(
            [sellerOrderA, sellerOrderB],
            CancellationToken.None);
        Assert.Equal(2, facts.Count);

        var factA = facts.Single(x => x.ShippingGroupId == groupA);
        Assert.Equal(ShippingDispatchSchedulingAuthority.WindowAwarePolicyVersion, factA.PolicyVersion);
        Assert.Equal(zeroPair.Value.PreparationStartUtc, factA.PreparationStartUtc);
        Assert.Equal(zeroPair.Value.TransferDepartureUtc, factA.TransferDepartureUtc);
        Assert.Equal(zeroPair.Value.HandoffDeadlineUtc, factA.HandoffDeadlineUtc);
        Assert.Equal(factA.PreparationStartUtc, factA.TransferDepartureUtc);
        Assert.Equal(factA.TransferDepartureUtc, factA.HandoffDeadlineUtc);

        var factB = facts.Single(x => x.ShippingGroupId == groupB);
        Assert.Equal(zeroPrep.Value.PreparationStartUtc, factB.PreparationStartUtc);
        Assert.Equal(zeroPrep.Value.TransferDepartureUtc, factB.TransferDepartureUtc);
        Assert.Equal(zeroPrep.Value.HandoffDeadlineUtc, factB.HandoffDeadlineUtc);
        Assert.Equal(factB.PreparationStartUtc, factB.TransferDepartureUtc);

        // ---------- 4) format version 4 (processing week) is accepted by the durable constraint ----------
        await using (var connection = new NpgsqlConnection(target.ConnectionString))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText =
                """
                UPDATE "order".order_shipping_groups
                SET accepted_format_version = 4
                WHERE shipping_group_id = @id
                """;
            command.Parameters.AddWithValue("id", groupA);
            Assert.Equal(1, await command.ExecuteNonQueryAsync());
        }

        // ---------- 5) the additive constraint still refuses each unlawful shape ----------
        await AssertRefusedAsync(target, groupA, "accepted_dispatch_preparation_start_utc = accepted_dispatch_transfer_departure_utc - interval '1 minute'");
        // groupB still declares a 30-minute handoff, so a zero count no longer collapses its own boundary.
        await AssertRefusedAsync(target, groupB, "accepted_dispatch_handoff_minutes = 0");
        await AssertRefusedAsync(target, groupB, "accepted_dispatch_handoff_deadline_utc = accepted_dispatch_transfer_departure_utc - interval '1 minute'");

        // Every refusal left the lawful rows exactly as they were written.
        orderDb.ChangeTracker.Clear();
        var storedA = await orderDb.OrderShippingGroups.AsNoTracking().SingleAsync(x => x.ShippingGroupId == groupA);
        var storedPlanA = storedA.AcceptedEvidence!.PlannedDispatch!;
        Assert.Equal(zeroPair.Value.PreparationStartUtc, storedPlanA.PreparationStartUtc);
        Assert.Equal(zeroPair.Value.TransferDepartureUtc, storedPlanA.TransferDepartureUtc);
        Assert.Equal(zeroPair.Value.HandoffDeadlineUtc, storedPlanA.HandoffDeadlineUtc);
        Assert.Equal(0, storedPlanA.PreparationMinutes);
        Assert.Equal(0, storedPlanA.HandoffMinutes);

        var storedB = await orderDb.OrderShippingGroups.AsNoTracking().SingleAsync(x => x.ShippingGroupId == groupB);
        Assert.Equal(30, storedB.AcceptedEvidence!.PlannedDispatch!.HandoffMinutes);

        var line =
            $"{tag} checkout={checkoutId} day={Day:yyyy-MM-dd} "
            + $"zeroPairGroup={groupA} start={factA.PreparationStartUtc:O} transfer={factA.TransferDepartureUtc:O} "
            + $"deadline={factA.HandoffDeadlineUtc:O} policy={factA.PolicyVersion} format=3->4-accepted "
            + $"zeroPrepGroup={groupB} transfer={factB.TransferDepartureUtc:O} deadline={factB.HandoffDeadlineUtc:O} "
            + "decreasing=23514-refused zeroHandoffMismatch=23514-refused deadlineBeforeDeparture=23514-refused bridge=exact";
        _output.WriteLine(line);
        var evidenceDirectory = Environment.GetEnvironmentVariable("TB264_EVIDENCE_DIR");
        if (!string.IsNullOrWhiteSpace(evidenceDirectory))
        {
            Directory.CreateDirectory(evidenceDirectory);
            File.AppendAllText(Path.Combine(evidenceDirectory, "tb264r1-relational.txt"), line + Environment.NewLine);
        }
    }

    // ------------------------------------------------------------------ helpers

    private static async Task AssertRefusedAsync(
        NpgsqlConnectionStringBuilder target,
        Guid groupId,
        string assignment)
    {
        await using var connection = new NpgsqlConnection(target.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            $"""
             UPDATE "order".order_shipping_groups
             SET {assignment}
             WHERE shipping_group_id = @id
             """;
        command.Parameters.AddWithValue("id", groupId);
        var failure = await Assert.ThrowsAsync<PostgresException>(() => command.ExecuteNonQueryAsync());
        Assert.Equal(PostgresErrorCodes.CheckViolation, failure.SqlState);
    }

    private static ShippingSchedulingWeekView CarrierWeek()
    {
        var days = new ShippingSchedulingDayView[7];
        for (var weekday = 1; weekday <= 7; weekday++)
        {
            var working = weekday <= 5;
            days[weekday - 1] = new ShippingSchedulingDayView(
                weekday, working, working ? new TimeOnly(9, 0) : null, working ? new TimeOnly(17, 0) : null);
        }

        return new ShippingSchedulingWeekView(Zone, days);
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
            Day,
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
            Zone,
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
