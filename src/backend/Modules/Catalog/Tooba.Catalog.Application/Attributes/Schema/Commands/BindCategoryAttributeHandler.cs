using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.Attributes.Schema.Models;
using Tooba.Catalog.Application.Attributes.Schema.Ports;

namespace Tooba.Catalog.Application.Attributes.Schema.Commands;

/// <summary>Binds a definition into a category attribute-schema.</summary>
public sealed class BindCategoryAttributeHandler
    : IRequestHandler<BindCategoryAttributeCommand, Result<CategoryAttributeSchemaMutationResult>>
{
    private readonly ICategoryAttributeSchemaDirectory _schema;

    /// <summary>Creates the handler.</summary>
    public BindCategoryAttributeHandler(ICategoryAttributeSchemaDirectory schema) =>
        _schema = schema;

    /// <inheritdoc />
    public Task<Result<CategoryAttributeSchemaMutationResult>> Handle(
        BindCategoryAttributeCommand request,
        CancellationToken cancellationToken)
    {
        var model = request.Model;
        return _schema.BindAsync(
            request.CategoryId,
            model.DefinitionId,
            model.DisplayOrder,
            new CategoryAttributeAssignmentFlags(
                model.IsRequired,
                model.IsFilterable,
                model.IsVariantAxis,
                model.IsComparable),
            cancellationToken);
    }
}
