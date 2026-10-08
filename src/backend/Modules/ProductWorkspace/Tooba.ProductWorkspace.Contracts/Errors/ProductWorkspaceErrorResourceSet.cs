using System.Globalization;
using System.Resources;
using Tooba.BuildingBlocks.Localization;

namespace Tooba.ProductWorkspace.Contracts.Errors;

/// <summary>Resource marker for ProductWorkspaceErrors.resx.</summary>
public static class ProductWorkspaceErrorResources
{
    /// <summary>ResourceManager for ProductWorkspaceErrors.resx.</summary>
    public static ResourceManager Manager { get; } =
        new("Tooba.ProductWorkspace.Contracts.Resources.ProductWorkspaceErrors", typeof(ProductWorkspaceErrorResources).Assembly);
}

/// <summary>
/// ProductWorkspace error resource set — supplies the bilingual text for the <c>workspace.</c> codes that
/// this composing module's HTTP boundary can surface. The <b>descriptors</b> for these codes are owned
/// once by <c>CatalogErrorCatalogContributor</c> (their natural bounded context is the Catalog product
/// write capability); the localization keys travel with the descriptor, so this set resolves them.
/// <para>
/// This is the same split the certified Cart / Fulfillment modules use: a module may own the localization
/// of a shared machine code while a single natural owner registers its descriptor.
/// </para>
/// </summary>
public sealed class ProductWorkspaceErrorResourceSet : IErrorResourceSet
{
    /// <inheritdoc />
    public bool Owns(string localizationKey) =>
        string.Equals(localizationKey, ProductWorkspaceErrorCodes.WorkspaceProductMissing, StringComparison.OrdinalIgnoreCase)
        || string.Equals(localizationKey, ProductWorkspaceErrorCodes.WorkspacePermissionDenied, StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public string? GetString(string localizationKey, CultureInfo culture) =>
        ProductWorkspaceErrorResources.Manager.GetString(localizationKey, culture);
}
