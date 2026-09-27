using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.Attributes.ProductValues.Ports;

namespace Tooba.Catalog.Application.Attributes.ProductValues.Queries;

/// <summary>Handles GetProductAttributeEditorStateQuery.</summary>
public sealed class GetProductAttributeEditorStateHandler
    : IRequestHandler<GetProductAttributeEditorStateQuery, Result<ProductAttributeEditorState>>
{
    private readonly IProductAttributeDirectory _directory;

    /// <summary>Creates the handler.</summary>
    public GetProductAttributeEditorStateHandler(IProductAttributeDirectory directory) =>
        _directory = directory;

    /// <inheritdoc />
    public Task<Result<ProductAttributeEditorState>> Handle(
        GetProductAttributeEditorStateQuery request,
        CancellationToken cancellationToken)
    {
        var locale = string.IsNullOrWhiteSpace(request.Locale) ? "fa-IR" : request.Locale.Trim();
        return _directory.GetEditorStateAsync(request.ProductId, locale, cancellationToken);
    }
}
