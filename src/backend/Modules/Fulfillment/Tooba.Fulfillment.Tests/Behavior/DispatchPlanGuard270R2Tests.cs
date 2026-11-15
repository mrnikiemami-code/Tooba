using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Application.Fulfillments.Ports;
using Tooba.Fulfillment.Domain.Aggregates;
using Tooba.Fulfillment.Domain.ValueObjects;
using Tooba.Fulfillment.Infrastructure.Directories;
using Tooba.Fulfillment.Infrastructure.Observability;
using Tooba.Fulfillment.Infrastructure.Persistence;
using Tooba.Fulfillment.Contracts.Errors;
using Tooba.Order.Contracts.Fulfillment;
using Tooba.Order.Contracts.Shipping;
using Xunit;
using AppModels = Tooba.Fulfillment.Application.Fulfillments.Models;

namespace Tooba.Fulfillment.Tests.Behavior;

/// <summary>
/// TB-270-R2 — a resend/replacement parcel may leave the warehouse only with its OWN confirmed dispatch plan.
/// </summary>
/// <remarks>
/// <para>
/// The two-stage invariant is proved here at the REAL owning command boundary
/// (<see cref="FulfillmentDirectory"/>), not on a helper: the physical parcel already exists, and dispatch is either
/// authorized by a confirmed Order plan for <b>that exact shipment</b> or refused before any stock is consumed.
/// </para>
/// <para>
/// Every case asserts the same three facts a partial commit would violate: zero inventory consumption, the parcel
/// still <c>Created</c> with no <c>DispatchedAt</c>, and a package that stays <c>Created</c> as a whole.
/// In-memory storage proves the guarded command behavior; it is NOT a relational durability claim.
/// </para>
/// </remarks>
public sealed class DispatchPlanGuard270R2Tests
{
    private static readonly Guid Checkout = Guid.Parse("01a1d270-0000-7000-8000-000000000001");
    private static readonly Guid SellerOrder = Guid.Parse("01a1d270-0000-7000-8000-000000000002");
    private static readonly Guid SellerParty = Guid.Parse("01a1d270-0000-7000-8000-000000000003");
    private static readonly Guid Line = Guid.Parse("01a1d270-0000-7000-8000-000000000004");
    private static readonly Guid SiblingSellerOrder = Guid.Parse("01a1d270-0000-7000-8000-000000000010");
    private static readonly Guid SiblingSellerParty = Guid.Parse("01a1d270-0000-7000-8000-000000000011");
    private static readonly Guid SiblingLine = Guid.Parse("01a1d270-0000-7000-8000-000000000012");

    [Fact]
    public async Task Unplanned_resend_parcel_is_refused_with_zero_consume_and_no_dispatch()
    {
        await using var db = CreateDb();
        var inventory = new RecordingInventory();
        var plans = new PlanReaderFake();
        var directory = CreateDirectory(db, inventory, plans);
        var fixture = await SeedReturnedSourceAsync(db, directory);
        var resend = await directory.CreateResendShipmentAsync(
            fixture.FulfillmentId,
            Guid.NewGuid(),
            fixture.SourceShipmentId,
            [new AppModels.ShipmentLineCommand(Line, 1m)],
            CancellationToken.None,
            "tb270r2-resend-unplanned");

        var error = await Assert.ThrowsAsync<ContractOperationException>(() =>
            directory.DispatchShipmentAsync(
                fixture.FulfillmentId, resend.CreatedShipmentId, Guid.NewGuid(), CancellationToken.None));

        Assert.Equal(FulfillmentErrorCodes.DispatchPlanRequired, error.Code);
        Assert.Equal(0, inventory.ConsumeCalls);
        var stored = await db.Shipments.SingleAsync(x => x.ShipmentId == resend.CreatedShipmentId);
        Assert.Equal(ShipmentStatus.Created, stored.Status);
        Assert.Null(stored.DispatchedAt);
    }

