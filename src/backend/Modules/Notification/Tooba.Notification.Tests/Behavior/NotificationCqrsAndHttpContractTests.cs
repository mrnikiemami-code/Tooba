using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Notification.Application.Commands.DismissCustomerNotification;
using Tooba.Notification.Application.Commands.MarkAllCustomerNotificationsRead;
using Tooba.Notification.Application.Commands.MarkCustomerNotificationRead;
using Tooba.Notification.Application.Errors;
using Tooba.Notification.Application.Models;
using Tooba.Notification.Application.Ports;
using Tooba.Notification.Application.Queries.GetCustomerUnreadNotificationCount;
using Tooba.Notification.Application.Queries.ListCustomerNotifications;
using Tooba.Notification.Application.Queries.ListSellerNotifications;
using Tooba.Notification.Contracts.Commands;
using Tooba.Notification.Contracts.Dtos;
using Tooba.Notification.Contracts.Routes;
using Tooba.Notification.Infrastructure.Directories;
using Tooba.Notification.Infrastructure.Observability;
using Tooba.Notification.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MediatR;
using Tooba.BuildingBlocks.DependencyInjection;
using Xunit;

namespace Tooba.Notification.Tests.Behavior;

public sealed class NotificationCqrsAndHttpContractTests
{
    [Fact]
    public async Task Customer_list_paging_locale_unread_mark_dismiss_preserve_shapes()
    {
        await using var provider = BuildProvider();
        var directory = provider.GetRequiredService<INotificationDirectory>();
        var sender = provider.GetRequiredService<ISender>();
        var actor = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaa0010");

        await SeedCustomerAsync(directory, actor, "c-1", "payment.succeeded");
        await SeedCustomerAsync(directory, actor, "c-2", "payment.failed");

        var list = await sender.Send(new ListCustomerNotificationsQuery(actor, 0, 1, "en"), CancellationToken.None);
        Assert.True(list.IsSuccess);
        Assert.Equal(1, list.Value.Items.Count);
        Assert.Equal(2, list.Value.TotalCount);
        Assert.Equal(0, list.Value.Skip);
        Assert.Equal(1, list.Value.Take);
        Assert.Equal(2, list.Value.UnreadCount);
        Assert.False(string.IsNullOrWhiteSpace(list.Value.Items[0].Title));
        Assert.False(string.IsNullOrWhiteSpace(list.Value.Items[0].TargetRoute));

        var unread = await sender.Send(new GetCustomerUnreadNotificationCountQuery(actor), CancellationToken.None);
        Assert.True(unread.IsSuccess);
        Assert.Equal(2, unread.Value.UnreadCount);

        var firstId = list.Value.Items[0].NotificationId;
        var marked = await sender.Send(new MarkCustomerNotificationReadCommand(firstId, actor), CancellationToken.None);
        Assert.True(marked.IsSuccess);

        var missing = await sender.Send(
            new MarkCustomerNotificationReadCommand(Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbb0011"), actor),
            CancellationToken.None);
        Assert.True(missing.IsFailure);
        Assert.Equal(NotificationErrorCodes.Missing, missing.Errors[0].Code);

        var markAll = await sender.Send(new MarkAllCustomerNotificationsReadCommand(actor), CancellationToken.None);
        Assert.True(markAll.IsSuccess);
        Assert.True(markAll.Value.MarkedCount >= 0);

        var dismissOk = await sender.Send(new DismissCustomerNotificationCommand(firstId, actor), CancellationToken.None);
        Assert.True(dismissOk.IsSuccess);
        var dismissMissing = await sender.Send(
            new DismissCustomerNotificationCommand(Guid.Parse("cccccccc-cccc-4ccc-8ccc-cccccccc0012"), actor),
            CancellationToken.None);
        Assert.True(dismissMissing.IsFailure);
        Assert.Equal(NotificationErrorCodes.Missing, dismissMissing.Errors[0].Code);
    }

    [Fact]
    public async Task Seller_list_and_mark_paths_scope_by_party()
    {
        await using var provider = BuildProvider();
        var directory = provider.GetRequiredService<INotificationDirectory>();
        var sender = provider.GetRequiredService<ISender>();
        var sellerA = Guid.Parse("dddddddd-dddd-4ddd-8ddd-dddddddd0013");
        var sellerB = Guid.Parse("eeeeeeee-eeee-4eee-8eee-eeeeeeee0014");

        await directory.CreateIfAbsentAsync(
            new CreateNotificationCommand(
                NotificationRecipientKind.Seller, sellerA, null, "order.paid.seller", new { },
                NotificationTargetRoutes.SellerOrder(Guid.Parse("ffffffff-ffff-4fff-8fff-ffffffff0015")),
                "s-a", "payment.succeeded.v1"),
            CancellationToken.None);
        await directory.CreateIfAbsentAsync(
            new CreateNotificationCommand(
                NotificationRecipientKind.Seller, sellerB, null, "order.paid.seller", new { },
                NotificationTargetRoutes.SellerOrder(Guid.Parse("11111111-1111-4111-8111-111111110016")),
                "s-b", "payment.succeeded.v1"),
            CancellationToken.None);

        var pageA = await sender.Send(new ListSellerNotificationsQuery(sellerA, 0, 20, "fa"), CancellationToken.None);
        Assert.True(pageA.IsSuccess);
        Assert.Equal(1, pageA.Value.TotalCount);

        var markAll = await sender.Send(new Application.Commands.MarkAllSellerNotificationsRead.MarkAllSellerNotificationsReadCommand(sellerA), CancellationToken.None);
        Assert.True(markAll.IsSuccess);

        var id = pageA.Value.Items[0].NotificationId;
        var dismiss = await sender.Send(
            new Application.Commands.DismissSellerNotification.DismissSellerNotificationCommand(id, sellerA),
            CancellationToken.None);
        Assert.True(dismiss.IsSuccess);
    }

    [Fact]
    public void Http_mapper_preserves_wire_field_contract()
    {
        var page = new NotificationListPage(
            [
                new NotificationListItemDto(
                    Guid.Parse("22222222-2222-4222-8222-222222220017"),
                    "payment.succeeded",
                    "payment",
                    "Title",
                    "Body",
                    "{}",
                    "/customer-panel/orders/x",
                    false,
                    null,
                    DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
                    "src")
            ],
            5,
            10,
            42,
            7);
        var mapped = NotificationHttpMapper.ToListResponse(page);
        Assert.Equal(5, mapped.Skip);
        Assert.Equal(10, mapped.Take);
        Assert.Equal(42, mapped.TotalCount);
        Assert.Equal(7, mapped.UnreadCount);
        Assert.Equal("payment.succeeded", mapped.Items[0].Type);
        Assert.Equal("payment", mapped.Items[0].Category);
        Assert.Equal("Title", mapped.Items[0].Title);
        Assert.Equal("Body", mapped.Items[0].Body);
        Assert.Equal("/customer-panel/orders/x", mapped.Items[0].TargetRoute);
        Assert.False(mapped.Items[0].IsRead);
    }

    [Fact]
    public void Customer_session_required_code_is_stable()
    {
        Assert.Equal("customer.session.required", NotificationErrorCodes.CustomerSessionRequired);
        Assert.Equal("notification.missing", NotificationErrorCodes.Missing);
        var failure = Result.Failure(new SemanticError(NotificationErrorCodes.CustomerSessionRequired));
        Assert.True(failure.IsFailure);
        Assert.Equal(NotificationErrorCodes.CustomerSessionRequired, failure.Errors[0].Code);
    }

    private static async Task SeedCustomerAsync(INotificationDirectory directory, Guid actor, string source, string type)
    {
        await directory.CreateIfAbsentAsync(
            new CreateNotificationCommand(
                NotificationRecipientKind.Customer,
                actor,
                actor,
                type,
                new { amount = 1 },
                NotificationTargetRoutes.CustomerOrder(Guid.Parse("33333333-3333-4333-8333-333333330018")),
                source,
                type + ".v1"),
            CancellationToken.None);
    }

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IClock, SystemUtcClock>();
        services.AddSingleton<IIdGenerator, UuidV7IdGenerator>();
        services.AddSingleton<NotificationInstrumentation>();
        services.AddDbContext<NotificationDbContext>(o => o.UseInMemoryDatabase("notification-cqrs-" + Guid.NewGuid().ToString("N")));
        services.AddScoped<NotificationDirectory>();
        services.AddScoped<INotificationDirectory>(sp => sp.GetRequiredService<NotificationDirectory>());
        services.AddToobaCqrsFoundation(typeof(ListCustomerNotificationsQuery).Assembly);
        return services.BuildServiceProvider();
    }
}
