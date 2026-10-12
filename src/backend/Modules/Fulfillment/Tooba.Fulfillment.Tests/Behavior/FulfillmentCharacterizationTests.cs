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
using Xunit;

namespace Tooba.Fulfillment.Tests.Behavior;

public sealed class FulfillmentCharacterizationTests
{
    private const string PostMetadata =
        """{"recipientName":"Test Buyer","destinationAddress":"Tehran Address 1","recipientPhone":"09121234567","postalCode":"1234567890"}""";

    [Fact]
    public async Task CreateFromPaid_is_idempotent_on_event_inbox()
    {
        await using var db = CreateDb();
        var inventory = new RecordingInventory();
        var directory = CreateDirectory(db, inventory);
        var sellerOrderId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var handoff = BuildHandoff(sellerOrderId);

        directory = CreateDirectory(db, inventory, new FixedOrderReader(handoff));
        await directory.CreateFromPaidSellerOrdersAsync(paymentId, eventId, [sellerOrderId], CancellationToken.None);
        await directory.CreateFromPaidSellerOrdersAsync(paymentId, eventId, [sellerOrderId], CancellationToken.None);

        Assert.Equal(1, await db.Fulfillments.CountAsync());
        Assert.Equal(1, await db.PaymentInbox.CountAsync());
    }

    [Fact]
    public async Task Dispatch_consumes_reservation_via_inventory_gateway()
    {
        await using var db = CreateDb();
        var reservationId = Guid.NewGuid();
        var inventory = new RecordingInventory();
        var sellerOrderId = Guid.NewGuid();
        var handoff = BuildHandoff(sellerOrderId, reservationId);
        var directory = CreateDirectory(db, inventory, new FixedOrderReader(handoff));
        await directory.CreateFromPaidSellerOrdersAsync(Guid.NewGuid(), Guid.NewGuid(), [sellerOrderId], CancellationToken.None);
        var unit = await db.Fulfillments.SingleAsync();
        await directory.MarkProcessingAsync(unit.FulfillmentId, Guid.NewGuid(), CancellationToken.None);
        await directory.MarkPackedAsync(unit.FulfillmentId, Guid.NewGuid(), CancellationToken.None);
        var snapshot = await directory.CreateShipmentAsync(
            unit.FulfillmentId,
            Guid.NewGuid(),
            "Post",
            [new ShipmentLineCommand(handoff.Lines[0].OrderLineId, 1)],
            CancellationToken.None);
        var shipmentId = snapshot.Shipments.Single().ShipmentId;
        await directory.AssignTrackingAsync(unit.FulfillmentId, shipmentId, Guid.NewGuid(), "TRK-1", CancellationToken.None);
        await directory.DispatchShipmentAsync(unit.FulfillmentId, shipmentId, Guid.NewGuid(), CancellationToken.None);

        Assert.Contains(reservationId, inventory.Consumed);
        var reloaded = await directory.GetAsync(unit.FulfillmentId, CancellationToken.None);
        Assert.Equal(FulfillmentStatus.Dispatched, reloaded!.Status);
    }

    [Fact]
    public async Task Consolidated_package_dispatch_forwards_deterministic_consumer_key()
    {
        await using var db = CreateDb();
        var inventory = new RecordingInventory();
        var checkoutId = Guid.NewGuid();
        var reservationA = Guid.NewGuid();
        var reservationB = Guid.NewGuid();
        var lineA = Guid.NewGuid();
        var lineB = Guid.NewGuid();
        var orderA = Guid.NewGuid();
        var orderB = Guid.NewGuid();
        var handoffA = BuildHandoff(orderA, reservationA, checkoutId, lineA);
        var handoffB = BuildHandoff(orderB, reservationB, checkoutId, lineB);
        var directory = CreateDirectory(db, inventory, new MapOrderReader(new Dictionary<Guid, OrderFulfillmentHandoffSnapshot>
        {
            [orderA] = handoffA,
            [orderB] = handoffB,
        }));

        await directory.CreateFromPaidSellerOrdersAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [orderA, orderB],
            CancellationToken.None);

        var shipmentByOrder = new Dictionary<Guid, Guid>();
        foreach (var orderId in new[] { orderA, orderB })
        {
            var unit = await directory.GetBySellerOrderAsync(orderId, CancellationToken.None);
            Assert.NotNull(unit);
            await directory.MarkProcessingAsync(unit!.FulfillmentId, Guid.NewGuid(), CancellationToken.None);
            await directory.MarkPackedAsync(unit.FulfillmentId, Guid.NewGuid(), CancellationToken.None);
            var shipped = await directory.CreateShipmentAsync(
                unit.FulfillmentId,
                Guid.NewGuid(),
                "پست",
                [new ShipmentLineCommand(unit.Items[0].OrderLineId, 1)],
                CancellationToken.None,
                shippingMethodCode: "post",
                providerMetadataJson: PostMetadata);
            shipmentByOrder[orderId] = shipped.Shipments.Single(x => x.Status == ShipmentStatus.Created).ShipmentId;
        }

