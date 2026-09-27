using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.Variants.Ports;

namespace Tooba.Catalog.Application.Variants.Queries;

/// <summary>Handles PreviewProductVariantsQuery and enriches ReferencedByOffers.</summary>
public sealed class PreviewProductVariantsHandler
    : IRequestHandler<PreviewProductVariantsQuery, Result<ProductVariantPreviewResult>>
{
    private readonly IProductVariantDirectory _directory;
    private readonly IVariantOfferLookup _offers;

    /// <summary>Creates the handler.</summary>
    public PreviewProductVariantsHandler(
        IProductVariantDirectory directory,
        IVariantOfferLookup offers)
    {
        _directory = directory;
        _offers = offers;
    }

    /// <inheritdoc />
    public async Task<Result<ProductVariantPreviewResult>> Handle(
        PreviewProductVariantsQuery request,
        CancellationToken cancellationToken)
    {
        var model = request.Model;
        var locale = string.IsNullOrWhiteSpace(model.Locale) ? "fa-IR" : model.Locale.Trim();
        var axes = (model.SelectedAxes ?? [])
            .Select(a => new ProductVariantSelectedAxisInput(a.DefinitionId, a.OptionIds ?? []))
            .ToList();
        var result = await _directory.PreviewCombinationsAsync(
            request.ProductId,
            axes,
            locale,
            cancellationToken);
        if (result.IsFailure)
        {
            return result;
        }

        var preview = result.Value;
        var variantIds = preview.Combinations
            .Where(c => c.ExistingVariantId is Guid)
            .Select(c => c.ExistingVariantId!.Value)
            .Distinct()
            .ToArray();
        var counts = await _offers.CountOffersByCatalogVariantIdsAsync(variantIds, cancellationToken);
        var combinations = preview.Combinations.Select(c =>
        {
            bool? referenced = null;
            if (c.ExistingVariantId is Guid id && counts.TryGetValue(id, out var count))
            {
                referenced = count > 0;
            }

            return c with { ReferencedByOffers = referenced };
        }).ToList();
        return Result.Success(preview with { Combinations = combinations });
    }
}
