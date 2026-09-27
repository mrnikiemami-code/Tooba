using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.ProductHistory.Models;

namespace Tooba.Catalog.Application.ProductHistory.Queries;

/// <summary>Admin GET product history page (optional section filter + skip/take).</summary>
public sealed record GetProductHistoryQuery(
    Guid ProductId,
    string? Section,
    int Skip,
    int Take)
    : IRequest<Result<ProductHistoryPageView>>;
