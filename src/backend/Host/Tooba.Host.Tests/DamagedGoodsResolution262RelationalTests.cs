using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Observability.Tracing;
using Tooba.Catalog.Contracts.Ports;
using Tooba.Fulfillment.Domain.Aggregates;
using Tooba.Fulfillment.Domain.ValueObjects;
using Tooba.Fulfillment.Infrastructure.Directories;
using Tooba.Fulfillment.Infrastructure.Gateways;
using Tooba.Fulfillment.Infrastructure.Observability;
using Tooba.Fulfillment.Infrastructure.Persistence;
using Tooba.Inventory.Application.Orders;
using Tooba.Inventory.Application.Ports;
using Tooba.Inventory.Domain.Aggregates;
using Tooba.Inventory.Domain.ValueObjects;
using Tooba.Inventory.Infrastructure.Directories;
using Tooba.Inventory.Infrastructure.Persistence;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Contracts.Ports;
using Tooba.Order.Contracts.Fulfillment;
using Tooba.Persistence;
using Xunit;
using Xunit.Abstractions;
using AppModels = Tooba.Fulfillment.Application.Fulfillments.Models;

namespace Tooba.Host.Tests;

/// <summary>
/// TB-262 on REAL storage and the REAL Inventory module: a damaged origin receipt becomes a durable, source-scoped
/// operator decision and either a linked replacement funded by its own healthy-stock reservation, or an honest
/// pending referral. Nothing here is an in-memory double on the stock side: the reservation, its consumption and its
/// release all go through <see cref="InventoryDirectory"/> against the canonical PostgreSQL database.
/// </summary>
/// <remarks>
/// Gated by <c>TB262_CONNECTION</c>; the target must be the ONE canonical database (127.0.0.1:5432/tooba_alpha).
/// Every row is created through the production domain/application paths, is task-labelled (<c>TB262-…</c>) and its
/// exact identities are appended to <c>TB262_EVIDENCE_DIR/tb262-relational.txt</c> when that variable is supplied.
/// No user order, parcel, receipt, reservation or stock row is read or written, and nothing is deleted.
/// </remarks>
[Collection("PostgresSerial")]
public sealed class DamagedGoodsResolution262RelationalTests
{
    private readonly ITestOutputHelper _output;

    public DamagedGoodsResolution262RelationalTests(ITestOutputHelper output) => _output = output;

    private static string? Connection =>
        Environment.GetEnvironmentVariable("TB262_CONNECTION") is { Length: > 0 } raw ? raw : null;

    // ------------------------------------------------------------------ A: damaged is not usable, not saleable

    [Fact]
    public async Task A_damaged_unit_grants_no_resend_and_no_stock_movement_but_owes_a_decision()
    {
        if (Connection is null)
        {
            return;
        }

        var fixture = await CreateReturnedFixtureAsync("A", usableA: 0m, damagedA: 1m, onHand: 10m);
        await using var lease = fixture.NewDirectory();

        var received = await lease.Directory.GetAsync(fixture.FulfillmentId, CancellationToken.None);
        Assert.NotNull(received);
        var lineA = received!.Items.Single(x => x.OrderLineId == fixture.LineAId);
        // A damaged receipt is never usable capacity, and the sibling healthy line keeps its own.
        Assert.Equal(0m, lineA.ResendableQuantity);
        Assert.Equal(1m, lineA.DamagedQuantity);
        Assert.Equal(1m, lineA.OutstandingDamagedQuantity);
        Assert.Equal(1m, received.Items.Single(x => x.OrderLineId == fixture.LineBId).ResendableQuantity);
        var sourceLine = received.Shipments.Single(x => x.ShipmentId == fixture.SourceShipmentId)
            .Items.Single(x => x.OrderLineId == fixture.LineAId);
        Assert.Equal(1m, sourceLine.DamagedQuantity);
        Assert.Equal(1m, sourceLine.OutstandingDamagedQuantity);

        // Recording the physical damage moved NO stock: the original sale already consumed it, and damaged goods are
        // not saleable. No restock happened, and nothing was deducted a second time.
        var (onHand, reserved) = await StockAsync(fixture);
        Assert.Equal(9m, onHand);
        Assert.Equal(0m, reserved);
        Assert.Equal(StockReservationStatus.Consumed, await ReservationStatusAsync(fixture, fixture.OriginalReservationId));

        fixture.Record(
            _output,
            $"A fulfillment={fixture.FulfillmentId} source={fixture.SourceShipmentId} lineA={fixture.LineAId} "
            + $"resendableA=0 damagedA=1 outstandingA=1 onHand=9 reserved=0");
    }

    // ------------------------------------------------------------------ B: mixed receipt, separate funds

