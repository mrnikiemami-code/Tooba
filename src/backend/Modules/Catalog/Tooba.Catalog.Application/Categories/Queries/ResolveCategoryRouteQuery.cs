using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;

namespace Tooba.Catalog.Application.Categories.Queries;

/// <summary>Resolves storefront/admin category route by locale+slug.</summary>
public sealed record ResolveCategoryRouteQuery(string Locale, string Slug, bool ForStorefront)
    : IRequest<Result<CategoryRouteResolveResult>>;
