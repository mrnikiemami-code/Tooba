using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;

namespace Tooba.Catalog.Application.Tags.Queries;

/// <summary>GET /v1/admin/catalog/categories/{categoryId}/tags/</summary>
public sealed record ListCategoryTagsQuery(Guid CategoryId, string? Locale)
    : IRequest<Result<IReadOnlyList<TagView>>>;
