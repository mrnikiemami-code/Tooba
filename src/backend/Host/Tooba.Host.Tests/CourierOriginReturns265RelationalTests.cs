using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Observability.Tracing;
using Tooba.Catalog.Contracts.Ports;
using Tooba.Fulfillment.Application.Fulfillments.Ports;
using Tooba.Fulfillment.Contracts.Operations;
using Tooba.Fulfillment.Domain.Aggregates;
using Tooba.Fulfillment.Domain.ValueObjects;
using Tooba.Fulfillment.Infrastructure.Directories;
using Tooba.Fulfillment.Infrastructure.Gateways;
using Tooba.Fulfillment.Infrastructure.Observability;
using Tooba.Fulfillment.Infrastructure.Persistence;
using Tooba.Fulfillment.Infrastructure.Returns;
using Tooba.Inventory.Application.Orders;
using Tooba.Inventory.Application.Ports;
using Tooba.Inventory.Contracts.Replacement;
using Tooba.Inventory.Contracts.Returns;
using Tooba.Inventory.Domain.Aggregates;
using Tooba.Inventory.Domain.ValueObjects;
using Tooba.Inventory.Infrastructure.Adapters;
using Tooba.Inventory.Infrastructure.Directories;
using Tooba.Inventory.Infrastructure.Persistence;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Contracts.Ports;
using Tooba.Order.Contracts.Fulfillment;
using Tooba.Order.Contracts.Returns;
using Tooba.Order.Infrastructure.Integrations.Returns;
using Tooba.Payment.Contracts.Returns;
using Tooba.Persistence;
using Tooba.Returns.Application.ReturnRequests.Commands;
using Tooba.Returns.Application.ReturnRequests.Models;
using Tooba.Returns.Application.ReturnRequests.Ports;
using Tooba.Returns.Domain.ValueObjects;
using Tooba.Returns.Infrastructure.Directories;
using Tooba.Returns.Infrastructure.Observability;
using Tooba.Returns.Infrastructure.Persistence;
using Tooba.Wallet.Contracts.Dtos;
using Tooba.Wallet.Contracts.Refunds;
using Xunit;
using Xunit.Abstractions;
using AppModels = Tooba.Fulfillment.Application.Fulfillments.Models;
using DomainClaimCondition = Tooba.Fulfillment.Domain.ValueObjects.CourierReturnClaimCondition;
using FulfillmentContracts = Tooba.Fulfillment.Contracts.Returns;

namespace Tooba.Host.Tests;

/// <summary>
/// TB-265 on REAL storage: a physically received courier-origin parcel funds exactly ONE branch. The Fulfillment
/// claim is a real row in <c>fulfillment.courier_return_claims</c>, the held release is a real partial release of a
/// real <see cref="StockReservation"/>, and the case really lives in <c>returns.return_requests</c>. The only
/// controlled seams are the Order read snapshot, the payment/wallet ports (no provider is called and no money moves)
/// and the Returns restock gateway (recorded calls, so a damaged unit can be proven never to become saleable).
/// </summary>
/// <remarks>
/// Gated by <c>TB265_CONNECTION</c>; the target must be the ONE canonical database (127.0.0.1:5432/tooba_alpha).
/// Every row comes from the production domain/application paths, is task-labelled (<c>TB265-…</c>) and its exact
/// identities are appended to <c>TB265_EVIDENCE_DIR/tb265-relational.txt</c> when that variable is supplied.
/// No user order, parcel, receipt, reservation, case or stock row is read or written, and nothing is deleted.
/// </remarks>
[Collection("PostgresSerial")]
public sealed class CourierOriginReturns265RelationalTests
{
    private readonly ITestOutputHelper _output;

    public CourierOriginReturns265RelationalTests(ITestOutputHelper output) => _output = output;

    private static string? Connection =>
        Environment.GetEnvironmentVariable("TB265_CONNECTION") is { Length: > 0 } raw ? raw : null;

    // ------------------------------------------------------------------ A: one exclusive, exact, replayable claim

