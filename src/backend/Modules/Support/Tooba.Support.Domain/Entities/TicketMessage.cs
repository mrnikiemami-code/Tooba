using Tooba.Support.Domain.ValueObjects;

namespace Tooba.Support.Domain.Entities;

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
            throw new InvalidOperationException("support.message.ids_required");
        if (string.IsNullOrWhiteSpace(body) || body.Trim().Length > BodyMaxLength)
            throw new InvalidOperationException("support.message.body_invalid");
        if (isInternalNote && authorKind != AuthorKind.Admin)
            throw new InvalidOperationException("support.message.internal_admin_only");
        string? key = null;
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            key = idempotencyKey.Trim();
            if (key.Length > IdempotencyKeyMaxLength)
                throw new InvalidOperationException("support.idempotency_key_invalid");
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
            throw new InvalidOperationException("support.message.ids_required");
        if (string.IsNullOrWhiteSpace(body) || body.Trim().Length > BodyMaxLength)
            throw new InvalidOperationException("support.message.body_invalid");
        string? key = null;
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            key = idempotencyKey.Trim();
            if (key.Length > IdempotencyKeyMaxLength)
                throw new InvalidOperationException("support.idempotency_key_invalid");
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