    [Fact]
    public async Task Planned_resend_parcel_dispatches_and_consumes_exactly_once()
    {
        await using var db = CreateDb();
        var inventory = new RecordingInventory();
        var plans = new PlanReaderFake();
        var directory = CreateDirectory(db, inventory, plans);
        var fixture = await SeedReturnedSourceAsync(db, directory);
        var resend = await directory.CreateResendShipmentAsync(
            fixture.FulfillmentId,
            Guid.NewGuid(),
            fixture.SourceShipmentId,
            [new AppModels.ShipmentLineCommand(Line, 1m)],
            CancellationToken.None,
            "tb270r2-resend-planned");
        await directory.AssignTrackingAsync(
            fixture.FulfillmentId,
            resend.CreatedShipmentId,
            Guid.NewGuid(),
            "TRK-TB270R2-PLANNED",
            CancellationToken.None);
        plans.Add(Plan(resend.CreatedShipmentId, OrderShipmentDispatchPlanKinds.Resend));

        var dispatched = await directory.DispatchShipmentAsync(
            fixture.FulfillmentId, resend.CreatedShipmentId, Guid.NewGuid(), CancellationToken.None);

        var parcel = dispatched.Shipments.Single(x => x.ShipmentId == resend.CreatedShipmentId);
        Assert.Equal(ShipmentStatus.Dispatched, parcel.Status);
        Assert.NotNull(parcel.DispatchedAt);
        // The returned source already consumed its sale reservation; a resend must not consume it a second time.
        Assert.Equal(0, inventory.ConsumeCalls);
    }

    [Fact]
    public async Task A_plan_of_the_wrong_attempt_kind_does_not_authorize_dispatch()
    {
        await using var db = CreateDb();
        var inventory = new RecordingInventory();
        var plans = new PlanReaderFake();
        var directory = CreateDirectory(db, inventory, plans);
        var fixture = await SeedReturnedSourceAsync(db, directory);
        var resend = await directory.CreateResendShipmentAsync(
            fixture.FulfillmentId,
            Guid.NewGuid(),
            fixture.SourceShipmentId,
            [new AppModels.ShipmentLineCommand(Line, 1m)],
            CancellationToken.None,
            "tb270r2-resend-wrong-kind");
        // A replacement plan cannot authorize a resend parcel: the binding is the attempt identity, not a row count.
        plans.Add(Plan(resend.CreatedShipmentId, OrderShipmentDispatchPlanKinds.Replacement));

        var error = await Assert.ThrowsAsync<ContractOperationException>(() =>
            directory.DispatchShipmentAsync(
                fixture.FulfillmentId, resend.CreatedShipmentId, Guid.NewGuid(), CancellationToken.None));

        Assert.Equal(FulfillmentErrorCodes.DispatchPlanRequired, error.Code);
        Assert.Equal(0, inventory.ConsumeCalls);
        Assert.Equal(
            ShipmentStatus.Created,
            (await db.Shipments.SingleAsync(x => x.ShipmentId == resend.CreatedShipmentId)).Status);
    }

