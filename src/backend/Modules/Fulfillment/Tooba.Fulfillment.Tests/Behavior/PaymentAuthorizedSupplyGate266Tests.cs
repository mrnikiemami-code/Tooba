using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Application.Fulfillments.Models;
using Tooba.Fulfillment.Application.Fulfillments.Ports;
using Tooba.Fulfillment.Domain.Aggregates;
using Tooba.Fulfillment.Domain.ValueObjects;
using Tooba.Fulfillment.Infrastructure.Directories;
using Tooba.Fulfillment.Infrastructure.Observability;
using Tooba.Fulfillment.Infrastructure.Persistence;
using Tooba.Order.Contracts.Fulfillment;
using Tooba.Order.Contracts.Shipping;
using Xunit;

namespace Tooba.Fulfillment.Tests.Behavior;

/// <summary>
/// TB-266: physical supply work (processing, packing, parcel creation) is authorized ONLY by the source order's
/// authoritative paid handoff. These cases exercise the real directory against a switchable Order contract reader:
/// the unit is created while the order is paid, then the recorded truth changes and every NEW physical command is
/// refused before it writes anything.
/// </summary>
public sealed class PaymentAuthorizedSupplyGate266Tests
{
    private static readonly Guid Checkout = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaa0266");
    private static readonly Guid SellerOrder = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbb0266");
    private static readonly Guid SellerParty = Guid.Parse("cccccccc-cccc-4ccc-8ccc-cccccccc0266");
    private static readonly Guid Line = Guid.Parse("33333333-3333-4333-8333-333333330266");

    [Fact]
    public async Task Paid_order_allows_processing_packing_and_parcel_creation()
    {
        await using var db = CreateDb();
        var reader = new SwitchableOrderReader(Handoff(paid: true));
        var directory = CreateDirectory(db, reader);
        var fulfillmentId = await SeedPaidUnitAsync(db, directory);

        await directory.MarkProcessingAsync(fulfillmentId, Guid.NewGuid(), CancellationToken.None);
        await directory.MarkPackedAsync(fulfillmentId, Guid.NewGuid(), CancellationToken.None);
        var created = await directory.CreateShipmentAsync(
            fulfillmentId,
            Guid.NewGuid(),
            "پیشتاز",
            [new ShipmentLineCommand(Line, 1m)],
            CancellationToken.None);

        Assert.Single(created.Snapshot.Shipments);
        Assert.Equal(FulfillmentStatus.Packed, created.Snapshot.Status);
    }

    [Fact]
    public async Task Order_that_lost_payment_authority_refuses_new_processing_without_writing()
    {
        await using var db = CreateDb();
        var reader = new SwitchableOrderReader(Handoff(paid: true));
        var directory = CreateDirectory(db, reader);
        var fulfillmentId = await SeedPaidUnitAsync(db, directory);

        reader.Snapshot = Handoff(paid: false);

        var error = await Assert.ThrowsAsync<ContractOperationException>(() =>
            directory.MarkProcessingAsync(fulfillmentId, Guid.NewGuid(), CancellationToken.None));

        Assert.Equal("fulfillment.order.payment_not_authorized", error.Code);
        var stored = await db.Fulfillments.SingleAsync();
        Assert.Equal(FulfillmentStatus.ReadyToFulfill, stored.Status);
    }

    [Fact]
    public async Task Order_that_lost_payment_authority_refuses_parcel_creation_with_zero_shipments()
    {
        await using var db = CreateDb();
        var reader = new SwitchableOrderReader(Handoff(paid: true));
        var directory = CreateDirectory(db, reader);
        var fulfillmentId = await SeedPaidUnitAsync(db, directory);
        await directory.MarkProcessingAsync(fulfillmentId, Guid.NewGuid(), CancellationToken.None);
        await directory.MarkPackedAsync(fulfillmentId, Guid.NewGuid(), CancellationToken.None);

        reader.Snapshot = Handoff(paid: false);

        var error = await Assert.ThrowsAsync<ContractOperationException>(() =>
            directory.CreateShipmentAsync(
                fulfillmentId,
                Guid.NewGuid(),
                "پیشتاز",
                [new ShipmentLineCommand(Line, 1m)],
                CancellationToken.None));

        Assert.Equal("fulfillment.order.payment_not_authorized", error.Code);
        Assert.Equal(0, await db.Shipments.CountAsync());
    }

    [Fact]
    public async Task Missing_order_handoff_refuses_new_physical_work()
    {
        await using var db = CreateDb();
        var reader = new SwitchableOrderReader(Handoff(paid: true));
        var directory = CreateDirectory(db, reader);
        var fulfillmentId = await SeedPaidUnitAsync(db, directory);

        reader.Snapshot = null;

        var error = await Assert.ThrowsAsync<ContractOperationException>(() =>
            directory.MarkProcessingAsync(fulfillmentId, Guid.NewGuid(), CancellationToken.None));

        Assert.Equal("fulfillment.order.not_found", error.Code);
        var stored = await db.Fulfillments.SingleAsync();
        Assert.Equal(FulfillmentStatus.ReadyToFulfill, stored.Status);
    }

