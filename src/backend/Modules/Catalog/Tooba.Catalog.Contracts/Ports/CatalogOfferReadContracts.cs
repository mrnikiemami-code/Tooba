namespace Tooba.Catalog.Contracts.Ports;

/// <summary>Catalog-owned descriptive data used to present an offer.</summary>
public sealed record CatalogOfferPresentation(
    Guid CatalogVariantId,
    Guid ProductId,
    string ProductTitle,
    string? BrandName,
    string? UnitCode,
    string? UnitName,
    string? UnitShortName,
    string? VariantCode = null,
    Guid? PrimaryMediaAssetId = null,
    string? VariantLabel = null,
    int UnitQuantityDecimalPlaces = 0,
    decimal? UnitQuantityStep = null,
    string? UnitRoundingMode = null);

/// <summary>Provides Catalog-owned offer presentation data without exposing persistence.</summary>
public interface ICatalogOfferReadGateway
{
    /// <summary>Reads presentation data for the requested variants.</summary>
    Task<IReadOnlyDictionary<Guid, CatalogOfferPresentation>> GetOfferPresentationsAsync(
        IReadOnlyCollection<Guid> catalogVariantIds,
        CancellationToken cancellationToken,
        string? locale = null);
}