    [Fact]
    public async Task A_receipt_capacity_is_claimed_exactly_once_and_across_both_conditions()
    {
        if (Connection is null)
        {
            return;
        }

        var fixture = await CreateReturnedFixtureAsync("A", usableA: 2m, damagedA: 1m, dispatchA: 3m);
        await using var lease = fixture.NewLease();

        var first = await lease.Source.ClaimAsync(
            fixture.Claim("tb265-a-1", (fixture.LineAId, 2m, DomainClaimCondition.Usable),
                (fixture.LineAId, 1m, DomainClaimCondition.Damaged)),
            CancellationToken.None);
        Assert.NotNull(first.Claim);
        Assert.Null(first.ConflictCode);
        Assert.Equal(FulfillmentContracts.CourierReturnClaimStatus.Requested, first.Claim!.Status);
        Assert.NotEqual(Guid.Empty, first.Claim.ShipmentReturnReceiptId);

        // The claim is the capacity: the same received units are no longer resend/replacement capacity, while the
        // untouched sibling line keeps its own capacity exactly.
        var after = await lease.Directory.GetAsync(fixture.FulfillmentId, CancellationToken.None);
        Assert.Equal(0m, after!.Items.Single(x => x.OrderLineId == fixture.LineAId).ResendableQuantity);
        Assert.Equal(1m, after.Items.Single(x => x.OrderLineId == fixture.LineAId).DamagedQuantity);
        Assert.Equal(0m, after.Items.Single(x => x.OrderLineId == fixture.LineAId).OutstandingDamagedQuantity);

        // The sibling line was never part of the receipt, so it has no received capacity of its own to resend.
        Assert.Equal(0m, after.Items.Single(x => x.OrderLineId == fixture.LineBId).ResendableQuantity);

        // EXACT replay answers the recorded claim and writes nothing.
        var replay = await lease.Source.ClaimAsync(fixture.Claim("tb265-a-1",
            (fixture.LineAId, 2m, DomainClaimCondition.Usable),
            (fixture.LineAId, 1m, DomainClaimCondition.Damaged)), CancellationToken.None);
        Assert.Equal(first.Claim.ClaimId, replay.Claim!.ClaimId);

        // Changed quantity/condition under the SAME occurrence key is a typed conflict with zero writes.
        var changed = await lease.Source.ClaimAsync(
            fixture.Claim("tb265-a-1", (fixture.LineAId, 1m, DomainClaimCondition.Usable)), CancellationToken.None);
        Assert.Null(changed.Claim);
        Assert.Equal("fulfillment.returns.claim_intent_conflict", changed.ConflictCode);

        // Over-claiming the free capacity is refused, even with a fresh key.
        var over = await lease.Source.ClaimAsync(
            fixture.Claim("tb265-a-2", (fixture.LineAId, 1m, DomainClaimCondition.Usable)), CancellationToken.None);
        Assert.Null(over.Claim);
        Assert.Equal("fulfillment.returns.claim_quantity_exceeded", over.ConflictCode);

        // A foreign/ambiguous source is refused before any row exists.
        var sibling = await lease.Source.ClaimAsync(
            fixture.Claim("tb265-a-3", (fixture.LineBId, 2m, DomainClaimCondition.Usable)), CancellationToken.None);
        Assert.Null(sibling.Claim);
        Assert.Equal("fulfillment.returns.source_line_missing", sibling.ConflictCode);

        var claims = await lease.Source.ListForShipmentAsync(fixture.SourceShipmentId, CancellationToken.None);
        Assert.Single(claims);
        Assert.Equal(0m, await StockOnHandAsync(fixture) - 7m);

        fixture.Record(
            _output,
            $"A fulfillment={fixture.FulfillmentId} shipment={fixture.SourceShipmentId} claim={first.Claim.ClaimId} "
            + $"status=Requested lines=2/1 resendableA=0 outstandingDamagedA=0 resendableB=1 claims=1 onHandDelta=0");
    }

    // ------------------------------------------------------------------ B: two contexts cannot overdraw one pool

    [Fact]
    public async Task B_two_independent_contexts_cannot_claim_the_same_quantity_twice()
    {
        if (Connection is null)
        {
            return;
        }

        var fixture = await CreateReturnedFixtureAsync("B", usableA: 2m, damagedA: 0m, dispatchA: 3m);

        await using var left = fixture.NewLease();
        await using var right = fixture.NewLease();

        var results = await Task.WhenAll(
            left.Source.ClaimAsync(fixture.Claim("tb265-b-left", (fixture.LineAId, 2m, DomainClaimCondition.Usable)),
                CancellationToken.None),
            right.Source.ClaimAsync(fixture.Claim("tb265-b-right", (fixture.LineAId, 2m, DomainClaimCondition.Usable)),
                CancellationToken.None));

        Assert.Single(results, x => x.Claim is not null);
        Assert.Single(results, x => x.Claim is null && x.ConflictCode == "fulfillment.returns.claim_quantity_exceeded");

        var claims = await left.Source.ListForShipmentAsync(fixture.SourceShipmentId, CancellationToken.None);
        Assert.Single(claims);
        Assert.Equal(2m, claims.Sum(x => x.Lines.Sum(l => l.Quantity)));

        fixture.Record(
            _output,
            $"B fulfillment={fixture.FulfillmentId} shipment={fixture.SourceShipmentId} "
            + $"winners=1 refusals=1 claimedTotal=2 free=0");
    }

    // ------------------------------------------------------------------ C: reject releases ONLY the branch claim

    [Fact]
    public async Task C_rejecting_a_requested_case_returns_only_its_branch_claim()
    {
        if (Connection is null)
        {
            return;
        }

        var fixture = await CreateReturnedFixtureAsync("C", usableA: 2m, damagedA: 1m, dispatchA: 3m);
        await using var lease = fixture.NewLease();

        var claim = await lease.Source.ClaimAsync(
            fixture.Claim("tb265-c-1", (fixture.LineAId, 1m, DomainClaimCondition.Usable),
                (fixture.LineAId, 1m, DomainClaimCondition.Damaged)), CancellationToken.None);
        Assert.NotNull(claim.Claim);

        var released = await lease.Source.ReleaseAsync(
            claim.Claim!.ClaimId, fixture.ActorUserId, "Rejected", CancellationToken.None);
        Assert.Equal(FulfillmentContracts.CourierReturnClaimStatus.Rejected, released.Claim!.Status);

        // Lawful capacity is available again — and ONLY the branch claim moved: the parcel receipt, the order hold
        // and the sibling line are untouched.
        var after = await lease.Directory.GetAsync(fixture.FulfillmentId, CancellationToken.None);
        Assert.Equal(2m, after!.Items.Single(x => x.OrderLineId == fixture.LineAId).ResendableQuantity);
        Assert.Equal(1m, after.Items.Single(x => x.OrderLineId == fixture.LineAId).OutstandingDamagedQuantity);
        Assert.Equal(0m, after.Items.Single(x => x.OrderLineId == fixture.LineBId).ResendableQuantity);

        // The released capacity is genuinely claimable again by a NEW reviewed intent.
        var again = await lease.Source.ClaimAsync(
            fixture.Claim("tb265-c-2", (fixture.LineAId, 1m, DomainClaimCondition.Usable)), CancellationToken.None);
        Assert.NotNull(again.Claim);

        fixture.Record(
            _output,
            $"C fulfillment={fixture.FulfillmentId} claim={claim.Claim!.ClaimId} status=Rejected "
            + $"resendableA=2 outstandingDamagedA=1 reClaim={again.Claim!.ClaimId}");
    }

    // ------------------------------------------------------------------ D: approval closes, releases once, disposes

