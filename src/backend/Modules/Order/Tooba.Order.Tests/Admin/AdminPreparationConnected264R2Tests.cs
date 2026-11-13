using Microsoft.EntityFrameworkCore;
using Npgsql;
using Tooba.BuildingBlocks.Grid;
using Tooba.Fulfillment.Application.Fulfillments.Ports;
using Tooba.Fulfillment.Application.WorkQueue.Models;
using Tooba.Fulfillment.Domain.Aggregates;
using Tooba.Fulfillment.Infrastructure.Persistence;
using Tooba.Fulfillment.Infrastructure.Queries;
using Tooba.Order.Infrastructure.Integrations.DispatchPlan;
using Tooba.Order.Infrastructure.Integrations.GridEnrichment;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Party.Contracts.Ports;
using Tooba.Persistence;
using Xunit;
using Xunit.Abstractions;

namespace Tooba.Order.Tests.Admin;

/// <summary>
/// TB-264-R2 on REAL canonical storage: the positive chain that the live public HTTP path produced must be visible —
/// unchanged — in the existing admin work queue, with the exact policy-3 preparation/transfer/handoff instants and the
/// exact receiving date/window that were accepted from the storefront selection.
/// </summary>
/// <remarks>
/// <para>
/// <b>Boundary (disclosed, never invented).</b> The checkout and its accepted group were created by the real public
/// storefront path (authoring publication → captured projection → <c>PUT /v1/storefront/shipping/selection</c> →
/// <c>POST /v1/storefront/shipping/commit</c>) on the canonical database, so the <b>plan</b> under test is genuine
/// authoritative Order data, not an injected fact. A paid order needs a real payment, which this task does not
/// perform; the <b>only</b> fixture layer added here is the Fulfillment unit for that same seller order/group, built
/// through the production <see cref="FulfillmentUnit.CreateFromPaidOrder"/> factory (the same owning path the paid
/// integration uses). Nothing in Order is inserted, updated or re-derived by this test.
/// </para>
/// <para>
/// Gated by <c>TB264R2_CONNECTION</c>; without a target nothing runs and nothing is claimed. The fixture unit is
/// task-labelled (<c>TB264R2C-…</c>) and the test is idempotent for the same checkout.
/// </para>
/// </remarks>
public sealed class AdminPreparationConnected264R2Tests
{
    private const string RelationalEnvVar = "TB264R2_CONNECTION";

    // Exact task-labelled live chain (public storefront path, canonical tooba_alpha).
    private static readonly Guid CheckoutId = Guid.Parse("01a1cdba-7d2d-7000-a800-4b6d216eb0e3");
    private static readonly Guid SellerOrderId = Guid.Parse("01a1cdba-7d41-7000-9391-1f0da5ed44ea");
    private static readonly Guid SellerPartyId = Guid.Parse("01a030d1-40cb-7000-8abe-6d31739956c5");
    private static readonly Guid ShippingGroupId = Guid.Parse("01a1cdba-67e1-7000-ba0c-90dbf18dfecf");
    private static readonly Guid OrderLineId = Guid.Parse("01a1cdba-7d45-7000-800a-d2341558b80c");

    private readonly ITestOutputHelper _output;

    public AdminPreparationConnected264R2Tests(ITestOutputHelper output) => _output = output;

    private static string? Connection =>
        Environment.GetEnvironmentVariable(RelationalEnvVar) is { Length: > 0 } raw ? raw : null;

