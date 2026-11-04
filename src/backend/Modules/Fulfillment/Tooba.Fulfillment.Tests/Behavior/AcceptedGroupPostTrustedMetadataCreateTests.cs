using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Application.Fulfillments.Models;
using Tooba.Fulfillment.Application.Fulfillments.Ports;
using Tooba.Fulfillment.Application.Shipping;
using Tooba.Fulfillment.Domain.Aggregates;
using Tooba.Fulfillment.Infrastructure.Directories;
using Tooba.Fulfillment.Infrastructure.Observability;
using Tooba.Fulfillment.Infrastructure.Persistence;
using Tooba.Order.Contracts.Fulfillment;
using Tooba.Order.Contracts.Shipping;
using Xunit;

namespace Tooba.Fulfillment.Tests.Behavior;

/// <summary>
/// TB-252-R2: real Fulfillment directory + Post validator with trusted server metadata (not client JSON).
/// </summary>
public sealed class AcceptedGroupPostTrustedMetadataCreateTests
{
    private static readonly Guid Checkout = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa");
    private static readonly Guid SellerOrder = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb");
    private static readonly Guid SellerParty = Guid.Parse("cccccccc-cccc-4ccc-8ccc-cccccccccccc");
    private static readonly Guid Group = Guid.Parse("11111111-1111-4111-8111-111111111111");
    private static readonly Guid Line = Guid.Parse("33333333-3333-4333-8333-333333333333");

    [Fact]
    public async Task Packed_manual_post_creates_parcel_with_trusted_recipient_metadata()
    {
        await using var db = CreateDb();
        var directory = await SeedPackedAsync(db);

        var facts = ValidFacts();
        var trusted = AcceptedGroupTrustedProviderMetadata.BuildFromFacts("post", facts);
        Assert.NotNull(trusted);
        Assert.Contains("محمد امامی", trusted!, StringComparison.Ordinal);

        var normalized = ShippingProviderMetadataValidator.ValidateAndNormalize("post", trusted);
        Assert.Contains("محمد امامی", normalized!, StringComparison.Ordinal);

        var created = await directory.CreateShipmentAsync(
            (await db.Fulfillments.SingleAsync()).FulfillmentId,
            Guid.NewGuid(),
            "پیشتاز",
            [new ShipmentLineCommand(Line, 1m)],
            CancellationToken.None,
            shippingMethodCode: "post",
            providerMetadataJson: trusted,
            shippingGroupId: Group);

        var shipment = Assert.Single(created.Snapshot.Shipments);
        Assert.Equal(Group, shipment.ShippingGroupId);
        Assert.Equal("post", shipment.ShippingMethodCode);
        Assert.Contains("محمد امامی", shipment.ProviderMetadataJson ?? "", StringComparison.Ordinal);
        Assert.Equal(1, await db.Shipments.CountAsync());
    }

    [Fact]
    public async Task Empty_metadata_refuses_post_before_persist()
    {
        await using var db = CreateDb();
        var directory = await SeedPackedAsync(db);
        var fulfillmentId = (await db.Fulfillments.SingleAsync()).FulfillmentId;

        var ex = await Assert.ThrowsAsync<ContractOperationException>(() =>
            directory.CreateShipmentAsync(
                fulfillmentId,
                Guid.NewGuid(),
                "پیشتاز",
                [new ShipmentLineCommand(Line, 1m)],
                CancellationToken.None,
                shippingMethodCode: "post",
                providerMetadataJson: null,
                shippingGroupId: Group));
        Assert.Equal("fulfillment.shipping.post.recipient_required", ex.Code);
        Assert.Equal(0, await db.Shipments.CountAsync());
    }

    [Fact]
    public void Trusted_builder_refuses_when_committed_recipient_name_absent()
    {
        var facts = ValidFacts() with { RecipientName = "   " };
        var ex = Assert.Throws<AcceptedGroupParcelCreateException>(() =>
            AcceptedGroupTrustedProviderMetadata.BuildFromFacts("post", facts));
        Assert.Equal("fulfillment.shipping.post.recipient_required", ex.Code);
    }

    private static OrderCommittedCarrierShipmentFacts ValidFacts() =>
        new(
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
            "محمد امامی",
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
            [new OrderCommittedCarrierExecutableLine(Line, 2m, 1000m, 2m)]);

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
