namespace Tooba.Catalog.Contracts;

/// <summary>Catalog-owned descriptive data used to present an offer.</summary>
public sealed record CatalogOfferPresentation(
    Guid CatalogVariantId,
    Guid ProductId,
    string ProductTitle,
    string? BrandName,
    string? UnitCode,
    string? UnitName,
    string? UnitShortName);

/// <summary>Provides Catalog-owned offer presentation data without exposing persistence.</summary>
public interface ICatalogOfferReadGateway
{
    /// <summary>Reads presentation data for the requested variants.</summary>
    Task<IReadOnlyDictionary<Guid, CatalogOfferPresentation>> GetOfferPresentationsAsync(
        IReadOnlyCollection<Guid> catalogVariantIds,
        CancellationToken cancellationToken);
}
