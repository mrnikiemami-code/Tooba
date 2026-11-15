using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Application.Fulfillments.Ports;
using Tooba.Fulfillment.Contracts.Operations;
using Tooba.Fulfillment.Domain.ValueObjects;
using Tooba.Fulfillment.Infrastructure.Directories;
using Tooba.Fulfillment.Infrastructure.Observability;
using Tooba.Fulfillment.Infrastructure.Persistence;
using Tooba.Order.Contracts.Fulfillment;
using Xunit;
using AppModels = Tooba.Fulfillment.Application.Fulfillments.Models;
using DamagedResolutionOutcome = Tooba.Fulfillment.Domain.ValueObjects.DamagedResolutionOutcome;
using DamagedResolutionStatus = Tooba.Fulfillment.Domain.ValueObjects.DamagedResolutionStatus;
using ReturnReceiptCondition = Tooba.Fulfillment.Domain.ValueObjects.ReturnReceiptCondition;

namespace Tooba.Fulfillment.Tests.Behavior;

/// <summary>
/// TB-270-R3 Repair C — a replacement parcel is "ready" only while Inventory still holds its OWN purpose-bound
/// reservation for the decision's exact line and quantity.
/// </summary>
/// <remarks>
/// <para>
/// The cases run the REAL owning reader (<see cref="FulfillmentDirectory.GetAsync(Guid, Guid, CancellationToken)"/>)
/// over a replacement that was created through the REAL owning command
/// (<see cref="FulfillmentDirectory.ResolveDamagedGoodsAsync"/>), so the decision/parcel/hold chain is produced by
/// production code rather than assembled by the test.
/// </para>
/// <para>
/// Inventory is the owner of the physical hold, so the test controls only that owning port's answer: a projected
/// decision status is never treated as readiness. Reading is asserted to write nothing.
/// </para>
/// </remarks>
public sealed class ReplacementReadiness270R3Tests
{
    private static readonly Guid Checkout = Guid.Parse("01a1d273-0000-7000-8000-000000000001");
    private static readonly Guid SellerOrder = Guid.Parse("01a1d273-0000-7000-8000-000000000002");
    private static readonly Guid SellerParty = Guid.Parse("01a1d273-0000-7000-8000-000000000003");
    private static readonly Guid Line = Guid.Parse("01a1d273-0000-7000-8000-000000000004");
    private static readonly Guid ShippingGroup = Guid.Parse("01a1d273-0000-7000-8000-000000000020");

    /// <summary>The committed SALE reservation of the ordered line; Inventory derives the offer identity from it.</summary>
    private static readonly Guid SaleReservation = Guid.Parse("01a1d273-0000-7000-8000-000000000030");

    [Fact]
    public async Task A_held_reservation_for_the_exact_decision_line_and_quantity_is_ready()
    {
        await using var db = CreateDb();
        var inventory = new HoldInventory();
        var directory = CreateDirectory(db, inventory);
        var seeded = await SeedReplacementAsync(directory, inventory);

        // The seed itself legitimately dispatches the ORIGINAL parcel (which consumes the sale reservation) and
        // reserves the replacement hold. Read-only means the READ adds nothing on top of that, so the baseline is
        // taken after seeding and compared after the read.
        var reserved = inventory.ReserveCalls;
        var released = inventory.ReleaseCalls;
        var consumed = inventory.ConsumeCalls;

        var fact = await directory.GetAsync(seeded.FulfillmentId, seeded.ReplacementShipmentId, CancellationToken.None);

        Assert.True(fact.IsReady, fact.ReasonCode);
        Assert.Equal(FulfillmentReplacementReadinessReasonCodes.Ready, fact.ReasonCode);
        Assert.Equal(seeded.FulfillmentId, fact.FulfillmentId);
        Assert.Equal(seeded.ReplacementShipmentId, fact.ShipmentId);
        Assert.Equal(seeded.SourceShipmentId, fact.SourceShipmentId);
        Assert.Equal(seeded.DecisionId, fact.DamagedResolutionId);
        Assert.Equal(seeded.ReservationId, fact.ReplacementReservationId);
        Assert.Equal(Line, fact.OrderLineId);
        Assert.Equal(1m, fact.Quantity);
        Assert.NotNull(fact.ReadyAtUtc);

        // Read-only: the owning read neither reserves/releases nor consumes, and it writes no row. The unit loader
        // tracks the aggregate it read (pre-existing behaviour, no SaveChanges), so "no write" is asserted as zero
        // added/deleted rows plus unchanged durable values — never as "the change tracker is empty".
        Assert.Equal(reserved, inventory.ReserveCalls);
        Assert.Equal(released, inventory.ReleaseCalls);
        Assert.Equal(consumed, inventory.ConsumeCalls);
        Assert.Equal(0, db.ChangeTracker.Entries().Count(x => x.State is EntityState.Added or EntityState.Deleted));
        var replacement = await db.Shipments.AsNoTracking()
            .SingleAsync(x => x.ShipmentId == seeded.ReplacementShipmentId);
        Assert.Equal(Domain.ValueObjects.ShipmentStatus.Created, replacement.Status);
        Assert.Null(replacement.DispatchedAt);
        var decision = await db.DamagedResolutions.AsNoTracking()
            .SingleAsync(x => x.DamagedGoodsResolutionId == seeded.DecisionId);
        Assert.Equal(DamagedResolutionStatus.Fulfilled, decision.Status);
    }