    [Fact]
    public async Task Dispatch_after_payment_authority_lost_is_refused_without_dispatch_or_consumption()
    {
        await using var db = CreateDb();
        var reader = new SwitchableOrderReader(Handoff(paid: true));
        var inventory = new NoopInventory();
        var directory = CreateDirectory(db, reader, inventory);
        var fulfillmentId = await SeedPaidUnitAsync(db, directory);
        await directory.MarkProcessingAsync(fulfillmentId, Guid.NewGuid(), CancellationToken.None);
        await directory.MarkPackedAsync(fulfillmentId, Guid.NewGuid(), CancellationToken.None);
        var created = await directory.CreateShipmentAsync(
            fulfillmentId,
            Guid.NewGuid(),
            "پیشتاز",
            [new ShipmentLineCommand(Line, 1m)],
            CancellationToken.None);
        var shipmentId = created.Snapshot.Shipments.Single().ShipmentId;
        Assert.Equal(0, inventory.ConsumeCalls);

        reader.Snapshot = Handoff(paid: false);

        var error = await Assert.ThrowsAsync<ContractOperationException>(() =>
            directory.DispatchShipmentAsync(fulfillmentId, shipmentId, Guid.NewGuid(), CancellationToken.None));

        Assert.Equal("fulfillment.order.payment_not_authorized", error.Code);
        var stored = await db.Shipments.SingleAsync();
        Assert.Equal(ShipmentStatus.Created, stored.Status);
        Assert.Null(stored.DispatchedAt);
        Assert.Equal(0, inventory.ConsumeCalls);
    }

    [Fact]
    public async Task Bulk_group_selections_after_payment_authority_lost_are_refused_without_writing()
    {
        await using var db = CreateDb();
        var reader = new SwitchableOrderReader(Handoff(paid: true));
        var directory = CreateDirectory(db, reader);
        var fulfillmentId = await SeedPaidUnitAsync(db, directory);

        reader.Snapshot = Handoff(paid: false);

        var processingError = await Assert.ThrowsAsync<ContractOperationException>(() =>
            directory.ProcessSelectionsAsync(
                fulfillmentId,
                Guid.NewGuid(),
                [new FulfillmentSelectionCommand(Line, 1m)],
                CancellationToken.None));
        Assert.Equal("fulfillment.order.payment_not_authorized", processingError.Code);

        var packingError = await Assert.ThrowsAsync<ContractOperationException>(() =>
            directory.PackSelectionsAsync(
                fulfillmentId,
                Guid.NewGuid(),
                [new FulfillmentSelectionCommand(Line, 1m)],
                CancellationToken.None));
        Assert.Equal("fulfillment.order.payment_not_authorized", packingError.Code);

        var item = await db.Items.SingleAsync();
        Assert.Equal(0m, item.QuantityProcessing);
        Assert.Equal(0m, item.QuantityPacked);
        Assert.Equal(FulfillmentStatus.ReadyToFulfill, (await db.Fulfillments.SingleAsync()).Status);
    }

    [Fact]
    public async Task Consolidated_package_with_unpaid_member_refuses_dispatch_and_writes_nothing()
    {
        await using var db = CreateDb();
        var sellerBOrder = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbb0267");
        var sellerBParty = Guid.Parse("cccccccc-cccc-4ccc-8ccc-cccccccc0267");
        var lineB = Guid.Parse("33333333-3333-4333-8333-333333330267");
        var reader = new MultiOrderReader(
            Handoff(paid: true),
            Handoff(paid: true, sellerBOrder, sellerBParty, lineB));
        var inventory = new NoopInventory();
        var directory = CreateDirectory(db, reader, inventory);

        await directory.CreateFromPaidSellerOrdersAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [SellerOrder, sellerBOrder],
            CancellationToken.None);

        var shipmentIds = new List<Guid>();
        foreach (var unit in await db.Fulfillments.ToListAsync())
        {
            await directory.MarkProcessingAsync(unit.FulfillmentId, Guid.NewGuid(), CancellationToken.None);
            await directory.MarkPackedAsync(unit.FulfillmentId, Guid.NewGuid(), CancellationToken.None);
            var line = unit.Items.Single().OrderLineId;
            var created = await directory.CreateShipmentAsync(
                unit.FulfillmentId,
                Guid.NewGuid(),
                "پیشتاز",
                [new ShipmentLineCommand(line, 1m)],
                CancellationToken.None,
                shippingMethodCode: "store_courier");
            shipmentIds.Add(created.Snapshot.Shipments.Single().ShipmentId);
        }

