using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;

namespace Tooba.Catalog.Application.Facets.Queries;

/// <summary>GET /v1/admin/catalog/categories/{categoryId}/facets/effective</summary>
public sealed record GetEffectiveCategoryFacetsQuery(Guid CategoryId, string? Locale)
    : IRequest<Result<IReadOnlyList<EffectiveCategoryFacet>>>;
