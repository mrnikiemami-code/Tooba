using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.ProductHistory.Ports;
using Tooba.Catalog.Contracts.Errors;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;

namespace Tooba.Catalog.Infrastructure.Directories;

/// <summary>Focused Catalog persistence for Admin product history read paging.</summary>
public sealed class ProductHistoryReader : IProductHistoryReader
{
    private readonly CatalogDbContext _db;

    /// <summary>Creates the reader.</summary>
    public ProductHistoryReader(CatalogDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result<ProductHistoryPage>> ListAsync(
        Guid productId,
        string? section,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        if (!await _db.Products.AsNoTracking().AnyAsync(x => x.ProductId == productId, cancellationToken))
        {
            return Result.Failure<ProductHistoryPage>(
                new SemanticError(CatalogErrorCodes.WorkspaceProductMissing));
        }

        skip = Math.Max(0, skip);
        take = Math.Clamp(take <= 0 ? 50 : take, 1, 100);
        var query = _db.ProductHistoryEntries.AsNoTracking().Where(x => x.ProductId == productId);
        if (!string.IsNullOrWhiteSpace(section))
        {
            var normalized = section.Trim();
            query = query.Where(x => x.Section == normalized);
        }

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(x => x.OccurredAt)
            .ThenByDescending(x => x.HistoryId)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return Result.Success(new ProductHistoryPage(rows.Select(ToHistoryDto).ToList(), total, skip, take));
    }

    internal static ProductHistoryEntryDto ToHistoryDto(CatalogProductHistoryEntry row) =>
        new(
            row.HistoryId,
            row.ProductId,
            row.EventType,
            row.Section,
            ProductHistoryRules.SectionLabelFa(row.Section),
            row.SummaryFa,
            row.BeforeSummary,
            row.AfterSummary,
            row.ActorUserId,
            string.IsNullOrWhiteSpace(row.ActorDisplayName)
                ? ProductHistoryRules.ActorSystemFa
                : row.ActorDisplayName!,
            row.OccurredAt);
}