    [Fact]
    public async Task D_approval_closes_the_line_obligation_releases_only_unconsumed_held_and_restocks_once()
    {
        if (Connection is null)
        {
            return;
        }

        var fixture = await CreateReturnedFixtureAsync("D", usableA: 1m, damagedA: 0m, dispatchA: 3m);
        await using var returns = fixture.NewReturns();

        var created = await returns.Directory.CreateCourierOriginAsync(
            fixture.FulfillmentId,
            fixture.SourceShipmentId,
            fixture.ActorUserId,
            $"tb265-d-1-{fixture.FulfillmentId:N}",
            $"courier-return:tb265-d-1-{fixture.FulfillmentId:N}",
            "بازگشت مرسوله",
            [(fixture.LineAId, 1m, ReturnItemCondition.Usable)],
            CancellationToken.None);

        Assert.True(created.Snapshot is not null, created.Conflict ?? "created snapshot missing");
        var caseId = created.Snapshot!.ReturnRequestId;
        Assert.Equal(ReturnOrigin.CourierOrigin, created.Snapshot.Origin);
        Assert.Equal(1000m, created.Snapshot.RefundAmount);

        // Creating the case moved no money and released no stock.
        Assert.Equal(0, returns.RefundCalls);
        Assert.Equal(0, returns.Restocked.Count);
        Assert.Equal(7m, await StockOnHandAsync(fixture));

        var approved = await returns.Directory.ApproveAsync(
            new ApproveReturnCommand(caseId, fixture.ActorUserId, fixture.SellerPartyId),
            CancellationToken.None);

        // Money ran once, through the existing attempt, and the closure phases are durable.
        Assert.Equal(1, returns.RefundCalls);
        Assert.Equal(ReturnRequestStatus.Completed, approved.Status);
        Assert.Equal(CourierOriginClosurePhase.RefundReleased, approved.ClosurePhase);
        Assert.NotNull(approved.ObligationClosedAt);
        Assert.NotNull(approved.StockSettledAt);

        // Consume is not release: the dispatched parcel's original sale stays Consumed and released NOTHING,
        // while the physically returned usable unit adds saleable stock exactly once through the return policy.
        Assert.Equal(0m, approved.ReleasedHeldQuantity);
        Assert.Equal(StockReservationStatus.Consumed, await ReservationStatusAsync(fixture, fixture.ReservationAId));
        Assert.Equal(7m, await StockOnHandAsync(fixture));
        Assert.Single(returns.Restocked);
        Assert.Equal(1m, returns.Restocked[0].Quantity);
        var item = Assert.Single(approved.Items);
        Assert.Equal(ReturnItemDisposition.SaleableRestock, item.Disposition);

        // A replay can never refund or restock a second time.
        await Assert.ThrowsAnyAsync<Exception>(() => returns.Directory.ApproveAsync(
            new ApproveReturnCommand(caseId, fixture.ActorUserId, fixture.SellerPartyId),
            CancellationToken.None));
        Assert.Equal(1, returns.RefundCalls);
        Assert.Single(returns.Restocked);
        Assert.Equal(7m, await StockOnHandAsync(fixture));

        fixture.Record(
            _output,
            $"D fulfillment={fixture.FulfillmentId} case={caseId} status=Completed phase=RefundReleased "
            + $"releasedHeld=0 refunds=1 restocks=1 disposition=SaleableRestock onHand=7 reservation=Consumed");
    }

    // ------------------------------------------------------------------ E: deliberate redirect of a recorded replacement

    [Fact]
    public async Task E_a_recorded_created_replacement_is_redirected_into_the_case_and_released_once()
    {
        if (Connection is null)
        {
            return;
        }

        var fixture = await CreateReturnedFixtureAsync("E", usableA: 0m, damagedA: 1m, dispatchA: 3m);
        
        await using var returns = fixture.NewReturns();

        var replacement = await returns.Fulfillment.ResolveReplacementAsync(fixture);
        
        Assert.NotNull(replacement.ReplacementShipmentId);
        Assert.NotNull(replacement.ReplacementReservationId);
        var replacementShipmentId = replacement.ReplacementShipmentId!.Value;
        var replacementReservationId = replacement.ReplacementReservationId!.Value;

        // The recorded decision keeps its unit committed, so ordinary replacement capacity is 0 — but a
        // not-yet-dispatched replacement is deliberately redirectable into the existing returns workflow.
        var beforeClaim = await returns.Fulfillment.Directory.GetAsync(fixture.FulfillmentId, CancellationToken.None);
        Assert.Equal(0m, beforeClaim!.Items.Single(x => x.OrderLineId == fixture.LineAId).OutstandingDamagedQuantity);
        Assert.Equal(7m, await StockOnHandAsync(fixture));

        var created = await returns.Directory.CreateCourierOriginAsync(
            fixture.FulfillmentId,
            fixture.SourceShipmentId,
            fixture.ActorUserId,
            $"tb265-e-1-{fixture.FulfillmentId:N}",
            $"courier-return:tb265-e-1-{fixture.FulfillmentId:N}",
            "تغییر مسیر به پروندهٔ مرجوعی",
            [(fixture.LineAId, 1m, ReturnItemCondition.Damaged)],
            CancellationToken.None);
        Assert.True(created.Snapshot is not null, created.Conflict ?? "created snapshot missing");

        // The retained replacement hold still belongs to the active order until approval closes that branch.
        Assert.Equal(StockReservationStatus.Held, await ReservationStatusAsync(fixture, replacementReservationId));
        Assert.Equal(0, returns.RefundCalls);

        var approved = await returns.Directory.ApproveAsync(
            new ApproveReturnCommand(created.Snapshot!.ReturnRequestId, fixture.ActorUserId, fixture.SellerPartyId),
            CancellationToken.None);

        // The switch is audited: the Created replacement was cancelled rather than left dispatchable, and only the
        // UNCONSUMED held quantity was released — once.
        Assert.Equal(ReturnRequestStatus.Completed, approved.Status);
        Assert.Equal(1m, approved.ReleasedHeldQuantity);
        Assert.Equal(StockReservationStatus.Released, await ReservationStatusAsync(fixture, replacementReservationId));
        Assert.Equal(7m, await StockOnHandAsync(fixture));
        var cancelled = await ShipmentStatusAsync(fixture, replacementShipmentId);
        Assert.Equal(ShipmentStatus.Cancelled, cancelled);

        // Damage is never saleable: no restock call at all, and the disposition is explicitly non-saleable.
        Assert.Empty(returns.Restocked);
        Assert.Equal(ReturnItemDisposition.DamagedNonSaleable, Assert.Single(approved.Items).Disposition);

        // Durable replay: nothing is released or refunded twice.
        await Assert.ThrowsAnyAsync<Exception>(() => returns.Directory.ApproveAsync(
            new ApproveReturnCommand(approved.ReturnRequestId, fixture.ActorUserId, fixture.SellerPartyId),
            CancellationToken.None));
        Assert.Equal(1, returns.RefundCalls);
        Assert.Equal(7m, await StockOnHandAsync(fixture));
        Assert.Equal(1m, approved.ReleasedHeldQuantity);

        fixture.Record(
            _output,
            $"E fulfillment={fixture.FulfillmentId} case={approved.ReturnRequestId} replacementParcel={replacementShipmentId} "
            + $"cancelled=Cancelled releasedHeld=1 heldReservation=Released damagedDisposition=DamagedNonSaleable "
            + $"restocks=0 onHand=7 refunds=1");
    }

