using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.Categories.Ports;

namespace Tooba.Catalog.Application.Categories.Queries;

/// <summary>Resolves category route.</summary>
public sealed class ResolveCategoryRouteHandler
    : IRequestHandler<ResolveCategoryRouteQuery, Result<CategoryRouteResolveResult>>
{
    private readonly ICategoryDirectory _categories;

    /// <summary>Creates the handler.</summary>
    public ResolveCategoryRouteHandler(ICategoryDirectory categories) => _categories = categories;

    /// <inheritdoc />
    public Task<Result<CategoryRouteResolveResult>> Handle(
        ResolveCategoryRouteQuery request,
        CancellationToken cancellationToken) =>
        _categories.ResolveRouteAsync(
            request.Locale,
            request.Slug,
            request.ForStorefront,
            cancellationToken);
}
