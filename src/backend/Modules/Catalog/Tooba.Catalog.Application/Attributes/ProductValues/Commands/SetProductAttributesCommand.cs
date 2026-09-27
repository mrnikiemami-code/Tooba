using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.Attributes.ProductValues.Models;

namespace Tooba.Catalog.Application.Attributes.ProductValues.Commands;

/// <summary>Bulk-sets product attribute values and returns refreshed editor state.</summary>
public sealed record SetProductAttributesCommand(Guid ProductId, SetProductAttributesWriteModel Model)
    : IRequest<Result<ProductAttributeEditorState>>;
