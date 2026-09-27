using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.Attributes.ProductValues.Ports;

namespace Tooba.Catalog.Application.Attributes.ProductValues.Commands;

/// <summary>Handles SetProductAttributesCommand.</summary>
public sealed class SetProductAttributesHandler
    : IRequestHandler<SetProductAttributesCommand, Result<ProductAttributeEditorState>>
{
    private readonly IProductAttributeDirectory _directory;

    /// <summary>Creates the handler.</summary>
    public SetProductAttributesHandler(IProductAttributeDirectory directory) =>
        _directory = directory;

    /// <inheritdoc />
    public Task<Result<ProductAttributeEditorState>> Handle(
        SetProductAttributesCommand request,
        CancellationToken cancellationToken)
    {
        var model = request.Model;
        var locale = string.IsNullOrWhiteSpace(model.Locale) ? "fa-IR" : model.Locale.Trim();
        IReadOnlyList<ProductAttributeValueInput> values = (model.Values ?? [])
            .Select(v => new ProductAttributeValueInput(v.DefinitionId, v.RawValue, v.EnumOptionId, v.Clear))
            .ToList();
        return _directory.SetBulkAsync(request.ProductId, values, locale, cancellationToken);
    }
}
