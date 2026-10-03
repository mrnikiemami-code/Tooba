using Tooba.BuildingBlocks;

namespace Tooba.Catalog.Domain.Enums;

/// <summary>
/// گونهٔ مقدار ویژگی تایپ‌شده. از EAV آزاد بدون نوع جلوگیری می‌کند.
/// </summary>
public enum CatalogAttributeValueKind
{
    /// <summary>
    /// متن آزاد محلی‌سازی‌پذیر در لایهٔ ترجمه، نه قیمت.
    /// </summary>
    Text = 0,

    /// <summary>
    /// عدد اعشاری توصیفی (وزن/اندازه)، نه مبلغ پول.
    /// </summary>
    Number = 1,

    /// <summary>
    /// مقدار بولی مشخصات، نه flag انبار.
    /// </summary>
    Boolean = 2,

    /// <summary>
    /// گزینه از فهرست بسته؛ محور Variant معمولاً از این گونه است.
    /// </summary>
    Enumeration = 3,

    /// <summary>
    /// تاریخ/زمان توصیفی، نه زمان تسویه.
    /// </summary>
    Instant = 4,
}
