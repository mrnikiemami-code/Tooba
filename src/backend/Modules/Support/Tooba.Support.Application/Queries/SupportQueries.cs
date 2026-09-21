namespace Tooba.Support.Application.Queries;

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
