using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Variants.Ports;

/// <summary>Catalog-owned port for Product Variant Axes + Matrix Admin operations.</summary>
public interface IProductVariantDirectory
{
    /// <summary>Replaces product variant-axis selection without generating the matrix.</summary>
    Task<Result> SetAxesAsync(
        Guid productId,
        IReadOnlyList<Guid> orderedDefinitionIds,
        CancellationToken cancellationToken);

    /// <summary>Editor state for product variant matrix (OfferCount left null for enrichment).</summary>
    Task<Result<ProductVariantEditorState>> GetEditorStateAsync(
        Guid productId,
        string locale,
        CancellationToken cancellationToken);

    /// <summary>Preview desired combinations without writing (ReferencedByOffers left null).</summary>
    Task<Result<ProductVariantPreviewResult>> PreviewCombinationsAsync(
        Guid productId,
        IReadOnlyList<ProductVariantSelectedAxisInput> selectedAxes,
        string locale,
        CancellationToken cancellationToken);

    /// <summary>Reconciles the variant matrix in one transaction and records product history.</summary>
    Task<Result<ProductVariantApplyResult>> ApplyMatrixAsync(
        Guid productId,
        ProductVariantApplyInput input,
        CancellationToken cancellationToken);

    /// <summary>Readiness for product variants ahead of publish.</summary>
    Task<Result<ProductVariantReadiness>> GetReadinessAsync(
        Guid productId,
        CancellationToken cancellationToken);
}
