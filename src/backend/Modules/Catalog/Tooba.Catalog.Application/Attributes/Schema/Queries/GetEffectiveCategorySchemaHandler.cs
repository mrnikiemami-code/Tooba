using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.Attributes.Schema.Ports;

namespace Tooba.Catalog.Application.Attributes.Schema.Queries;

/// <summary>Loads effective category attribute-schema.</summary>
public sealed class GetEffectiveCategorySchemaHandler
    : IRequestHandler<GetEffectiveCategorySchemaQuery, Result<IReadOnlyList<EffectiveSchemaEntry>>>
{
    private readonly ICategoryAttributeSchemaDirectory _schema;

    /// <summary>Creates the handler.</summary>
    public GetEffectiveCategorySchemaHandler(ICategoryAttributeSchemaDirectory schema) =>
        _schema = schema;

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<EffectiveSchemaEntry>>> Handle(
        GetEffectiveCategorySchemaQuery request,
        CancellationToken cancellationToken) =>
        _schema.GetEffectiveAsync(request.CategoryId, cancellationToken);
}
