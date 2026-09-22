using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Notification.Contracts.Commands;
using Tooba.Notification.Contracts.Copy;
using Tooba.Notification.Contracts.Ports;
using Tooba.Support.Application.Models;
using Tooba.Support.Infrastructure.Directories;
using Tooba.Support.Infrastructure.Persistence;
using Xunit;

namespace Tooba.Support.Tests.Behavior;

public sealed class SupportBehaviorCharacterizationTests
{
    [Fact]
    public async Task Ticket_create_reply_status_transition_and_admin_notification_semantics()
    {
        await using var db = CreateDb();
        var notes = new RecordingNotificationPort();
        var directory = new SupportDirectory(db, notes, new SystemUtcClock(), new UuidV7IdGenerator());
        var customer = Guid.Parse("55555555-5555-4555-8555-555555555555");
        var admin = Guid.Parse("66666666-6666-4666-8666-666666666666");

        var created = await directory.CreateForCustomerAsync(
            customer,
            new CreateTicketCommand("subject", "Order", "Normal", "hello", null, null, "idem-1"),
            CancellationToken.None);
        Assert.Equal("Open", created.Status);
        Assert.Equal(1, created.MessageCount);

        var dup = await directory.CreateForCustomerAsync(
            customer,
            new CreateTicketCommand("subject", "Order", "Normal", "hello", null, null, "idem-1"),
            CancellationToken.None);
        Assert.Equal(created.TicketId, dup.TicketId);

        var replied = await directory.ReplyForAdminAsync(
            admin,
            created.TicketId,
            new ReplyTicketCommand("admin public", false, "reply-1"),
            CancellationToken.None);
        Assert.Equal("WaitingForCustomer", replied.Status);
        Assert.Single(notes.Commands);
        Assert.Equal(NotificationSemanticTypes.SupportAdminReply, notes.Commands[0].Type);
        Assert.Equal($"support.admin-reply:{replied.Messages.Last().MessageId:D}", notes.Commands[0].SourceEventId);
        Assert.Equal("support.ticket.admin_reply", notes.Commands[0].SourceType);
        Assert.Contains("/customer-panel/tickets/", notes.Commands[0].TargetRoute, StringComparison.Ordinal);

        var patched = await directory.PatchForAdminAsync(
            created.TicketId,
            new AdminTicketPatchCommand("Resolved", null, null),
            CancellationToken.None);
        Assert.Equal("Resolved", patched.Status);
        var closed = await directory.CloseForCustomerAsync(customer, created.TicketId, CancellationToken.None);
        Assert.Equal("Closed", closed.Status);
        var reopened = await directory.ReopenForCustomerAsync(customer, created.TicketId, CancellationToken.None);
        Assert.Equal("Open", reopened.Status);

        var list = await directory.ListForCustomerAsync(customer, new AudienceTicketListQuery(null, 1, 20), CancellationToken.None);
        Assert.Equal(1, list.Total);
    }

    private static SupportDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<SupportDbContext>()
            .UseInMemoryDatabase("support-behavior-" + Guid.NewGuid().ToString("N"))
            .Options;
        return new SupportDbContext(options);
    }

    private sealed class RecordingNotificationPort : INotificationCreationPort
    {
        public List<CreateNotificationCommand> Commands { get; } = [];

        public Task<bool> CreateIfAbsentAsync(CreateNotificationCommand command, CancellationToken cancellationToken)
        {
            Commands.Add(command);
            return Task.FromResult(true);
        }
    }
}
