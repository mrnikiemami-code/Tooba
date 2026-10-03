using Tooba.BuildingBlocks;

namespace Tooba.Catalog.Domain.Aggregates;

/// <summary>
/// محورهای Variant انتخاب‌شده برای یک محصول. ماتریس کامل ترکیبی اینجا تولید نمی‌شود.
/// </summary>
public sealed class CatalogProductVariantAxis
{
    /// <summary>
    /// شناسهٔ ردیف.
    /// </summary>
    public Guid AxisId { get; init; }

    /// <summary>
    /// محصول مالک محورهای انتخاب‌شده.
    /// </summary>
    public Guid ProductId { get; init; }

    /// <summary>
    /// تعریف مجاز محور (باید IsVariantAxis=true باشد).
    /// </summary>
    public Guid DefinitionId { get; init; }

    /// <summary>
    /// ترتیب محور در هویت ترکیب.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// ردیف محور محصول می‌سازد.
    /// </summary>
    public static CatalogProductVariantAxis Create(Guid productId, Guid definitionId, int displayOrder) =>
        new()
        {
            AxisId = UuidV7.New(),
            ProductId = productId,
            DefinitionId = definitionId,
            DisplayOrder = displayOrder,
        };
}
