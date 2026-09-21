using Tooba.Support.Domain.ValueObjects;

namespace Tooba.Support.Domain.Aggregates;

/// <summary>تجمیع تیکت پشتیبانی در schema مستقل support.</summary>
public sealed class SupportTicket
{
    /// <summary>حداکثر طول موضوع.</summary>
    public const int SubjectMaxLength = 200;
    /// <summary>حداکثر طول کلید idempotency.</summary>
    public const int IdempotencyKeyMaxLength = 128;
    /// <summary>حداکثر طول نوع موجودیت مرتبط.</summary>
    public const int RelatedEntityTypeMaxLength = 64;

    private SupportTicket()
    {
    }

    /// <summary>شناسهٔ پایدار تیکت.</summary>
    public Guid TicketId { get; init; }

    /// <summary>نقش درخواست‌کننده.</summary>
    public RequesterKind RequesterKind { get; init; }

    /// <summary>Actor درخواست‌کننده.</summary>
    public Guid RequesterActorUserId { get; init; }

    /// <summary>Party اختیاری درخواست‌کننده.</summary>
    public Guid? RequesterPartyId { get; init; }

    /// <summary>کلید scope فروشنده برای تیکت‌های Seller.</summary>
    public Guid? SellerPartyId { get; init; }

    /// <summary>موضوع تیکت.</summary>
    public string Subject { get; private set; } = string.Empty;

    /// <summary>دسته.</summary>
    public TicketCategory Category { get; private set; }

    /// <summary>اولویت.</summary>
    public TicketPriority Priority { get; private set; }

    /// <summary>وضعیت.</summary>
    public TicketStatus Status { get; private set; }

    /// <summary>اپراتور ارجاع‌شده.</summary>
    public Guid? AssignedOperatorActorUserId { get; private set; }

    /// <summary>نوع موجودیت مرتبط (مثلاً Order).</summary>
    public string? RelatedEntityType { get; private set; }

    /// <summary>شناسهٔ موجودیت مرتبط؛ بدون JOIN.</summary>
    public Guid? RelatedEntityId { get; private set; }

    /// <summary>کلید idempotency ایجاد؛ اختیاری و یکتا.</summary>
    public string? IdempotencyKey { get; init; }

    /// <summary>زمان ایجاد UTC.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>زمان آخرین به‌روزرسانی UTC.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>زمان بسته‌شدن UTC.</summary>
    public DateTimeOffset? ClosedAt { get; private set; }

    /// <summary>زمان آخرین پیام UTC.</summary>
    public DateTimeOffset? LastMessageAt { get; private set; }

    /// <summary>شمارش پیام‌ها.</summary>
    public int MessageCount { get; private set; }

    /// <summary>تیکت Open معتبر می‌سازد.</summary>
    public static SupportTicket Create(
        Guid ticketId,
        RequesterKind requesterKind,
        Guid requesterActorUserId,
        Guid? requesterPartyId,
        Guid? sellerPartyId,
        string subject,
        TicketCategory category,
        TicketPriority priority,
        string? relatedEntityType,
        Guid? relatedEntityId,
        string? idempotencyKey,
        DateTimeOffset now)
    {
        if (ticketId == Guid.Empty)
            throw new InvalidOperationException("support.ticket.id_required");
        if (requesterActorUserId == Guid.Empty)
            throw new InvalidOperationException("support.requester_required");
        if (requesterKind == RequesterKind.Seller && (sellerPartyId is null || sellerPartyId == Guid.Empty))
            throw new InvalidOperationException("support.seller_party_required");
        if (string.IsNullOrWhiteSpace(subject) || subject.Trim().Length > SubjectMaxLength)
            throw new InvalidOperationException("support.subject_invalid");
        SoftValidateRelated(relatedEntityType, relatedEntityId);
        var key = NormalizeIdempotency(idempotencyKey);
        return new SupportTicket
        {
            TicketId = ticketId,
            RequesterKind = requesterKind,
            RequesterActorUserId = requesterActorUserId,
            RequesterPartyId = requesterPartyId,
            SellerPartyId = sellerPartyId,
            Subject = subject.Trim(),
            Category = category,
            Priority = priority,
            Status = TicketStatus.Open,
            RelatedEntityType = string.IsNullOrWhiteSpace(relatedEntityType) ? null : relatedEntityType.Trim(),
            RelatedEntityId = relatedEntityId,
            IdempotencyKey = key,
            CreatedAt = now,
            UpdatedAt = now,
            MessageCount = 0,
        };
    }

