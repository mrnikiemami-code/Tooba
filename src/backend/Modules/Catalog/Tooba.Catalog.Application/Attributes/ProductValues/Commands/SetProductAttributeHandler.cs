using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Attributes.ProductValues.Models;
using Tooba.Catalog.Application.Attributes.ProductValues.Ports;

namespace Tooba.Catalog.Application.Attributes.ProductValues.Commands;

/// <summary>Handles SetProductAttributeCommand.</summary>
public sealed class SetProductAttributeHandler
    : IRequestHandler<SetProductAttributeCommand, Result<ProductAttributeMutationOk>>
{
    private readonly IProductAttributeDirectory _directory;

    /// <summary>Creates the handler.</summary>
    public SetProductAttributeHandler(IProductAttributeDirectory directory) =>
        _directory = directory;

    /// <inheritdoc />
    public async Task<Result<ProductAttributeMutationOk>> Handle(
        SetProductAttributeCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _directory.SetSingleAsync(
            request.ProductId,
            request.DefinitionId,
            request.Model.RawValue,
            request.Model.EnumOptionId,
            cancellationToken);
        return result.IsFailure
            ? Result.Failure<ProductAttributeMutationOk>(result.FirstError)
            : Result.Success(new ProductAttributeMutationOk());
    }
}