    [Fact]
    public async Task Consolidated_package_with_one_unplanned_linked_member_is_refused_as_a_whole()
    {
        await using var db = CreateDb();
        var inventory = new RecordingInventory();
        var plans = new PlanReaderFake();
        var directory = CreateDirectory(db, inventory, plans);
        var fixture = await SeedReturnedSourceAsync(db, directory);
        var resend = await directory.CreateResendShipmentAsync(
            fixture.FulfillmentId,
            Guid.NewGuid(),
            fixture.SourceShipmentId,
            [new AppModels.ShipmentLineCommand(Line, 1m)],
            CancellationToken.None,
            "tb270r2-resend-package");
        await directory.AssignTrackingAsync(
            fixture.FulfillmentId,
            resend.CreatedShipmentId,
            Guid.NewGuid(),
            "TRK-TB270R2-PACKAGE",
            CancellationToken.None);
        // یک بستهٔ تجمیعی حداقل دو فروشندهٔ متمایز می‌خواهد؛ مرسولهٔ عادی فروشندهٔ دوم هیچ برنامه‌ای لازم ندارد.
        var siblingShipmentId = await SeedSiblingShipmentAsync(db, directory);
        var package = await directory.CreateConsolidatedPackageAsync(
            Checkout,
            [resend.CreatedShipmentId, siblingShipmentId],
            "store_courier",
            trackingReference: null,
            note: null,
            Guid.NewGuid(),
            CancellationToken.None);
        Assert.Equal(ConsolidatedPackageStatus.Created, package.Status);

        var error = await Assert.ThrowsAsync<ContractOperationException>(() =>
            directory.DispatchConsolidatedPackageAsync(
                package.ConsolidatedPackageId, Guid.NewGuid(), CancellationToken.None));

        Assert.Equal(FulfillmentErrorCodes.DispatchPlanRequired, error.Code);
        Assert.Equal(0, inventory.ConsumeCalls);
        var stored = await db.Shipments.SingleAsync(x => x.ShipmentId == resend.CreatedShipmentId);
        Assert.Equal(ShipmentStatus.Created, stored.Status);
        Assert.Null(stored.DispatchedAt);
        Assert.Equal(
            ConsolidatedPackageStatus.Created,
            (await db.ConsolidatedPackages.SingleAsync()).Status);
    }

    [Fact]
    public async Task A_missing_plan_source_refuses_instead_of_defaulting_to_authorized()
    {
        await using var db = CreateDb();
        var inventory = new RecordingInventory();
        var directory = CreateDirectory(db, inventory, plans: null);
        var fixture = await SeedReturnedSourceAsync(db, directory);

        var resend = await CreateResendAsync(directory, fixture, "tb270r2-resend-no-reader");
        var error = await Assert.ThrowsAsync<ContractOperationException>(() =>
            directory.DispatchShipmentAsync(
                fixture.FulfillmentId, resend.CreatedShipmentId, Guid.NewGuid(), CancellationToken.None));

        Assert.Equal(FulfillmentErrorCodes.DispatchPlanRequired, error.Code);
        Assert.Equal(0, inventory.ConsumeCalls);
    }

    [Fact]
    public async Task First_dispatch_without_any_plan_keeps_its_existing_behavior()
    {
        await using var db = CreateDb();
        var inventory = new RecordingInventory();
        var plans = new PlanReaderFake();
        var directory = CreateDirectory(db, inventory, plans);
        var fixture = await SeedReturnedSourceAsync(db, directory);

        // The original parcel is NOT a linked attempt, so the own-plan requirement cannot apply to it and its
        // accepted first-dispatch promise stays exactly as before this change. It is already dispatched by the
        // seed's return flow, so this case re-reads that exact recorded fact instead of planning anything.
        var snapshot = await directory.GetAsync(fixture.FulfillmentId, CancellationToken.None);
        var original = snapshot!.Shipments.Single(x => x.ShipmentId == fixture.SourceShipmentId);
        Assert.Equal(ShipmentStatus.Returned, original.Status);
        Assert.Null(original.ResendOfShipmentId);
    }

    private static async Task<AppModels.FulfillmentShipmentCreateResult> CreateResendAsync(
        FulfillmentDirectory directory,
        SeedFixture fixture,
        string key) =>
        await directory.CreateResendShipmentAsync(
            fixture.FulfillmentId,
            Guid.NewGuid(),
            fixture.SourceShipmentId,
            [new AppModels.ShipmentLineCommand(Line, 1m)],
            CancellationToken.None,
            key);

