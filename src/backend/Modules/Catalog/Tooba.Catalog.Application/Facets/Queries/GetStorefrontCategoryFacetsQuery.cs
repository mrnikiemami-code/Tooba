using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;

namespace Tooba.Catalog.Application.Facets.Queries;

/// <summary>GET /v1/storefront/categories/{categoryId}/facets</summary>
public sealed record GetStorefrontCategoryFacetsQuery(Guid CategoryId, string? Locale)
    : IRequest<Result<IReadOnlyList<EffectiveCategoryFacet>>>;