        var package = await directory.CreateConsolidatedPackageAsync(
            checkoutId,
            [shipmentByOrder[orderA], shipmentByOrder[orderB]],
            "post",
            "TRK-CENTRAL",
            null,
            Guid.NewGuid(),
            CancellationToken.None);
        Assert.Empty(inventory.KeyedConsumed);

        await directory.DispatchConsolidatedPackageAsync(package.ConsolidatedPackageId, Guid.NewGuid(), CancellationToken.None);

        // Only the consolidated-package path is keyed; the unkeyed seam must stay untouched.
        Assert.Empty(inventory.Consumed);
        Assert.Equal(2, inventory.KeyedConsumed.Count);

        var expectedA = FulfillmentInventoryConsumptionKey.For(package.ConsolidatedPackageId, shipmentByOrder[orderA], lineA);
        var expectedB = FulfillmentInventoryConsumptionKey.For(package.ConsolidatedPackageId, shipmentByOrder[orderB], lineB);
        Assert.Contains((reservationA, expectedA), inventory.KeyedConsumed);
        Assert.Contains((reservationB, expectedB), inventory.KeyedConsumed);

        // Deterministic on retry: same stable inputs reproduce the identical key (no time/random/hash).
        Assert.Equal(expectedA, FulfillmentInventoryConsumptionKey.For(package.ConsolidatedPackageId, shipmentByOrder[orderA], lineA));
        Assert.NotEqual(expectedA, expectedB);
        Assert.All(inventory.KeyedConsumed, x => Assert.True(x.ConsumerKey.Length <= 128, x.ConsumerKey));
    }

    [Fact]
    public async Task Consolidated_package_dispatch_propagates_keyed_consume_failure()
    {
        await using var db = CreateDb();
        var inventory = new ThrowingKeyedInventory();
        var checkoutId = Guid.NewGuid();
        var orderA = Guid.NewGuid();
        var orderB = Guid.NewGuid();
        var handoffA = BuildHandoff(orderA, Guid.NewGuid(), checkoutId, Guid.NewGuid());
        var handoffB = BuildHandoff(orderB, Guid.NewGuid(), checkoutId, Guid.NewGuid());
        var directory = CreateDirectory(db, inventory, new MapOrderReader(new Dictionary<Guid, OrderFulfillmentHandoffSnapshot>
        {
            [orderA] = handoffA,
            [orderB] = handoffB,
        }));

        await directory.CreateFromPaidSellerOrdersAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [orderA, orderB],
            CancellationToken.None);

        var shipmentByOrder = new Dictionary<Guid, Guid>();
        foreach (var orderId in new[] { orderA, orderB })
        {
            var unit = await directory.GetBySellerOrderAsync(orderId, CancellationToken.None);
            await directory.MarkProcessingAsync(unit!.FulfillmentId, Guid.NewGuid(), CancellationToken.None);
            await directory.MarkPackedAsync(unit.FulfillmentId, Guid.NewGuid(), CancellationToken.None);
            var shipped = await directory.CreateShipmentAsync(
                unit.FulfillmentId,
                Guid.NewGuid(),
                "پست",
                [new ShipmentLineCommand(unit.Items[0].OrderLineId, 1)],
                CancellationToken.None,
                shippingMethodCode: "post",
                providerMetadataJson: PostMetadata);
            shipmentByOrder[orderId] = shipped.Shipments.Single(x => x.Status == ShipmentStatus.Created).ShipmentId;
        }

        var package = await directory.CreateConsolidatedPackageAsync(
            checkoutId,
            [shipmentByOrder[orderA], shipmentByOrder[orderB]],
            "post",
            "TRK-CENTRAL",
            null,
            Guid.NewGuid(),
            CancellationToken.None);

        var failure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            directory.DispatchConsolidatedPackageAsync(package.ConsolidatedPackageId, Guid.NewGuid(), CancellationToken.None));
        Assert.Equal("inventory.consume.injected_failure", failure.Message);

        // The typed failure must not be swallowed: no member shipment is persisted as dispatched.
        foreach (var orderId in new[] { orderA, orderB })
        {
            var unit = await directory.GetBySellerOrderAsync(orderId, CancellationToken.None);
            Assert.Equal(FulfillmentStatus.Packed, unit!.Status);
        }
    }

    [Fact]
    public void Cancel_gate_reports_HasDispatchedQuantity()
    {
        var unit = FulfillmentUnit.CreateFromPaidOrder(
            Guid.NewGuid(),
            () => Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "A",
            "09",
            "P",
            "C",
            "Addr",
            "1",
            "post",
            "Post",
            [(Guid.NewGuid(), 2m, null)],
            DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
        Assert.False(unit.HasDispatchedQuantity());
    }

    private static FulfillmentDirectory CreateDirectory(
        FulfillmentDbContext db,
        IFulfillmentInventoryGateway inventory,
        IOrderFulfillmentReader? orders = null) =>
        new(
            db,
            new OpenFulfillmentUseCaseGuard(),
            orders ?? new FixedOrderReader(BuildHandoff(Guid.NewGuid())),
            inventory,
            new FulfillmentInstrumentation(),
            new SystemUtcClock(),
            new UuidV7IdGenerator());

    private static OrderFulfillmentHandoffSnapshot BuildHandoff(
        Guid sellerOrderId,
        Guid? reservationId = null,
        Guid? checkoutId = null,
        Guid? orderLineId = null) =>
        new(
            sellerOrderId,
            checkoutId ?? Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            true,
            "Buyer",
            "0912",
            "Tehran",
            "Tehran",
            "Addr",
            "12345",
            "post",
            "Post",
            [new OrderFulfillmentLineSnapshot(orderLineId ?? Guid.NewGuid(), 1, reservationId)]);

    private static FulfillmentDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<FulfillmentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new FulfillmentDbContext(options);
    }

    private sealed class RecordingInventory : IFulfillmentInventoryGateway
    {
        public List<Guid> Consumed { get; } = [];

        public List<(Guid ReservationId, string ConsumerKey)> KeyedConsumed { get; } = [];

        public List<Guid> Committed { get; } = [];

        public Task ConsumeReservationAsync(Guid reservationId, CancellationToken cancellationToken)
        {
            Consumed.Add(reservationId);
            return Task.CompletedTask;
        }

        public Task ConsumeReservationAsync(Guid reservationId, string consumerKey, CancellationToken cancellationToken)
        {
            KeyedConsumed.Add((reservationId, consumerKey));
            return Task.CompletedTask;
        }

        public Task CommitReservationForPaidOrderAsync(Guid reservationId, CancellationToken cancellationToken)
        {
            Committed.Add(reservationId);
            return Task.CompletedTask;
        }
    }

    private sealed class MapOrderReader(IReadOnlyDictionary<Guid, OrderFulfillmentHandoffSnapshot> handoffs)
        : IOrderFulfillmentReader
    {
        public Task<OrderFulfillmentHandoffSnapshot?> GetHandoffAsync(
            Guid sellerOrderId,
            CancellationToken cancellationToken) =>
            Task.FromResult(handoffs.TryGetValue(sellerOrderId, out var snapshot) ? snapshot : null);

        public Task<OrderFulfillmentHandoffSnapshot?> GetHandoffForCheckoutAsync(
            Guid checkoutId,
            Guid actorUserId,
            CancellationToken cancellationToken) =>
            Task.FromResult<OrderFulfillmentHandoffSnapshot?>(null);
    }

    private sealed class FixedOrderReader(OrderFulfillmentHandoffSnapshot snapshot) : IOrderFulfillmentReader
    {
        public Task<OrderFulfillmentHandoffSnapshot?> GetHandoffAsync(Guid sellerOrderId, CancellationToken cancellationToken) =>
            Task.FromResult<OrderFulfillmentHandoffSnapshot?>(
                snapshot.SellerOrderId == sellerOrderId ? snapshot : null);

        public Task<OrderFulfillmentHandoffSnapshot?> GetHandoffForCheckoutAsync(
            Guid checkoutId,
            Guid actorUserId,
            CancellationToken cancellationToken) =>
            Task.FromResult<OrderFulfillmentHandoffSnapshot?>(null);
    }

    /// <summary>درز موجودی با شکست تزریقی روی اولین مصرف کلیددار (برای اثبات عدم بلع خطا).</summary>
    private sealed class ThrowingKeyedInventory : IFulfillmentInventoryGateway
    {
        public Task ConsumeReservationAsync(Guid reservationId, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task ConsumeReservationAsync(Guid reservationId, string consumerKey, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("inventory.consume.injected_failure");

        public Task CommitReservationForPaidOrderAsync(Guid reservationId, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