    [Fact]
    public async Task B_mixed_usable_two_damaged_one_funds_two_resends_and_exactly_one_replacement()
    {
        if (Connection is null)
        {
            return;
        }

        var fixture = await CreateReturnedFixtureAsync("B", usableA: 2m, damagedA: 1m, onHand: 10m);
        await using var lease = fixture.NewDirectory();

        var received = await lease.Directory.GetAsync(fixture.FulfillmentId, CancellationToken.None);
        var lineA = received!.Items.Single(x => x.OrderLineId == fixture.LineAId);
        // Healthy returned capacity funds exactly 2; the damaged unit is a separate obligation.
        Assert.Equal(2m, lineA.ResendableQuantity);
        Assert.Equal(1m, lineA.DamagedQuantity);
        Assert.Equal(1m, lineA.OutstandingDamagedQuantity);

        // Over-quantity, a healthy-only line and a foreign line are refused BEFORE any allocation.
        await AssertCodeAsync("fulfillment.damage.exceeds_outstanding", () =>
            lease.Directory.ResolveDamagedGoodsAsync(
                fixture.FulfillmentId, fixture.ActorUserId, fixture.SourceShipmentId, fixture.LineAId, 2m,
                DamagedResolutionOutcome.Replacement, $"tb262-b-over-{fixture.SourceShipmentId:N}", null,
                CancellationToken.None));
        await AssertCodeAsync("fulfillment.damage.no_damaged_quantity", () =>
            lease.Directory.ResolveDamagedGoodsAsync(
                fixture.FulfillmentId, fixture.ActorUserId, fixture.SourceShipmentId, fixture.LineBId, 1m,
                DamagedResolutionOutcome.Replacement, $"tb262-b-healthy-{fixture.SourceShipmentId:N}", null,
                CancellationToken.None));
        await AssertCodeAsync("fulfillment.damage.line_foreign", () =>
            lease.Directory.ResolveDamagedGoodsAsync(
                fixture.FulfillmentId, fixture.ActorUserId, fixture.SourceShipmentId, Guid.NewGuid(), 1m,
                DamagedResolutionOutcome.Replacement, $"tb262-b-foreign-{fixture.SourceShipmentId:N}", null,
                CancellationToken.None));

        var occurrence = $"tb262-b-replacement-{fixture.SourceShipmentId:N}";
        var resolved = await lease.Directory.ResolveDamagedGoodsAsync(
            fixture.FulfillmentId, fixture.ActorUserId, fixture.SourceShipmentId, fixture.LineAId, 1m,
            DamagedResolutionOutcome.Replacement, occurrence, "TB262-B", CancellationToken.None);

        Assert.Null(resolved.BlockerCode);
        Assert.Equal(DamagedResolutionStatus.Fulfilled, resolved.Status);
        Assert.NotNull(resolved.ReplacementReservationId);
        Assert.NotNull(resolved.ReplacementShipmentId);

        // The replacement consumed a NEW hold of healthy stock; healthy returned capacity was untouched by it.
        var (onHand, reserved) = await StockAsync(fixture);
        Assert.Equal(9m, onHand);
        Assert.Equal(1m, reserved);
        var replacementLine = resolved.Snapshot.Items.Single(x => x.OrderLineId == fixture.LineAId);
        Assert.Equal(2m, replacementLine.ResendableQuantity);
        Assert.Equal(0m, replacementLine.OutstandingDamagedQuantity);

        // The SAME unit can never be decided twice; the same occurrence with a changed outcome conflicts.
        await AssertCodeAsync("fulfillment.damage.exceeds_outstanding", () =>
            lease.Directory.ResolveDamagedGoodsAsync(
                fixture.FulfillmentId, fixture.ActorUserId, fixture.SourceShipmentId, fixture.LineAId, 1m,
                DamagedResolutionOutcome.Replacement, $"tb262-b-second-{fixture.SourceShipmentId:N}", null,
                CancellationToken.None));
        await AssertCodeAsync("fulfillment.damage.resolution_intent_conflict", () =>
            lease.Directory.ResolveDamagedGoodsAsync(
                fixture.FulfillmentId, fixture.ActorUserId, fixture.SourceShipmentId, fixture.LineAId, 1m,
                DamagedResolutionOutcome.Referral, occurrence, null, CancellationToken.None));

        Assert.Equal(1, await ResolutionRowCountAsync(fixture));
        Assert.Equal(1, await ReplacementReservationRowCountAsync(fixture));

        fixture.Record(
            _output,
            $"B fulfillment={fixture.FulfillmentId} source={fixture.SourceShipmentId} "
            + $"resendable=2 damaged=1 reserved=1 replacement={resolved.ReplacementShipmentId} "
            + $"reservation={resolved.ReplacementReservationId} rows=1");
    }

