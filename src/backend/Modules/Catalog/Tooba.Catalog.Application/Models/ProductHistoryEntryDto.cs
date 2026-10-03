using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Models;

/// <summary>یک ردیف تاریخچهٔ انسانی محصول برای Admin.</summary>
public sealed record ProductHistoryEntryDto(
    Guid HistoryId,
    Guid ProductId,
    string EventType,
    string Section,
    string SectionLabelFa,
    string SummaryFa,
    string? BeforeSummary,
    string? AfterSummary,
    Guid? ActorUserId,
    string ActorDisplayName,
    DateTimeOffset OccurredAt);
