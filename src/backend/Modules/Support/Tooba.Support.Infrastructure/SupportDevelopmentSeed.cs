using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tooba.Notification.Application;
using Tooba.Notification.Domain;
using Tooba.Support.Domain;
using Tooba.Support.Infrastructure.Persistence;

namespace Tooba.Support.Infrastructure;

/// <summary>دانهٔ توسعهٔ idempotent برای تیکت‌های نمایشی.</summary>
public static class SupportDevelopmentSeed
{
    /// <summary>
    /// حداقل دو تیکت مشتری، دو تیکت فروشنده، یک Related Order soft،
    /// و یک پاسخ عمومی Admin (با اعلان) درج می‌کند.
    /// </summary>
    public static async Task ApplyAsync(
        IServiceProvider services,
        Guid customerActorUserId,
        Guid sellerPartyId,
        Guid sellerActorUserId,
        Guid adminActorUserId,
        CancellationToken cancellationToken = default)
    {
        var db = services.GetRequiredService<SupportDbContext>();
        var notifications = services.GetRequiredService<INotificationDirectory>();
        var now = new DateTimeOffset(2026, 8, 27, 12, 0, 0, TimeSpan.Zero);

        await EnsureCustomerOpenAsync(db, notifications, customerActorUserId, adminActorUserId, now, cancellationToken);
        await EnsureCustomerResolvedAsync(db, customerActorUserId, now, cancellationToken);
        await EnsureSellerWaitingAsync(db, sellerPartyId, sellerActorUserId, now, cancellationToken);
        await EnsureSellerOpenAsync(db, sellerPartyId, sellerActorUserId, now, cancellationToken);

        SupportDemoSnapshotStore.Publish(
            new SupportDemoSnapshot(
                SupportDemoIds.CustomerOpenTicketId,
                SupportDemoIds.CustomerResolvedTicketId,
                SupportDemoIds.SellerWaitingTicketId,
                SupportDemoIds.SellerOpenTicketId,
                customerActorUserId,
                sellerPartyId,
                sellerActorUserId,
                adminActorUserId,
                SupportDemoIds.DemoRelatedOrderId,
                "support-demo-customer-open / support-demo-seller-waiting"));
    }

    private static async Task EnsureCustomerOpenAsync(
        SupportDbContext db,
        INotificationDirectory notifications,
        Guid customerActorUserId,
        Guid adminActorUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (await db.Tickets.AnyAsync(t => t.TicketId == SupportDemoIds.CustomerOpenTicketId, cancellationToken))
            return;

        var ticket = SupportTicket.CreateSeeded(
            SupportDemoIds.CustomerOpenTicketId,
            RequesterKind.Customer,
            customerActorUserId,
            null,
            null,
            "support-demo-customer-open",
            TicketCategory.Order,
            TicketPriority.Normal,
            TicketStatus.WaitingForCustomer,
            "Order",
            SupportDemoIds.DemoRelatedOrderId,
            "support-seed-customer-open-v1",
            now);
        var first = TicketMessage.CreateSeeded(
            SupportDemoIds.CustomerOpenFirstMessageId,
            ticket.TicketId,
            AuthorKind.Customer,
            customerActorUserId,
            "سفارش من تأخیر دارد؛ لطفاً وضعیت را بررسی کنید.",
            false,
            "support-seed-customer-open-v1:first",
            now);
        ticket.RegisterMessage(now);
        var reply = TicketMessage.CreateSeeded(
            SupportDemoIds.CustomerOpenAdminReplyId,
            ticket.TicketId,
            AuthorKind.Admin,
            adminActorUserId,
            "در حال پیگیری با انبار هستیم؛ به‌زودی به‌روزرسانی می‌شود.",
            false,
            "support-seed-customer-open-v1:admin",
            now.AddMinutes(10));
        ticket.RegisterMessage(now.AddMinutes(10));
        db.Tickets.Add(ticket);
        db.Messages.Add(first);
        db.Messages.Add(reply);
        await db.SaveChangesAsync(cancellationToken);

        await notifications.CreateIfAbsentAsync(
            new CreateNotificationCommand(
                NotificationRecipientKind.Customer,
                customerActorUserId,
                customerActorUserId,
                NotificationCopy.SupportAdminReply,
                new { ticketId = ticket.TicketId, subject = ticket.Subject },
                NotificationTargetRoutes.CustomerTicket(ticket.TicketId),
                $"support.admin-reply:{reply.MessageId:D}",
                "support.ticket.admin_reply"),
            cancellationToken);
    }

