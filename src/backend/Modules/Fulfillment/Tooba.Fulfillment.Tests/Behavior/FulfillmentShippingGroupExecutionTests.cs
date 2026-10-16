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

/// <summary>
/// M3 — اجرای گروه‌محور محموله: هر گروه ارسال پذیرفته‌شدهٔ سفارش باید محمولهٔ مستقل خودش را
/// در همان واحد SellerOrder بگیرد، عضویت خطوط دقیق باشد و تکراری/متقاطع ساخته نشود.
/// </summary>
public sealed class FulfillmentShippingGroupExecutionTests
{
    [Fact]
    public async Task One_seller_two_methods_creates_two_group_linked_shipments_in_same_unit()
    {
        await using var db = CreateDb();
        var sellerOrderId = Guid.NewGuid();
        var sellerPartyId = Guid.NewGuid();
        var lineA = Guid.NewGuid();
        var lineB = Guid.NewGuid();
        var groupA = Guid.NewGuid();
        var groupB = Guid.NewGuid();
        var handoff = BuildGroupedHandoff(
            sellerOrderId,
            sellerPartyId,
            [(lineA, 1m, null), (lineB, 1m, null)],
            [
                new OrderFulfillmentShippingGroupSnapshot(
                    groupA, sellerOrderId, sellerPartyId, "post", null, null, "Post", null,
                    new DateOnly(2026, 1, 5), null, null, 1, [lineA]),
                new OrderFulfillmentShippingGroupSnapshot(
                    groupB, sellerOrderId, sellerPartyId, "tipax", null, null, "Tipax", null,
                    new DateOnly(2026, 1, 6), null, null, 2, [lineB]),
            ]);
        var directory = CreateDirectory(db, new FixedOrderReader(handoff));

        await directory.CreateFromPaidSellerOrdersAsync(Guid.NewGuid(), Guid.NewGuid(), [sellerOrderId], CancellationToken.None);

        Assert.Equal(1, await db.Fulfillments.CountAsync());
        Assert.Equal(2, await db.ShippingGroups.CountAsync());
        var unitId = (await db.Fulfillments.SingleAsync()).FulfillmentId;
        await directory.MarkProcessingAsync(unitId, Guid.NewGuid(), CancellationToken.None);
        await directory.MarkPackedAsync(unitId, Guid.NewGuid(), CancellationToken.None);

        var first = await directory.CreateShipmentAsync(
            unitId, Guid.NewGuid(), "Post", [new ShipmentLineCommand(lineA, 1m)],
            CancellationToken.None, shippingGroupId: groupA);
        var second = await directory.CreateShipmentAsync(
            unitId, Guid.NewGuid(), "Tipax", [new ShipmentLineCommand(lineB, 1m)],
            CancellationToken.None, shippingGroupId: groupB);

        Assert.Single(first.Shipments);
        Assert.Equal(groupA, Assert.Single(first.Shipments, x => x.ShippingGroupId == groupA).ShippingGroupId);
        Assert.Equal(2, second.Shipments.Count);
        Assert.Equal(groupB, Assert.Single(second.Shipments, x => x.ShippingGroupId == groupB).ShippingGroupId);

        // خط گروه دیگر را نمی‌توان زیر این گروه فرستاد.
        var mixed = await Assert.ThrowsAsync<ContractOperationException>(() => directory.CreateShipmentAsync(
            unitId, Guid.NewGuid(), "Post", [new ShipmentLineCommand(lineB, 1m)],
            CancellationToken.None, shippingGroupId: groupA));
        Assert.Equal(FulfillmentShippingGroupFault.ShipmentGroupMixed, mixed.Code);

        var persisted = await directory.GetAsync(unitId, CancellationToken.None);
        Assert.Equal(2, persisted!.ShippingGroups!.Count);
        Assert.Equal([lineA], Assert.Single(persisted.ShippingGroups, x => x.ShippingGroupId == groupA).OrderLineIds);
        Assert.Equal([lineB], Assert.Single(persisted.ShippingGroups, x => x.ShippingGroupId == groupB).OrderLineIds);
        Assert.Equal(groupA, Assert.Single(persisted.Items, x => x.OrderLineId == lineA).ShippingGroupId);
        Assert.Equal(groupB, Assert.Single(persisted.Items, x => x.OrderLineId == lineB).ShippingGroupId);
    }