    [Fact]
    public async Task A_released_or_consumed_hold_is_never_reported_as_ready()
    {
        await using var db = CreateDb();
        var inventory = new HoldInventory();
        var directory = CreateDirectory(db, inventory);
        var seeded = await SeedReplacementAsync(directory, inventory);
        var releasedCallsAfterSeed = inventory.ReleaseCalls;
        var consumeCallsAfterSeed = inventory.ConsumeCalls;

        // Released: Inventory no longer answers a reservation for this identity.
        inventory.Hold = null;
        var released = await directory.GetAsync(
            seeded.FulfillmentId, seeded.ReplacementShipmentId, CancellationToken.None);
        Assert.False(released.IsReady);
        Assert.Equal(FulfillmentReplacementReadinessReasonCodes.HoldReleased, released.ReasonCode);

        // Consumed by the original sale: the row still exists, so the STATUS is what refuses it.
        inventory.Hold = inventory.HoldWith(status: "Consumed", quantity: 1m, reservationId: seeded.ReservationId);
        var consumed = await directory.GetAsync(
            seeded.FulfillmentId, seeded.ReplacementShipmentId, CancellationToken.None);
        Assert.False(consumed.IsReady);
        Assert.Equal(FulfillmentReplacementReadinessReasonCodes.HoldReleased, consumed.ReasonCode);

        // A hold answering for a DIFFERENT reservation id is not this decision's hold, even if it is "Held": the
        // owning port's answer is re-checked against the exact identity instead of being trusted by status alone.
        inventory.Hold = inventory.HoldWith(status: "Held", quantity: 1m, reservationId: seeded.ReservationId);
        var exact = await directory.GetAsync(
            seeded.FulfillmentId, seeded.ReplacementShipmentId, CancellationToken.None);
        Assert.True(exact.IsReady, exact.ReasonCode);

        inventory.Hold = inventory.HoldWith(status: "Held", quantity: 1m, reservationId: Guid.NewGuid());
        var foreign = await directory.GetAsync(
            seeded.FulfillmentId, seeded.ReplacementShipmentId, CancellationToken.None);
        Assert.False(foreign.IsReady);
        Assert.Equal(FulfillmentReplacementReadinessReasonCodes.HoldQuantityMismatch, foreign.ReasonCode);

        Assert.Equal(releasedCallsAfterSeed, inventory.ReleaseCalls);
        Assert.Equal(consumeCallsAfterSeed, inventory.ConsumeCalls);
    }

    [Fact]
    public async Task A_hold_that_does_not_cover_the_parcel_quantity_is_a_quantity_mismatch()
    {
        await using var db = CreateDb();
        var inventory = new HoldInventory();
        var directory = CreateDirectory(db, inventory);
        var seeded = await SeedReplacementAsync(directory, inventory);

        inventory.Hold = inventory.HoldWith(status: "Held", quantity: 2m, reservationId: seeded.ReservationId);
        var tooMuch = await directory.GetAsync(
            seeded.FulfillmentId, seeded.ReplacementShipmentId, CancellationToken.None);
        Assert.False(tooMuch.IsReady);
        Assert.Equal(FulfillmentReplacementReadinessReasonCodes.HoldQuantityMismatch, tooMuch.ReasonCode);

        inventory.Hold = inventory.HoldWith(status: "Held", quantity: 0.5m, reservationId: seeded.ReservationId);
        var tooLittle = await directory.GetAsync(
            seeded.FulfillmentId, seeded.ReplacementShipmentId, CancellationToken.None);
        Assert.False(tooLittle.IsReady);
        Assert.Equal(FulfillmentReplacementReadinessReasonCodes.HoldQuantityMismatch, tooLittle.ReasonCode);
    }

