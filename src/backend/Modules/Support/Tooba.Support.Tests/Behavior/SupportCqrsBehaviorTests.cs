using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Notification.Contracts.Commands;
using Tooba.Notification.Contracts.Ports;
using Tooba.Support.Application.Commands.CloseCustomerTicket;
using Tooba.Support.Application.Commands.CreateCustomerTicket;
using Tooba.Support.Application.Commands.CreateSellerTicket;
using Tooba.Support.Application.Commands.PatchAdminTicket;
using Tooba.Support.Application.Commands.ReopenCustomerTicket;
using Tooba.Support.Application.Commands.ReplyAdminTicket;
using Tooba.Support.Application.Commands.ReplyCustomerTicket;
using Tooba.Support.Application.Commands.ReplySellerTicket;
using Tooba.Support.Application.Errors;
using Tooba.Support.Application.Models;
using Tooba.Support.Application.Ports;
using Tooba.Support.Application.Queries.GetCustomerTicket;
using Tooba.Support.Application.Queries.GetSupportDemoPreview;
using Tooba.Support.Application.Queries.ListAdminTickets;
using Tooba.Support.Application.Queries.ListCustomerTickets;
using Tooba.Support.Application.Queries.ListSellerTickets;
using Tooba.Support.Infrastructure.Adapters;
using Tooba.Support.Infrastructure.Directories;
using Tooba.Support.Infrastructure.Persistence;
using Xunit;

namespace Tooba.Support.Tests.Behavior;

public sealed class SupportCqrsBehaviorTests
{
    [Fact]
    public async Task Customer_create_get_reply_close_reopen_and_idempotency()
    {
        await using var provider = BuildProvider();
        var sender = provider.GetRequiredService<ISender>();
        var customer = Guid.Parse("55555555-5555-4555-8555-555555555555");

        var created = await sender.Send(new CreateCustomerTicketCommand(
            customer, "subject", "Order", "Normal", "hello", null, null, "idem-1"));
        Assert.True(created.IsSuccess);
        Assert.Equal("Open", created.Value.Status);

        var dup = await sender.Send(new CreateCustomerTicketCommand(
            customer, "subject", "Order", "Normal", "hello", null, null, "idem-1"));
        Assert.True(dup.IsSuccess);
        Assert.Equal(created.Value.TicketId, dup.Value.TicketId);

        var got = await sender.Send(new GetCustomerTicketQuery(customer, created.Value.TicketId));
        Assert.True(got.IsSuccess);

        var missing = await sender.Send(new GetCustomerTicketQuery(customer, Guid.Parse("01999999-9999-7999-8999-999999999999")));
        Assert.True(missing.IsFailure);
        Assert.Equal(SupportErrorCodes.Missing, missing.Errors[0].Code);

        var replied = await sender.Send(new ReplyCustomerTicketCommand(
            customer, created.Value.TicketId, "follow-up", "reply-idem-1"));
        Assert.True(replied.IsSuccess);
        var repliedDup = await sender.Send(new ReplyCustomerTicketCommand(
            customer, created.Value.TicketId, "follow-up", "reply-idem-1"));
        Assert.True(repliedDup.IsSuccess);
        Assert.Equal(replied.Value.MessageCount, repliedDup.Value.MessageCount);

        var closed = await sender.Send(new CloseCustomerTicketCommand(customer, created.Value.TicketId));
        Assert.True(closed.IsSuccess);
        Assert.Equal("Closed", closed.Value.Status);

        var reopened = await sender.Send(new ReopenCustomerTicketCommand(customer, created.Value.TicketId));
        Assert.True(reopened.IsSuccess);
        Assert.Equal("Open", reopened.Value.Status);

        var list = await sender.Send(new ListCustomerTicketsQuery(customer, null, 1, 20));
        Assert.True(list.IsSuccess);
        Assert.Equal(1, list.Value.Total);
    }