    // ------------------------------------------------------------------ C: the real end-to-end replacement

    [Fact]
    public async Task C_replacement_moves_through_tracking_dispatch_delivery_and_consumes_its_own_hold_once()
    {
        if (Connection is null)
        {
            return;
        }

        var fixture = await CreateReturnedFixtureAsync("C", usableA: 2m, damagedA: 1m, onHand: 10m);
        await using var lease = fixture.NewDirectory();

        var resolved = await lease.Directory.ResolveDamagedGoodsAsync(
            fixture.FulfillmentId, fixture.ActorUserId, fixture.SourceShipmentId, fixture.LineAId, 1m,
            DamagedResolutionOutcome.Replacement, $"tb262-c-replacement-{fixture.SourceShipmentId:N}", null,
            CancellationToken.None);
        var replacementId = resolved.ReplacementShipmentId!.Value;
        var reservationId = resolved.ReplacementReservationId!.Value;

        var replacement = resolved.Snapshot.Shipments.Single(x => x.ShipmentId == replacementId);
        // The replacement keeps the accepted group/method and is visibly linked to its damaged source.
        Assert.True(replacement.IsReplacement);
        Assert.Equal(fixture.SourceShipmentId, replacement.ResendOfShipmentId);
        Assert.Equal(fixture.ShippingGroupId, replacement.ShippingGroupId);
        Assert.Equal("post", replacement.ShippingMethodCode);
        Assert.Equal(1m, replacement.Items.Single(x => x.OrderLineId == fixture.LineAId).Quantity);
        // The damaged source keeps its full history.
        Assert.Equal(
            ShipmentStatus.Returned,
            resolved.Snapshot.Shipments.Single(x => x.ShipmentId == fixture.SourceShipmentId).Status);

        await lease.Directory.AssignTrackingAsync(
            fixture.FulfillmentId, replacementId, fixture.ActorUserId, $"TRK-TB262-C-{replacementId:N}",
            CancellationToken.None);
        var dispatched = await lease.Directory.DispatchShipmentAsync(
            fixture.FulfillmentId, replacementId, fixture.ActorUserId, CancellationToken.None);
        Assert.Equal(
            ShipmentStatus.Dispatched,
            dispatched.Shipments.Single(x => x.ShipmentId == replacementId).Status);

        // The replacement consumed its OWN new hold exactly once; the original sale stayed consumed exactly once, by
        // the first dispatch, and is never consumed or released again.
        Assert.Equal(StockReservationStatus.Consumed, await ReservationStatusAsync(fixture, reservationId));
        Assert.Equal(StockReservationStatus.Consumed, await ReservationStatusAsync(fixture, fixture.OriginalReservationId));
        Assert.Equal(1, await ConsumptionRowsAsync(fixture, reservationId));
        Assert.Equal(1, await ConsumptionRowsAsync(fixture, fixture.OriginalReservationId));
        var (onHand, reserved) = await StockAsync(fixture);
        Assert.Equal(8m, onHand);
        Assert.Equal(0m, reserved);

        var delivered = await lease.Directory.DeliverShipmentAsync(
            fixture.FulfillmentId, replacementId, fixture.ActorUserId, CancellationToken.None);
        Assert.Equal(
            ShipmentStatus.Delivered,
            delivered.Shipments.Single(x => x.ShipmentId == replacementId).Status);
        // One physical terminal delivery of the replacement unit, counted once across the chain. The unit as a whole is
        // NOT delivered: only 1 of the 3 ordered units physically reached the customer, and the 2 usable returned units
        // still have to travel (their own resend), so the truthful unit state stays in transit.
        Assert.Equal(1m, delivered.Items.Single(x => x.OrderLineId == fixture.LineAId).DeliveredQuantity);
        Assert.Equal(FulfillmentStatus.InTransit, delivered.Status);

        // An exact replay of the same occurrence still answers the SAME parcel after fresh readiness disappeared.
        var replay = await lease.Directory.ResolveDamagedGoodsAsync(
            fixture.FulfillmentId, fixture.ActorUserId, fixture.SourceShipmentId, fixture.LineAId, 1m,
            DamagedResolutionOutcome.Replacement, $"tb262-c-replacement-{fixture.SourceShipmentId:N}", null,
            CancellationToken.None);
        Assert.Equal(replacementId, replay.ReplacementShipmentId);
        Assert.Equal(reservationId, replay.ReplacementReservationId);
        var reconciled = await lease.Directory.ReconcileDamagedResolutionAsync(
            fixture.FulfillmentId, fixture.SourceShipmentId, fixture.LineAId, 1m,
            DamagedResolutionOutcome.Replacement, $"tb262-c-replacement-{fixture.SourceShipmentId:N}",
            CancellationToken.None);
        Assert.Equal(AppModels.FulfillmentDamagedIntentRecord.Recorded, reconciled.State);
        Assert.Equal(replacementId, reconciled.Resolution!.ReplacementShipmentId);
        Assert.Equal(1, await ResolutionRowCountAsync(fixture));

        fixture.Record(
            _output,
            $"C fulfillment={fixture.FulfillmentId} source={fixture.SourceShipmentId} replacement={replacementId} "
            + $"reservation={reservationId} original={fixture.OriginalReservationId} consumed=1 onHand=8 reserved=0 "
            + $"delivered=1 replay=same");
    }

