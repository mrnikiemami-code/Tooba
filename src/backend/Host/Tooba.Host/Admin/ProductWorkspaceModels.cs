namespace Tooba.Host.Admin;

/// <summary>انتساب دستهٔ اضافی (کشف/PLP) بدون تغییر schema.</summary>
public sealed record AdminProductAdditionalCategoryRequest(
    Guid CategoryId,
    DateTimeOffset ExpectedUpdatedAt);

/// <summary>انتساب ردهٔ محصول با تأیید صریح تغییر.</summary>
public sealed record AdminProductCategoryAssignRequest(
    Guid CategoryId,
    bool ConfirmSchemaImpact,
    DateTimeOffset ExpectedUpdatedAt);

/// <summary>انتساب برند Catalog به محصول؛ null یعنی حذف برند.</summary>
public sealed record AdminProductBrandAssignRequest(
    Guid? BrandId,
    DateTimeOffset ExpectedUpdatedAt);

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