    private static async Task EnsureCustomerResolvedAsync(
        SupportDbContext db,
        Guid customerActorUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (await db.Tickets.AnyAsync(t => t.TicketId == SupportDemoIds.CustomerResolvedTicketId, cancellationToken))
            return;

        var ticket = SupportTicket.CreateSeeded(
            SupportDemoIds.CustomerResolvedTicketId,
            RequesterKind.Customer,
            customerActorUserId,
            null,
            null,
            "support-demo-customer-resolved",
            TicketCategory.Payment,
            TicketPriority.Low,
            TicketStatus.Resolved,
            null,
            null,
            "support-seed-customer-resolved-v1",
            now);
        var first = TicketMessage.CreateSeeded(
            SupportDemoIds.CustomerResolvedFirstMessageId,
            ticket.TicketId,
            AuthorKind.Customer,
            customerActorUserId,
            "رسید پرداخت را دریافت نکردم.",
            false,
            "support-seed-customer-resolved-v1:first",
            now);
        ticket.RegisterMessage(now);
        db.Tickets.Add(ticket);
        db.Messages.Add(first);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureSellerWaitingAsync(
        SupportDbContext db,
        Guid sellerPartyId,
        Guid sellerActorUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (await db.Tickets.AnyAsync(t => t.TicketId == SupportDemoIds.SellerWaitingTicketId, cancellationToken))
            return;

        var ticket = SupportTicket.CreateSeeded(
            SupportDemoIds.SellerWaitingTicketId,
            RequesterKind.Seller,
            sellerActorUserId,
            sellerPartyId,
            sellerPartyId,
            "support-demo-seller-waiting",
            TicketCategory.Product,
            TicketPriority.High,
            TicketStatus.WaitingForSeller,
            null,
            null,
            "support-seed-seller-waiting-v1",
            now);
        var first = TicketMessage.CreateSeeded(
            SupportDemoIds.SellerWaitingFirstMessageId,
            ticket.TicketId,
            AuthorKind.Seller,
            sellerActorUserId,
            "نیاز به راهنمایی برای انتشار محصول دارم.",
            false,
            "support-seed-seller-waiting-v1:first",
            now);
        ticket.RegisterMessage(now);
        db.Tickets.Add(ticket);
        db.Messages.Add(first);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureSellerOpenAsync(
        SupportDbContext db,
        Guid sellerPartyId,
        Guid sellerActorUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (await db.Tickets.AnyAsync(t => t.TicketId == SupportDemoIds.SellerOpenTicketId, cancellationToken))
            return;

        var ticket = SupportTicket.CreateSeeded(
            SupportDemoIds.SellerOpenTicketId,
            RequesterKind.Seller,
            sellerActorUserId,
            sellerPartyId,
            sellerPartyId,
            "support-demo-seller-open",
            TicketCategory.Other,
            TicketPriority.Normal,
            TicketStatus.Open,
            null,
            null,
            "support-seed-seller-open-v1",
            now);
        var first = TicketMessage.CreateSeeded(
            SupportDemoIds.SellerOpenFirstMessageId,
            ticket.TicketId,
            AuthorKind.Seller,
            sellerActorUserId,
            "سؤال دربارهٔ تسویه حساب پنل فروشنده.",
            false,
            "support-seed-seller-open-v1:first",
            now);
        ticket.RegisterMessage(now);
        db.Tickets.Add(ticket);
        db.Messages.Add(first);
        await db.SaveChangesAsync(cancellationToken);
    }
}
