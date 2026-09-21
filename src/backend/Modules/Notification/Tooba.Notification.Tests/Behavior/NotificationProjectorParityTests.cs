using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Notification.Application.Models;
using Tooba.Notification.Application.Rendering;
using Tooba.Notification.Contracts.Dtos;
using Tooba.Notification.Contracts.Routes;
using Tooba.Notification.Infrastructure.Directories;
using Tooba.Notification.Infrastructure.Observability;
using Tooba.Notification.Infrastructure.Persistence;
using Tooba.Notification.Infrastructure.Projectors;
using Tooba.Order.Contracts.Notifications;
using Xunit;

namespace Tooba.Notification.Tests.Behavior;

public sealed class NotificationProjectorParityTests
{
    [Fact]
    public async Task ProjectFromCheckout_creates_customer_and_seller_recipients()
    {
        await using var db = CreateDb();
        var directory = CreateDirectory(db);
        var checkoutId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaa0101");
        var sellerOrderA = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbb0101");
        var sellerOrderB = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbb0102");
        var placedBy = Guid.Parse("11111111-1111-4111-8111-111111111101");
        var buyer = Guid.Parse("22222222-2222-4222-8222-222222222201");
        var sellerA = Guid.Parse("33333333-3333-4333-8333-333333333301");
        var sellerB = Guid.Parse("33333333-3333-4333-8333-333333333302");

        var reader = new StubOrderNotificationReader(new OrderNotificationRecipientSnapshot(
            checkoutId,
            buyer,
            placedBy,
            [
                new OrderNotificationSellerSnapshot(sellerOrderA, sellerA),
                new OrderNotificationSellerSnapshot(sellerOrderB, sellerB),
            ]));

        var projector = new NotificationProjector(directory, reader);
        var payload = new { checkoutId, amount = 42 };
        await projector.ProjectFromCheckoutAsync(
            checkoutId,
            "src-checkout-1",
            "payment.succeeded.v1",
            NotificationCopy.PaymentSucceeded,
            NotificationCopy.OrderPaidSeller,
            payload,
            payload,
            NotificationTargetRoutes.CustomerOrder(checkoutId),
            sellerOrderId => NotificationTargetRoutes.SellerOrder(sellerOrderId),
            CancellationToken.None);

        var customerPage = await directory.ListAsync(
            new NotificationRecipientQuery(NotificationRecipientKind.Customer, placedBy, placedBy, 0, 20, "en"),
            CancellationToken.None);
        Assert.Equal(1, customerPage.TotalCount);
        Assert.Equal("payment.succeeded.v1", customerPage.Items[0].SourceType);
        Assert.Equal(NotificationTargetRoutes.CustomerOrder(checkoutId), customerPage.Items[0].TargetRoute);

        var sellerAPage = await directory.ListAsync(
            new NotificationRecipientQuery(NotificationRecipientKind.Seller, sellerA, null, 0, 20, "en"),
            CancellationToken.None);
        var sellerBPage = await directory.ListAsync(
            new NotificationRecipientQuery(NotificationRecipientKind.Seller, sellerB, null, 0, 20, "en"),
            CancellationToken.None);
        Assert.Equal(1, sellerAPage.TotalCount);
        Assert.Equal(1, sellerBPage.TotalCount);
        Assert.Equal(NotificationTargetRoutes.SellerOrder(sellerOrderA), sellerAPage.Items[0].TargetRoute);
        Assert.Equal(NotificationTargetRoutes.SellerOrder(sellerOrderB), sellerBPage.Items[0].TargetRoute);
        Assert.Equal("payment.succeeded.v1", sellerAPage.Items[0].SourceType);
        Assert.Equal("payment.succeeded.v1", sellerBPage.Items[0].SourceType);

        var persisted = await db.Notifications.AsNoTracking().ToListAsync();
        Assert.Equal(3, persisted.Count);
        Assert.All(persisted, x => Assert.Equal("src-checkout-1", x.SourceEventId));
        Assert.All(persisted, x => Assert.Equal("payment.succeeded.v1", x.SourceType));
    }

