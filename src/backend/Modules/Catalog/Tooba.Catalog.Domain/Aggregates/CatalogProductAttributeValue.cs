using Tooba.BuildingBlocks;

namespace Tooba.Catalog.Domain.Aggregates;

/// <summary>
/// مقدار ویژگی روی محصول (غیرمحور Variant). مبلغ و موجودی نیست.
/// </summary>
public sealed class CatalogProductAttributeValue
{
    /// <summary>
    /// شناسهٔ مقدار.
    /// </summary>
    public Guid ValueId { get; init; }

    /// <summary>
    /// محصول.
    /// </summary>
    public Guid ProductId { get; init; }

    /// <summary>
    /// تعریف ویژگی.
    /// </summary>
    public Guid DefinitionId { get; init; }

    /// <summary>
    /// مقدار نرمال‌شده برای مقایسهٔ نوعی.
    /// </summary>
    public string CanonicalValue { get; init; } = "";

    /// <summary>
    /// مقدار محصول می‌سازد.
    /// </summary>
    public static CatalogProductAttributeValue Create(Guid productId, Guid definitionId, string canonicalValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(canonicalValue);
        return new CatalogProductAttributeValue
        {
            ValueId = UuidV7.New(),
            ProductId = productId,
            DefinitionId = definitionId,
            CanonicalValue = canonicalValue.Trim(),
        };
    }
}