    [Fact]
    public async Task Case_A_accepted_policy3_plan_reaches_the_existing_work_queue_unchanged()
    {
        if (Connection is null)
        {
            return;
        }

        var target = CanonicalTarget();
        var now = DateTimeOffset.UtcNow;
        var tag = $"TB264R2C-{now:HHmmss}";

        await using var orderDb = NewOrderDb(target);
        await using var fulfillmentDb = NewFulfillmentDb(target);

        var accepted = await ReadAcceptedPlanAsync(orderDb);
        if (accepted is null)
        {
            // The live task fixture is not present in this database: claim nothing.
            _output.WriteLine("TB264-R2 connected: accepted plan row absent; nothing claimed.");
            return;
        }

        Assert.Equal(3, accepted.Value.PolicyVersion);
        Assert.Equal(300, accepted.Value.PreparationMinutes);
        Assert.Equal(75, accepted.Value.HandoffMinutes);
        Assert.Equal(new DateTimeOffset(2026, 11, 12, 9, 30, 0, TimeSpan.Zero), accepted.Value.PreparationStartUtc);
        Assert.Equal(new DateTimeOffset(2026, 11, 12, 15, 15, 0, TimeSpan.Zero), accepted.Value.TransferDepartureUtc);
        Assert.Equal(new DateTimeOffset(2026, 11, 12, 16, 30, 0, TimeSpan.Zero), accepted.Value.HandoffDeadlineUtc);
        Assert.Equal(4, accepted.Value.FormatVersion);
        Assert.Equal(4, accepted.Value.SelectionVersion);

        // The ONLY fixture layer: the Fulfillment unit for this exact seller order/group (production factory).
        var recipient = await fulfillmentDb.Fulfillments
            .Where(x => x.CheckoutId == CheckoutId)
            .Select(x => x.RecipientName)
            .FirstOrDefaultAsync();
        if (recipient is null)
        {
            recipient = tag;
            var unit = FulfillmentUnit.CreateFromPaidOrder(
                Guid.NewGuid(),
                () => Guid.NewGuid(),
                SellerOrderId,
                CheckoutId,
                SellerPartyId,
                Guid.Parse("01a1cd9f-5170-7000-992f-65e18438f543"),
                tag,
                "09992642001",
                "تهران",
                "تهران",
                "تهران، خیابان آزمایشی ۲۶۴، پلاک ۱",
                "1234567890",
                "tipax\u001Fexpress",
                "TB264R2 تیپاکس اکسپرس آزمایشی",
                [(OrderLineId, 1m, (Guid?)null)],
                now,
                [
                    new FulfillmentShippingGroupPlan(
                        ShippingGroupId,
                        SellerPartyId,
                        "tipax",
                        "express",
                        null,
                        "TB264R2 تیپاکس اکسپرس آزمایشی",
                        null,
                        new DateOnly(2026, 11, 14),
                        new DateOnly(2026, 11, 14),
                        "09:00-10:00",
                        1,
                        [OrderLineId]),
                ]);
            fulfillmentDb.Fulfillments.Add(unit);
            fulfillmentDb.Items.AddRange(unit.Items);
            fulfillmentDb.ShippingGroups.AddRange(unit.ShippingGroups);
            fulfillmentDb.Shipments.AddRange(unit.Shipments);
            fulfillmentDb.ShipmentItems.AddRange(unit.Shipments.SelectMany(x => x.Items));
            await fulfillmentDb.SaveChangesAsync();
        }

        var engine = new AdminFulfillmentWorkQueueQueryEngine(
            fulfillmentDb,
            new EmptyPartyLookup(),
            new OrderGridEnrichmentBridge(orderDb),
            new OrderDispatchPlanFactsBridge(orderDb),
            new FixedClock(now));
        var page = await engine.QueryAsync(Request(1, 50, recipient), CancellationToken.None);

        var row = Assert.Single(page.Items.Where(x => x.WorkUnitKey == $"group:{ShippingGroupId}"));
        Assert.Equal(AdminFulfillmentWorkUnitKinds.Group, row.WorkUnitKind);
        Assert.Equal(CheckoutId, row.CheckoutId);
        Assert.Equal(ShippingGroupId, row.TargetShippingGroupId);

        // The queue reads the SAME authoritative plan — no recomputation, no borrowed sibling plan.
        Assert.Equal(3, row.DispatchPolicyVersion);
        Assert.Equal(accepted.Value.PreparationStartUtc, row.PreparationStartUtc);
        Assert.Equal(accepted.Value.TransferDepartureUtc, row.TransferDepartureUtc);
        Assert.Equal(accepted.Value.HandoffDeadlineUtc, row.HandoffDeadlineUtc);
        Assert.Equal(accepted.Value.HandoffDeadlineUtc, row.DispatchDeadlineUtc);

        Assert.Equal(new DateOnly(2026, 11, 12), row.PlannedHandoffDate);
        Assert.Equal("Asia/Tehran", row.PlannedHandoffTimeZone);
        Assert.Equal(1, row.HandoffPlanVariants);
        Assert.Equal(new DateOnly(2026, 11, 14), row.CustomerReceivingDate);
        Assert.Equal("09:00-10:00", row.CustomerReceivingWindow);

        // The bucket is time-relative by design: the same durable plan must classify truthfully on any day, so the
        // expectation is derived from its own handoff date in the carrier zone, not frozen to the day it was created.
        var todayInCarrierZone = DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTime(now, TimeZoneInfo.FindSystemTimeZoneById("Asia/Tehran")).DateTime);
        var expectedBucket = row.PlannedHandoffDate < todayInCarrierZone
            ? AdminFulfillmentDispatchBucketNames.Overdue
            : row.PlannedHandoffDate == todayInCarrierZone
                ? AdminFulfillmentDispatchBucketNames.Today
                : AdminFulfillmentDispatchBucketNames.Upcoming;
        Assert.Equal(expectedBucket, row.DispatchBucket);
        Assert.NotEqual("", row.HandoffWindowState);
        Assert.True(row.PendingDispatch);
        Assert.NotEqual("", row.NextStepCode);

