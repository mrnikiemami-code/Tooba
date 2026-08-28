namespace Tooba.Catalog.Domain;

/// <summary>
/// نقطهٔ توسعهٔ آینده برای پیکربندی facet رده (TB بعدی). مالکیت با Category است؛ پیاده‌سازی کامل اینجا نیست.
/// </summary>
public sealed record CategoryFacetConfigurationMarker;

/// <summary>
/// جداسازی مفهومی Mega Menu از taxonomy: آیتم منو به CategoryId اشاره می‌کند؛
/// Category مالک layout/ستون منو نیست.
/// </summary>
public sealed record MegaMenuItemCategoryBindingMarker(Guid CategoryId);

/// <summary>
/// نقطهٔ توسعه برای انتساب ویژگی به رده (از قبل binding زنده است؛ این فقط نشانگر معماری است).
/// </summary>
public sealed record CategoryAttributeAssignmentMarker;

/// <summary>
/// نقطهٔ توسعه برای ارث schema ویژگی در درخت رده.
/// </summary>
public sealed record AttributeInheritanceMarker;

/// <summary>
/// نقطهٔ توسعه برای محورهای Variant در سطح رده.
/// </summary>
public sealed record VariantAxisAssignmentMarker;
