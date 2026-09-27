using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;

namespace Tooba.Catalog.Application.Tags.Queries;

/// <summary>GET /v1/admin/catalog/tags/</summary>
public sealed record ListTagsQuery(string? Locale, string? Search)
    : IRequest<Result<IReadOnlyList<TagView>>>;
