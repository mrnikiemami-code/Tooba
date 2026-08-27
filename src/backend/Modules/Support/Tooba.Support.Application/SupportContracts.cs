using Tooba.Support.Domain;

namespace Tooba.Support.Application;

/// <summary>پیام تیکت مطابق قرارداد FE.</summary>
public sealed record TicketMessageDto(
    Guid MessageId,
    Guid TicketId,
    string AuthorKind,
    Guid AuthorActorUserId,
    string Body,
    DateTimeOffset CreatedAt,
    bool IsInternalNote);

/// <summary>جزئیات کامل تیکت مطابق TicketSnapshot.</summary>
public sealed record TicketSnapshotDto(
    Guid TicketId,
    string RequesterKind,
    Guid RequesterActorUserId,
    Guid? RequesterPartyId,
    Guid? SellerPartyId,
    string Subject,
    string Category,
    string Priority,
    string Status,
    Guid? AssignedOperatorActorUserId,
    string? RelatedEntityType,
    Guid? RelatedEntityId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? ClosedAt,
    DateTimeOffset? LastMessageAt,
    int MessageCount,
    IReadOnlyList<TicketMessageDto> Messages);

/// <summary>ردیف فهرست تیکت.</summary>
public sealed record TicketListRowDto(
    Guid Id,
    Guid TicketId,
    string Subject,
    string Category,
    string Priority,
    string Status,
    string RequesterKind,
    int MessageCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastMessageAt);

/// <summary>صفحهٔ فهرست.</summary>
public sealed record TicketListPageDto(
    IReadOnlyList<TicketListRowDto> Items,
    int Total,
    int Page,
    int PageSize);

/// <summary>ورودی ایجاد تیکت.</summary>
public sealed record CreateTicketCommand(
    string Subject,
    string Category,
    string? Priority,
    string Body,
    string? RelatedEntityType,
    Guid? RelatedEntityId,
    string? IdempotencyKey);

/// <summary>ورودی پاسخ.</summary>
public sealed record ReplyTicketCommand(
    string Body,
    bool IsInternalNote,
    string? IdempotencyKey);

/// <summary>پچ Admin.</summary>
public sealed record AdminTicketPatchCommand(
    string? Status,
    string? Priority,
    Guid? AssignedOperatorActorUserId);

/// <summary>فیلتر فهرست Admin.</summary>
public sealed record AdminTicketListQuery(
    string? Status,
    string? RequesterKind,
    string? Category,
    string? Priority,
    string? Q,
    int Page,
    int PageSize);

/// <summary>فیلتر فهرست مشتری/فروشنده.</summary>
public sealed record AudienceTicketListQuery(
    string? Status,
    int Page,
    int PageSize);

/// <summary>دایرکتوری کاربردی تیکت پشتیبانی.</summary>
public interface ISupportDirectory
{
    /// <summary>فهرست تیکت‌های مشتری مالک.</summary>
    Task<TicketListPageDto> ListForCustomerAsync(Guid actorUserId, AudienceTicketListQuery query, CancellationToken cancellationToken);

    /// <summary>جزئیات تیکت مشتری بدون یادداشت داخلی.</summary>
    Task<TicketSnapshotDto?> GetForCustomerAsync(Guid actorUserId, Guid ticketId, CancellationToken cancellationToken);

    /// <summary>ایجاد تیکت مشتری.</summary>
    Task<TicketSnapshotDto> CreateForCustomerAsync(Guid actorUserId, CreateTicketCommand command, CancellationToken cancellationToken);

    /// <summary>پاسخ مشتری.</summary>
    Task<TicketSnapshotDto> ReplyForCustomerAsync(Guid actorUserId, Guid ticketId, ReplyTicketCommand command, CancellationToken cancellationToken);

    /// <summary>بستن تیکت مشتری.</summary>
    Task<TicketSnapshotDto> CloseForCustomerAsync(Guid actorUserId, Guid ticketId, CancellationToken cancellationToken);

    /// <summary>بازگشایی تیکت مشتری.</summary>
    Task<TicketSnapshotDto> ReopenForCustomerAsync(Guid actorUserId, Guid ticketId, CancellationToken cancellationToken);