    // ------------------------------------------------------------------ F: a dispatched replacement is not stock

    [Fact]
    public async Task F_an_already_dispatched_replacement_can_never_be_redirected_or_refunded_as_unused()
    {
        if (Connection is null)
        {
            return;
        }

        var fixture = await CreateReturnedFixtureAsync("F", usableA: 0m, damagedA: 1m, dispatchA: 3m);
        await using var returns = fixture.NewReturns();

        var replacement = await returns.Fulfillment.ResolveReplacementAsync(fixture);
        await returns.Fulfillment.Directory.AssignTrackingAsync(
            fixture.FulfillmentId, replacement.ReplacementShipmentId!.Value, fixture.ActorUserId,
            $"TRK-TB265-F-{replacement.ReplacementShipmentId:N}", CancellationToken.None);
        await returns.Fulfillment.Directory.DispatchShipmentAsync(
            fixture.FulfillmentId, replacement.ReplacementShipmentId.Value, fixture.ActorUserId, CancellationToken.None);

        // No unused held stock remains, so the case cannot claim that unit: the precise blocker names the live
        // replacement instead of pretending the parcel was never sent.
        var claim = await returns.Lease.Source.ClaimAsync(
            fixture.Claim("tb265-f-1", (fixture.LineAId, 1m, DomainClaimCondition.Damaged)), CancellationToken.None);
        Assert.Null(claim.Claim);
        Assert.Equal("fulfillment.returns.replacement_already_dispatched", claim.ConflictCode);

        // The replacement's own reservation is Consumed by dispatch and never released.
        Assert.Equal(StockReservationStatus.Consumed, await ReservationStatusAsync(fixture, replacement.ReplacementReservationId!.Value));
        Assert.Equal(ShipmentStatus.Dispatched, await ShipmentStatusAsync(fixture, replacement.ReplacementShipmentId.Value));

        fixture.Record(
            _output,
            $"F fulfillment={fixture.FulfillmentId} replacementParcel={replacement.ReplacementShipmentId} status=Dispatched "
            + $"claimRefused=replacement_already_dispatched reservation=Consumed releasedHeld=0");
    }

    // ================================================================== fixture and helpers

    private sealed record Fixture(
        string Connection,
        Guid FulfillmentId,
        Guid CheckoutId,
        Guid SellerOrderId,
        Guid SellerPartyId,
        Guid LineAId,
        Guid LineBId,
        Guid SourceShipmentId,
        Guid ActorUserId,
        Guid StockItemId,
        Guid OfferId,
        Guid ReservationAId)
    {
        public FulfillmentContracts.CreateCourierReturnClaimCommand Claim(
            string occurrenceKey,
            params (Guid LineId, decimal Quantity, DomainClaimCondition Condition)[] lines) =>
            new(
                FulfillmentId,
                SourceShipmentId,
                ActorUserId,
                occurrenceKey,
                lines.Select(x => new FulfillmentContracts.CourierReturnClaimLine(x.LineId, x.Quantity,
                    (FulfillmentContracts.CourierReturnClaimCondition)(int)x.Condition)).ToList());

        public Lease NewLease()
        {
            var fulfillmentDb = CreateFulfillmentDb(Connection);
            var inventoryDb = CreateInventoryDb(Connection);
            var inventory = NewInventoryDirectory(inventoryDb);
            var gateway = new FulfillmentInventoryGateway(
                inventory, new OrderInventoryLifecycleAdapter(inventory), inventory);
            var directory = new FulfillmentDirectory(
                fulfillmentDb,
                new OpenFulfillmentUseCaseGuard(),
                new UnusedOrderReader(),
                gateway,
                new FulfillmentInstrumentation(),
                new SystemUtcClock(),
                new UuidV7IdGenerator());
            var release = new InventoryReturnObligationRelease(
                inventoryDb, new OpenInventoryGuard(), new SystemUtcClock());
            var source = new CourierReturnSourceDirectory(
                fulfillmentDb, new OpenFulfillmentUseCaseGuard(), release, new SystemUtcClock(), new UuidV7IdGenerator());
            return new Lease(fulfillmentDb, inventoryDb, directory, source);
        }

        public ReturnsLease NewReturns()
        {
            var lease = NewLease();
            var returnsDb = CreateReturnsDb(Connection);
            var inventoryDb = CreateInventoryDb(Connection);
            var inventory = NewInventoryDirectory(inventoryDb);
            var release = new InventoryReturnObligationRelease(
                inventoryDb, new OpenInventoryGuard(), new SystemUtcClock());
            var source = new CourierReturnSourceDirectory(
                CreateFulfillmentDb(Connection), new OpenFulfillmentUseCaseGuard(), release,
                new SystemUtcClock(), new UuidV7IdGenerator());
            var recorder = new RecordingReturnInventoryGateway();
            var payments = new ControlledPayment();
            var refunds = new ControlledRefundGateway();
            var fulfillmentAdmin = new FixtureFulfillmentAdmin(this, lease.Directory);
            var directory = new ReturnDirectory(
                returnsDb,
                new OpenReturnGuard(),
                new FixtureOrderReader(this),
                new UnusedEligibility(),
                payments,
                refunds,
                new UnusedWallet(),
                recorder,
                new ReturnsInstrumentation(),
                NullLogger<ReturnDirectory>.Instance,
                new SystemUtcClock(),
                new UuidV7IdGenerator(),
                source,
                new OrderReturnObligationClosure(new FixtureOrderReader(this)),
                fulfillmentAdmin);
            return new ReturnsLease(lease, returnsDb, inventoryDb, directory, recorder, refunds, source, fulfillmentAdmin);
        }

        public void Record(ITestOutputHelper output, string line)
        {
            output.WriteLine(line);
            var dir = Environment.GetEnvironmentVariable("TB265_EVIDENCE_DIR");
            if (string.IsNullOrWhiteSpace(dir))
            {
                return;
            }

            Directory.CreateDirectory(dir);
            File.AppendAllText(Path.Combine(dir, "tb265-relational.txt"), line + Environment.NewLine);
        }
    }

