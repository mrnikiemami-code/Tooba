using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;

namespace Tooba.Catalog.Application.Facets.Queries;

/// <summary>GET /v1/admin/catalog/categories/{categoryId}/facets/local</summary>
public sealed record ListLocalCategoryFacetsQuery(Guid CategoryId)
    : IRequest<Result<IReadOnlyList<CategoryFacetConfigurationView>>>;
