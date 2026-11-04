using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Application.Fulfillments.Ports;
using Tooba.Fulfillment.Domain.Aggregates;
using Tooba.Fulfillment.Infrastructure.Directories;
using Tooba.Fulfillment.Infrastructure.Observability;
using Tooba.Fulfillment.Infrastructure.Persistence;
using Tooba.Order.Application.Admin.Operations.Models;
using Tooba.Order.Application.Admin.Operations.Policies;
using Tooba.Order.Contracts.Fulfillment;
using Tooba.Order.Contracts.Shipping;
using Xunit;
using AppShipmentLine = Tooba.Fulfillment.Application.Fulfillments.Models.ShipmentLineCommand;
using ContractsFulfillmentSnapshot = Tooba.Fulfillment.Contracts.Operations.FulfillmentSnapshot;
using ContractsFulfillmentItemSnapshot = Tooba.Fulfillment.Contracts.Operations.FulfillmentItemSnapshot;
using ContractsFulfillmentStatus = Tooba.Fulfillment.Contracts.Operations.FulfillmentOperationStatus;

namespace Tooba.Order.Tests.Admin;

/// <summary>
/// TB-252-R3: binder trusted Post metadata reaches real Fulfillment directory + validator + persist.
/// </summary>
public sealed class AcceptedGroupTrustedPostBinderDirectoryWiringTests
{
    private static readonly Guid Checkout = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa");
    private static readonly Guid SellerOrder = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb");
    private static readonly Guid SellerParty = Guid.Parse("cccccccc-cccc-4ccc-8ccc-cccccccccccc");
    private static readonly Guid Group = Guid.Parse("11111111-1111-4111-8111-111111111111");
    private static readonly Guid Line = Guid.Parse("33333333-3333-4333-8333-333333333333");

    [Fact]
    public async Task Binder_trusted_post_metadata_creates_persisted_group_parcel_via_directory()
    {
        await using var db = CreateDb();
        var directory = await SeedPackedAsync(db);
        var fulfillmentId = (await db.Fulfillments.SingleAsync()).FulfillmentId;

        const string recipient = "محمد امامی";
        var contractsSnapshot = new ContractsFulfillmentSnapshot(
            fulfillmentId,
            SellerOrder,
            Checkout,
            SellerParty,
            ContractsFulfillmentStatus.Packed,
            recipient,
            "09121234567",
            "تهران",
            "تهران",
            "تهران خیابان آزمایش ۱",
            "1234567890",
            "post",
            "پیشتاز",
            [new ContractsFulfillmentItemSnapshot(Guid.NewGuid(), Line, 2m, 0m, null, 2m, 2m)],
            []);

        var facts = new OrderCommittedCarrierShipmentFacts(
            Checkout,
            Group,
            SellerParty,
            SellerOrder,
            "post",
            "پیشتاز",
            "Manual",
            0,
            "store-alpha",
            null,
            recipient,
            "09121234567",
            "تهران خیابان آزمایش ۱",
            "1234567890",
            "تهران",
            "تهران",
            "IRR",
            125000m,
            0m,
            false,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            "Paid",
            false,
            false,
            false,
            "not_carrier_api",
            [Line],
            [new OrderCommittedCarrierExecutableLine(Line, 2m, 1000m, 2m)],
            fulfillmentId);

        var request = new AdminOrderOperationRequest(
            "create_shipment",
            SellerOrder,
            fulfillmentId,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            [new AdminOrderLineSelection(Line, 1m)],
            "post",
            null,
            null,
            null,
            Group);

        var bound = AcceptedGroupShipmentCreateBinder.Resolve(contractsSnapshot, request, [facts]);
        Assert.False(string.IsNullOrWhiteSpace(bound.TrustedProviderMetadataJson));
        Assert.Contains(recipient, bound.TrustedProviderMetadataJson!, StringComparison.Ordinal);

        var created = await directory.CreateShipmentAsync(
            fulfillmentId,
            Guid.NewGuid(),
            bound.CarrierDisplayName,
            bound.Lines.Select(x => new AppShipmentLine(x.OrderLineId, x.Quantity)).ToArray(),
            CancellationToken.None,
            shippingMethodCode: bound.ShippingMethodCode,
            providerMetadataJson: bound.TrustedProviderMetadataJson,
            shippingGroupId: bound.ShippingGroupId);

        var shipment = Assert.Single(created.Snapshot.Shipments);
        Assert.Equal(Group, shipment.ShippingGroupId);
        Assert.Equal("post", shipment.ShippingMethodCode);
        Assert.Contains(recipient, shipment.ProviderMetadataJson ?? "", StringComparison.Ordinal);
        Assert.Equal(1, await db.Shipments.CountAsync());
    }

    private static async Task<FulfillmentDirectory> SeedPackedAsync(FulfillmentDbContext db)
    {
        var handoff = new OrderFulfillmentHandoffSnapshot(
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
            "post",
            "پیشتاز",
            [new OrderFulfillmentLineSnapshot(Line, 2m, null)],
            [
                new OrderFulfillmentShippingGroupSnapshot(
                    Group,
                    SellerOrder,
                    SellerParty,
                    "post",
                    null,
                    null,
                    "پیشتاز",
                    null,
                    new DateOnly(2026, 11, 23),
                    new DateOnly(2026, 11, 23),
                    "09:00–10:00",
                    1,
                    [Line]),
            ]);
        var directory = CreateDirectory(db, new FixedOrderReader(handoff));
        await directory.CreateFromPaidSellerOrdersAsync(Guid.NewGuid(), Guid.NewGuid(), [SellerOrder], CancellationToken.None);
        var unitId = (await db.Fulfillments.SingleAsync()).FulfillmentId;
        await directory.MarkProcessingAsync(unitId, Guid.NewGuid(), CancellationToken.None);
        await directory.MarkPackedAsync(unitId, Guid.NewGuid(), CancellationToken.None);
        return directory;
    }

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

    private sealed class NoopInventory : IFulfillmentInventoryGateway
    {
        public Task ConsumeReservationAsync(Guid reservationId, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task CommitReservationForPaidOrderAsync(Guid reservationId, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class FixedOrderReader(OrderFulfillmentHandoffSnapshot snapshot) : IOrderFulfillmentReader
    {
        public Task<OrderFulfillmentHandoffSnapshot?> GetHandoffAsync(Guid sellerOrderId, CancellationToken cancellationToken) =>
            Task.FromResult<OrderFulfillmentHandoffSnapshot?>(snapshot);

        public Task<OrderFulfillmentHandoffSnapshot?> GetHandoffForCheckoutAsync(
            Guid checkoutId,
            Guid actorUserId,
            CancellationToken cancellationToken) =>
            Task.FromResult<OrderFulfillmentHandoffSnapshot?>(null);
    }
}
