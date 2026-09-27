namespace Tooba.Catalog.Application.ProductHistory.Models;

/// <summary>Admin HTTP history page — preserves Host JSON property names.</summary>
public sealed record ProductHistoryPageView(
    IReadOnlyList<ProductHistoryItemView> Items,
    int TotalCount,
    int Skip,
    int Take);

/// <summary>Admin HTTP history row — preserves Host JSON property names.</summary>
public sealed record ProductHistoryItemView(
    Guid HistoryId,
    string EventType,
    string Section,
    string SectionLabelFa,
    string SummaryFa,
    string? BeforeSummary,
    string? AfterSummary,
    string ActorDisplayName,
    DateTimeOffset OccurredAt);
