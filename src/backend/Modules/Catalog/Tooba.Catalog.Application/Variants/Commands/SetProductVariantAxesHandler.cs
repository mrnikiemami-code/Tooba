using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Variants.Models;
using Tooba.Catalog.Application.Variants.Ports;

namespace Tooba.Catalog.Application.Variants.Commands;

/// <summary>Handles SetProductVariantAxesCommand.</summary>
public sealed class SetProductVariantAxesHandler
    : IRequestHandler<SetProductVariantAxesCommand, Result<ProductVariantAxesMutationOk>>
{
    private readonly IProductVariantDirectory _directory;

    /// <summary>Creates the handler.</summary>
    public SetProductVariantAxesHandler(IProductVariantDirectory directory) =>
        _directory = directory;

    /// <inheritdoc />
    public async Task<Result<ProductVariantAxesMutationOk>> Handle(
        SetProductVariantAxesCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _directory.SetAxesAsync(
            request.ProductId,
            request.Model.OrderedDefinitionIds ?? [],
            cancellationToken);
        return result.IsFailure
            ? Result.Failure<ProductVariantAxesMutationOk>(result.Errors)
            : Result.Success(new ProductVariantAxesMutationOk());
    }
}