    private sealed class Lease(
        FulfillmentDbContext fulfillmentDb,
        InventoryDbContext inventoryDb,
        FulfillmentDirectory directory,
        CourierReturnSourceDirectory source) : IAsyncDisposable
    {
        public FulfillmentDirectory Directory { get; } = directory;
        public CourierReturnSourceDirectory Source { get; } = source;

        public async ValueTask DisposeAsync()
        {
            await inventoryDb.DisposeAsync();
            await fulfillmentDb.DisposeAsync();
        }
    }

    private sealed class ReturnsLease(
        Lease lease,
        ReturnsDbContext returnsDb,
        InventoryDbContext inventoryDb,
        ReturnDirectory directory,
        RecordingReturnInventoryGateway recorder,
        ControlledRefundGateway refunds,
        CourierReturnSourceDirectory source,
        FixtureFulfillmentAdmin fulfillment) : IAsyncDisposable
    {
        public Lease Lease { get; } = lease;
        public ReturnDirectory Directory { get; } = directory;
        public RecordingReturnInventoryGateway Recorder { get; } = recorder;
        public ControlledRefundGateway Refunds { get; } = refunds;
        public CourierReturnSourceDirectory Source { get; } = source;

        public FixtureFulfillmentAdmin Fulfillment { get; } = fulfillment;
        public IReadOnlyList<(Guid ReservationId, decimal Quantity)> Restocked => Recorder.Calls;
        public int RefundCalls => Refunds.Calls;

        public async ValueTask DisposeAsync()
        {
            await returnsDb.DisposeAsync();
            await inventoryDb.DisposeAsync();
            await lease.DisposeAsync();
        }
    }

    private const string PostMetadataJson =
        "{\"recipientName\":\"TB265 receiver\",\"recipientPhone\":\"09120000000\","
        + "\"postalCode\":\"1234567890\",\"destinationAddress\":\"TB265 task address\"}";

    /// <summary>
    /// One paid-order unit whose parcel was dispatched, refused, returned and physically received at origin with the
    /// requested split of line A. Everything comes from the production domain/application paths.
    /// </summary>
    private static async Task<Fixture> CreateReturnedFixtureAsync(
        string tag,
        decimal usableA,
        decimal damagedA,
        decimal dispatchA)
    {
        var connection = Connection!;
        var fulfillmentId = Guid.NewGuid();
        var checkoutId = Guid.NewGuid();
        var sellerOrderId = Guid.NewGuid();
        var sellerPartyId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var lineA = Guid.NewGuid();
        var lineB = Guid.NewGuid();
        var stockItemId = Guid.NewGuid();
        var offerId = Guid.NewGuid();
        var reservationA = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 11, 13, 6, 0, 0, TimeSpan.Zero);

        await using (var inventoryDb = CreateInventoryDb(connection))
        {
            var position = StockPosition.Open(stockItemId, offerId, Guid.NewGuid(), Guid.NewGuid(), now);
            position.SyncQuantities(10m, 3m, now);
            var sale = StockReservation.Hold(
                reservationA, stockItemId, dispatchA, $"TB265-{tag} sale", $"tb265-sale-{reservationA:N}", now, null);
            inventoryDb.Positions.Add(position);
            inventoryDb.Reservations.Add(sale);
            await inventoryDb.SaveChangesAsync();
        }

        await using (var db = CreateFulfillmentDb(connection))
        {
            var unit = FulfillmentUnit.CreateFromPaidOrder(
                fulfillmentId,
                Guid.NewGuid,
                sellerOrderId,
                checkoutId,
                sellerPartyId,
                Guid.NewGuid(),
                $"TB265-{tag} گیرنده",
                "09120000000",
                "تهران",
                "تهران",
                $"نشانی آزمون تسک 265 {tag}",
                "1234567890",
                "post",
                "پیشتاز",
                [(lineA, 3m, (Guid?)reservationA), (lineB, 1m, (Guid?)null)],
                now,
                [
                    new FulfillmentShippingGroupPlan(
                        groupId, sellerPartyId, "post", null, null, "پیشتاز", null,
                        DateOnly.FromDateTime(now.UtcDateTime), null, null, 1, [lineA, lineB]),
                ]);
            db.Fulfillments.Add(unit);
            db.Items.AddRange(unit.Items);
            db.ShippingGroups.AddRange(unit.ShippingGroups);
            await db.SaveChangesAsync();
        }

