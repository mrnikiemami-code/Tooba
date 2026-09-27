using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Brands.Models;
using Tooba.Catalog.Application.Brands.Ports;

namespace Tooba.Catalog.Application.Brands.Queries;

/// <summary>Handles <see cref="ListBrandOptionsQuery"/>.</summary>
public sealed class ListBrandOptionsHandler
    : IRequestHandler<ListBrandOptionsQuery, Result<IReadOnlyList<BrandOptionView>>>
{
    private readonly IBrandOptionReader _reader;

    /// <summary>Creates the handler.</summary>
    public ListBrandOptionsHandler(IBrandOptionReader reader) => _reader = reader;

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<BrandOptionView>>> Handle(
        ListBrandOptionsQuery request,
        CancellationToken cancellationToken) =>
        _reader.ListAsync(request.Search, cancellationToken);
}
