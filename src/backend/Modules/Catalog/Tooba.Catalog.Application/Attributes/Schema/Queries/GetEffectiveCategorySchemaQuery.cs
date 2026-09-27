using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;

namespace Tooba.Catalog.Application.Attributes.Schema.Queries;

/// <summary>GET /v1/admin/catalog/categories/{categoryId}/attribute-schema/effective</summary>
public sealed record GetEffectiveCategorySchemaQuery(Guid CategoryId)
    : IRequest<Result<IReadOnlyList<EffectiveSchemaEntry>>>;
