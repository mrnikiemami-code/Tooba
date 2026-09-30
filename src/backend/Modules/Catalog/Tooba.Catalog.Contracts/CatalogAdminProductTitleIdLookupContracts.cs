namespace Tooba.Catalog.Contracts;

/// <summary>
/// فیلتر متنی عنوان محصول برای resolve شناسه در گریدهای Admin ماژول‌های دیگر.
/// بدون افشای persistence یا نوع Grid Host.
/// </summary>
public sealed record CatalogProductTitleTextFilter(
    string Operator,
    string? Value);

/// <summary>
/// مرز Contracts برای resolve شناسهٔ محصول از روی عنوان محلی (filter/search گرید نظرات Admin).
/// </summary>
public interface ICatalogAdminProductTitleIdLookup
{
    /// <summary>شناسه محصولاتی که عنوانشان شامل عبارت است.</summary>
    Task<IReadOnlySet<Guid>> ResolveProductIdsByTitleContainsAsync(
        string term,
        CancellationToken cancellationToken);

    /// <summary>شناسه محصولاتی که عنوانشان با عملگر فیلتر متنی مطابقت دارد.</summary>
    Task<IReadOnlySet<Guid>> ResolveProductIdsByTitleFilterAsync(
        CatalogProductTitleTextFilter filter,
        CancellationToken cancellationToken);
}
