using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Attributes.Schema.Models;
using Tooba.Catalog.Application.Attributes.Schema.Ports;

namespace Tooba.Catalog.Application.Attributes.Schema.Commands;

/// <summary>Reorders local category attribute-schema bindings.</summary>
public sealed class ReorderCategoryAttributeBindingsHandler
    : IRequestHandler<ReorderCategoryAttributeBindingsCommand, Result<CategoryAttributeSchemaMutationResult>>
{
    private readonly ICategoryAttributeSchemaDirectory _schema;

    /// <summary>Creates the handler.</summary>
    public ReorderCategoryAttributeBindingsHandler(ICategoryAttributeSchemaDirectory schema) =>
        _schema = schema;

    /// <inheritdoc />
    public Task<Result<CategoryAttributeSchemaMutationResult>> Handle(
        ReorderCategoryAttributeBindingsCommand request,
        CancellationToken cancellationToken) =>
        _schema.ReorderAsync(
            request.CategoryId,
            request.OrderedDefinitionIds ?? Array.Empty<Guid>(),
            cancellationToken);
}
