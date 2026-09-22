namespace Tooba.Support.Application.Models;

/// <summary>ورودی ایجاد تیکت برای پورت دایرکتوری.</summary>
public sealed record CreateTicketCommand(
    string Subject,
    string Category,
    string? Priority,
    string Body,
    string? RelatedEntityType,
    Guid? RelatedEntityId,
    string? IdempotencyKey);

/// <summary>ورودی پاسخ برای پورت دایرکتوری.</summary>
public sealed record ReplyTicketCommand(
    string Body,
    bool IsInternalNote,
    string? IdempotencyKey);

/// <summary>پچ Admin برای پورت دایرکتوری.</summary>
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
