namespace Tooba.Catalog.Contracts.Ports;

/// <summary>ردیف محصول Published برای انتخاب CTA استوری/ویترین.</summary>
public sealed record CatalogPublishedProductOption(Guid ProductId, string Title);

/// <summary>
/// جستجو و صفحه‌بندی محصولات Published از مالک Catalog — بدون sampler توسعه و بدون نشت DbContext.
/// </summary>
public interface ICatalogPublishedProductSearchLookup
{
    /// <summary>
    /// جستجو روی مجموعهٔ Published (فیلتر قبل از صفحه)، سپس enrich عنوان صفحهٔ انتخابی.
    /// </summary>
    Task<(IReadOnlyList<CatalogPublishedProductOption> Items, int TotalCount)> SearchPublishedAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
