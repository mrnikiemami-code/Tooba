using Microsoft.EntityFrameworkCore;
using Tooba.Notification.Application;
using Tooba.Notification.Domain;
using Tooba.Support.Application;
using Tooba.Support.Domain;
using Tooba.Support.Infrastructure.Persistence;

namespace Tooba.Support.Infrastructure;

/// <summary>پیاده‌سازی دایرکتوری تیکت در schema support.</summary>
public sealed class SupportDirectory : ISupportDirectory
{
    private readonly SupportDbContext _db;
    private readonly INotificationDirectory _notifications;

    /// <summary>دایرکتوری را می‌سازد.</summary>
    public SupportDirectory(SupportDbContext db, INotificationDirectory notifications)
    {
        _db = db;
        _notifications = notifications;
    }

    /// <inheritdoc />
    public Task<TicketListPageDto> ListForCustomerAsync(
        Guid actorUserId,
        AudienceTicketListQuery query,
        CancellationToken cancellationToken) =>
        ListAsync(
            q => q.Where(t => t.RequesterKind == RequesterKind.Customer && t.RequesterActorUserId == actorUserId),
            query.Status,
            query.Page,
            query.PageSize,
            cancellationToken);

    /// <inheritdoc />
    public async Task<TicketSnapshotDto?> GetForCustomerAsync(
        Guid actorUserId,
        Guid ticketId,
        CancellationToken cancellationToken)
    {
        var ticket = await _db.Tickets.AsNoTracking()
            .SingleOrDefaultAsync(
                t => t.TicketId == ticketId
                     && t.RequesterKind == RequesterKind.Customer
                     && t.RequesterActorUserId == actorUserId,
                cancellationToken);
        return ticket is null ? null : await MapSnapshotAsync(ticket, includeInternal: false, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TicketSnapshotDto> CreateForCustomerAsync(
        Guid actorUserId,
        CreateTicketCommand command,
        CancellationToken cancellationToken) =>
        await CreateAsync(
            RequesterKind.Customer,
            actorUserId,
            requesterPartyId: null,
            sellerPartyId: null,
            AuthorKind.Customer,
            command,
            cancellationToken);

    /// <inheritdoc />
    public Task<TicketSnapshotDto> ReplyForCustomerAsync(
        Guid actorUserId,
        Guid ticketId,
        ReplyTicketCommand command,
        CancellationToken cancellationToken) =>
        ReplyOwnedAsync(
            t => t.TicketId == ticketId
                 && t.RequesterKind == RequesterKind.Customer
                 && t.RequesterActorUserId == actorUserId,
            AuthorKind.Customer,
            actorUserId,
            command with { IsInternalNote = false },
            notify: false,
            cancellationToken);

    /// <inheritdoc />
    public Task<TicketSnapshotDto> CloseForCustomerAsync(
        Guid actorUserId,
        Guid ticketId,
        CancellationToken cancellationToken) =>
        MutateOwnedAsync(
            t => t.TicketId == ticketId
                 && t.RequesterKind == RequesterKind.Customer
                 && t.RequesterActorUserId == actorUserId,
            ticket => ticket.CloseByRequester(DateTimeOffset.UtcNow),
            cancellationToken);

    /// <inheritdoc />
    public Task<TicketSnapshotDto> ReopenForCustomerAsync(
        Guid actorUserId,
        Guid ticketId,
        CancellationToken cancellationToken) =>
        MutateOwnedAsync(
            t => t.TicketId == ticketId
                 && t.RequesterKind == RequesterKind.Customer
                 && t.RequesterActorUserId == actorUserId,
            ticket => ticket.ReopenByRequester(DateTimeOffset.UtcNow),
            cancellationToken);

    /// <inheritdoc />
    public Task<TicketListPageDto> ListForSellerAsync(
        Guid sellerPartyId,
        AudienceTicketListQuery query,
        CancellationToken cancellationToken) =>
        ListAsync(
            q => q.Where(t => t.RequesterKind == RequesterKind.Seller && t.SellerPartyId == sellerPartyId),
            query.Status,
            query.Page,
            query.PageSize,
            cancellationToken);

    /// <inheritdoc />
    public async Task<TicketSnapshotDto?> GetForSellerAsync(
        Guid sellerPartyId,
        Guid ticketId,
        CancellationToken cancellationToken)
    {
        var ticket = await _db.Tickets.AsNoTracking()
            .SingleOrDefaultAsync(
                t => t.TicketId == ticketId
                     && t.RequesterKind == RequesterKind.Seller
                     && t.SellerPartyId == sellerPartyId,
                cancellationToken);
        return ticket is null ? null : await MapSnapshotAsync(ticket, includeInternal: false, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TicketSnapshotDto> CreateForSellerAsync(
        Guid actorUserId,
        Guid sellerPartyId,
        CreateTicketCommand command,
        CancellationToken cancellationToken) =>
        await CreateAsync(
            RequesterKind.Seller,
            actorUserId,
            requesterPartyId: sellerPartyId,
            sellerPartyId,
            AuthorKind.Seller,
            command,
            cancellationToken);

    /// <inheritdoc />
    public Task<TicketSnapshotDto> ReplyForSellerAsync(
        Guid actorUserId,
        Guid sellerPartyId,
        Guid ticketId,
        ReplyTicketCommand command,
        CancellationToken cancellationToken) =>
        ReplyOwnedAsync(
            t => t.TicketId == ticketId
                 && t.RequesterKind == RequesterKind.Seller
                 && t.SellerPartyId == sellerPartyId,
            AuthorKind.Seller,
            actorUserId,
            command with { IsInternalNote = false },
            notify: false,
            cancellationToken);

    /// <inheritdoc />
    public Task<TicketSnapshotDto> CloseForSellerAsync(
        Guid sellerPartyId,
        Guid ticketId,
        CancellationToken cancellationToken) =>
        MutateOwnedAsync(
            t => t.TicketId == ticketId
                 && t.RequesterKind == RequesterKind.Seller
                 && t.SellerPartyId == sellerPartyId,
            ticket => ticket.CloseByRequester(DateTimeOffset.UtcNow),
            cancellationToken);

    /// <inheritdoc />
    public Task<TicketSnapshotDto> ReopenForSellerAsync(
        Guid sellerPartyId,
        Guid ticketId,
        CancellationToken cancellationToken) =>
        MutateOwnedAsync(
            t => t.TicketId == ticketId
                 && t.RequesterKind == RequesterKind.Seller
                 && t.SellerPartyId == sellerPartyId,
            ticket => ticket.ReopenByRequester(DateTimeOffset.UtcNow),
            cancellationToken);

    /// <inheritdoc />
    public async Task<TicketListPageDto> ListForAdminAsync(AdminTicketListQuery query, CancellationToken cancellationToken)
    {
        var (page, pageSize) = NormalizePaging(query.Page, query.PageSize);
        var status = SupportEnumParsing.TryParseStatus(query.Status);
        var requester = SupportEnumParsing.TryParseRequesterKind(query.RequesterKind);
        var category = string.IsNullOrWhiteSpace(query.Category)
            ? (TicketCategory?)null
            : SupportEnumParsing.ParseCategory(query.Category);
        var priority = string.IsNullOrWhiteSpace(query.Priority)
            ? (TicketPriority?)null
            : SupportEnumParsing.ParsePriority(query.Priority);
        var q = query.Q?.Trim();

        var filtered = _db.Tickets.AsNoTracking().AsQueryable();
        if (status is { } st) filtered = filtered.Where(t => t.Status == st);
        if (requester is { } rk) filtered = filtered.Where(t => t.RequesterKind == rk);
        if (category is { } cat) filtered = filtered.Where(t => t.Category == cat);
        if (priority is { } pr) filtered = filtered.Where(t => t.Priority == pr);
        if (!string.IsNullOrWhiteSpace(q))
            filtered = filtered.Where(t => EF.Functions.ILike(t.Subject, $"%{EscapeLike(q)}%"));

        var total = await filtered.CountAsync(cancellationToken);
        var rows = await filtered
            .OrderByDescending(t => t.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return new TicketListPageDto(rows.Select(MapRow).ToList(), total, page, pageSize);
    }

    /// <inheritdoc />
    public async Task<TicketSnapshotDto?> GetForAdminAsync(Guid ticketId, CancellationToken cancellationToken)
    {
        var ticket = await _db.Tickets.AsNoTracking()
            .SingleOrDefaultAsync(t => t.TicketId == ticketId, cancellationToken);
        return ticket is null ? null : await MapSnapshotAsync(ticket, includeInternal: true, cancellationToken);
    }

    /// <inheritdoc />
    public Task<TicketSnapshotDto> ReplyForAdminAsync(
        Guid actorUserId,
        Guid ticketId,
        ReplyTicketCommand command,
        CancellationToken cancellationToken) =>
        ReplyOwnedAsync(
            t => t.TicketId == ticketId,
            AuthorKind.Admin,
            actorUserId,
            command,
            notify: !command.IsInternalNote,
            cancellationToken);

    /// <inheritdoc />
    public async Task<TicketSnapshotDto> PatchForAdminAsync(
        Guid ticketId,
        AdminTicketPatchCommand command,
        CancellationToken cancellationToken)
    {
        var ticket = await _db.Tickets.SingleOrDefaultAsync(t => t.TicketId == ticketId, cancellationToken)
            ?? throw new InvalidOperationException("تیکت پیدا نشد.");
        TicketStatus? status = string.IsNullOrWhiteSpace(command.Status)
            ? null
            : SupportEnumParsing.ParseStatus(command.Status);
        TicketPriority? priority = string.IsNullOrWhiteSpace(command.Priority)
            ? null
            : SupportEnumParsing.ParsePriority(command.Priority);
        var clearAssignee = command.AssignedOperatorActorUserId == Guid.Empty;
        ticket.ApplyAdminPatch(status, priority, command.AssignedOperatorActorUserId, clearAssignee, DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        return (await MapSnapshotAsync(ticket, includeInternal: true, cancellationToken))!;
    }

    private async Task<TicketSnapshotDto> CreateAsync(
        RequesterKind requesterKind,
        Guid actorUserId,
        Guid? requesterPartyId,
        Guid? sellerPartyId,
        AuthorKind authorKind,
        CreateTicketCommand command,
        CancellationToken cancellationToken)
    {
        SoftCheckRelatedOrder(command.RelatedEntityType, command.RelatedEntityId);
        if (!string.IsNullOrWhiteSpace(command.IdempotencyKey))
        {
            var existing = await _db.Tickets.AsNoTracking()
                .SingleOrDefaultAsync(t => t.IdempotencyKey == command.IdempotencyKey.Trim(), cancellationToken);
            if (existing is not null)
                return (await MapSnapshotAsync(existing, includeInternal: false, cancellationToken))!;
        }

        var now = DateTimeOffset.UtcNow;
        var ticket = SupportTicket.Create(
            requesterKind,
            actorUserId,
            requesterPartyId,
            sellerPartyId,
            command.Subject,
            SupportEnumParsing.ParseCategory(command.Category),
            SupportEnumParsing.ParsePriority(command.Priority),
            command.RelatedEntityType,
            command.RelatedEntityId,
            command.IdempotencyKey,
            now);
        var message = TicketMessage.Create(
            ticket.TicketId,
            authorKind,
            actorUserId,
            command.Body,
            isInternalNote: false,
            idempotencyKey: command.IdempotencyKey is null ? null : $"{command.IdempotencyKey}:first",
            now);
        ticket.RegisterMessage(now);
        _db.Tickets.Add(ticket);
        _db.Messages.Add(message);
        await _db.SaveChangesAsync(cancellationToken);
        return (await MapSnapshotAsync(ticket, includeInternal: false, cancellationToken))!;
    }

    private async Task<TicketSnapshotDto> ReplyOwnedAsync(
        System.Linq.Expressions.Expression<Func<SupportTicket, bool>> predicate,
        AuthorKind authorKind,
        Guid actorUserId,
        ReplyTicketCommand command,
        bool notify,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(command.IdempotencyKey))
        {
            var prior = await _db.Messages.AsNoTracking()
                .SingleOrDefaultAsync(m => m.IdempotencyKey == command.IdempotencyKey.Trim(), cancellationToken);
            if (prior is not null)
            {
                var priorTicket = await _db.Tickets.AsNoTracking()
                    .SingleAsync(t => t.TicketId == prior.TicketId, cancellationToken);
                return (await MapSnapshotAsync(priorTicket, includeInternal: authorKind == AuthorKind.Admin, cancellationToken))!;
            }
        }

        var ticket = await _db.Tickets.SingleOrDefaultAsync(predicate, cancellationToken)
            ?? throw new InvalidOperationException("تیکت پیدا نشد.");
        if (ticket.Status == TicketStatus.Closed)
            throw new InvalidOperationException("تیکت بسته‌شده قابل پاسخ نیست؛ ابتدا بازگشایی کنید.");

        var now = DateTimeOffset.UtcNow;
        var message = TicketMessage.Create(
            ticket.TicketId,
            authorKind,
            actorUserId,
            command.Body,
            command.IsInternalNote,
            command.IdempotencyKey,
            now);
        ticket.RegisterMessage(now);
        if (authorKind == AuthorKind.Admin && !command.IsInternalNote)
            ticket.MarkWaitingAfterAdminPublicReply(now);

        _db.Messages.Add(message);
        await _db.SaveChangesAsync(cancellationToken);

        if (notify)
            await NotifyRequesterAsync(ticket, message, cancellationToken);

        return (await MapSnapshotAsync(ticket, includeInternal: authorKind == AuthorKind.Admin, cancellationToken))!;
    }

    private async Task<TicketSnapshotDto> MutateOwnedAsync(
        System.Linq.Expressions.Expression<Func<SupportTicket, bool>> predicate,
        Action<SupportTicket> mutate,
        CancellationToken cancellationToken)
    {
        var ticket = await _db.Tickets.SingleOrDefaultAsync(predicate, cancellationToken)
            ?? throw new InvalidOperationException("تیکت پیدا نشد.");
        mutate(ticket);
        await _db.SaveChangesAsync(cancellationToken);
        return (await MapSnapshotAsync(ticket, includeInternal: false, cancellationToken))!;
    }

    private async Task NotifyRequesterAsync(
        SupportTicket ticket,
        TicketMessage message,
        CancellationToken cancellationToken)
    {
        var sourceEventId = $"support.admin-reply:{message.MessageId:D}";
        if (ticket.RequesterKind == RequesterKind.Customer)
        {
            await _notifications.CreateIfAbsentAsync(
                new CreateNotificationCommand(
                    NotificationRecipientKind.Customer,
                    ticket.RequesterActorUserId,
                    ticket.RequesterActorUserId,
                    NotificationCopy.SupportAdminReply,
                    new { ticketId = ticket.TicketId, subject = ticket.Subject },
                    NotificationTargetRoutes.CustomerTicket(ticket.TicketId),
                    sourceEventId,
                    "support.ticket.admin_reply"),
                cancellationToken);
            return;
        }

        if (ticket.RequesterKind == RequesterKind.Seller && ticket.SellerPartyId is { } sellerPartyId)
        {
            await _notifications.CreateIfAbsentAsync(
                new CreateNotificationCommand(
                    NotificationRecipientKind.Seller,
                    sellerPartyId,
                    null,
                    NotificationCopy.SupportAdminReply,
                    new { ticketId = ticket.TicketId, subject = ticket.Subject },
                    NotificationTargetRoutes.SellerTicket(ticket.TicketId),
                    sourceEventId,
                    "support.ticket.admin_reply"),
                cancellationToken);
        }
    }

    private async Task<TicketListPageDto> ListAsync(
        Func<IQueryable<SupportTicket>, IQueryable<SupportTicket>> scope,
        string? statusRaw,
        int pageRaw,
        int pageSizeRaw,
        CancellationToken cancellationToken)
    {
        var (page, pageSize) = NormalizePaging(pageRaw, pageSizeRaw);
        var status = SupportEnumParsing.TryParseStatus(statusRaw);
        var filtered = scope(_db.Tickets.AsNoTracking());
        if (status is { } st) filtered = filtered.Where(t => t.Status == st);
        var total = await filtered.CountAsync(cancellationToken);
        var rows = await filtered
            .OrderByDescending(t => t.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return new TicketListPageDto(rows.Select(MapRow).ToList(), total, page, pageSize);
    }

    private async Task<TicketSnapshotDto> MapSnapshotAsync(
        SupportTicket ticket,
        bool includeInternal,
        CancellationToken cancellationToken)
    {
        var messagesQuery = _db.Messages.AsNoTracking().Where(m => m.TicketId == ticket.TicketId);
        if (!includeInternal)
            messagesQuery = messagesQuery.Where(m => !m.IsInternalNote);
        var messages = await messagesQuery.OrderBy(m => m.CreatedAt).ToListAsync(cancellationToken);
        return new TicketSnapshotDto(
            ticket.TicketId,
            ticket.RequesterKind.ToString(),
            ticket.RequesterActorUserId,
            ticket.RequesterPartyId,
            ticket.SellerPartyId,
            ticket.Subject,
            ticket.Category.ToString(),
            ticket.Priority.ToString(),
            ticket.Status.ToString(),
            ticket.AssignedOperatorActorUserId,
            ticket.RelatedEntityType,
            ticket.RelatedEntityId,
            ticket.CreatedAt,
            ticket.UpdatedAt,
            ticket.ClosedAt,
            ticket.LastMessageAt,
            includeInternal ? ticket.MessageCount : messages.Count,
            messages.Select(m => new TicketMessageDto(
                m.MessageId,
                m.TicketId,
                m.AuthorKind.ToString(),
                m.AuthorActorUserId,
                m.Body,
                m.CreatedAt,
                m.IsInternalNote)).ToList());
    }

    private static TicketListRowDto MapRow(SupportTicket ticket) =>
        new(
            ticket.TicketId,
            ticket.TicketId,
            ticket.Subject,
            ticket.Category.ToString(),
            ticket.Priority.ToString(),
            ticket.Status.ToString(),
            ticket.RequesterKind.ToString(),
            ticket.MessageCount,
            ticket.CreatedAt,
            ticket.LastMessageAt);

    private static (int Page, int PageSize) NormalizePaging(int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;
        return (page, pageSize);
    }

    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);

    /// <summary>
    /// اعتبارسنجی soft بدون JOIN: فقط شکل GUID برای RelatedEntityType=Order.
    /// در صورت وجود gateway اختصاصی Order در آینده می‌توان سخت‌گیرتر کرد.
    /// </summary>
    private static void SoftCheckRelatedOrder(string? relatedEntityType, Guid? relatedEntityId)
    {
        if (string.IsNullOrWhiteSpace(relatedEntityType)) return;
        if (!relatedEntityType.Equals("Order", StringComparison.OrdinalIgnoreCase)) return;
        if (relatedEntityId is null || relatedEntityId == Guid.Empty)
            throw new InvalidOperationException("شناسهٔ سفارش مرتبط نامعتبر است.");
    }
}
