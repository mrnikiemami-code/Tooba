namespace Tooba.Support.Application.Models;

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