    // ------------------------------------------------------------------ D: cancellation, blockers and failure windows

    [Fact]
    public async Task D_cancel_releases_only_the_new_hold_and_no_stock_leaves_zero_parcel()
    {
        if (Connection is null)
        {
            return;
        }

        var fixture = await CreateReturnedFixtureAsync("D", usableA: 1m, damagedA: 1m, onHand: 10m);
        await using var lease = fixture.NewDirectory();
        var occurrence = $"tb262-d-replacement-{fixture.SourceShipmentId:N}";

        var resolved = await lease.Directory.ResolveDamagedGoodsAsync(
            fixture.FulfillmentId, fixture.ActorUserId, fixture.SourceShipmentId, fixture.LineAId, 1m,
            DamagedResolutionOutcome.Replacement, occurrence, null, CancellationToken.None);
        var firstReservation = resolved.ReplacementReservationId!.Value;
        var firstParcel = resolved.ReplacementShipmentId!.Value;
        Assert.Equal(1m, (await StockAsync(fixture)).Reserved);

        var cancelled = await lease.Directory.CancelShipmentAsync(
            fixture.FulfillmentId, firstParcel, fixture.ActorUserId, CancellationToken.None);
        Assert.Equal(
            ShipmentStatus.Cancelled,
            cancelled.Shipments.Single(x => x.ShipmentId == firstParcel).Status);
        // Only the NEW hold was released; the damaged source stays returned and non-saleable.
        Assert.Equal(StockReservationStatus.Released, await ReservationStatusAsync(fixture, firstReservation));
        Assert.Equal((9m, 0m), await StockAsync(fixture));
        Assert.Equal(
            ShipmentStatus.Returned,
            cancelled.Shipments.Single(x => x.ShipmentId == fixture.SourceShipmentId).Status);
        Assert.Equal(1m, cancelled.Items.Single(x => x.OrderLineId == fixture.LineAId).DamagedQuantity);

        // The decision still OWES a replacement: a retry with the same occurrence acquires a NEW hold + parcel.
        var retried = await lease.Directory.ResolveDamagedGoodsAsync(
            fixture.FulfillmentId, fixture.ActorUserId, fixture.SourceShipmentId, fixture.LineAId, 1m,
            DamagedResolutionOutcome.Replacement, occurrence, null, CancellationToken.None);
        Assert.NotNull(retried.ReplacementShipmentId);
        Assert.NotEqual(firstParcel, retried.ReplacementShipmentId);
        Assert.NotEqual(firstReservation, retried.ReplacementReservationId);
        Assert.Equal(1m, (await StockAsync(fixture)).Reserved);
        Assert.Equal(1, await ResolutionRowCountAsync(fixture));
        Assert.Equal(
            firstParcel,
            cancelled.Shipments.Single(x => x.ShipmentId == firstParcel).ShipmentId);

        // No healthy stock: a precise blocker, zero parcel, zero leaked hold, and the decision stays recorded so the
        // SAME unresolved intent can be retried safely.
        var starved = await CreateReturnedFixtureAsync("D0", usableA: 1m, damagedA: 1m, onHand: 1m);
        await using var starvedLease = starved.NewDirectory();
        var blocked = await starvedLease.Directory.ResolveDamagedGoodsAsync(
            starved.FulfillmentId, starved.ActorUserId, starved.SourceShipmentId, starved.LineAId, 1m,
            DamagedResolutionOutcome.Replacement, $"tb262-d0-{starved.SourceShipmentId:N}", null,
            CancellationToken.None);
        Assert.Equal("fulfillment.damage.replacement_unavailable", blocked.BlockerCode);
        Assert.Null(blocked.ReplacementShipmentId);
        Assert.Null(blocked.ReplacementReservationId);
        Assert.Equal(1, await ResolutionRowCountAsync(starved));
        Assert.Equal(0, await ReplacementReservationRowCountAsync(starved));
        Assert.Equal(1, blocked.Snapshot.Shipments.Count);

        fixture.Record(
            _output,
            $"D first={firstParcel} released=1 retry={retried.ReplacementShipmentId} "
            + $"retryReservation={retried.ReplacementReservationId} blocked={blocked.DamagedGoodsResolutionId} "
            + $"blocker={blocked.BlockerCode} starvedParcels=1 starvedHolds=0");
    }

