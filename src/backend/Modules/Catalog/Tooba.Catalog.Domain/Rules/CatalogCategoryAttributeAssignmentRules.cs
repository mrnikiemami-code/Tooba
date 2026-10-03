using Tooba.BuildingBlocks;

namespace Tooba.Catalog.Domain.Rules;

/// <summary>
/// قواعد اعتبارسنجی assignment رفتار category-specific.
/// </summary>
public static class CatalogCategoryAttributeAssignmentRules
{
    /// <summary>
    /// آیا نوع مقدار ذاتاً از محور تنوع پشتیبانی می‌کند.
    /// </summary>
    public static bool ValueKindSupportsVariantAxis(CatalogAttributeValueKind valueKind) =>
        valueKind is CatalogAttributeValueKind.Enumeration or CatalogAttributeValueKind.Number;

    /// <summary>
    /// فعال‌سازی capability محور تنوع را در برابر ValueKind بررسی می‌کند.
    /// </summary>
    public static void ValidateVariantAxisCapabilityEnable(CatalogAttributeValueKind valueKind)
    {
        if (!ValueKindSupportsVariantAxis(valueKind))
        {
            throw new InvalidOperationException("catalog.attribute.variant_axis.value_kind.invalid");
        }
    }

    /// <summary>
    /// فعال‌سازی محور تنوع را در برابر capability/type تعریف بررسی می‌کند.
    /// </summary>
    public static void ValidateVariantAxis(CatalogAttributeDefinition definition, bool isVariantAxisEnabled)
    {
        if (!isVariantAxisEnabled)
        {
            return;
        }

        if (!definition.IsVariantAxisAllowed)
        {
            throw new InvalidOperationException("catalog.attribute.variant_axis.capability_disabled");
        }

        if (!ValueKindSupportsVariantAxis(definition.ValueKind))
        {
            throw new InvalidOperationException("catalog.attribute.variant_axis.value_kind.invalid");
        }
    }
}