        var package = await directory.CreateConsolidatedPackageAsync(
            Checkout,
            shipmentIds,
            "store_courier",
            trackingReference: null,
            note: null,
            Guid.NewGuid(),
            CancellationToken.None);
        Assert.Equal(ConsolidatedPackageStatus.Created, package.Status);
        Assert.Equal(0, inventory.ConsumeCalls);

        reader.SetPaid(sellerBOrder, paid: false);

        var error = await Assert.ThrowsAsync<ContractOperationException>(() =>
            directory.DispatchConsolidatedPackageAsync(package.ConsolidatedPackageId, Guid.NewGuid(), CancellationToken.None));

        Assert.Equal("fulfillment.order.payment_not_authorized", error.Code);
        Assert.Equal(2, await db.Shipments.CountAsync());
        foreach (var stored in await db.Shipments.ToListAsync())
        {
            Assert.Equal(ShipmentStatus.Created, stored.Status);
            Assert.Null(stored.DispatchedAt);
        }
        Assert.Equal(0, inventory.ConsumeCalls);
        var storedPackage = await db.ConsolidatedPackages.SingleAsync();
        Assert.Equal(ConsolidatedPackageStatus.Created, storedPackage.Status);
    }

    private static async Task<Guid> SeedPaidUnitAsync(FulfillmentDbContext db, FulfillmentDirectory directory)
    {
        await directory.CreateFromPaidSellerOrdersAsync(Guid.NewGuid(), Guid.NewGuid(), [SellerOrder], CancellationToken.None);
        return (await db.Fulfillments.SingleAsync()).FulfillmentId;
    }

    private static OrderFulfillmentHandoffSnapshot Handoff(
        bool paid,
        Guid? sellerOrder = null,
        Guid? sellerParty = null,
        Guid? line = null) =>
        new(
            sellerOrder ?? SellerOrder,
            Checkout,
            sellerParty ?? SellerParty,
            Guid.NewGuid(),
            paid,
            "محمد امامی",
            "09121234567",
            "تهران",
            "تهران",
            "تهران خیابان آزمایش ۱",
            "1234567890",
            "store_courier",
            "پیک فروشگاه",
            [new OrderFulfillmentLineSnapshot(line ?? Line, 1m, null)]);

    private static FulfillmentDirectory CreateDirectory(
        FulfillmentDbContext db,
        IOrderFulfillmentReader orders,
        NoopInventory? inventory = null) =>
        new(
            db,
            new OpenFulfillmentUseCaseGuard(),
            orders,
            inventory ?? new NoopInventory(),
            new FulfillmentInstrumentation(),
            new SystemUtcClock(),
            new UuidV7IdGenerator());

    private static FulfillmentDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<FulfillmentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new FulfillmentDbContext(options);
    }

    private sealed class SwitchableOrderReader(OrderFulfillmentHandoffSnapshot? snapshot) : IOrderFulfillmentReader
    {
        public OrderFulfillmentHandoffSnapshot? Snapshot { get; set; } = snapshot;

        public Task<OrderFulfillmentHandoffSnapshot?> GetHandoffAsync(Guid sellerOrderId, CancellationToken cancellationToken) =>
            Task.FromResult(Snapshot is not null && Snapshot.SellerOrderId == sellerOrderId ? Snapshot : null);

        public Task<OrderFulfillmentHandoffSnapshot?> GetHandoffForCheckoutAsync(
            Guid checkoutId,
            Guid actorUserId,
            CancellationToken cancellationToken) =>
            Task.FromResult<OrderFulfillmentHandoffSnapshot?>(null);
    }

    private sealed class MultiOrderReader(params OrderFulfillmentHandoffSnapshot[] snapshots)
        : IOrderFulfillmentReader
    {
        private readonly Dictionary<Guid, OrderFulfillmentHandoffSnapshot> _bySeller =
            snapshots.ToDictionary(x => x.SellerOrderId);

        public void SetPaid(Guid sellerOrderId, bool paid) =>
            _bySeller[sellerOrderId] = _bySeller[sellerOrderId] with { IsPaid = paid };

        public Task<OrderFulfillmentHandoffSnapshot?> GetHandoffAsync(
            Guid sellerOrderId,
            CancellationToken cancellationToken) =>
            Task.FromResult(_bySeller.TryGetValue(sellerOrderId, out var snapshot) ? snapshot : null);

        public Task<OrderFulfillmentHandoffSnapshot?> GetHandoffForCheckoutAsync(
            Guid checkoutId,
            Guid actorUserId,
            CancellationToken cancellationToken) =>
            Task.FromResult<OrderFulfillmentHandoffSnapshot?>(null);
    }

    private sealed class NoopInventory : IFulfillmentInventoryGateway
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
}

