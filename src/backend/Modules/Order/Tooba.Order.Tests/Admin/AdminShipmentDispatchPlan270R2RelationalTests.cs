using Microsoft.EntityFrameworkCore;
using Npgsql;
using Tooba.Order.Application.Admin.Operations.Ports;
using Tooba.Order.Domain.Aggregates;
using Tooba.Order.Infrastructure.Admin.Operations;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Persistence;
using Xunit;
using Xunit.Abstractions;

namespace Tooba.Order.Tests.Admin;

/// <summary>
/// TB-270-R2 on REAL canonical storage: the confirmed plan row is immutable, and the unique
/// <c>shipment_id</c> index is the database half of "one plan per physical parcel".
/// </summary>
/// <remarks>
/// <para>
/// Both cases run inside a task-owned transaction that is ALWAYS rolled back, so no plan row, no business row and no
/// demo dataset is left behind. The shipment ids are random task-labelled values; the table has no FK to Fulfillment
/// (parcels live in another module), so nothing else is touched.
/// </para>
/// <para>
/// Gated by <c>TB270R2_CONNECTION</c> and restricted to the ONE canonical <c>tooba_alpha</c> at 127.0.0.1:5432. The
/// real <see cref="OrderShipmentDispatchPlanStore"/> is the code under test; the second writer is the real EF model.
/// </para>
/// <para>
/// TB-270-R3 PROOF LABEL: these are SEQUENTIAL single-context STORE/INDEX facts — one writer and one raw duplicate
/// INSERT at a time inside one rolled-back transaction. They prove row immutability, exact replay, intent conflict
/// and the UNIQUE <c>shipment_id</c> index. They do NOT prove a race between two independent application writers, and
/// nothing here claims that. An absent connection is reported as an explicit SKIP with its reason (never as a silent
/// pass), so a "green" run without the variable cannot be mistaken for storage proof.
/// </para>
/// </remarks>
public sealed class AdminShipmentDispatchPlan270R2RelationalTests
{
    private const string RelationalEnvVar = "TB270R2_CONNECTION";

    private static readonly DateTimeOffset Confirmed = new(2026, 11, 16, 7, 0, 0, TimeSpan.Zero);

    private readonly ITestOutputHelper _output;

    public AdminShipmentDispatchPlan270R2RelationalTests(ITestOutputHelper output) => _output = output;

    private static string? Connection =>
        Environment.GetEnvironmentVariable(RelationalEnvVar) is { Length: > 0 } raw ? raw : null;

