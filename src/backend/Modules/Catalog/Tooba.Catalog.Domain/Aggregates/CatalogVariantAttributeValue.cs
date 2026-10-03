using Tooba.BuildingBlocks;

namespace Tooba.Catalog.Domain.Aggregates;

/// <summary>
/// مقدار محور Variant. ترکیب این مقادیر هویت Offer فروشنده نیست.
/// </summary>
public sealed class CatalogVariantAttributeValue
{
    /// <summary>
    /// شناسهٔ مقدار.
    /// </summary>
    public Guid ValueId { get; init; }

    /// <summary>
    /// گونهٔ Catalog.
    /// </summary>
    public Guid VariantId { get; init; }

    /// <summary>
    /// تعریف محور.
    /// </summary>
    public Guid DefinitionId { get; init; }

    /// <summary>
    /// مقدار نرمال ترکیب.
    /// </summary>
    public string CanonicalValue { get; init; } = "";

    /// <summary>
    /// مقدار محور می‌سازد.
    /// </summary>
    public static CatalogVariantAttributeValue Create(Guid variantId, Guid definitionId, string canonicalValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(canonicalValue);
        return new CatalogVariantAttributeValue
        {
            ValueId = UuidV7.New(),
            VariantId = variantId,
            DefinitionId = definitionId,
            CanonicalValue = canonicalValue.Trim(),
        };
    }
}
