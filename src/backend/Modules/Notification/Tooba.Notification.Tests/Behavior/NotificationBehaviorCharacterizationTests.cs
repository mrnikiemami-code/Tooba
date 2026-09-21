using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Notification.Application.Models;
using Tooba.Notification.Contracts.Commands;
using Tooba.Notification.Contracts.Copy;
using Tooba.Notification.Contracts.Dtos;
using Tooba.Notification.Contracts.Routes;
using Tooba.Notification.Infrastructure.Directories;
using Tooba.Notification.Infrastructure.Observability;
using Tooba.Notification.Infrastructure.Persistence;
using Xunit;

namespace Tooba.Notification.Tests.Behavior;

public sealed class NotificationBehaviorCharacterizationTests
{
    [Fact]
    public async Task CreateIfAbsent_suppresses_duplicates_and_preserves_source_semantics()
    {
        await using var db = CreateDb();
        var directory = CreateDirectory(db);
        var party = Guid.Parse("11111111-1111-4111-8111-111111111111");
        var cmd = new CreateNotificationCommand(
            NotificationRecipientKind.Customer,
            party,
            party,
            NotificationSemanticTypes.WalletPaymentSucceeded,
            new { amount = 10, currency = "IRR" },
            NotificationTargetRoutes.CustomerWallet(),
            "src-1",
            "wallet.payment.succeeded");

        var first = await directory.CreateIfAbsentAsync(cmd, CancellationToken.None);
        var second = await directory.CreateIfAbsentAsync(cmd, CancellationToken.None);
        Assert.NotNull(first);
        Assert.Null(second);
        Assert.Equal("src-1", first!.SourceEventId);
        Assert.Equal("wallet.payment.succeeded", first.SourceType);
        Assert.Equal(NotificationSemanticTypes.WalletPaymentSucceeded, first.Type);
    }

    [Fact]
    public async Task Customer_and_seller_recipient_filters_are_isolated()
    {
        await using var db = CreateDb();
        var directory = CreateDirectory(db);
        var customer = Guid.Parse("22222222-2222-4222-8222-222222222222");
        var seller = Guid.Parse("33333333-3333-4333-8333-333333333333");
        await directory.CreateIfAbsentAsync(
            new CreateNotificationCommand(
                NotificationRecipientKind.Customer, customer, customer, "payment.succeeded", new { },
                NotificationTargetRoutes.CustomerOrder(Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaa0001")),
                "c1", "payment.succeeded.v1"),
            CancellationToken.None);
        await directory.CreateIfAbsentAsync(
            new CreateNotificationCommand(
                NotificationRecipientKind.Seller, seller, null, "order.paid.seller", new { },
                NotificationTargetRoutes.SellerOrder(Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbb0002")),
                "s1", "payment.succeeded.v1"),
            CancellationToken.None);

        var customerPage = await directory.ListAsync(
            new NotificationRecipientQuery(NotificationRecipientKind.Customer, customer, customer, 0, 20, "en"),
            CancellationToken.None);
        var sellerPage = await directory.ListAsync(
            new NotificationRecipientQuery(NotificationRecipientKind.Seller, seller, null, 0, 20, "en"),
            CancellationToken.None);
        Assert.Equal(1, customerPage.TotalCount);
        Assert.Equal(1, sellerPage.TotalCount);
        Assert.Equal(0, (await directory.ListAsync(
            new NotificationRecipientQuery(NotificationRecipientKind.Seller, customer, null, 0, 20, "en"),
            CancellationToken.None)).TotalCount);
    }

    [Fact]
    public async Task SoftDelete_is_idempotent()
    {
        await using var db = CreateDb();
        var directory = CreateDirectory(db);
        var seller = Guid.Parse("88888888-8888-4888-8888-888888888888");
        var created = await directory.CreateIfAbsentAsync(
            new CreateNotificationCommand(
                NotificationRecipientKind.Seller, seller, null, "order.paid.seller", new { },
                NotificationTargetRoutes.SellerOrder(Guid.Parse("99999999-9999-4999-8999-999999999999")),
                "del-1", "payment.succeeded.v1"),
            CancellationToken.None);
        Assert.NotNull(created);
        Assert.True(await directory.SoftDeleteAsync(created!.NotificationId, NotificationRecipientKind.Seller, seller, null, CancellationToken.None));
        Assert.True(await directory.SoftDeleteAsync(created.NotificationId, NotificationRecipientKind.Seller, seller, null, CancellationToken.None));
        var page = await directory.ListAsync(
            new NotificationRecipientQuery(NotificationRecipientKind.Seller, seller, null, 0, 20, "en"),
            CancellationToken.None);
        Assert.Equal(0, page.TotalCount);
    }

    [Fact]
    public async Task MarkRead_and_MarkAllRead_are_idempotent()
    {
        await using var db = CreateDb();
        var directory = CreateDirectory(db);
        var party = Guid.Parse("44444444-4444-4444-8444-444444444444");
        var created = await directory.CreateIfAbsentAsync(
            new CreateNotificationCommand(
                NotificationRecipientKind.Customer, party, party, "payment.failed", new { },
                NotificationTargetRoutes.CustomerPaymentResult(Guid.Parse("cccccccc-cccc-4ccc-8ccc-cccccccc0003")),
                "m1", "payment.failed.v1"),
            CancellationToken.None);
        Assert.NotNull(created);

        Assert.True(await directory.MarkReadAsync(created!.NotificationId, NotificationRecipientKind.Customer, party, party, CancellationToken.None));
        Assert.True(await directory.MarkReadAsync(created.NotificationId, NotificationRecipientKind.Customer, party, party, CancellationToken.None));
        Assert.Equal(0, await directory.UnreadCountAsync(NotificationRecipientKind.Customer, party, party, CancellationToken.None));

        await directory.CreateIfAbsentAsync(
            new CreateNotificationCommand(
                NotificationRecipientKind.Customer, party, party, "payment.succeeded", new { },
                NotificationTargetRoutes.CustomerOrder(Guid.Parse("dddddddd-dddd-4ddd-8ddd-dddddddd0004")),
                "m2", "payment.succeeded.v1"),
            CancellationToken.None);
        Assert.Equal(1, await directory.MarkAllReadAsync(NotificationRecipientKind.Customer, party, party, CancellationToken.None));
        Assert.Equal(0, await directory.MarkAllReadAsync(NotificationRecipientKind.Customer, party, party, CancellationToken.None));
    }

    [Fact]
    public void Route_allowlist_rejects_unsafe_paths()
    {
        Assert.Throws<InvalidOperationException>(() => NotificationTargetRoutes.RequireAllowed("/admin/secret"));
        Assert.Throws<InvalidOperationException>(() => NotificationTargetRoutes.RequireAllowed("javascript:alert(1)"));
        Assert.Equal("/customer-panel/wallet", NotificationTargetRoutes.CustomerWallet());
    }

    private static NotificationDirectory CreateDirectory(NotificationDbContext db) =>
        new(db, new NotificationInstrumentation(), new SystemUtcClock(), new UuidV7IdGenerator());

    private static NotificationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<NotificationDbContext>()
            .UseInMemoryDatabase("notification-behavior-" + Guid.NewGuid().ToString("N"))
            .Options;
        return new NotificationDbContext(options);
    }
}