    [Fact]
    public async Task ProjectFromSellerOrder_preserves_fallback_and_target_behavior()
    {
        await using var db = CreateDb();
        var directory = CreateDirectory(db);
        var checkoutId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaa0202");
        var targetSellerOrder = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbb0202");
        var otherSellerOrder = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbb0299");
        var placedBy = Guid.Parse("11111111-1111-4111-8111-111111111102");
        var targetSeller = Guid.Parse("33333333-3333-4333-8333-333333333311");
        var otherSeller = Guid.Parse("33333333-3333-4333-8333-333333333399");

        var reader = new StubOrderNotificationReader(new OrderNotificationRecipientSnapshot(
            checkoutId,
            BuyerPartyId: null,
            placedBy,
            [
                new OrderNotificationSellerSnapshot(otherSellerOrder, otherSeller),
                new OrderNotificationSellerSnapshot(targetSellerOrder, targetSeller),
            ]));

        var projector = new NotificationProjector(directory, reader);
        var payload = new { sellerOrderId = targetSellerOrder };
        var sellerTarget = NotificationTargetRoutes.SellerOrder(targetSellerOrder);
        await projector.ProjectFromSellerOrderAsync(
            targetSellerOrder,
            "src-seller-1",
            "fulfillment.created.v1",
            NotificationCopy.FulfillmentCreated,
            NotificationCopy.FulfillmentCreated,
            payload,
            id => NotificationTargetRoutes.CustomerOrder(id),
            sellerTarget,
            CancellationToken.None);

        var customerPage = await directory.ListAsync(
            new NotificationRecipientQuery(NotificationRecipientKind.Customer, placedBy, placedBy, 0, 20, "en"),
            CancellationToken.None);
        Assert.Equal(1, customerPage.TotalCount);
        Assert.Equal(NotificationTargetRoutes.CustomerOrder(checkoutId), customerPage.Items[0].TargetRoute);
        Assert.Equal("fulfillment.created.v1", customerPage.Items[0].SourceType);

        var targetSellerPage = await directory.ListAsync(
            new NotificationRecipientQuery(NotificationRecipientKind.Seller, targetSeller, null, 0, 20, "en"),
            CancellationToken.None);
        var otherSellerPage = await directory.ListAsync(
            new NotificationRecipientQuery(NotificationRecipientKind.Seller, otherSeller, null, 0, 20, "en"),
            CancellationToken.None);
        Assert.Equal(1, targetSellerPage.TotalCount);
        Assert.Equal(0, otherSellerPage.TotalCount);
        Assert.Equal(sellerTarget, targetSellerPage.Items[0].TargetRoute);
        Assert.All(await db.Notifications.AsNoTracking().ToListAsync(), x => Assert.Equal("src-seller-1", x.SourceEventId));

        // Missing exact seller → fallback to first seller in snapshot.
        await using var dbFallback = CreateDb();
        var directoryFallback = CreateDirectory(dbFallback);
        var missingSellerOrder = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbb0888");
        var fallbackReader = new StubOrderNotificationReader(new OrderNotificationRecipientSnapshot(
            checkoutId,
            null,
            placedBy,
            [new OrderNotificationSellerSnapshot(otherSellerOrder, otherSeller)]));
        var fallbackProjector = new NotificationProjector(directoryFallback, fallbackReader);
        await fallbackProjector.ProjectFromSellerOrderAsync(
            missingSellerOrder,
            "src-seller-fallback",
            "fulfillment.created.v1",
            NotificationCopy.FulfillmentCreated,
            NotificationCopy.FulfillmentCreated,
            payload,
            id => NotificationTargetRoutes.CustomerOrder(id),
            NotificationTargetRoutes.SellerOrder(missingSellerOrder),
            CancellationToken.None);
        var fallbackSellerPage = await directoryFallback.ListAsync(
            new NotificationRecipientQuery(NotificationRecipientKind.Seller, otherSeller, null, 0, 20, "en"),
            CancellationToken.None);
        Assert.Equal(1, fallbackSellerPage.TotalCount);
        Assert.Equal(NotificationTargetRoutes.SellerOrder(missingSellerOrder), fallbackSellerPage.Items[0].TargetRoute);
    }

    private static NotificationDirectory CreateDirectory(NotificationDbContext db) =>
        new(db, new NotificationInstrumentation(), new SystemUtcClock(), new UuidV7IdGenerator());

    private static NotificationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<NotificationDbContext>()
            .UseInMemoryDatabase("notification-projector-" + Guid.NewGuid().ToString("N"))
            .Options;
        return new NotificationDbContext(options);
    }

    private sealed class StubOrderNotificationReader : IOrderNotificationReader
    {
        private readonly OrderNotificationRecipientSnapshot? _snapshot;

        public StubOrderNotificationReader(OrderNotificationRecipientSnapshot? snapshot) => _snapshot = snapshot;

        public Task<OrderNotificationRecipientSnapshot?> GetByCheckoutIdAsync(Guid checkoutId, CancellationToken cancellationToken) =>
            Task.FromResult(_snapshot is not null && _snapshot.CheckoutId == checkoutId ? _snapshot : null);

        public Task<OrderNotificationRecipientSnapshot?> GetBySellerOrderIdAsync(Guid sellerOrderId, CancellationToken cancellationToken) =>
            Task.FromResult(_snapshot);
    }
}
