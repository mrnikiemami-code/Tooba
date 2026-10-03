using Tooba.BuildingBlocks;

namespace Tooba.Catalog.Domain.Enums;

/// <summary>
/// مالک متن چندزبانه. Locale با Market/Currency قاطی نمی‌شود.
/// </summary>
public enum CatalogLocalizedOwnerKind
{
    /// <summary>
    /// نام/شرح محصول Catalog.
    /// </summary>
    Product = 0,

    /// <summary>
    /// نام ردهٔ طبقه‌بندی.
    /// </summary>
    Category = 1,

    /// <summary>
    /// نام برند تحریری.
    /// </summary>
    Brand = 2,

    /// <summary>
    /// برچسب تعریف ویژگی.
    /// </summary>
    AttributeDefinition = 3,

    /// <summary>
    /// برچسب گزینهٔ شمارشی.
    /// </summary>
    AttributeOption = 4,

    /// <summary>
    /// نام برچسب تاکسونومی Catalog (نه meta keywords).
    /// </summary>
    Tag = 5,
}