    [Fact]
    public async Task Seller_create_list_reply_and_admin_patch_demo()
    {
        await using var provider = BuildProvider();
        var sender = provider.GetRequiredService<ISender>();
        var actor = Guid.Parse("77777777-7777-4777-8777-777777777777");
        var sellerParty = Guid.Parse("88888888-8888-4888-8888-888888888888");
        var admin = Guid.Parse("66666666-6666-4666-8666-666666666666");

        var created = await sender.Send(new CreateSellerTicketCommand(
            actor, sellerParty, "seller subject", "Order", "High", "body", null, null, "seller-idem-1"));
        Assert.True(created.IsSuccess);

        var list = await sender.Send(new ListSellerTicketsQuery(sellerParty, null, 1, 20));
        Assert.True(list.IsSuccess);
        Assert.Equal(1, list.Value.Total);

        var replied = await sender.Send(new ReplySellerTicketCommand(
            actor, sellerParty, created.Value.TicketId, "seller reply", "seller-reply-1"));
        Assert.True(replied.IsSuccess);

        var adminReply = await sender.Send(new ReplyAdminTicketCommand(
            admin, created.Value.TicketId, "admin note", true, "admin-reply-1"));
        Assert.True(adminReply.IsSuccess);

        var patched = await sender.Send(new PatchAdminTicketCommand(
            created.Value.TicketId, "Resolved", "Low", admin));
        Assert.True(patched.IsSuccess);
        Assert.Equal("Resolved", patched.Value.Status);
        Assert.Equal("Low", patched.Value.Priority);

        var adminList = await sender.Send(new ListAdminTicketsQuery(
            null, "Seller", null, null, null, 1, 20));
        Assert.True(adminList.IsSuccess);
        Assert.True(adminList.Value.Total >= 1);

        SupportDemoSnapshotStore.Publish(null);
        var notReady = await sender.Send(new GetSupportDemoPreviewQuery());
        Assert.True(notReady.IsFailure);
        Assert.Equal(SupportErrorCodes.DemoNotReady, notReady.Errors[0].Code);

        SupportDemoSnapshotStore.Publish(new SupportDemoSnapshotDto(
            Guid.Parse("01900000-0000-7000-8000-000000000001"),
            Guid.Parse("01900000-0000-7000-8000-000000000002"),
            Guid.Parse("01900000-0000-7000-8000-000000000003"),
            Guid.Parse("01900000-0000-7000-8000-000000000004"),
            actor, sellerParty, actor, admin, null, "test-demo"));
        var ready = await sender.Send(new GetSupportDemoPreviewQuery());
        Assert.True(ready.IsSuccess);
        Assert.Equal("test-demo", ready.Value.Note);
    }

    [Fact]
    public async Task Invalid_category_maps_to_rejected()
    {
        await using var provider = BuildProvider();
        var sender = provider.GetRequiredService<ISender>();
        var customer = Guid.Parse("55555555-5555-4555-8555-555555555555");

        var result = await sender.Send(new CreateCustomerTicketCommand(
            customer, "subject", "NotACategory", "Normal", "hello", null, null, null));
        Assert.True(result.IsFailure);
        Assert.Equal(SupportErrorCodes.Rejected, result.Errors[0].Code);
    }

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(
            typeof(CreateCustomerTicketCommand).Assembly));
        services.AddSingleton<IClock, SystemUtcClock>();
        services.AddSingleton<IIdGenerator, UuidV7IdGenerator>();
        services.AddSingleton<INotificationCreationPort, NoopNotificationPort>();
        services.AddSingleton<ISupportDemoPreviewPort, SupportDemoPreviewAdapter>();
        services.AddDbContext<SupportDbContext>(options =>
            options.UseInMemoryDatabase("support-cqrs-" + Guid.NewGuid().ToString("N")));
        services.AddScoped<ISupportDirectory, SupportDirectory>();
        return services.BuildServiceProvider();
    }

    private sealed class NoopNotificationPort : INotificationCreationPort
    {
        public Task<bool> CreateIfAbsentAsync(CreateNotificationCommand command, CancellationToken cancellationToken) =>
            Task.FromResult(true);
    }
}