    [Fact]
    public async Task Two_sellers_same_method_never_merge_and_foreign_group_is_refused()
    {
        await using var db = CreateDb();
        var checkoutId = Guid.NewGuid();
        var sellerA = Guid.NewGuid();
        var sellerB = Guid.NewGuid();
        var orderA = Guid.NewGuid();
        var orderB = Guid.NewGuid();
        var lineA = Guid.NewGuid();
        var lineB = Guid.NewGuid();
        var groupA = Guid.NewGuid();
        var groupB = Guid.NewGuid();
        var handoffA = BuildGroupedHandoff(
            orderA, sellerA, [(lineA, 1m, null)],
            [new OrderFulfillmentShippingGroupSnapshot(
                groupA, orderA, sellerA, "post", null, null, "Post", null,
                new DateOnly(2026, 1, 5), null, null, 1, [lineA])],
            checkoutId);
        var handoffB = BuildGroupedHandoff(
            orderB, sellerB, [(lineB, 1m, null)],
            [new OrderFulfillmentShippingGroupSnapshot(
                groupB, orderB, sellerB, "post", null, null, "Post", null,
                new DateOnly(2026, 1, 5), null, null, 1, [lineB])],
            checkoutId);
        var directory = CreateDirectory(db, new MapOrderReader(new Dictionary<Guid, OrderFulfillmentHandoffSnapshot>
        {
            [orderA] = handoffA,
            [orderB] = handoffB,
        }));

        await directory.CreateFromPaidSellerOrdersAsync(
            Guid.NewGuid(), Guid.NewGuid(), [orderA, orderB], CancellationToken.None);

        Assert.Equal(2, await db.Fulfillments.CountAsync());
        Assert.DoesNotContain(await db.ShippingGroups.ToListAsync(), x => x.ShippingGroupId == groupB && x.SellerOrderId == orderA);

        var unitA = await directory.GetBySellerOrderAsync(orderA, CancellationToken.None);
        await directory.MarkProcessingAsync(unitA!.FulfillmentId, Guid.NewGuid(), CancellationToken.None);
        await directory.MarkPackedAsync(unitA.FulfillmentId, Guid.NewGuid(), CancellationToken.None);

        // گروه فروشندهٔ دیگر در این واحد وجود ندارد و نباید پذیرفته شود.
        var foreign = await Assert.ThrowsAsync<ContractOperationException>(() => directory.CreateShipmentAsync(
            unitA.FulfillmentId, Guid.NewGuid(), "Post", [new ShipmentLineCommand(lineA, 1m)],
            CancellationToken.None, shippingGroupId: groupB));
        Assert.Equal(FulfillmentShippingGroupFault.GroupNotFound, foreign.Code);

        // خط گروه A را نمی‌توان به گروه B (که مالک این واحد نیست) نسبت داد.
        var foreignLine = await Assert.ThrowsAsync<ContractOperationException>(() => directory.CreateShipmentAsync(
            unitA.FulfillmentId, Guid.NewGuid(), "Post", [new ShipmentLineCommand(lineB, 1m)],
            CancellationToken.None, shippingGroupId: groupA));
        Assert.Equal("fulfillment.order_line.not_found", foreignLine.Code);
    }