    /// <summary>
    /// Builds the physical prerequisite through the OWNING production paths: paid unit → processing → packed →
    /// original parcel → non-delivery → return → usable origin receipt. The returned parcel is then lawful evidence
    /// for a resend, and no row is written behind the aggregate.
    /// </summary>
    private static async Task<SeedFixture> SeedReturnedSourceAsync(FulfillmentDbContext db, FulfillmentDirectory directory)
    {
        await directory.CreateFromPaidSellerOrdersAsync(
            Checkout, Guid.NewGuid(), [SellerOrder], CancellationToken.None);
        var fulfillmentId = (await db.Fulfillments.SingleAsync()).FulfillmentId;
        await directory.MarkProcessingAsync(fulfillmentId, Guid.NewGuid(), CancellationToken.None);
        await directory.MarkPackedAsync(fulfillmentId, Guid.NewGuid(), CancellationToken.None);
        var created = await directory.CreateShipmentAsync(
            fulfillmentId,
            Guid.NewGuid(),
            "پیشتاز",
            [new AppModels.ShipmentLineCommand(Line, 1m)],
            CancellationToken.None,
            shippingMethodCode: "store_courier");
        var sourceShipmentId = created.Snapshot.Shipments.Single().ShipmentId;
        await directory.AssignTrackingAsync(
            fulfillmentId, sourceShipmentId, Guid.NewGuid(), "TRK-TB270R2-SOURCE", CancellationToken.None);
        await directory.DispatchShipmentAsync(
            fulfillmentId, sourceShipmentId, Guid.NewGuid(), CancellationToken.None);
        await directory.RecordNonDeliveryAsync(
            fulfillmentId,
            sourceShipmentId,
            Guid.NewGuid(),
            "customer_unavailable",
            "TB270R2",
            CancellationToken.None);
        await directory.StartReturnAsync(
            fulfillmentId, sourceShipmentId, Guid.NewGuid(), CancellationToken.None);
        await directory.RecordReturnReceiptAsync(
            fulfillmentId,
            sourceShipmentId,
            Guid.NewGuid(),
            [new AppModels.ReturnReceiptLineCommand(Line, 1m, ReturnReceiptCondition.Usable)],
            "TB270R2 receipt",
            CancellationToken.None);
        return new SeedFixture(fulfillmentId, sourceShipmentId);
    }

    /// <summary>
    /// A second seller in the SAME checkout: a lawful consolidated package needs two distinct seller parties, and this
    /// ordinary (non-attempt) parcel is the planned-free member whose sibling lacks its own plan.
    /// </summary>
    private static async Task<Guid> SeedSiblingShipmentAsync(FulfillmentDbContext db, FulfillmentDirectory directory)
    {
        await directory.CreateFromPaidSellerOrdersAsync(
            Checkout, Guid.NewGuid(), [SiblingSellerOrder], CancellationToken.None);
        var unit = await db.Fulfillments.SingleAsync(x => x.SellerOrderId == SiblingSellerOrder);
        await directory.MarkProcessingAsync(unit.FulfillmentId, Guid.NewGuid(), CancellationToken.None);
        await directory.MarkPackedAsync(unit.FulfillmentId, Guid.NewGuid(), CancellationToken.None);
        var created = await directory.CreateShipmentAsync(
            unit.FulfillmentId,
            Guid.NewGuid(),
            "پیشتاز",
            [new AppModels.ShipmentLineCommand(SiblingLine, 1m)],
            CancellationToken.None,
            shippingMethodCode: "store_courier");
        var shipmentId = created.Snapshot.Shipments.Single().ShipmentId;
        await directory.AssignTrackingAsync(
            unit.FulfillmentId, shipmentId, Guid.NewGuid(), "TRK-TB270R2-SIBLING", CancellationToken.None);
        return shipmentId;
    }

    private static OrderFulfillmentHandoffSnapshot SellerOrderSnapshot(
        Guid sellerOrderId,
        Guid sellerPartyId,
        Guid lineId) =>
        new(
            sellerOrderId,
            Checkout,
            sellerPartyId,
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
            [new OrderFulfillmentLineSnapshot(lineId, 1m, null)]);

