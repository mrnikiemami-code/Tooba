using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.ProductHistory.Models;
using Tooba.Catalog.Application.ProductHistory.Ports;

namespace Tooba.Catalog.Application.ProductHistory.Queries;

/// <summary>Handles GetProductHistoryQuery.</summary>
public sealed class GetProductHistoryHandler
    : IRequestHandler<GetProductHistoryQuery, Result<ProductHistoryPageView>>
{
    private readonly IProductHistoryReader _reader;

    /// <summary>Creates the handler.</summary>
    public GetProductHistoryHandler(IProductHistoryReader reader) => _reader = reader;

    /// <inheritdoc />
    public async Task<Result<ProductHistoryPageView>> Handle(
        GetProductHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var page = await _reader.ListAsync(
            request.ProductId,
            request.Section,
            request.Skip,
            request.Take,
            cancellationToken);
        if (page.IsFailure)
        {
            return Result.Failure<ProductHistoryPageView>(page.Errors);
        }

        return Result.Success(Map(page.Value));
    }

    /// <summary>Maps directory page DTO to Admin HTTP view.</summary>
    public static ProductHistoryPageView Map(ProductHistoryPage page) =>
        new(
            page.Items.Select(MapItem).ToList(),
            page.TotalCount,
            page.Skip,
            page.Take);

    /// <summary>Maps directory entry DTO to Admin HTTP item view.</summary>
    public static ProductHistoryItemView MapItem(ProductHistoryEntryDto row) =>
        new(
            row.HistoryId,
            row.EventType,
            row.Section,
            row.SectionLabelFa,
            row.SummaryFa,
            row.BeforeSummary,
            row.AfterSummary,
            row.ActorDisplayName,
            row.OccurredAt);
}
