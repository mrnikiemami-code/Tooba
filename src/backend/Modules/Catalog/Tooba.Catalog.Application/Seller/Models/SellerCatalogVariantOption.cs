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
