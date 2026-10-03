using Tooba.BuildingBlocks;

namespace Tooba.Catalog.Domain.Enums;

/// <summary>
/// نوع نمایش فیلتر در PLP — نه نام کامپوننت frontend.
/// </summary>
public enum CatalogFacetDisplayType
{
    /// <summary>چندانتخابی با checkbox.</summary>
    CheckboxList = 0,

    /// <summary>انتخاب با جستجو.</summary>
    SearchableSelect = 1,

    /// <summary>بازهٔ عددی.</summary>
    Range = 2,

    /// <summary>نمایش رنگ (نیاز به متادیتای رنگ گزینه).</summary>
    ColorSwatch = 3,

    /// <summary>کلید روشن/خاموش برای بولی.</summary>
    BooleanToggle = 4,
}
