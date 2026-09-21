using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Application.Ports;
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
            [new Tooba.Fulfillment.Application.Models.ShipmentLineCommand(handoff.Lines[0].OrderLineId, 1)],
            CancellationToken.None);
        var shipmentId = snapshot.Shipments.Single().ShipmentId;
        await directory.AssignTrackingAsync(unit.FulfillmentId, shipmentId, Guid.NewGuid(), "TRK-1", CancellationToken.None);
        await directory.DispatchShipmentAsync(unit.FulfillmentId, shipmentId, Guid.NewGuid(), CancellationToken.None);

        Assert.Contains(reservationId, inventory.Consumed);
        var reloaded = await directory.GetAsync(unit.FulfillmentId, CancellationToken.None);
        Assert.Equal(FulfillmentStatus.Dispatched, reloaded!.Status);
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
        RecordingInventory inventory,
        IOrderFulfillmentReader? orders = null) =>
        new(
            db,
            new OpenFulfillmentUseCaseGuard(),
            orders ?? new FixedOrderReader(BuildHandoff(Guid.NewGuid())),
            inventory,
            new FulfillmentInstrumentation(),
            new SystemUtcClock(),
            new UuidV7IdGenerator());

    private static OrderFulfillmentHandoffSnapshot BuildHandoff(Guid sellerOrderId, Guid? reservationId = null) =>
        new(
            sellerOrderId,
            Guid.NewGuid(),
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
            [new OrderFulfillmentLineSnapshot(Guid.NewGuid(), 1, reservationId)]);

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
        public List<Guid> Committed { get; } = [];

        public Task ConsumeReservationAsync(Guid reservationId, CancellationToken cancellationToken)
        {
            Consumed.Add(reservationId);
            return Task.CompletedTask;
        }

        public Task CommitReservationForPaidOrderAsync(Guid reservationId, CancellationToken cancellationToken)
        {
            Committed.Add(reservationId);
            return Task.CompletedTask;
        }
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
}
