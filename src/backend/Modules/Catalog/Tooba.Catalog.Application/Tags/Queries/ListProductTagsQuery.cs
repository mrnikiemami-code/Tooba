using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;

namespace Tooba.Catalog.Application.Tags.Queries;

/// <summary>GET /v1/admin/catalog/products/{productId}/tags/</summary>
public sealed record ListProductTagsQuery(Guid ProductId, string? Locale)
    : IRequest<Result<IReadOnlyList<TagView>>>;
