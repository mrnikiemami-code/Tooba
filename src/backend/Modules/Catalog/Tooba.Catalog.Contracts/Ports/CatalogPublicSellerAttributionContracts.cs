namespace Tooba.Catalog.Contracts.Ports;

/// <summary>
/// انتساب عمومی ویترین برای یک شناسهٔ Party فروشنده.
/// فقط فروشندگانی که واقعاً در ویترین عمومی‌اند برمی‌گردند؛ شناسه‌های ناشناخته/غیرعمومی حذف می‌شوند.
/// </summary>
public sealed record CatalogPublicSellerAttribution(
    Guid SellerPartyId,
    string PublicId,
    string DisplayName);

/// <summary>جستجوی دسته‌ای شناسه و نام عمومی ویترین بر اساس Party داخلی فروشنده.</summary>
public interface ICatalogPublicSellerAttributionLookup
{
    /// <summary>
    /// انتساب‌های عمومی را برای شناسه‌های Party درخواستی resolve می‌کند.
    /// شناسه‌های حذف‌شده فروشندهٔ عمومی ویترین نیستند؛ فراخواننده نباید شناسهٔ عمومی جعل کند.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, CatalogPublicSellerAttribution>> ResolveAsync(
        IReadOnlyCollection<Guid> sellerPartyIds,
        CancellationToken cancellationToken);
}
