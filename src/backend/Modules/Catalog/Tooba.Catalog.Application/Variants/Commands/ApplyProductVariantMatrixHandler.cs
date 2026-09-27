using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.Variants.Models;
using Tooba.Catalog.Application.Variants.Ports;
using Tooba.Catalog.Contracts.Errors;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Variants.Commands;

/// <summary>Handles ApplyProductVariantMatrixCommand and enriches OfferCount.</summary>
public sealed class ApplyProductVariantMatrixHandler
    : IRequestHandler<ApplyProductVariantMatrixCommand, Result<ProductVariantApplyResult>>
{
    private readonly IProductVariantDirectory _directory;
    private readonly IVariantOfferLookup _offers;

    /// <summary>Creates the handler.</summary>
    public ApplyProductVariantMatrixHandler(
        IProductVariantDirectory directory,
        IVariantOfferLookup offers)
    {
        _directory = directory;
        _offers = offers;
    }

    /// <inheritdoc />
    public async Task<Result<ProductVariantApplyResult>> Handle(
        ApplyProductVariantMatrixCommand request,
        CancellationToken cancellationToken)
    {
        var model = request.Model;
        var patches = new List<ProductVariantPatchInput>();
        foreach (var patch in model.VariantPatches ?? [])
        {
            if (!ProductVariantPatchStatusMapper.TryParse(patch.Status, out var status))
            {
                return Result.Failure<ProductVariantApplyResult>(
                    new SemanticError(CatalogErrorCodes.VariantPatchStatusInvalid));
            }

            patches.Add(new ProductVariantPatchInput(
                patch.VariantId,
                status,
                patch.CatalogCodeSeam,
                patch.SortOrder,
                patch.IsDefault));
        }

        var axes = (model.SelectedAxes ?? [])
            .Select(a => new ProductVariantSelectedAxisInput(a.DefinitionId, a.OptionIds ?? []))
            .ToList();
        var input = new ProductVariantApplyInput(model.Locale, axes, model.DefaultVariantId, patches);
        var result = await _directory.ApplyMatrixAsync(request.ProductId, input, cancellationToken);
        if (result.IsFailure)
        {
            return result;
        }

        var applied = result.Value;
        var counts = await _offers.CountOffersByCatalogVariantIdsAsync(
            applied.Variants.Select(v => v.VariantId).ToArray(),
            cancellationToken);
        var variants = applied.Variants
            .Select(v => v with { OfferCount = counts.GetValueOrDefault(v.VariantId) })
            .ToList();
        return Result.Success(applied with { Variants = variants });
    }
}
