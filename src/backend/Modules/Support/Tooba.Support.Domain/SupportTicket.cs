using Tooba.BuildingBlocks;

namespace Tooba.Support.Domain;

/// <summary>وضعیت چرخهٔ تیکت پشتیبانی.</summary>
public enum TicketStatus
{
    /// <summary>باز و در صف بررسی.</summary>
    Open = 0,
    /// <summary>در حال بررسی توسط اپراتور.</summary>
    InProgress = 1,
    /// <summary>در انتظار پاسخ مشتری.</summary>
    WaitingForCustomer = 2,
    /// <summary>در انتظار پاسخ فروشنده.</summary>
    WaitingForSeller = 3,
    /// <summary>حل‌شده.</summary>
    Resolved = 4,
    /// <summary>بسته‌شده.</summary>
    Closed = 5,
}

/// <summary>اولویت تیکت.</summary>
public enum TicketPriority
{
    /// <summary>پایین.</summary>
    Low = 0,
    /// <summary>عادی.</summary>
    Normal = 1,
    /// <summary>بالا.</summary>
    High = 2,
}

/// <summary>دستهٔ موضوع تیکت.</summary>
public enum TicketCategory
{
    /// <summary>سفارش.</summary>
    Order = 0,
    /// <summary>پرداخت.</summary>
    Payment = 1,
    /// <summary>مرجوعی.</summary>
    Return = 2,
    /// <summary>محصول.</summary>
    Product = 3,
    /// <summary>سایر.</summary>
    Other = 4,
}

/// <summary>نقش درخواست‌کنندهٔ تیکت.</summary>
public enum RequesterKind
{
    /// <summary>مشتری.</summary>
    Customer = 0,
    /// <summary>فروشنده.</summary>
    Seller = 1,
    /// <summary>مدیر.</summary>
    Admin = 2,
}

/// <summary>نقش نویسندهٔ پیام.</summary>
public enum AuthorKind
{
    /// <summary>مشتری.</summary>
    Customer = 0,
    /// <summary>فروشنده.</summary>
    Seller = 1,
    /// <summary>مدیر.</summary>
    Admin = 2,
    /// <summary>سیستم.</summary>
    System = 3,
}

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
        if (requesterActorUserId == Guid.Empty)
            throw new InvalidOperationException("هویت درخواست‌کننده الزامی است.");
        if (requesterKind == RequesterKind.Seller && (sellerPartyId is null || sellerPartyId == Guid.Empty))
            throw new InvalidOperationException("SellerPartyId برای تیکت فروشنده الزامی است.");
        if (string.IsNullOrWhiteSpace(subject) || subject.Trim().Length > SubjectMaxLength)
            throw new InvalidOperationException("موضوع تیکت معتبر نیست.");
        SoftValidateRelated(relatedEntityType, relatedEntityId);
        var key = NormalizeIdempotency(idempotencyKey);
        return new SupportTicket
        {
            TicketId = UuidV7.New(),
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
        if (ticketId == Guid.Empty) throw new InvalidOperationException("TicketId الزامی است.");
        if (requesterActorUserId == Guid.Empty)
            throw new InvalidOperationException("هویت درخواست‌کننده الزامی است.");
        if (requesterKind == RequesterKind.Seller && (sellerPartyId is null || sellerPartyId == Guid.Empty))
            throw new InvalidOperationException("SellerPartyId برای تیکت فروشنده الزامی است.");
        if (string.IsNullOrWhiteSpace(subject) || subject.Trim().Length > SubjectMaxLength)
            throw new InvalidOperationException("موضوع تیکت معتبر نیست.");
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
            throw new InvalidOperationException("فقط تیکت Open یا Resolved قابل بستن است.");
        Status = TicketStatus.Closed;
        ClosedAt = now;
        UpdatedAt = now;
    }

    /// <summary>تیکت Closed را دوباره باز می‌کند.</summary>
    public void ReopenByRequester(DateTimeOffset now)
    {
        if (Status != TicketStatus.Closed)
            throw new InvalidOperationException("فقط تیکت Closed قابل بازگشایی است.");
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
                throw new InvalidOperationException("RelatedEntityId بدون RelatedEntityType نامعتبر است.");
            return;
        }

        if (relatedEntityType.Trim().Length > RelatedEntityTypeMaxLength)
            throw new InvalidOperationException("RelatedEntityType معتبر نیست.");
        if (relatedEntityId is null || relatedEntityId == Guid.Empty)
            throw new InvalidOperationException("RelatedEntityId برای نوع مرتبط الزامی است.");
        // بدون JOIN: فقط شکل GUID؛ مالکیت/وجود از gateway Application در لایه بالاتر.
    }

    private static string? NormalizeIdempotency(string? key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        var trimmed = key.Trim();
        if (trimmed.Length > IdempotencyKeyMaxLength)
            throw new InvalidOperationException("IdempotencyKey معتبر نیست.");
        return trimmed;
    }
}