    /// <summary>تیکت با شناسهٔ ثابت برای دانهٔ توسعه.</summary>
    public static SupportTicket CreateSeeded(
        Guid ticketId,
        RequesterKind requesterKind,
        Guid requesterActorUserId,
        Guid? requesterPartyId,
        Guid? sellerPartyId,
        string subject,
        TicketCategory category,
        TicketPriority priority,
        TicketStatus status,
        string? relatedEntityType,
        Guid? relatedEntityId,
        string? idempotencyKey,
        DateTimeOffset now)
    {
        if (ticketId == Guid.Empty)
            throw new InvalidOperationException("support.ticket.id_required");
        if (requesterActorUserId == Guid.Empty)
            throw new InvalidOperationException("support.requester_required");
        if (requesterKind == RequesterKind.Seller && (sellerPartyId is null || sellerPartyId == Guid.Empty))
            throw new InvalidOperationException("support.seller_party_required");
        if (string.IsNullOrWhiteSpace(subject) || subject.Trim().Length > SubjectMaxLength)
            throw new InvalidOperationException("support.subject_invalid");
        SoftValidateRelated(relatedEntityType, relatedEntityId);
        return new SupportTicket
        {
            TicketId = ticketId,
            RequesterKind = requesterKind,
            RequesterActorUserId = requesterActorUserId,
            RequesterPartyId = requesterPartyId,
            SellerPartyId = sellerPartyId,
            Subject = subject.Trim(),
            Category = category,
            Priority = priority,
            Status = status,
            RelatedEntityType = string.IsNullOrWhiteSpace(relatedEntityType) ? null : relatedEntityType.Trim(),
            RelatedEntityId = relatedEntityId,
            IdempotencyKey = NormalizeIdempotency(idempotencyKey),
            CreatedAt = now,
            UpdatedAt = now,
            ClosedAt = status == TicketStatus.Closed ? now : null,
            MessageCount = 0,
        };
    }

    /// <summary>پس از افزودن پیام، شمارنده و زمان را به‌روز می‌کند.</summary>
    public void RegisterMessage(DateTimeOffset now)
    {
        MessageCount += 1;
        LastMessageAt = now;
        UpdatedAt = now;
    }

    /// <summary>وضعیت را برای مشتری/فروشنده می‌بندد.</summary>
    public void CloseByRequester(DateTimeOffset now)
    {
        if (Status is not (TicketStatus.Open or TicketStatus.Resolved))
            throw new InvalidOperationException("support.close_not_allowed");
        Status = TicketStatus.Closed;
        ClosedAt = now;
        UpdatedAt = now;
    }

    /// <summary>تیکت Closed را دوباره باز می‌کند.</summary>
    public void ReopenByRequester(DateTimeOffset now)
    {
        if (Status != TicketStatus.Closed)
            throw new InvalidOperationException("support.reopen_not_allowed");
        Status = TicketStatus.Open;
        ClosedAt = null;
        UpdatedAt = now;
    }

    /// <summary>به‌روزرسانی وضعیت/اولویت/ارجاع توسط Admin.</summary>
    public void ApplyAdminPatch(TicketStatus? status, TicketPriority? priority, Guid? assignedOperatorActorUserId, bool clearAssignee, DateTimeOffset now)
    {
        if (status is { } nextStatus)
        {
            Status = nextStatus;
            ClosedAt = nextStatus == TicketStatus.Closed ? now : null;
        }

        if (priority is { } nextPriority)
            Priority = nextPriority;

        if (clearAssignee)
            AssignedOperatorActorUserId = null;
        else if (assignedOperatorActorUserId is { } assignee)
            AssignedOperatorActorUserId = assignee == Guid.Empty ? null : assignee;

        UpdatedAt = now;
    }

    /// <summary>پس از پاسخ عمومی Admin وضعیت را به انتظار درخواست‌کننده می‌برد.</summary>
    public void MarkWaitingAfterAdminPublicReply(DateTimeOffset now)
    {
        Status = RequesterKind == RequesterKind.Seller
            ? TicketStatus.WaitingForSeller
            : TicketStatus.WaitingForCustomer;
        UpdatedAt = now;
    }

    private static void SoftValidateRelated(string? relatedEntityType, Guid? relatedEntityId)
    {
        if (string.IsNullOrWhiteSpace(relatedEntityType))
        {
            if (relatedEntityId is not null)
                throw new InvalidOperationException("support.related_id_without_type");
            return;
        }

        if (relatedEntityType.Trim().Length > RelatedEntityTypeMaxLength)
            throw new InvalidOperationException("support.related_type_invalid");
        if (relatedEntityId is null || relatedEntityId == Guid.Empty)
            throw new InvalidOperationException("support.related_id_required");
    }

    private static string? NormalizeIdempotency(string? key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        var trimmed = key.Trim();
        if (trimmed.Length > IdempotencyKeyMaxLength)
            throw new InvalidOperationException("support.idempotency_key_invalid");
        return trimmed;
    }
}
