using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Attributes.ProductValues.Models;

namespace Tooba.Catalog.Application.Attributes.ProductValues.Commands;

/// <summary>Sets a single product attribute value.</summary>
public sealed record SetProductAttributeCommand(
    Guid ProductId,
    Guid DefinitionId,
    SetProductAttributeWriteModel Model)
    : IRequest<Result<ProductAttributeMutationOk>>;
