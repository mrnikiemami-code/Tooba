namespace Tooba.Catalog.Application.Seller.Models;

/// <summary>
/// گزینهٔ انتخاب گونهٔ Catalog منتشرشده برای ایجاد Offer؛ فقط‌خواندنی است.
/// شکل JSON برای پنل فروشنده بدون تغییر نسبت به قرارداد قبلی حفظ شده است.
/// </summary>
public sealed record SellerCatalogVariantOption(
    Guid CatalogVariantId,
    Guid ProductId,
    string ProductTitle,
    string? CatalogCode,
    string ProductStatus);

/// <summary>Published Catalog product picker row (offer create). No price/stock is exposed.</summary>
public sealed record CatalogPickerProductOption(
    Guid ProductId,
    string Title,
    string? BrandName,
    Guid? PrimaryMediaAssetId,
    int PublishedVariantCount,
    string? UnitCode = null,
    string? UnitName = null,
    int QuantityDecimalPlaces = 0,
    decimal? QuantityStep = null,
    string? RoundingMode = null);

/// <summary>Bounded server page for the published Catalog product picker.</summary>
public sealed record CatalogPickerProductPage(
    IReadOnlyList<CatalogPickerProductOption> Items,
    int TotalCount,
    int Page,
    int PageSize,
    bool HasMore);

/// <summary>One human-readable variant axis label from the real Catalog schema.</summary>
public sealed record CatalogPickerAxisLabel(string Name, string Value);

/// <summary>Published Catalog variant picker row with human labels, not a technical code stand-in.</summary>
public sealed record CatalogPickerVariantOption(
    Guid CatalogVariantId,
    Guid ProductId,
    string DisplayLabel,
    IReadOnlyList<CatalogPickerAxisLabel> AxisLabels,
    string? CatalogCode,
    string Status);
