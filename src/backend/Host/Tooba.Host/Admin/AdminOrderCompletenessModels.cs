namespace Tooba.Host.Admin;

/// <summary>درخواست افزودن یادداشت عملیاتی داخلی.</summary>
public sealed record AdminOrderNoteRequest(string Body);

/// <summary>snapshot یادداشت برای پنل ادمین.</summary>
public sealed record AdminOrderNoteView(
    Guid NoteId,
    Guid CheckoutId,
    string Body,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAt,
    string ActorKind,
    string ActorDisplayName,
    string ActorDisplayFa,
    string ActorDisplayEn);

/// <summary>ورودی تایم‌لاین عملیاتی ترکیبی (بدون event store جدید).</summary>
public sealed record AdminOperationalHistoryEntry(
    DateTimeOffset OccurredAt,
    string Kind,
    string LabelFa,
    string LabelEn,
    string ActorKind,
    string ActorDisplayName,
    string ActorDisplayFa,
    string ActorDisplayEn,
    string? SummaryFa = null,
    string? SummaryEn = null);

/// <summary>صفحهٔ تاریخچهٔ عملیاتی.</summary>
public sealed record AdminOperationalHistoryPage(
    Guid CheckoutId,
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyList<AdminOperationalHistoryEntry> Items);