/// <summary>پیام تیکت پشتیبانی.</summary>
public sealed class TicketMessage
{
    /// <summary>حداکثر طول بدنه.</summary>
    public const int BodyMaxLength = 4000;
    /// <summary>حداکثر طول کلید idempotency.</summary>
    public const int IdempotencyKeyMaxLength = 128;

    private TicketMessage()
    {
    }

    /// <summary>شناسهٔ پایدار پیام.</summary>
    public Guid MessageId { get; init; }

    /// <summary>تیکت والد.</summary>
    public Guid TicketId { get; init; }

    /// <summary>نقش نویسنده.</summary>
    public AuthorKind AuthorKind { get; init; }

    /// <summary>Actor نویسنده.</summary>
    public Guid AuthorActorUserId { get; init; }

    /// <summary>متن پیام.</summary>
    public string Body { get; init; } = string.Empty;

    /// <summary>زمان ایجاد UTC.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>یادداشت داخلی فقط برای Admin.</summary>
    public bool IsInternalNote { get; init; }

    /// <summary>کلید idempotency پاسخ؛ اختیاری و یکتا.</summary>
    public string? IdempotencyKey { get; init; }

    /// <summary>پیام معتبر می‌سازد.</summary>
    public static TicketMessage Create(
        Guid ticketId,
        AuthorKind authorKind,
        Guid authorActorUserId,
        string body,
        bool isInternalNote,
        string? idempotencyKey,
        DateTimeOffset now)
    {
        if (ticketId == Guid.Empty || authorActorUserId == Guid.Empty)
            throw new InvalidOperationException("هویت تیکت و نویسنده الزامی است.");
        if (string.IsNullOrWhiteSpace(body) || body.Trim().Length > BodyMaxLength)
            throw new InvalidOperationException("متن پیام معتبر نیست.");
        if (isInternalNote && authorKind != AuthorKind.Admin)
            throw new InvalidOperationException("فقط Admin می‌تواند یادداشت داخلی بنویسد.");
        string? key = null;
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            key = idempotencyKey.Trim();
            if (key.Length > IdempotencyKeyMaxLength)
                throw new InvalidOperationException("IdempotencyKey معتبر نیست.");
        }

        return new TicketMessage
        {
            MessageId = UuidV7.New(),
            TicketId = ticketId,
            AuthorKind = authorKind,
            AuthorActorUserId = authorActorUserId,
            Body = body.Trim(),
            CreatedAt = now,
            IsInternalNote = isInternalNote,
            IdempotencyKey = key,
        };
    }

    /// <summary>پیام با شناسهٔ ثابت برای دانهٔ توسعه.</summary>
    public static TicketMessage CreateSeeded(
        Guid messageId,
        Guid ticketId,
        AuthorKind authorKind,
        Guid authorActorUserId,
        string body,
        bool isInternalNote,
        string? idempotencyKey,
        DateTimeOffset now)
    {
        if (messageId == Guid.Empty || ticketId == Guid.Empty || authorActorUserId == Guid.Empty)
            throw new InvalidOperationException("هویت پیام، تیکت و نویسنده الزامی است.");
        if (string.IsNullOrWhiteSpace(body) || body.Trim().Length > BodyMaxLength)
            throw new InvalidOperationException("متن پیام معتبر نیست.");
        string? key = null;
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            key = idempotencyKey.Trim();
            if (key.Length > IdempotencyKeyMaxLength)
                throw new InvalidOperationException("IdempotencyKey معتبر نیست.");
        }

        return new TicketMessage
        {
            MessageId = messageId,
            TicketId = ticketId,
            AuthorKind = authorKind,
            AuthorActorUserId = authorActorUserId,
            Body = body.Trim(),
            CreatedAt = now,
            IsInternalNote = isInternalNote,
            IdempotencyKey = key,
        };
    }
}