        _output.WriteLine(
            $"TB264-R2 connected: key={row.WorkUnitKey} checkout={row.CheckoutId} bucket={row.DispatchBucket} " +
            $"window={row.HandoffWindowState} policyV={row.DispatchPolicyVersion} " +
            $"prep={row.PreparationStartUtc:O} transfer={row.TransferDepartureUtc:O} handoff={row.HandoffDeadlineUtc:O} " +
            $"carrier={row.PlannedHandoffDate} {row.PlannedHandoffWindow} {row.PlannedHandoffTimeZone} " +
            $"customer={row.CustomerReceivingDate} {row.CustomerReceivingWindow} " +
            $"state={row.PreparationState} next={row.NextStepCode}");
    }

    /// <summary>
    /// TB-264-R2 final-publication chain (revision 8 → selection 5). Same public path, the newest published capture,
    /// and the handoff allowance the operator edited in the real form (80 minutes, Thursday 09:00–17:30).
    /// </summary>
    [Fact]
    public async Task Case_B_final_publication_selection_reaches_the_existing_work_queue_unchanged()
    {
        if (Connection is null)
        {
            return;
        }

        var target = CanonicalTarget();
        var now = DateTimeOffset.UtcNow;
        var checkoutId = Guid.Parse("01a1d511-adbb-7000-8575-d3b957453d76");
        var sellerOrderId = Guid.Parse("01a1d511-adce-7000-8819-0185f673c4bf");
        var sellerPartyId = Guid.Parse("01a030d1-40cb-7000-8abe-6d31739956c5");
        var shippingGroupId = Guid.Parse("01a1d511-7412-7000-a5a7-2d42aef7c393");
        var orderLineId = Guid.Parse("01a1d511-add3-7000-89b0-1e9cd9ff11d1");
        var buyerId = Guid.Parse("01a1d511-23a3-7000-8238-a5ecff4171c7");
        var tag = $"TB264R2C2-{now:HHmmss}";

        await using var orderDb = NewOrderDb(target);
        await using var fulfillmentDb = NewFulfillmentDb(target);

        var accepted = await ReadAcceptedPlanAsync(orderDb, checkoutId, shippingGroupId);
        if (accepted is null)
        {
            _output.WriteLine("TB264-R2 connected (final publication): accepted plan row absent; nothing claimed.");
            return;
        }

        Assert.Equal(3, accepted.Value.PolicyVersion);
        Assert.Equal(300, accepted.Value.PreparationMinutes);
        Assert.Equal(80, accepted.Value.HandoffMinutes);
        Assert.Equal(new DateTimeOffset(2026, 11, 14, 9, 30, 0, TimeSpan.Zero), accepted.Value.PreparationStartUtc);
        Assert.Equal(new DateTimeOffset(2026, 11, 14, 15, 10, 0, TimeSpan.Zero), accepted.Value.TransferDepartureUtc);
        Assert.Equal(new DateTimeOffset(2026, 11, 14, 16, 30, 0, TimeSpan.Zero), accepted.Value.HandoffDeadlineUtc);
        Assert.Equal(4, accepted.Value.FormatVersion);
        Assert.Equal(5, accepted.Value.SelectionVersion);

        var recipient = await fulfillmentDb.Fulfillments
            .Where(x => x.CheckoutId == checkoutId)
            .Select(x => x.RecipientName)
            .FirstOrDefaultAsync();
        if (recipient is null)
        {
            recipient = tag;
            var unit = FulfillmentUnit.CreateFromPaidOrder(
                Guid.NewGuid(),
                () => Guid.NewGuid(),
                sellerOrderId,
                checkoutId,
                sellerPartyId,
                buyerId,
                tag,
                "09992642003",
                "تهران",
                "تهران",
                "تهران، خیابان آزمایشی ۲۶۴، پلاک ۲",
                "1234567890",
                "tipax\u001Fexpress",
                "TB264R2 تیپاکس اکسپرس آزمایشی",
                [(orderLineId, 1m, (Guid?)null)],
                now,
                [
                    new FulfillmentShippingGroupPlan(
                        shippingGroupId,
                        sellerPartyId,
                        "tipax",
                        "express",
                        null,
                        "TB264R2 تیپاکس اکسپرس آزمایشی",
                        null,
                        new DateOnly(2026, 11, 14),
                        new DateOnly(2026, 11, 16),
                        "09:00-10:00",
                        1,
                        [orderLineId]),
                ]);
            fulfillmentDb.Fulfillments.Add(unit);
            fulfillmentDb.Items.AddRange(unit.Items);
            fulfillmentDb.ShippingGroups.AddRange(unit.ShippingGroups);
            fulfillmentDb.Shipments.AddRange(unit.Shipments);
            fulfillmentDb.ShipmentItems.AddRange(unit.Shipments.SelectMany(x => x.Items));
            await fulfillmentDb.SaveChangesAsync();
        }

        var engine = new AdminFulfillmentWorkQueueQueryEngine(
            fulfillmentDb,
            new EmptyPartyLookup(),
            new OrderGridEnrichmentBridge(orderDb),
            new OrderDispatchPlanFactsBridge(orderDb),
            new FixedClock(now));
        var page = await engine.QueryAsync(Request(1, 50, recipient), CancellationToken.None);

        var row = Assert.Single(page.Items.Where(x => x.WorkUnitKey == $"group:{shippingGroupId}"));
        Assert.Equal(AdminFulfillmentWorkUnitKinds.Group, row.WorkUnitKind);
        Assert.Equal(checkoutId, row.CheckoutId);
        Assert.Equal(shippingGroupId, row.TargetShippingGroupId);
        Assert.Equal(3, row.DispatchPolicyVersion);
        Assert.Equal(accepted.Value.PreparationStartUtc, row.PreparationStartUtc);
        Assert.Equal(accepted.Value.TransferDepartureUtc, row.TransferDepartureUtc);
        Assert.Equal(accepted.Value.HandoffDeadlineUtc, row.HandoffDeadlineUtc);
        Assert.Equal(accepted.Value.HandoffDeadlineUtc, row.DispatchDeadlineUtc);
        Assert.Equal(new DateOnly(2026, 11, 14), row.PlannedHandoffDate);
        Assert.Equal("Asia/Tehran", row.PlannedHandoffTimeZone);
        Assert.Equal(1, row.HandoffPlanVariants);
        Assert.Equal(new DateOnly(2026, 11, 16), row.CustomerReceivingDate);
        Assert.Equal("09:00-10:00", row.CustomerReceivingWindow);
        Assert.True(row.PendingDispatch);
        Assert.NotEqual("", row.NextStepCode);

        _output.WriteLine(
            $"TB264-R2 connected (final publication): key={row.WorkUnitKey} checkout={row.CheckoutId} " +
            $"bucket={row.DispatchBucket} window={row.HandoffWindowState} policyV={row.DispatchPolicyVersion} " +
            $"prep={row.PreparationStartUtc:O} transfer={row.TransferDepartureUtc:O} handoff={row.HandoffDeadlineUtc:O} " +
            $"carrier={row.PlannedHandoffDate} {row.PlannedHandoffWindow} {row.PlannedHandoffTimeZone} " +
            $"customer={row.CustomerReceivingDate} {row.CustomerReceivingWindow} " +
            $"state={row.PreparationState} next={row.NextStepCode}");
    }

    private static Task<(int PolicyVersion, int PreparationMinutes, int HandoffMinutes,
        DateTimeOffset PreparationStartUtc, DateTimeOffset TransferDepartureUtc, DateTimeOffset HandoffDeadlineUtc,
        int FormatVersion, long SelectionVersion)?> ReadAcceptedPlanAsync(OrderDbContext db) =>
        ReadAcceptedPlanAsync(db, CheckoutId, ShippingGroupId);

    private static async Task<(int PolicyVersion, int PreparationMinutes, int HandoffMinutes,
        DateTimeOffset PreparationStartUtc, DateTimeOffset TransferDepartureUtc, DateTimeOffset HandoffDeadlineUtc,
        int FormatVersion, long SelectionVersion)?> ReadAcceptedPlanAsync(
        OrderDbContext db,
        Guid checkoutId,
        Guid shippingGroupId)
    {
        await db.Database.OpenConnectionAsync();
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText =
            """
            SELECT accepted_dispatch_policy_version, accepted_dispatch_preparation_minutes,
                   accepted_dispatch_handoff_minutes, accepted_dispatch_preparation_start_utc,
                   accepted_dispatch_transfer_departure_utc, accepted_dispatch_handoff_deadline_utc,
                   accepted_format_version, accepted_selection_version
            FROM "order".order_shipping_groups
            WHERE checkout_id = @checkout AND shipping_group_id = @group
            LIMIT 1
            """;
        var checkout = command.CreateParameter();
        checkout.ParameterName = "checkout";
        checkout.Value = checkoutId;
        command.Parameters.Add(checkout);
        var group = command.CreateParameter();
        group.ParameterName = "group";
        group.Value = shippingGroupId;
        command.Parameters.Add(group);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        return (
            reader.GetInt32(0),
            reader.GetInt32(1),
            reader.GetInt32(2),
            reader.GetFieldValue<DateTimeOffset>(3),
            reader.GetFieldValue<DateTimeOffset>(4),
            reader.GetFieldValue<DateTimeOffset>(5),
            reader.GetInt32(6),
            reader.GetInt64(7));
    }

    private static GridQueryRequest Request(int page, int pageSize, string search) =>
        new(page, pageSize, search, [], []);

    private static (string Host, int Port, string Database) CanonicalTarget()
    {
        var builder = new NpgsqlConnectionStringBuilder(Connection!);
        if (builder.Host != "127.0.0.1" || builder.Port != 5432 || builder.Database != "tooba_alpha")
        {
            throw new InvalidOperationException(
                "TB-264-R2 connected proof runs against the ONE canonical database only.");
        }

        return (builder.Host, builder.Port, builder.Database);
    }

    private static OrderDbContext NewOrderDb((string Host, int Port, string Database) target)
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            options, Connection!, OrderDbContext.Schema, typeof(OrderDbContext));
        return new OrderDbContext(options.Options);
    }

    private static FulfillmentDbContext NewFulfillmentDb((string Host, int Port, string Database) target)
    {
        var options = new DbContextOptionsBuilder<FulfillmentDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            options, Connection!, FulfillmentDbContext.Schema, typeof(FulfillmentDbContext));
        return new FulfillmentDbContext(options.Options);
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

    private sealed class FixedClock(DateTimeOffset at) : Tooba.BuildingBlocks.IClock
    {
        public DateTimeOffset UtcNow { get; } = at;
    }
}
