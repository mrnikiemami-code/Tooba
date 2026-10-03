using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Models;

/// <summary>صفحهٔ تاریخچهٔ محصول.</summary>
public sealed record ProductHistoryPage(
    IReadOnlyList<ProductHistoryEntryDto> Items,
    int TotalCount,
    int Skip,
    int Take);
