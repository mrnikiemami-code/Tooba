namespace Tooba.Catalog.Application.Seller;

/// <summary>
/// Stable machine codes for the seller Catalog capability.
/// <para>
/// <see cref="SellerMissing"/> is a shared machine code whose canonical descriptor and
/// localization are owned by the Order module's error catalog. Catalog deliberately consumes
/// the same string instead of registering a duplicate descriptor (duplicate registration fails
/// fast in <c>ErrorDefinitionCatalog</c>) and reuses the same status (404) the Host slice emitted.
/// </para>
/// </summary>
public static class SellerCatalogErrorCodes
{
    /// <summary>Referenced seller party was not found.</summary>
    public const string SellerMissing = "seller.missing";
}