    [Fact]
    public async Task A_parcel_that_is_not_a_replacement_is_never_ready()
    {
        await using var db = CreateDb();
        var inventory = new HoldInventory();
        var directory = CreateDirectory(db, inventory);
        var seeded = await SeedReplacementAsync(directory, inventory);

        // The returned ORIGINAL parcel is not a replacement: its own dispatch history is not a supply promise.
        var original = await directory.GetAsync(
            seeded.FulfillmentId, seeded.SourceShipmentId, CancellationToken.None);
        Assert.False(original.IsReady);
        Assert.Equal(FulfillmentReplacementReadinessReasonCodes.NotReplacement, original.ReasonCode);

        var unknown = await directory.GetAsync(
            seeded.FulfillmentId, Guid.NewGuid(), CancellationToken.None);
        Assert.False(unknown.IsReady);
        Assert.Equal(FulfillmentReplacementReadinessReasonCodes.NotReplacement, unknown.ReasonCode);
    }

    /// <summary>
    /// Builds the physical prerequisite through OWNING production paths: paid unit → processing → packed →
    /// original parcel → non-delivery → return → receipt with damaged units → damaged-goods REPLACEMENT decision
    /// (which reserves the healthy stock and creates the linked parcel). No row is written behind the aggregate.
    /// </summary>
    private static async Task<SeedFixture> SeedReplacementAsync(
        FulfillmentDirectory directory,
        HoldInventory inventory)
    {
        await directory.CreateFromPaidSellerOrdersAsync(
            Checkout, Guid.NewGuid(), [SellerOrder], CancellationToken.None);
        var fulfillmentId = (await directory.ListForCheckoutAsync(Checkout, CancellationToken.None))
            .Single()
            .FulfillmentId;
        await directory.MarkProcessingAsync(fulfillmentId, Guid.NewGuid(), CancellationToken.None);
        await directory.MarkPackedAsync(fulfillmentId, Guid.NewGuid(), CancellationToken.None);
        var created = await directory.CreateShipmentAsync(
            fulfillmentId,
            Guid.NewGuid(),
            "پیشتاز",
            [new AppModels.ShipmentLineCommand(Line, 3m)],
            CancellationToken.None,
            shippingMethodCode: "store_courier",
            shippingGroupId: ShippingGroup);
        var sourceShipmentId = created.Snapshot.Shipments.Single().ShipmentId;
        await directory.AssignTrackingAsync(
            fulfillmentId, sourceShipmentId, Guid.NewGuid(), "TRK-TB270R3-READINESS", CancellationToken.None);
        await directory.DispatchShipmentAsync(
            fulfillmentId, sourceShipmentId, Guid.NewGuid(), CancellationToken.None);
        await directory.RecordNonDeliveryAsync(
            fulfillmentId,
            sourceShipmentId,
            Guid.NewGuid(),
            "customer_unavailable",
            "TB270R3",
            CancellationToken.None);
        await directory.StartReturnAsync(
            fulfillmentId, sourceShipmentId, Guid.NewGuid(), CancellationToken.None);
        await directory.RecordReturnReceiptAsync(
            fulfillmentId,
            sourceShipmentId,
            Guid.NewGuid(),
            [
                new AppModels.ReturnReceiptLineCommand(Line, 2m, ReturnReceiptCondition.Usable),
                new AppModels.ReturnReceiptLineCommand(Line, 1m, ReturnReceiptCondition.Damaged),
            ],
            "TB270R3 receipt",
            CancellationToken.None);

        var resolution = await directory.ResolveDamagedGoodsAsync(
            fulfillmentId,
            Guid.NewGuid(),
            sourceShipmentId,
            Line,
            1m,
            DamagedResolutionOutcome.Replacement,
            "tb270r3-damaged",
            "TB270R3 replacement",
            CancellationToken.None);

        Assert.Null(resolution.BlockerCode);
        Assert.NotNull(resolution.ReplacementShipmentId);
        Assert.NotNull(resolution.ReplacementReservationId);
        Assert.NotNull(inventory.Hold);

        return new SeedFixture(
            fulfillmentId,
            sourceShipmentId,
            resolution.ReplacementShipmentId!.Value,
            resolution.DamagedGoodsResolutionId,
            resolution.ReplacementReservationId!.Value);
    }

