using Tooba.BuildingBlocks;

namespace Tooba.Catalog.Domain.Enums;

/// <summary>
/// نقش پیوند محصول↔رده: دسته اصلی (schema) یا اضافی (کشف/PLP).
/// </summary>
public enum CatalogProductCategoryRole : byte
{
    /// <summary>دسته اصلی — منبع schema و breadcrumb.</summary>
    Primary = 0,

    /// <summary>دسته اضافی — فقط کشف/ناوبری/PLP.</summary>
    Additional = 1,
}
