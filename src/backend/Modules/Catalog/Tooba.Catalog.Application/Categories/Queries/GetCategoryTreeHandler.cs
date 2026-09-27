using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.Categories.Ports;

namespace Tooba.Catalog.Application.Categories.Queries;

/// <summary>Reads the Admin category tree.</summary>
public sealed class GetCategoryTreeHandler
    : IRequestHandler<GetCategoryTreeQuery, Result<IReadOnlyList<CategoryTreeNodeDto>>>
{
    private readonly ICategoryDirectory _categories;

    /// <summary>Creates the handler.</summary>
    public GetCategoryTreeHandler(ICategoryDirectory categories) => _categories = categories;

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<CategoryTreeNodeDto>>> Handle(
        GetCategoryTreeQuery request,
        CancellationToken cancellationToken) =>
        _categories.GetTreeAsync(request.Locale, request.Search, cancellationToken);
}