    [Fact]
    public async Task D2_a_failure_between_the_new_hold_and_the_parcel_recovers_the_exact_hold_and_parcel()
    {
        if (Connection is null)
        {
            return;
        }

        var fixture = await CreateReturnedFixtureAsync("D2", usableA: 1m, damagedA: 1m, onHand: 10m);
        var occurrence = $"tb262-d2-{fixture.SourceShipmentId:N}";

        // The reviewed decision and its new hold persist, then the parcel write is interrupted: the real failure
        // window between "reserved" and "linked parcel".
        await using (var faulty = fixture.NewDirectory(new FailOnNthSaveInterceptor(failOn: 3)))
        {
            await Assert.ThrowsAnyAsync<Exception>(() => faulty.Directory.ResolveDamagedGoodsAsync(
                fixture.FulfillmentId, fixture.ActorUserId, fixture.SourceShipmentId, fixture.LineAId, 1m,
                DamagedResolutionOutcome.Replacement, occurrence, null, CancellationToken.None));
        }

        Assert.Equal(1, await ResolutionRowCountAsync(fixture));
        Assert.Equal(1, await ReplacementReservationRowCountAsync(fixture));
        var heldAfterCrash = await NewReplacementReservationIdAsync(fixture);
        Assert.NotNull(heldAfterCrash);
        Assert.Equal(1m, (await StockAsync(fixture)).Reserved);

        // The retry answers the SAME hold (purpose-bound key) and creates exactly one parcel.
        await using var lease = fixture.NewDirectory();
        var recovered = await lease.Directory.ResolveDamagedGoodsAsync(
            fixture.FulfillmentId, fixture.ActorUserId, fixture.SourceShipmentId, fixture.LineAId, 1m,
            DamagedResolutionOutcome.Replacement, occurrence, null, CancellationToken.None);
        Assert.Equal(heldAfterCrash, recovered.ReplacementReservationId);
        Assert.NotNull(recovered.ReplacementShipmentId);
        Assert.Equal(1, await ReplacementReservationRowCountAsync(fixture));
        Assert.Equal(1m, (await StockAsync(fixture)).Reserved);

        fixture.Record(
            _output,
            $"D2 recoveredReservation={recovered.ReplacementReservationId} parcel={recovered.ReplacementShipmentId} "
            + $"holds=1 rows=1");
    }

    // ------------------------------------------------------------------ E: concurrency

    [Fact]
    public async Task E_two_independent_contexts_cannot_resolve_one_damaged_unit_twice()
    {
        if (Connection is null)
        {
            return;
        }

        var fixture = await CreateReturnedFixtureAsync("E", usableA: 1m, damagedA: 1m, onHand: 10m);
        await using var left = fixture.NewDirectory();
        await using var right = fixture.NewDirectory();

        var results = await Task.WhenAll(
            TryResolveAsync(left.Directory, fixture, $"tb262-e-left-{fixture.SourceShipmentId:N}"),
            TryResolveAsync(right.Directory, fixture, $"tb262-e-right-{fixture.SourceShipmentId:N}"));

        Assert.Contains(results, x => x.Ok);
        Assert.Contains(results, x => !x.Ok);
        // Exactly one durable decision and exactly one new hold exist, so one unit never funded two replacements.
        Assert.Equal(1, await ResolutionRowCountAsync(fixture));
        Assert.Equal(1, await ReplacementReservationRowCountAsync(fixture));
        Assert.Equal(1m, (await StockAsync(fixture)).Reserved);
        var loser = results.Single(x => !x.Ok);
        Assert.Contains(
            loser.Code,
            new[]
            {
                "fulfillment.damage.exceeds_outstanding",
                "fulfillment.damage.concurrency_conflict",
            });

        fixture.Record(
            _output,
            $"E winner={results.First(x => x.Ok).Parcel} loser={loser.Code} rows=1 holds=1");
    }

    // ------------------------------------------------------------------ helpers

