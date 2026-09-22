namespace Tooba.Order.Application.Admin.Completeness.Models;

public sealed record AdminOrderActor(Guid UserId);

public sealed record AdminOrderNoteView(
    Guid NoteId,
    Guid CheckoutId,
    string Body,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAt,
    string ActorKind,
    string ActorDisplayName,
    string ActorDisplayFa,
    string ActorDisplayEn,
    bool CanDelete);

public sealed record AdminOrderHistoryEntry(
    DateTimeOffset OccurredAt,
    string Kind,
    string LabelFa,
    string LabelEn,
    string ActorKind,
    string ActorDisplayName,
    string ActorDisplayFa,
    string ActorDisplayEn,
    string? SummaryFa,
    string? SummaryEn);

public sealed record AdminOrderOperationalHistoryPage(
    Guid CheckoutId,
    int Page,
    int PageSize,
    int Total,
    IReadOnlyList<AdminOrderHistoryEntry> Items);

public sealed record AdminOrderPrintableDocument(string Html);
