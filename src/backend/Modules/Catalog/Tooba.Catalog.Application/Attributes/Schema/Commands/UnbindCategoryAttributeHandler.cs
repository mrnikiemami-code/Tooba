using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Attributes.Schema.Models;
using Tooba.Catalog.Application.Attributes.Schema.Ports;

namespace Tooba.Catalog.Application.Attributes.Schema.Commands;

/// <summary>Unbinds a definition from a category attribute-schema.</summary>
public sealed class UnbindCategoryAttributeHandler
    : IRequestHandler<UnbindCategoryAttributeCommand, Result<CategoryAttributeSchemaMutationResult>>
{
    private readonly ICategoryAttributeSchemaDirectory _schema;

    /// <summary>Creates the handler.</summary>
    public UnbindCategoryAttributeHandler(ICategoryAttributeSchemaDirectory schema) =>
        _schema = schema;

    /// <inheritdoc />
    public Task<Result<CategoryAttributeSchemaMutationResult>> Handle(
        UnbindCategoryAttributeCommand request,
        CancellationToken cancellationToken) =>
        _schema.UnbindAsync(request.CategoryId, request.DefinitionId, cancellationToken);
}