    /// <summary>فهرست تیکت‌های SellerParty.</summary>
    Task<TicketListPageDto> ListForSellerAsync(Guid sellerPartyId, AudienceTicketListQuery query, CancellationToken cancellationToken);

    /// <summary>جزئیات تیکت فروشنده بدون یادداشت داخلی.</summary>
    Task<TicketSnapshotDto?> GetForSellerAsync(Guid sellerPartyId, Guid ticketId, CancellationToken cancellationToken);

    /// <summary>ایجاد تیکت فروشنده.</summary>
    Task<TicketSnapshotDto> CreateForSellerAsync(
        Guid actorUserId,
        Guid sellerPartyId,
        CreateTicketCommand command,
        CancellationToken cancellationToken);

    /// <summary>پاسخ فروشنده.</summary>
    Task<TicketSnapshotDto> ReplyForSellerAsync(
        Guid actorUserId,
        Guid sellerPartyId,
        Guid ticketId,
        ReplyTicketCommand command,
        CancellationToken cancellationToken);

    /// <summary>بستن تیکت فروشنده.</summary>
    Task<TicketSnapshotDto> CloseForSellerAsync(Guid sellerPartyId, Guid ticketId, CancellationToken cancellationToken);

    /// <summary>بازگشایی تیکت فروشنده.</summary>
    Task<TicketSnapshotDto> ReopenForSellerAsync(Guid sellerPartyId, Guid ticketId, CancellationToken cancellationToken);

    /// <summary>فهرست Admin با فیلتر.</summary>
    Task<TicketListPageDto> ListForAdminAsync(AdminTicketListQuery query, CancellationToken cancellationToken);

    /// <summary>جزئیات Admin شامل یادداشت داخلی.</summary>
    Task<TicketSnapshotDto?> GetForAdminAsync(Guid ticketId, CancellationToken cancellationToken);

    /// <summary>پاسخ Admin؛ پاسخ عمومی ممکن است اعلان بسازد.</summary>
    Task<TicketSnapshotDto> ReplyForAdminAsync(Guid actorUserId, Guid ticketId, ReplyTicketCommand command, CancellationToken cancellationToken);

    /// <summary>پچ وضعیت/اولویت/ارجاع Admin.</summary>
    Task<TicketSnapshotDto> PatchForAdminAsync(Guid ticketId, AdminTicketPatchCommand command, CancellationToken cancellationToken);
}

/// <summary>کمک‌های پارس enum برای مرز Application.</summary>
public static class SupportEnumParsing
{
    /// <summary>دسته را پارس می‌کند.</summary>
    public static TicketCategory ParseCategory(string value) =>
        Enum.TryParse<TicketCategory>(value, ignoreCase: true, out var parsed)
            ? parsed
            : throw new InvalidOperationException("دستهٔ تیکت نامعتبر است.");

    /// <summary>اولویت را پارس می‌کند؛ پیش‌فرض Normal.</summary>
    public static TicketPriority ParsePriority(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? TicketPriority.Normal
            : Enum.TryParse<TicketPriority>(value, ignoreCase: true, out var parsed)
                ? parsed
                : throw new InvalidOperationException("اولویت تیکت نامعتبر است.");

    /// <summary>وضعیت اختیاری فیلتر را پارس می‌کند.</summary>
    public static TicketStatus? TryParseStatus(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : Enum.TryParse<TicketStatus>(value, ignoreCase: true, out var parsed)
                ? parsed
                : throw new InvalidOperationException("وضعیت تیکت نامعتبر است.");

    /// <summary>وضعیت اجباری پچ را پارس می‌کند.</summary>
    public static TicketStatus ParseStatus(string value) =>
        Enum.TryParse<TicketStatus>(value, ignoreCase: true, out var parsed)
            ? parsed
            : throw new InvalidOperationException("وضعیت تیکت نامعتبر است.");

    /// <summary>RequesterKind فیلتر را پارس می‌کند.</summary>
    public static RequesterKind? TryParseRequesterKind(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : Enum.TryParse<RequesterKind>(value, ignoreCase: true, out var parsed)
                ? parsed
                : throw new InvalidOperationException("RequesterKind نامعتبر است.");
}