    private static async Task<(bool Ok, string? Code, Guid? Parcel)> TryResolveAsync(
        FulfillmentDirectory directory,
        Fixture fixture,
        string occurrence)
    {
        try
        {
            var resolved = await directory.ResolveDamagedGoodsAsync(
                fixture.FulfillmentId, fixture.ActorUserId, fixture.SourceShipmentId, fixture.LineAId, 1m,
                DamagedResolutionOutcome.Replacement, occurrence, null, CancellationToken.None);
            return (true, resolved.BlockerCode, resolved.ReplacementShipmentId);
        }
        catch (ContractOperationException ex)
        {
            return (false, ex.Code, null);
        }
    }

    private static async Task AssertCodeAsync(string expected, Func<Task> action)
    {
        var error = await Assert.ThrowsAsync<ContractOperationException>(action);
        Assert.Equal(expected, error.Code);
    }

    private static async Task<(decimal OnHand, decimal Reserved)> StockAsync(Fixture fixture)
    {
        await using var db = CreateInventoryDb(fixture.Connection);
        var position = await db.Positions.AsNoTracking().SingleAsync(x => x.StockItemId == fixture.StockItemId);
        return (position.OnHand, position.Reserved);
    }

    private static async Task<StockReservationStatus> ReservationStatusAsync(Fixture fixture, Guid reservationId)
    {
        await using var db = CreateInventoryDb(fixture.Connection);
        return await db.Reservations.AsNoTracking()
            .Where(x => x.ReservationId == reservationId)
            .Select(x => x.Status)
            .SingleAsync();
    }

    private static async Task<int> ConsumptionRowsAsync(Fixture fixture, Guid reservationId)
    {
        await using var db = CreateInventoryDb(fixture.Connection);
        return await db.ReservationConsumptions.AsNoTracking()
            .CountAsync(x => x.ReservationId == reservationId);
    }

    private static async Task<int> ResolutionRowCountAsync(Fixture fixture)
    {
        await using var db = CreateFulfillmentDb(fixture.Connection);
        return await db.DamagedResolutions.AsNoTracking()
            .CountAsync(x => x.FulfillmentId == fixture.FulfillmentId);
    }

    /// <summary>The LIVE replacement holds of this fixture (released/cancelled attempts are excluded on purpose).</summary>
    private static async Task<int> ReplacementReservationRowCountAsync(Fixture fixture)
    {
        await using var db = CreateInventoryDb(fixture.Connection);
        return await db.Reservations.AsNoTracking()
            .CountAsync(x => x.StockItemId == fixture.StockItemId
                && x.Status == StockReservationStatus.Held
                && x.IdempotencyKey != null
                && x.IdempotencyKey.StartsWith("damage-replacement:"));
    }

