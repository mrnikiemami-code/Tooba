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

    private static async Task<Guid> SeedPaidUnitAsync(FulfillmentDbContext db, FulfillmentDirectory directory)
    {
        await directory.CreateFromPaidSellerOrdersAsync(Guid.NewGuid(), Guid.NewGuid(), [SellerOrder], CancellationToken.None);
        return (await db.Fulfillments.SingleAsync()).FulfillmentId;
    }

    private static OrderFulfillmentHandoffSnapshot Handoff(bool paid) =>
        new(
            SellerOrder,
            Checkout,
            SellerParty,
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
            [new OrderFulfillmentLineSnapshot(Line, 1m, null)]);

    private static FulfillmentDirectory CreateDirectory(FulfillmentDbContext db, IOrderFulfillmentReader orders) =>
        new(
            db,
            new OpenFulfillmentUseCaseGuard(),
            orders,
            new NoopInventory(),
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

    private sealed class NoopInventory : IFulfillmentInventoryGateway
    {
        public Task ConsumeReservationAsync(Guid reservationId, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task CommitReservationForPaidOrderAsync(Guid reservationId, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}

