namespace Tooba.Host.Admin;

/// <summary>
/// فرمان ایجاد محصول Catalog به‌صورت پیش‌نویس؛ قیمت و موجودی اینجا نیست.
/// </summary>
public sealed record AdminProductCreateRequest(
    string Title,
    string? Slug,
    Guid? CategoryId,
    string? Locale);

/// <summary>
/// به‌روزرسانی هستهٔ محصول در یک locale (عنوان، slug انسانی، شرح‌ها، SEO).
/// </summary>
public sealed record AdminProductCoreUpdateRequest(
    string Locale,
    string Title,
    string? Slug,
    string? ShortDescription,
    string? Description,
    string? SeoTitle,
    string? SeoDescription,
    DateTimeOffset ExpectedUpdatedAt);

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

/// <summary>گزینهٔ انتخاب برند برای Admin.</summary>
public sealed record AdminBrandOption(Guid BrandId, string Name, string Status);

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

/// <summary>بدنهٔ به‌روزرسانی سیاست مقدار محصول.</summary>
public sealed record AdminProductQuantityPolicyRequest(
    Guid UnitOfMeasureId,
    int DecimalPlaces,
    decimal? Step,
    DateTimeOffset ExpectedUpdatedAt);

/// <summary>محور یک گونهٔ جدید.</summary>
public sealed record AdminProductVariantAxisRequest(Guid DefinitionId, string? RawValue, Guid? EnumOptionId);

/// <summary>بدنهٔ ایجاد گونه.</summary>
public sealed record AdminProductVariantCreateRequest(
    string? CatalogCodeSeam,
    IReadOnlyList<AdminProductVariantAxisRequest> Axes);

/// <summary>بدنهٔ ویرایش وضعیت/کد گونه بدون تغییر اثرانگشت.</summary>
public sealed record AdminProductVariantPatchRequest(string? Status, string? CatalogCodeSeam);