    /// <summary>
    /// TB-270-R3: without the connection variable the case is SKIPPED with a stated reason, so the result can never
    /// look like a passing storage proof that never touched storage.
    /// </summary>
    public sealed class CanonicalRelationalFactAttribute : FactAttribute
    {
        public CanonicalRelationalFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(RelationalEnvVar)))
            {
                Skip = $"NOT RUN: {RelationalEnvVar} is not set; the canonical tooba_alpha relational proof was not exercised.";
            }
        }
    }

    [CanonicalRelationalFact]
    [Trait("Case", "A-immutable-and-replay")]
    public async Task A_stored_plan_is_immutable_and_exact_replay_and_changed_intent_are_distinguished()
    {
        CanonicalTarget();

        var shipmentId = Guid.NewGuid();
        await using var db = NewOrderDb();
        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            var store = new OrderShipmentDispatchPlanStore(db);
            var first = Plan(shipmentId, "tb270r2-relational-a", new DateOnly(2026, 11, 20));

            Assert.Equal(OrderShipmentDispatchPlanInsertOutcome.Inserted, await store.TryInsertAsync(first, default));
            Assert.Equal(1, await db.ShipmentDispatchPlans.CountAsync(x => x.ShipmentId == shipmentId));

            // Exact replay (identical intent) answers the stored row and writes nothing.
            var replay = Plan(shipmentId, "tb270r2-relational-a", new DateOnly(2026, 11, 20));
            Assert.Equal(
                OrderShipmentDispatchPlanInsertOutcome.DuplicateExact,
                await store.TryInsertAsync(replay, default));

            // A changed selection under the SAME key is a conflict, not an overwrite.
            var changed = Plan(shipmentId, "tb270r2-relational-a", new DateOnly(2026, 11, 21));
            Assert.Equal(
                OrderShipmentDispatchPlanInsertOutcome.DuplicateConflict,
                await store.TryInsertAsync(changed, default));

            // A different key for the same parcel is also a conflict; the winner stays byte-identical.
            var intruder = Plan(shipmentId, "tb270r2-relational-b", new DateOnly(2026, 11, 22));
            Assert.Equal(
                OrderShipmentDispatchPlanInsertOutcome.DuplicateConflict,
                await store.TryInsertAsync(intruder, default));

            var stored = await store.FindAsync(shipmentId, default);
            Assert.NotNull(stored);
            Assert.Equal(first.PlanId, stored!.PlanId);
            Assert.Equal("tb270r2-relational-a", stored.OccurrenceKey);
            Assert.Equal(new DateOnly(2026, 11, 20), stored.CustomerReceivingDate);
            Assert.Equal(1, await db.ShipmentDispatchPlans.CountAsync(x => x.ShipmentId == shipmentId));

            _output.WriteLine(
                $"A shipment={shipmentId} plan={stored.PlanId} rows=1 occurrence={stored.OccurrenceKey}");
        }
        finally
        {
            await transaction.RollbackAsync();
        }

        Assert.Equal(0, await db.ShipmentDispatchPlans.CountAsync(x => x.ShipmentId == shipmentId));
    }

    [CanonicalRelationalFact]
    [Trait("Case", "B-unique-index")]
    public async Task B_the_database_itself_refuses_a_second_plan_row_for_the_same_parcel()
    {
        CanonicalTarget();

        var shipmentId = Guid.NewGuid();
        await using var db = NewOrderDb();
        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            var store = new OrderShipmentDispatchPlanStore(db);
            Assert.Equal(
                OrderShipmentDispatchPlanInsertOutcome.Inserted,
                await store.TryInsertAsync(Plan(shipmentId, "tb270r2-relational-c", new DateOnly(2026, 11, 20)), default));

            // Bypass the store's own pre-check: the UNIQUE index must be the last line of defence even when the
            // in-process read saw nothing (the concurrent-commit case).
            var violation = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO "order".shipment_dispatch_plans
                    (plan_id, shipment_id, checkout_id, seller_order_id, fulfillment_id, shipping_group_id,
                     attempt_kind, source_shipment_id, damaged_resolution_id, replacement_reservation_id,
                     source_fingerprint, owner_kind, owner_tenant_key, owner_seller_party_id, method_code,
                     method_label, service_code, option_code, snapshot_id, draft_id, source_revision,
                     format_version, selection_version, area_group_id, rate_row_id, customer_receiving_date,
                     customer_receiving_open_ticks, customer_receiving_close_ticks, customer_receiving_time_zone,
                     handoff_date, handoff_open_ticks, handoff_close_ticks, handoff_time_zone,
                     preparation_start_utc, transfer_departure_utc, handoff_deadline_utc, preparation_minutes,
                     handoff_minutes, transport_working_days, policy_version, readiness_source, readiness_at_utc,
                     occurrence_key, confirmed_by_user_id, confirmed_at_utc)
                SELECT {1}, shipment_id, checkout_id, seller_order_id, fulfillment_id,
                       shipping_group_id, attempt_kind, source_shipment_id, damaged_resolution_id,
                       replacement_reservation_id, 'other-fingerprint', owner_kind, owner_tenant_key,
                       owner_seller_party_id, method_code, method_label, service_code, option_code, snapshot_id,
                       draft_id, source_revision, format_version, selection_version, area_group_id, rate_row_id,
                       date '2026-11-25', customer_receiving_open_ticks, customer_receiving_close_ticks,
                       customer_receiving_time_zone, date '2026-11-24', handoff_open_ticks, handoff_close_ticks,
                       handoff_time_zone, preparation_start_utc, transfer_departure_utc, handoff_deadline_utc,
                       preparation_minutes, handoff_minutes, transport_working_days, policy_version,
                       readiness_source, readiness_at_utc, 'tb270r2-relational-d', confirmed_by_user_id,
                       confirmed_at_utc
                FROM "order".shipment_dispatch_plans WHERE shipment_id = {0}
                """,
                shipmentId,
                Guid.NewGuid()));

            // 23505 = unique_violation; the errant statement aborted only its own implicit savepoint. The exact
            // constraint is asserted too: the store's arbitration may only reinterpret THIS index.
            Assert.Equal("23505", violation.SqlState);
            Assert.Equal(
                OrderShipmentDispatchPlanStore.UniqueShipmentConstraint,
                violation.ConstraintName);

            // The store's own catch path must accept exactly that violation and nothing else.
            Assert.True(OrderShipmentDispatchPlanStore.IsUniqueShipmentViolation(
                new DbUpdateException("concurrent winner", violation)));
            _output.WriteLine(
                $"B shipment={shipmentId} second-row refused with SqlState={violation.SqlState} constraint={violation.ConstraintName}");
        }
        finally
        {
            await transaction.RollbackAsync();
        }

        Assert.Equal(0, await db.ShipmentDispatchPlans.CountAsync(x => x.ShipmentId == shipmentId));
    }

    private static void CanonicalTarget()
    {
        var builder = new NpgsqlConnectionStringBuilder(Connection!);
        if (builder.Host != "127.0.0.1" || builder.Port != 5432 || builder.Database != "tooba_alpha")
        {
            throw new InvalidOperationException(
                "TB-270-R2 relational proof runs against the ONE canonical database only.");
        }
    }

    private static OrderDbContext NewOrderDb()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            options, Connection!, OrderDbContext.Schema, typeof(OrderDbContext));
        return new OrderDbContext(options.Options);
    }

    /// <summary>
    /// A complete, valid resend plan for a random task-labelled parcel (no business rows involved).
    /// </summary>
    /// <remarks>
    /// Every identity EXCEPT the plan id is derived deterministically from <paramref name="shipmentId"/>, because the
    /// reviewed intent includes the captured selection identity: an exact replay must repeat the SAME snapshot/draft/
    /// area/rate references, while a different parcel must not collide with them.
    /// </remarks>
    private static OrderShipmentDispatchPlan Plan(Guid shipmentId, string key, DateOnly receivingDate) =>
        OrderShipmentDispatchPlan.Create(
            Guid.NewGuid(),
            shipmentId,
            Tagged(shipmentId, 1),
            Tagged(shipmentId, 2),
            Tagged(shipmentId, 3),
            Tagged(shipmentId, 4),
            OrderShipmentAttemptKinds.Resend,
            Tagged(shipmentId, 5),
            null,
            null,
            "tb270r2-line=1",
            0,
            "store-alpha",
            null,
            "store_courier",
            "پیشتاز",
            "store_courier",
            null,
            Tagged(shipmentId, 6),
            Tagged(shipmentId, 7),
            7,
            4,
            9,
            Tagged(shipmentId, 8),
            Tagged(shipmentId, 9),
            receivingDate,
            new TimeOnly(9, 0).Ticks,
            new TimeOnly(10, 0).Ticks,
            "Asia/Tehran",
            receivingDate.AddDays(-2),
            new TimeOnly(8, 0).Ticks,
            new TimeOnly(20, 0).Ticks,
            "Asia/Tehran",
            Confirmed.AddHours(-9),
            Confirmed.AddHours(-5),
            Confirmed.AddHours(8),
            120,
            60,
            2,
            3,
            OrderShipmentReadinessSources.ReturnReceipt,
            Confirmed.AddDays(-1),
            key,
            Guid.NewGuid(),
            Confirmed);

    private static Guid Tagged(Guid seed, byte tag)
    {
        var bytes = seed.ToByteArray();
        bytes[^1] = tag;
        return new Guid(bytes);
    }
}