        var fixture = new Fixture(
            connection, fulfillmentId, checkoutId, sellerOrderId, sellerPartyId, lineA, lineB,
            Guid.Empty, actorUserId, stockItemId, offerId, reservationA);

        await using (var seeded = fixture.NewLease())
        {
            await seeded.Directory.MarkProcessingAsync(fulfillmentId, actorUserId, CancellationToken.None);
            await seeded.Directory.PackSelectionsAsync(
                fulfillmentId, actorUserId,
                [new AppModels.FulfillmentSelectionCommand(lineA, dispatchA)],
                CancellationToken.None);
            var created = await seeded.Directory.CreateShipmentAsync(
                fulfillmentId, actorUserId, "پست",
                [new AppModels.ShipmentLineCommand(lineA, dispatchA)],
                CancellationToken.None,
                shippingMethodCode: "post",
                providerMetadataJson: PostMetadataJson,
                shippingGroupId: groupId);
            var shipmentId = created.CreatedShipmentId;
            await seeded.Directory.AssignTrackingAsync(
                fulfillmentId, shipmentId, actorUserId, $"TRK-TB265-{tag}-{shipmentId:N}", CancellationToken.None);
            await seeded.Directory.DispatchShipmentAsync(
                fulfillmentId, shipmentId, actorUserId, CancellationToken.None);
            await seeded.Directory.RecordNonDeliveryAsync(
                fulfillmentId, shipmentId, actorUserId, "customer_unavailable", $"TB265-{tag}", CancellationToken.None);
            await seeded.Directory.StartReturnAsync(
                fulfillmentId, shipmentId, actorUserId, CancellationToken.None);

            var lines = new List<AppModels.ReturnReceiptLineCommand>();
            if (usableA > 0m)
            {
                lines.Add(new AppModels.ReturnReceiptLineCommand(lineA, usableA, Tooba.Fulfillment.Domain.ValueObjects.ReturnReceiptCondition.Usable));
            }

            if (damagedA > 0m)
            {
                lines.Add(new AppModels.ReturnReceiptLineCommand(lineA, damagedA, Tooba.Fulfillment.Domain.ValueObjects.ReturnReceiptCondition.Damaged));
            }

            await seeded.Directory.RecordReturnReceiptAsync(
                fulfillmentId, shipmentId, actorUserId, [.. lines], $"TB265-{tag} receipt", CancellationToken.None);

            return fixture with { SourceShipmentId = shipmentId };
        }
    }

    private static ReturnsDbContext CreateReturnsDb(string connectionString)
    {
        var options = new DbContextOptionsBuilder<ReturnsDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            options, connectionString, ReturnsDbContext.Schema, typeof(ReturnsDbContext));
        return new ReturnsDbContext(options.Options);
    }

    private static InventoryDbContext CreateInventoryDb(string connectionString)
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            options, connectionString, InventoryDbContext.Schema, typeof(InventoryDbContext));
        return new InventoryDbContext(options.Options);
    }

    private static FulfillmentDbContext CreateFulfillmentDb(string connectionString)
    {
        var options = new DbContextOptionsBuilder<FulfillmentDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            options, connectionString, FulfillmentDbContext.Schema, typeof(FulfillmentDbContext));
        return new FulfillmentDbContext(options.Options);
    }

    private static InventoryDirectory NewInventoryDirectory(InventoryDbContext db) =>
        new(
            db,
            new OpenInventoryGuard(),
            new UnusedOfferLookup(),
            new UnusedCatalogLookup(),
            new SystemUtcClock(),
            new UuidV7IdGenerator(),
            new ModuleCallTracer());

    private static async Task<decimal> StockOnHandAsync(Fixture fixture)
    {
        await using var db = CreateInventoryDb(fixture.Connection);
        return await db.Positions.AsNoTracking()
            .Where(x => x.StockItemId == fixture.StockItemId)
            .Select(x => x.OnHand)
            .SingleAsync();
    }

    private static async Task<StockReservationStatus> ReservationStatusAsync(Fixture fixture, Guid reservationId)
    {
        await using var db = CreateInventoryDb(fixture.Connection);
        return await db.Reservations.AsNoTracking()
            .Where(x => x.ReservationId == reservationId)
            .Select(x => x.Status)
            .SingleAsync();
    }

    private static async Task<ShipmentStatus> ShipmentStatusAsync(Fixture fixture, Guid shipmentId)
    {
        await using var db = CreateFulfillmentDb(fixture.Connection);
        return await db.Shipments.AsNoTracking()
            .Where(x => x.ShipmentId == shipmentId)
            .Select(x => x.Status)
            .SingleAsync();
    }

    // ------------------------------------------------------------------ controlled seams (disclosed)

    private sealed class RecordingReturnInventoryGateway : IReturnInventoryGateway
    {
        public List<(Guid ReservationId, decimal Quantity)> Calls { get; } = [];

        public Task RestockConsumedReservationAsync(
            Guid reservationId, decimal quantity, string idempotencyKey, CancellationToken cancellationToken)
        {
            Calls.Add((reservationId, quantity));
            return Task.CompletedTask;
        }
    }

    private sealed class ControlledRefundGateway : IPaymentRefundGateway
    {
        public int Calls { get; private set; }

        public Task<GatewayRefundResult> RefundAsync(
            Guid paymentId, decimal amount, string currency, string idempotencyKey, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(new GatewayRefundResult(true, $"tb265-refund-{Calls}", null));
        }
    }

    private sealed class ControlledPayment : IPaymentReturnReader
    {
        public Task<PaymentReturnSnapshot?> GetAsync(
            Guid paymentId, Guid actorUserId, Guid? buyerPartyId, CancellationToken cancellationToken) =>
            Task.FromResult<PaymentReturnSnapshot?>(new PaymentReturnSnapshot(paymentId, Guid.Empty, 1000m, "IRR", "Succeeded"));

        public Task<PaymentReturnSnapshot?> GetLatestForCheckoutAsync(
            Guid checkoutId, Guid actorUserId, Guid? buyerPartyId, CancellationToken cancellationToken) =>
            Task.FromResult<PaymentReturnSnapshot?>(new PaymentReturnSnapshot(Guid.NewGuid(), checkoutId, 1000m, "IRR", "Succeeded"));
    }

    private sealed class UnusedWallet : IWalletRefundCreditPort
    {
        public Task<WalletRefundCreditResultDto> CreditRefundAsync(
            Guid customerActorId, decimal amount, string currency, Guid returnRequestId, string idempotencyKey,
            CancellationToken cancellationToken) =>
            Task.FromResult(new WalletRefundCreditResultDto(amount, false));
    }

    private sealed class FixtureOrderReader(Fixture fixture) : IOrderReturnReader
    {
        public Task<OrderReturnContextSnapshot?> GetReturnContextAsync(
            Guid sellerOrderId, CancellationToken cancellationToken) =>
            Task.FromResult<OrderReturnContextSnapshot?>(new OrderReturnContextSnapshot(
                fixture.SellerOrderId,
                fixture.CheckoutId,
                fixture.SellerPartyId,
                Guid.NewGuid(),
                true,
                "IRR",
                [
                    new OrderReturnLineSnapshot(fixture.LineAId, 3m, 1000m, "IRR", fixture.ReservationAId),
                    new OrderReturnLineSnapshot(fixture.LineBId, 1m, 500m, "IRR", null),
                ]));
    }

    private sealed class FixtureFulfillmentAdmin(Fixture fixture, FulfillmentDirectory directory) : IFulfillmentAdminOperations
    {
        public FulfillmentDirectory Directory { get; } = directory;

        public async Task<Tooba.Fulfillment.Application.Fulfillments.Models.FulfillmentDamagedResolution> ResolveReplacementAsync(Fixture f)
        {
            var snapshot = await Directory.GetAsync(f.FulfillmentId, CancellationToken.None);
            var shipment = snapshot!.Shipments.Single(x => x.ShipmentId == f.SourceShipmentId);
            var line = shipment.Items.Single(x => x.OrderLineId == f.LineAId);
            return await Directory.ResolveDamagedGoodsAsync(
                f.FulfillmentId,
                f.ActorUserId,
                f.SourceShipmentId,
                f.LineAId,
                line.OutstandingDamagedQuantity,
                Tooba.Fulfillment.Domain.ValueObjects.DamagedResolutionOutcome.Replacement,
                $"tb265-replacement-{f.SourceShipmentId:N}",
                null,
                CancellationToken.None);
        }

        public Task<FulfillmentSnapshot?> GetAsync(Guid fulfillmentId, CancellationToken cancellationToken) =>
            Task.FromResult<FulfillmentSnapshot?>(Snapshot());

        private FulfillmentSnapshot Snapshot() =>
            new(
                fixture.FulfillmentId,
                fixture.SellerOrderId,
                fixture.CheckoutId,
                fixture.SellerPartyId,
                FulfillmentOperationStatus.InTransit,
                "r",
                "09120000000",
                "تهران",
                "تهران",
                "a",
                "1234567890",
                "post",
                "پست",
                [new FulfillmentItemSnapshot(fixture.LineAId, Guid.NewGuid(), 3m, 0m, null, 0m, 0m)],
                []);

        public Task<IReadOnlyList<FulfillmentSnapshot>> ListForCheckoutAsync(Guid checkoutId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task<IReadOnlyList<ConsolidatedPackageSnapshot>> GetPackagesForCheckoutAsync(Guid checkoutId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task<IReadOnlyList<ActivePackageMembershipSnapshot>> GetActiveMembershipByShipmentIdsAsync(IReadOnlyList<Guid> shipmentIds, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public IReadOnlyList<ShippingMethodOption> ListEnabledShippingMethods() => throw new NotSupportedException();
        public string ResolveShippingMethodLabel(string? code, string? fallbackDisplayName) => throw new NotSupportedException();
        public Task<FulfillmentSnapshot> MarkProcessingAsync(Guid fulfillmentId, Guid actorUserId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<FulfillmentSnapshot> ProcessSelectionsAsync(Guid fulfillmentId, Guid actorUserId, IReadOnlyList<FulfillmentSelectionCommand> selections, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<FulfillmentSnapshot> UnprocessSelectionsAsync(Guid fulfillmentId, Guid actorUserId, IReadOnlyList<FulfillmentSelectionCommand> selections, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<FulfillmentSnapshot> PackSelectionsAsync(Guid fulfillmentId, Guid actorUserId, IReadOnlyList<FulfillmentSelectionCommand> selections, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<FulfillmentSnapshot> UnpackSelectionsAsync(Guid fulfillmentId, Guid actorUserId, IReadOnlyList<FulfillmentSelectionCommand> selections, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<FulfillmentShipmentCreateResult> CreateShipmentAsync(Guid fulfillmentId, Guid actorUserId, string carrierDisplayName, IReadOnlyList<ShipmentLineCommand> items, string? shippingMethodCode, string? providerMetadataJson, CancellationToken cancellationToken, Guid? shippingGroupId = null, string? shippingDispatchKey = null) => throw new NotSupportedException();
        public Task<FulfillmentSnapshot> CancelShipmentAsync(Guid fulfillmentId, Guid shipmentId, Guid actorUserId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<FulfillmentSnapshot> AssignTrackingAsync(Guid fulfillmentId, Guid shipmentId, Guid actorUserId, string trackingReference, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<FulfillmentSnapshot> AssignAutomaticTrackingAsync(Guid fulfillmentId, Guid shipmentId, Guid actorUserId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<FulfillmentSnapshot> CorrectTrackingAsync(Guid fulfillmentId, Guid shipmentId, Guid actorUserId, string trackingReference, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<FulfillmentSnapshot> DeliverShipmentAsync(Guid fulfillmentId, Guid shipmentId, Guid actorUserId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<FulfillmentSnapshot> RecordNonDeliveryAsync(Guid fulfillmentId, Guid shipmentId, Guid actorUserId, string reason, string? note, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<FulfillmentSnapshot> StartReturnAsync(Guid fulfillmentId, Guid shipmentId, Guid actorUserId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<FulfillmentSnapshot> RecordReturnReceiptAsync(Guid fulfillmentId, Guid shipmentId, Guid actorUserId, IReadOnlyList<Tooba.Fulfillment.Contracts.Operations.ReturnReceiptLineCommand> items, string? note, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<FulfillmentShipmentCreateResult> CreateResendShipmentAsync(Guid fulfillmentId, Guid actorUserId, Guid originalShipmentId, IReadOnlyList<ShipmentLineCommand> items, CancellationToken cancellationToken, string? shippingDispatchKey = null) => throw new NotSupportedException();
        public Task<Tooba.Fulfillment.Contracts.Operations.DamagedResolutionResult> ResolveDamagedGoodsAsync(Guid fulfillmentId, Guid actorUserId, Guid shipmentId, Guid orderLineId, decimal quantity, Tooba.Fulfillment.Contracts.Operations.DamagedResolutionOutcome outcome, string? occurrenceKey, string? note, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<PostDispatchIntentResult> ReconcilePostDispatchIntentAsync(PostDispatchIntent intent, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<DamagedResolutionIntentResult> ReconcileDamagedResolutionAsync(DamagedResolutionIntent intent, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<FulfillmentSnapshot> DispatchShipmentAsync(Guid fulfillmentId, Guid shipmentId, Guid actorUserId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<ConsolidatedPackageSnapshot> CreateConsolidatedPackageAsync(Guid checkoutId, IReadOnlyList<Guid> shipmentIds, string? shippingMethodCode, string? trackingReference, string? note, Guid actorUserId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<ConsolidatedPackageSnapshot> CancelConsolidatedPackageAsync(Guid consolidatedPackageId, Guid actorUserId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<ConsolidatedPackageSnapshot> AssignConsolidatedPackageTrackingAsync(Guid consolidatedPackageId, string trackingReference, Guid actorUserId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<ConsolidatedPackageSnapshot> DispatchConsolidatedPackageAsync(Guid consolidatedPackageId, Guid actorUserId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<ConsolidatedPackageSnapshot> DeliverConsolidatedPackageAsync(Guid consolidatedPackageId, Guid actorUserId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task AbortForCheckoutCancelAsync(Guid checkoutId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task ReactivateAfterOrderRestoreAsync(Guid checkoutId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task EnsureCreatedForPaidCheckoutAsync(Guid checkoutId, IReadOnlyList<Guid> sellerOrderIds, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task VoidUnstartedForCheckoutAsync(Guid checkoutId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task RebindActiveReservationsFromOrderAsync(Guid checkoutId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyDictionary<Guid, decimal>> GetShippedByOrderLineIdsForCheckoutsAsync(IReadOnlyList<Guid> checkoutIds, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class OpenFulfillmentUseCaseGuard : IFulfillmentUseCaseGuard
    {
        public Task EnsureCanMutateAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class OpenInventoryGuard : IInventoryUseCaseGuard
    {
        public Task EnsureCanMutateAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class OpenReturnGuard : IReturnUseCaseGuard
    {
        public Task EnsureCanMutateAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class UnusedEligibility : IReturnEligibilityEvaluator
    {
        public Task<ReturnEligibilityResult> EvaluateAsync(Guid sellerOrderId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class UnusedOfferLookup : IOfferLookupGateway
    {
        public Task<OfferReference?> FindOfferAsync(Guid offerId, CancellationToken cancellationToken) =>
            Task.FromResult<OfferReference?>(null);

        public Task<IReadOnlyDictionary<Guid, OfferReference>> FindOffersBatchAsync(
            IReadOnlyCollection<Guid> offerIds, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, OfferReference>>(new Dictionary<Guid, OfferReference>());

        public Task<IReadOnlyDictionary<Guid, int>> CountOffersByCatalogVariantIdsAsync(
            IReadOnlyCollection<Guid> catalogVariantIds, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, int>>(new Dictionary<Guid, int>());
    }

    private sealed class UnusedCatalogLookup : ICatalogVariantLookup
    {
        public Task<CatalogVariantLookupResult?> FindVariantAsync(Guid variantId, CancellationToken cancellationToken) =>
            Task.FromResult<CatalogVariantLookupResult?>(null);

        public Task<IReadOnlyDictionary<Guid, Guid?>> GetPrimaryCategoryIdsByVariantIdsAsync(
            IReadOnlyList<Guid> variantIds, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, Guid?>>(new Dictionary<Guid, Guid?>());

        public Task<IReadOnlyDictionary<Guid, string>> GetVariantTitlesAsync(
            IReadOnlyList<Guid> variantIds, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, string>>(new Dictionary<Guid, string>());

        public Task<IReadOnlyList<Guid>> GetPublishedVariantIdsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);
    }

    private sealed class UnusedOrderReader : IOrderFulfillmentReader
    {
        public Task<OrderFulfillmentHandoffSnapshot?> GetHandoffAsync(
            Guid sellerOrderId, CancellationToken cancellationToken) =>
            Task.FromResult<OrderFulfillmentHandoffSnapshot?>(null);

        public Task<OrderFulfillmentHandoffSnapshot?> GetHandoffForCheckoutAsync(
            Guid checkoutId, Guid actorUserId, CancellationToken cancellationToken) =>
            Task.FromResult<OrderFulfillmentHandoffSnapshot?>(null);
    }
}