    private static FulfillmentDirectory CreateDirectory(FulfillmentDbContext db, IFulfillmentInventoryGateway inventory) =>
        new(
            db,
            new OpenFulfillmentUseCaseGuard(),
            new PaidOrderReader(),
            inventory,
            new FulfillmentInstrumentation(),
            new SystemUtcClock(),
            new UuidV7IdGenerator(),
            shipmentDispatchPlans: null);

    private static FulfillmentDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<FulfillmentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(w => w.Ignore(
                Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new FulfillmentDbContext(options);
    }

    private sealed record SeedFixture(
        Guid FulfillmentId,
        Guid SourceShipmentId,
        Guid ReplacementShipmentId,
        Guid DecisionId,
        Guid ReservationId);

    private sealed class PaidOrderReader : IOrderFulfillmentReader
    {
        public Task<OrderFulfillmentHandoffSnapshot?> GetHandoffAsync(
            Guid sellerOrderId,
            CancellationToken cancellationToken) =>
            Task.FromResult<OrderFulfillmentHandoffSnapshot?>(
                sellerOrderId == SellerOrder
                    ? new OrderFulfillmentHandoffSnapshot(
                        SellerOrder,
                        Checkout,
                        SellerParty,
                        Guid.NewGuid(),
                        true,
                        "محمد امامی",
                        "09121234567",
                        "تهران",
                        "تهران",
                        "تهران خیابان آزمایش ۱",
                        "1234567890",
                        "store_courier",
                        "پیشتاز",
                        [new OrderFulfillmentLineSnapshot(Line, 3m, SaleReservation)],
                        [
                            new OrderFulfillmentShippingGroupSnapshot(
                                ShippingGroup,
                                SellerOrder,
                                SellerParty,
                                "store_courier",
                                "store_courier",
                                null,
                                "پیشتاز",
                                null,
                                new DateOnly(2026, 11, 20),
                                null,
                                null,
                                0,
                                [Line]),
                        ])
                    : null);

        public Task<OrderFulfillmentHandoffSnapshot?> GetHandoffForCheckoutAsync(
            Guid checkoutId,
            Guid actorUserId,
            CancellationToken cancellationToken) =>
            Task.FromResult<OrderFulfillmentHandoffSnapshot?>(null);
    }

    /// <summary>
    /// The Inventory-owned reservation read. Only this port is controlled: the hold's existence/status/quantity is
    /// Inventory's fact, and the owning Fulfillment reader must consume it as-is.
    /// </summary>
    private sealed class HoldInventory : IFulfillmentInventoryGateway
    {
        public Guid ReservationId { get; } = Guid.NewGuid();

        public FulfillmentReplacementReservation? Hold { get; set; }

        public int ReserveCalls { get; private set; }

        public int FindCalls { get; private set; }

        public int ConsumeCalls { get; private set; }

        public int ReleaseCalls { get; private set; }

        public FulfillmentReplacementReservation HoldWith(string status, decimal quantity, Guid reservationId) =>
            new(reservationId, Guid.NewGuid(), Guid.NewGuid(), quantity, status);

        public Task ConsumeReservationAsync(Guid reservationId, CancellationToken cancellationToken)
        {
            ConsumeCalls++;
            return Task.CompletedTask;
        }

        public Task CommitReservationForPaidOrderAsync(Guid reservationId, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<FulfillmentReplacementReservation> ReserveReplacementReservationAsync(
            Guid originalReservationId,
            decimal quantity,
            string reservationKey,
            string externalReference,
            CancellationToken cancellationToken)
        {
            ReserveCalls++;
            Hold = new FulfillmentReplacementReservation(
                ReservationId,
                Guid.NewGuid(),
                Guid.NewGuid(),
                quantity,
                "Held");
            return Task.FromResult(Hold);
        }

        public Task<FulfillmentReplacementReservation?> FindReplacementReservationAsync(
            Guid reservationId,
            CancellationToken cancellationToken)
        {
            FindCalls++;
            // The owning Inventory port answers for the requested id; the reader must still re-check the identity.
            return Task.FromResult(Hold);
        }

        public Task ReleaseReplacementReservationAsync(Guid reservationId, CancellationToken cancellationToken)
        {
            ReleaseCalls++;
            return Task.CompletedTask;
        }
    }
}
