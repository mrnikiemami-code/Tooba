namespace Tooba.Host.Admin;

/// <summary>
/// ردیف فهرست Admin. مبلغ و واحد قابل‌فروش از Offer/Price/Inventory ترکیب می‌شوند؛ روی هویت Product نیستند.
/// CategorySummary سازگاری قدیمی است (برگ‌ها، نه مسیر کامل). شبکهٔ Admin از PrimaryCategoryName و AdditionalCategoryNames استفاده می‌کند.
/// </summary>
public sealed record AdminProductListItem(
    Guid ProductId,
    string Title,
    string Status,
    int VariantCount,
    int OfferCount,
    string CategorySummary,
    string OfferAmountRange,
    decimal SellableUnits,
    int LocationCount,
    DateTimeOffset UpdatedAt,
    Guid? PrimaryMediaAssetId,
    Guid? PrimaryCategoryId = null,
    string? BrandName = null,
    string? PrimaryCategoryName = null,
    IReadOnlyList<string>? AdditionalCategoryNames = null,
    int AdditionalCategoryCount = 0);