    [Fact]
    public void Wrong_seller_group_is_refused_at_materialization()
    {
        var sellerPartyId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var ex = Assert.Throws<ContractOperationException>(() => FulfillmentUnit.CreateFromPaidOrder(
            Guid.NewGuid(), () => Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), sellerPartyId, Guid.NewGuid(),
            "A", "09", "P", "C", "Addr", "1", "post", "Post",
            [(lineId, 1m, null)],
            DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
            [new FulfillmentShippingGroupPlan(
                Guid.NewGuid(), Guid.NewGuid(), "post", null, null, "Post", null,
                new DateOnly(2026, 1, 5), null, null, 1, [lineId])]));
        Assert.Equal(FulfillmentShippingGroupFault.SellerMismatch, ex.Code);
    }

    [Theory]
    [InlineData("uncovered")]
    [InlineData("unknown")]
    [InlineData("multiple")]
    public void Inexact_member_line_coverage_fails_closed(string scenario)
    {
        var sellerOrderId = Guid.NewGuid();
        var sellerPartyId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var otherLineId = Guid.NewGuid();
        var lines = new List<(Guid OrderLineId, decimal Quantity, Guid? ReservationId)> { (lineId, 1m, null) };
        if (scenario == "uncovered")
        {
            lines.Add((otherLineId, 1m, null));
        }

        var groupLines = new List<Guid> { lineId };
        if (scenario == "unknown")
        {
            groupLines.Add(Guid.NewGuid());
        }

        var plans = new List<FulfillmentShippingGroupPlan> { Plan(Guid.NewGuid(), sellerPartyId, groupLines) };
        if (scenario == "multiple")
        {
            plans.Add(Plan(Guid.NewGuid(), sellerPartyId, groupLines));
        }

        var ex = Assert.Throws<ContractOperationException>(() =>
        {
            _ = FulfillmentUnit.CreateFromPaidOrder(
                Guid.NewGuid(), () => Guid.NewGuid(),
                sellerOrderId, Guid.NewGuid(), sellerPartyId, Guid.NewGuid(),
                "A", "09", "P", "C", "Addr", "1", "post", "Post",
                lines,
                DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
                plans);
        });
        var expected = scenario switch
        {
            "uncovered" => FulfillmentShippingGroupFault.LineUncovered,
            "unknown" => FulfillmentShippingGroupFault.LineUnknown,
            _ => FulfillmentShippingGroupFault.LineInMultipleGroups,
        };
        Assert.Equal(expected, ex.Code);
    }

    [Fact]
    public async Task Replay_of_paid_order_event_does_not_duplicate_groups_or_shipments()
    {
        await using var db = CreateDb();
        var sellerOrderId = Guid.NewGuid();
        var sellerPartyId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var handoff = BuildGroupedHandoff(
            sellerOrderId, sellerPartyId, [(lineId, 1m, null)],
            [new OrderFulfillmentShippingGroupSnapshot(
                groupId, sellerOrderId, sellerPartyId, "post", null, null, "Post", null,
                new DateOnly(2026, 1, 5), null, null, 1, [lineId])]);
        var directory = CreateDirectory(db, new FixedOrderReader(handoff));
        var eventId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();

        await directory.CreateFromPaidSellerOrdersAsync(paymentId, eventId, [sellerOrderId], CancellationToken.None);
        await directory.CreateFromPaidSellerOrdersAsync(paymentId, eventId, [sellerOrderId], CancellationToken.None);

        Assert.Equal(1, await db.Fulfillments.CountAsync());
        Assert.Equal(1, await db.ShippingGroups.CountAsync());
        var unitId = (await db.Fulfillments.SingleAsync()).FulfillmentId;
        await directory.MarkProcessingAsync(unitId, Guid.NewGuid(), CancellationToken.None);
        await directory.MarkPackedAsync(unitId, Guid.NewGuid(), CancellationToken.None);
        await directory.CreateShipmentAsync(
            unitId, Guid.NewGuid(), "Post", [new ShipmentLineCommand(lineId, 1m)],
            CancellationToken.None, shippingGroupId: groupId);

        var snapshot = await directory.GetAsync(unitId, CancellationToken.None);
        Assert.Single(snapshot!.Shipments);
        Assert.Single(snapshot.ShippingGroups!);
    }

    [Fact]
    public async Task Legacy_single_method_order_stays_ungrouped_and_works()
    {
        await using var db = CreateDb();
        var sellerOrderId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var handoff = BuildHandoff(sellerOrderId, lineId);
        var directory = CreateDirectory(db, new FixedOrderReader(handoff));

        await directory.CreateFromPaidSellerOrdersAsync(Guid.NewGuid(), Guid.NewGuid(), [sellerOrderId], CancellationToken.None);

        Assert.Empty(await db.ShippingGroups.ToListAsync());
        var unitId = (await db.Fulfillments.SingleAsync()).FulfillmentId;
        await directory.MarkProcessingAsync(unitId, Guid.NewGuid(), CancellationToken.None);
        await directory.MarkPackedAsync(unitId, Guid.NewGuid(), CancellationToken.None);

        // مسیر تک‌روشی قدیمی بدون گروه کار می‌کند.
        var legacy = await directory.CreateShipmentAsync(
            unitId, Guid.NewGuid(), "Post", [new ShipmentLineCommand(lineId, 1m)],
            CancellationToken.None);
        Assert.Null(Assert.Single(legacy.Shipments).ShippingGroupId);

        // سفارش بدون گروه‌بندی نباید ادعای گروه کند.
        var bogus = await Assert.ThrowsAsync<ContractOperationException>(() => directory.CreateShipmentAsync(
            unitId, Guid.NewGuid(), "Post", [new ShipmentLineCommand(lineId, 1m)],
            CancellationToken.None, shippingGroupId: Guid.NewGuid()));
        Assert.Equal(FulfillmentShippingGroupFault.GroupNotApplicable, bogus.Code);
    }

    [Fact]
    public async Task Grouped_order_requires_group_on_shipment_creation()
    {
        await using var db = CreateDb();
        var sellerOrderId = Guid.NewGuid();
        var sellerPartyId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var directory = CreateDirectory(db, new FixedOrderReader(BuildGroupedHandoff(
            sellerOrderId, sellerPartyId, [(lineId, 1m, null)],
            [new OrderFulfillmentShippingGroupSnapshot(
                Guid.NewGuid(), sellerOrderId, sellerPartyId, "post", null, null, "Post", null,
                new DateOnly(2026, 1, 5), null, null, 1, [lineId])])));

        await directory.CreateFromPaidSellerOrdersAsync(Guid.NewGuid(), Guid.NewGuid(), [sellerOrderId], CancellationToken.None);
        var unitId = (await db.Fulfillments.SingleAsync()).FulfillmentId;
        await directory.MarkProcessingAsync(unitId, Guid.NewGuid(), CancellationToken.None);
        await directory.MarkPackedAsync(unitId, Guid.NewGuid(), CancellationToken.None);

        var ex = await Assert.ThrowsAsync<ContractOperationException>(() => directory.CreateShipmentAsync(
            unitId, Guid.NewGuid(), "Post", [new ShipmentLineCommand(lineId, 1m)],
            CancellationToken.None));
        Assert.Equal(FulfillmentShippingGroupFault.ShipmentGroupRequired, ex.Code);
    }

    [Fact]
    public async Task Each_group_transitions_independently()
    {
        await using var db = CreateDb();
        var sellerOrderId = Guid.NewGuid();
        var sellerPartyId = Guid.NewGuid();
        var lineA = Guid.NewGuid();
        var lineB = Guid.NewGuid();
        var groupA = Guid.NewGuid();
        var groupB = Guid.NewGuid();
        var directory = CreateDirectory(db, new FixedOrderReader(BuildGroupedHandoff(
            sellerOrderId, sellerPartyId, [(lineA, 1m, null), (lineB, 1m, null)],
            [
                new OrderFulfillmentShippingGroupSnapshot(
                    groupA, sellerOrderId, sellerPartyId, "post", null, null, "Post", null,
                    new DateOnly(2026, 1, 5), null, null, 1, [lineA]),
                new OrderFulfillmentShippingGroupSnapshot(
                    groupB, sellerOrderId, sellerPartyId, "tipax", null, null, "Tipax", null,
                    new DateOnly(2026, 1, 6), null, null, 2, [lineB]),
            ])));

        await directory.CreateFromPaidSellerOrdersAsync(Guid.NewGuid(), Guid.NewGuid(), [sellerOrderId], CancellationToken.None);
        var unitId = (await db.Fulfillments.SingleAsync()).FulfillmentId;
        await directory.MarkProcessingAsync(unitId, Guid.NewGuid(), CancellationToken.None);
        await directory.MarkPackedAsync(unitId, Guid.NewGuid(), CancellationToken.None);
        var groupAShipment = (await directory.CreateShipmentAsync(
            unitId, Guid.NewGuid(), "Post", [new ShipmentLineCommand(lineA, 1m)],
            CancellationToken.None, shippingGroupId: groupA)).Shipments.Single().ShipmentId;
        var groupBShipment = (await directory.CreateShipmentAsync(
            unitId, Guid.NewGuid(), "Tipax", [new ShipmentLineCommand(lineB, 1m)],
            CancellationToken.None, shippingGroupId: groupB)).Shipments.Single(x => x.ShippingGroupId == groupB).ShipmentId;

        await directory.AssignTrackingAsync(unitId, groupAShipment, Guid.NewGuid(), "TRK-A", CancellationToken.None);
        await directory.DispatchShipmentAsync(unitId, groupAShipment, Guid.NewGuid(), CancellationToken.None);

        var snapshot = await directory.GetAsync(unitId, CancellationToken.None);
        Assert.Equal(ShipmentStatus.Dispatched, Assert.Single(snapshot!.Shipments, x => x.ShipmentId == groupAShipment).Status);
        Assert.Equal(ShipmentStatus.Created, Assert.Single(snapshot.Shipments, x => x.ShipmentId == groupBShipment).Status);

        // گروه دیگر دست‌نخورده می‌ماند و می‌تواند مستقل ابطال شود.
        await directory.CancelShipmentAsync(unitId, groupBShipment, Guid.NewGuid(), CancellationToken.None);
        var afterCancel = await directory.GetAsync(unitId, CancellationToken.None);
        Assert.Equal(ShipmentStatus.Cancelled, Assert.Single(afterCancel!.Shipments, x => x.ShipmentId == groupBShipment).Status);
        Assert.Equal(ShipmentStatus.Dispatched, Assert.Single(afterCancel.Shipments, x => x.ShipmentId == groupAShipment).Status);
    }

    private static FulfillmentShippingGroupPlan Plan(Guid groupId, Guid sellerPartyId, IReadOnlyList<Guid> lines) =>
        new(
            groupId, sellerPartyId, "post", null, null, "Post", null,
            new DateOnly(2026, 1, 5), null, null, 1, lines);

    private static FulfillmentDirectory CreateDirectory(FulfillmentDbContext db, IOrderFulfillmentReader orders) =>
        new(
            db,
            new OpenFulfillmentUseCaseGuard(),
            orders,
            new NoopInventory(),
            new FulfillmentInstrumentation(),
            new SystemUtcClock(),
            new UuidV7IdGenerator());

    private static OrderFulfillmentHandoffSnapshot BuildHandoff(Guid sellerOrderId, Guid orderLineId) =>
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
            [new OrderFulfillmentLineSnapshot(orderLineId, 1m, null)]);

    private static OrderFulfillmentHandoffSnapshot BuildGroupedHandoff(
        Guid sellerOrderId,
        Guid sellerPartyId,
        IReadOnlyList<(Guid OrderLineId, decimal Quantity, Guid? ReservationId)> lines,
        IReadOnlyList<OrderFulfillmentShippingGroupSnapshot> groups,
        Guid? checkoutId = null) =>
        new(
            sellerOrderId,
            checkoutId ?? Guid.NewGuid(),
            sellerPartyId,
            Guid.NewGuid(),
            true,
            "Buyer",
            "0912",
            "Tehran",
            "Tehran",
            "Addr",
            "12345",
            string.Empty,
            string.Empty,
            lines.Select(x => new OrderFulfillmentLineSnapshot(x.OrderLineId, x.Quantity, x.ReservationId)).ToArray(),
            groups);

    private static FulfillmentDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<FulfillmentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new FulfillmentDbContext(options);
    }

    private sealed class NoopInventory : IFulfillmentInventoryGateway
    {
        public Task ConsumeReservationAsync(Guid reservationId, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task CommitReservationForPaidOrderAsync(Guid reservationId, CancellationToken cancellationToken) =>
            Task.CompletedTask;
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
}