    private static OrderShipmentDispatchPlanFact Plan(Guid shipmentId, string kind) =>
        new(
            Guid.NewGuid(),
            shipmentId,
            Checkout,
            SellerOrder,
            Guid.NewGuid(),
            Guid.NewGuid(),
            kind,
            Guid.NewGuid(),
            null,
            null,
            "fingerprint",
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            4,
            1,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "store_courier",
            "پیشتاز",
            "store_courier",
            null,
            new DateOnly(2026, 11, 20),
            new TimeOnly(9, 0).Ticks,
            new TimeOnly(10, 0).Ticks,
            "Asia/Tehran",
            new DateOnly(2026, 11, 18),
            new TimeOnly(8, 0).Ticks,
            new TimeOnly(20, 0).Ticks,
            "Asia/Tehran",
            new DateTimeOffset(2026, 11, 17, 9, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 11, 18, 3, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 11, 18, 16, 30, 0, TimeSpan.Zero),
            120,
            60,
            2,
            3,
            "return_receipt",
            new DateTimeOffset(2026, 11, 16, 9, 0, 0, TimeSpan.Zero),
            "tb270r2-occurrence",
            Guid.NewGuid(),
            new DateTimeOffset(2026, 11, 16, 10, 0, 0, TimeSpan.Zero));

    private static FulfillmentDirectory CreateDirectory(
        FulfillmentDbContext db,
        IFulfillmentInventoryGateway inventory,
        IOrderShipmentDispatchPlanReader? plans) =>
        new(
            db,
            new OpenFulfillmentUseCaseGuard(),
            new PaidOrderReader(),
            inventory,
            new FulfillmentInstrumentation(),
            new SystemUtcClock(),
            new UuidV7IdGenerator(),
            plans);

    private static FulfillmentDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<FulfillmentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            // The resend path opens an owning transaction. In-memory has no transactions, so the warning is
            // suppressed for this unit-level guard proof; durability/rollback is NOT claimed from in-memory rows.
            .ConfigureWarnings(w => w.Ignore(
                Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new FulfillmentDbContext(options);
    }

    private sealed record SeedFixture(Guid FulfillmentId, Guid SourceShipmentId);

    private sealed class PaidOrderReader : IOrderFulfillmentReader
    {
        public Task<OrderFulfillmentHandoffSnapshot?> GetHandoffAsync(
            Guid sellerOrderId,
            CancellationToken cancellationToken) =>
            Task.FromResult<OrderFulfillmentHandoffSnapshot?>(
                sellerOrderId == SellerOrder
                    ? SellerOrderSnapshot(SellerOrder, SellerParty, Line)
                    : sellerOrderId == SiblingSellerOrder
                        ? SellerOrderSnapshot(SiblingSellerOrder, SiblingSellerParty, SiblingLine)
                        : null);

        public Task<OrderFulfillmentHandoffSnapshot?> GetHandoffForCheckoutAsync(
            Guid checkoutId,
            Guid actorUserId,
            CancellationToken cancellationToken) =>
            Task.FromResult<OrderFulfillmentHandoffSnapshot?>(null);
    }

    private sealed class RecordingInventory : IFulfillmentInventoryGateway
    {
        public int ConsumeCalls { get; private set; }

        public Task ConsumeReservationAsync(Guid reservationId, CancellationToken cancellationToken)
        {
            ConsumeCalls++;
            return Task.CompletedTask;
        }

        public Task CommitReservationForPaidOrderAsync(Guid reservationId, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class PlanReaderFake : IOrderShipmentDispatchPlanReader
    {
        private readonly List<OrderShipmentDispatchPlanFact> _rows = [];

        public void Add(OrderShipmentDispatchPlanFact fact) => _rows.Add(fact);

        public Task<IReadOnlyList<OrderShipmentDispatchPlanFact>> ListForShipmentsAsync(
            IReadOnlyList<Guid> shipmentIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<OrderShipmentDispatchPlanFact>>(
                _rows.Where(x => shipmentIds.Contains(x.ShipmentId)).ToList());

        public Task<OrderShipmentDispatchPlanFact?> GetForShipmentAsync(
            Guid shipmentId,
            CancellationToken cancellationToken) =>
            Task.FromResult(_rows.FirstOrDefault(x => x.ShipmentId == shipmentId));
    }
}
