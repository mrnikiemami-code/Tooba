namespace Tooba.Order.Application.Admin.Completeness;

public sealed record AdminOrderActor(Guid UserId);

public sealed record AdminOrderNoteView(
    Guid NoteId,
    Guid CheckoutId,
    string Body,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAt,
    bool CanDelete);

public sealed record AdminOrderHistoryEntry(
    DateTimeOffset OccurredAt,
    string Kind,
    string LabelFa,
    string LabelEn,
    string? SummaryFa,
    string? SummaryEn);

public sealed record AdminOrderOperationalHistoryPage(
    Guid CheckoutId,
    int Page,
    int PageSize,
    int Total,
    IReadOnlyList<AdminOrderHistoryEntry> Items);

public sealed record AdminOrderPrintableDocument(string Html);
