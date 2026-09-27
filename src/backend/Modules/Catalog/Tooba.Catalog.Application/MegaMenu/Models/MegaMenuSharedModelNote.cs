namespace Tooba.Catalog.Application.MegaMenu.Models;

/// <summary>
/// MegaMenu view/input DTOs remain Application-root (<c>CategoryMegaMenuBindingInput</c>,
/// configuration/placement/storefront views) so ICatalogDirectory and CatalogDemo keep one authoritative shape.
/// This capability folder owns CQRS requests/ports/validators; no duplicate command-shaped Models.
/// </summary>
internal static class MegaMenuSharedModelNote
{
}
