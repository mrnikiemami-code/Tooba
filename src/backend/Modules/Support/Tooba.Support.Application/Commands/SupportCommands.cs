namespace Tooba.Support.Application.Commands;

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
