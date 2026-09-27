using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;

namespace Tooba.Catalog.Application.Tags.Queries;

/// <summary>GET /v1/admin/catalog/tags/{tagId}</summary>
public sealed record GetTagQuery(Guid TagId, string? Locale) : IRequest<Result<TagView>>;
