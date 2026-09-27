using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.Variants.Ports;

namespace Tooba.Catalog.Application.Variants.Queries;

/// <summary>Handles GetProductVariantEditorStateQuery and enriches OfferCount.</summary>
public sealed class GetProductVariantEditorStateHandler
    : IRequestHandler<GetProductVariantEditorStateQuery, Result<ProductVariantEditorState>>
{
    private readonly IProductVariantDirectory _directory;
    private readonly IVariantOfferLookup _offers;

    /// <summary>Creates the handler.</summary>
    public GetProductVariantEditorStateHandler(
        IProductVariantDirectory directory,
        IVariantOfferLookup offers)
    {
        _directory = directory;
        _offers = offers;
    }

    /// <inheritdoc />
    public async Task<Result<ProductVariantEditorState>> Handle(
        GetProductVariantEditorStateQuery request,
        CancellationToken cancellationToken)
    {
        var locale = string.IsNullOrWhiteSpace(request.Locale) ? "fa-IR" : request.Locale.Trim();
        var result = await _directory.GetEditorStateAsync(request.ProductId, locale, cancellationToken);
        if (result.IsFailure)
        {
            return result;
        }

        var state = result.Value;
        var counts = await _offers.CountOffersByCatalogVariantIdsAsync(
            state.Variants.Select(v => v.VariantId).ToArray(),
            cancellationToken);
        var variants = state.Variants
            .Select(v => v with { OfferCount = counts.GetValueOrDefault(v.VariantId) })
            .ToList();
        return Result.Success(state with { Variants = variants });
    }
}