    private static async Task<Guid?> NewReplacementReservationIdAsync(Fixture fixture)
    {
        await using var db = CreateInventoryDb(fixture.Connection);
        return await db.Reservations.AsNoTracking()
            .Where(x => x.StockItemId == fixture.StockItemId
                && x.IdempotencyKey != null
                && x.IdempotencyKey.StartsWith("damage-replacement:"))
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => (Guid?)x.ReservationId)
            .FirstOrDefaultAsync();
    }

    private static InventoryDbContext CreateInventoryDb(string connectionString)
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            options, connectionString, InventoryDbContext.Schema, typeof(InventoryDbContext));
        return new InventoryDbContext(options.Options);
    }

    private static FulfillmentDbContext CreateFulfillmentDb(
        string connectionString,
        SaveChangesInterceptor? interceptor = null)
    {
        var options = new DbContextOptionsBuilder<FulfillmentDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            options, connectionString, FulfillmentDbContext.Schema, typeof(FulfillmentDbContext));
        if (interceptor is not null)
        {
            options.AddInterceptors(interceptor);
        }

        return new FulfillmentDbContext(options.Options);
    }

    private sealed record Fixture(
        string Connection,
        Guid FulfillmentId,
        Guid CheckoutId,
        Guid ShippingGroupId,
        Guid LineAId,
        Guid LineBId,
        Guid SourceShipmentId,
        Guid ActorUserId,
        Guid StockItemId,
        Guid OfferId,
        Guid OriginalReservationId)
    {
        public DirectoryLease NewDirectory(SaveChangesInterceptor? interceptor = null)
        {
            var fulfillmentDb = CreateFulfillmentDb(Connection, interceptor);
            var inventoryDb = CreateInventoryDb(Connection);
            var inventory = new InventoryDirectory(
                inventoryDb,
                new OpenInventoryGuard(),
                new UnusedOfferLookup(),
                new UnusedCatalogLookup(),
                new SystemUtcClock(),
                new UuidV7IdGenerator(),
                new ModuleCallTracer());
            var gateway = new FulfillmentInventoryGateway(
                inventory,
                new OrderInventoryLifecycleAdapter(inventory),
                inventory);
            var directory = new FulfillmentDirectory(
                fulfillmentDb,
                new OpenFulfillmentUseCaseGuard(),
                new UnusedOrderReader(),
                gateway,
                new FulfillmentInstrumentation(),
                new SystemUtcClock(),
                new UuidV7IdGenerator());
            return new DirectoryLease(fulfillmentDb, inventoryDb, directory);
        }

        public void Record(ITestOutputHelper output, string line)
        {
            output.WriteLine(line);
            var directory = Environment.GetEnvironmentVariable("TB262_EVIDENCE_DIR");
            if (string.IsNullOrWhiteSpace(directory))
            {
                return;
            }

            Directory.CreateDirectory(directory);
            File.AppendAllText(Path.Combine(directory, "tb262-relational.txt"), line + Environment.NewLine);
        }
    }

    private sealed class DirectoryLease(
        FulfillmentDbContext fulfillmentDb,
        InventoryDbContext inventoryDb,
        FulfillmentDirectory directory) : IAsyncDisposable
    {
        public FulfillmentDirectory Directory { get; } = directory;

        public async ValueTask DisposeAsync()
        {
            await inventoryDb.DisposeAsync();
            await fulfillmentDb.DisposeAsync();
        }
    }

    private const string PostMetadataJson =
        "{\"recipientName\":\"TB262 receiver\",\"recipientPhone\":\"09120000000\","
        + "\"postalCode\":\"1234567890\",\"destinationAddress\":\"TB262 task address\"}";

    /// <summary>
    /// One paid-order unit whose single parcel was dispatched, refused, returned and physically received at origin with
    /// the requested split of line A (healthy + damaged). All rows come from the production domain/application paths.
    /// </summary>
    private static async Task<Fixture> CreateReturnedFixtureAsync(
        string tag,
        decimal usableA,
        decimal damagedA,
        decimal onHand)
    {
        var connection = Connection!;
        var fulfillmentId = Guid.NewGuid();
        var checkoutId = Guid.NewGuid();
        var sellerPartyId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var lineA = Guid.NewGuid();
        var lineB = Guid.NewGuid();
        var stockItemId = Guid.NewGuid();
        var offerId = Guid.NewGuid();
        var originalReservationId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 11, 11, 9, 0, 0, TimeSpan.Zero);

        // Inventory fixture through the owning domain factories: one healthy position of the ordered offer plus the
        // committed sale reservation of that order line (evidence of the real offer identity, never reused).
        await using (var inventoryDb = CreateInventoryDb(connection))
        {
            var position = StockPosition.Open(stockItemId, offerId, Guid.NewGuid(), Guid.NewGuid(), now);
            // The committed sale already holds the ordered unit, exactly as the real reservation path leaves it.
            position.SyncQuantities(onHand, 1m, now);
            var saleReservation = StockReservation.Hold(
                originalReservationId,
                stockItemId,
                1m,
                $"TB262-{tag} sale",
                $"tb262-sale-{originalReservationId:N}",
                now,
                null);
            inventoryDb.Positions.Add(position);
            inventoryDb.Reservations.Add(saleReservation);
            await inventoryDb.SaveChangesAsync();
        }

        await using var db = CreateFulfillmentDb(connection);
        var unit = FulfillmentUnit.CreateFromPaidOrder(
            fulfillmentId,
            Guid.NewGuid,
            Guid.NewGuid(),
            checkoutId,
            sellerPartyId,
            Guid.NewGuid(),
            $"TB262-{tag} گیرنده",
            "09120000000",
            "تهران",
            "تهران",
            $"نشانی آزمون تسک 262 {tag}",
            "1234567890",
            "post",
            "پیشتاز",
            [(lineA, 3m, (Guid?)originalReservationId), (lineB, 1m, (Guid?)null)],
            now,
            [
                new FulfillmentShippingGroupPlan(
                    groupId,
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
                    [lineA, lineB]),
            ]);
        db.Fulfillments.Add(unit);
        db.Items.AddRange(unit.Items);
        db.ShippingGroups.AddRange(unit.ShippingGroups);
        await db.SaveChangesAsync();

        var lease = new Fixture(
            connection, fulfillmentId, checkoutId, groupId, lineA, lineB, Guid.Empty, actorUserId,
            stockItemId, offerId, originalReservationId);
        await using (var seeded = lease.NewDirectory())
        {
            await seeded.Directory.MarkProcessingAsync(fulfillmentId, actorUserId, CancellationToken.None);
            await seeded.Directory.PackSelectionsAsync(
                fulfillmentId,
                actorUserId,
                [new AppModels.FulfillmentSelectionCommand(lineA, 3m), new AppModels.FulfillmentSelectionCommand(lineB, 1m)],
                CancellationToken.None);
            var created = await seeded.Directory.CreateShipmentAsync(
                fulfillmentId,
                actorUserId,
                "پست",
                [
                    new AppModels.ShipmentLineCommand(lineA, 3m),
                    new AppModels.ShipmentLineCommand(lineB, 1m),
                ],
                CancellationToken.None,
                shippingMethodCode: "post",
                providerMetadataJson: PostMetadataJson,
                shippingGroupId: groupId);
            var shipmentId = created.CreatedShipmentId;
            await seeded.Directory.AssignTrackingAsync(
                fulfillmentId, shipmentId, actorUserId, $"TRK-TB262-{tag}-{shipmentId:N}", CancellationToken.None);
            await seeded.Directory.DispatchShipmentAsync(
                fulfillmentId, shipmentId, actorUserId, CancellationToken.None);
            await seeded.Directory.RecordNonDeliveryAsync(
                fulfillmentId, shipmentId, actorUserId, "customer_unavailable", $"TB262-{tag}", CancellationToken.None);
            await seeded.Directory.StartReturnAsync(
                fulfillmentId, shipmentId, actorUserId, CancellationToken.None);

            var lines = new List<AppModels.ReturnReceiptLineCommand>();
            if (usableA > 0m)
            {
                lines.Add(new AppModels.ReturnReceiptLineCommand(lineA, usableA, ReturnReceiptCondition.Usable));
            }

            if (damagedA > 0m)
            {
                lines.Add(new AppModels.ReturnReceiptLineCommand(lineA, damagedA, ReturnReceiptCondition.Damaged));
            }

            lines.Add(new AppModels.ReturnReceiptLineCommand(lineB, 1m, ReturnReceiptCondition.Usable));
            await seeded.Directory.RecordReturnReceiptAsync(
                fulfillmentId, shipmentId, actorUserId, [.. lines], $"TB262-{tag} receipt", CancellationToken.None);

            return lease with { SourceShipmentId = shipmentId };
        }
    }

    /// <summary>Fails the n-th save of one context, so a real multi-write flow stops between two commits.</summary>
    private sealed class FailOnNthSaveInterceptor(int failOn) : SaveChangesInterceptor
    {
        private int _count;

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (++_count == failOn)
            {
                throw new InvalidOperationException("TB262 simulated failure between commits.");
            }

            return ValueTask.FromResult(result);
        }
    }

    private sealed class OpenInventoryGuard : IInventoryUseCaseGuard
    {
        public Task EnsureCanMutateAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class UnusedOfferLookup : IOfferLookupGateway
    {
        public Task<OfferReference?> FindOfferAsync(Guid offerId, CancellationToken cancellationToken) =>
            Task.FromResult<OfferReference?>(null);

        public Task<IReadOnlyDictionary<Guid, OfferReference>> FindOffersBatchAsync(
            IReadOnlyCollection<Guid> offerIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, OfferReference>>(
                new Dictionary<Guid, OfferReference>());

        public Task<IReadOnlyDictionary<Guid, int>> CountOffersByCatalogVariantIdsAsync(
            IReadOnlyCollection<Guid> catalogVariantIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, int>>(new Dictionary<Guid, int>());
    }

    private sealed class UnusedCatalogLookup : ICatalogVariantLookup
    {
        public Task<CatalogVariantLookupResult?> FindVariantAsync(
            Guid variantId,
            CancellationToken cancellationToken) =>
            Task.FromResult<CatalogVariantLookupResult?>(null);

        public Task<IReadOnlyDictionary<Guid, Guid?>> GetPrimaryCategoryIdsByVariantIdsAsync(
            IReadOnlyList<Guid> variantIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, Guid?>>(new Dictionary<Guid, Guid?>());

        public Task<IReadOnlyDictionary<Guid, string>> GetVariantTitlesAsync(
            IReadOnlyList<Guid> variantIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, string>>(new Dictionary<Guid, string>());

        public Task<IReadOnlyList<Guid>> GetPublishedVariantIdsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);
    }

    private sealed class UnusedOrderReader : IOrderFulfillmentReader
    {
        public Task<OrderFulfillmentHandoffSnapshot?> GetHandoffAsync(
            Guid sellerOrderId,
            CancellationToken cancellationToken) =>
            Task.FromResult<OrderFulfillmentHandoffSnapshot?>(null);

        public Task<OrderFulfillmentHandoffSnapshot?> GetHandoffForCheckoutAsync(
            Guid checkoutId,
            Guid actorUserId,
            CancellationToken cancellationToken) =>
            Task.FromResult<OrderFulfillmentHandoffSnapshot?>(null);
    }
}
