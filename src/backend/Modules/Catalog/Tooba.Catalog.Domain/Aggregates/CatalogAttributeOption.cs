using Tooba.BuildingBlocks;

namespace Tooba.Catalog.Domain.Aggregates;

/// <summary>
/// گزینهٔ شمارشی یک تعریف. هویت Offer فروشنده نیست.
/// </summary>
public sealed class CatalogAttributeOption
{
    /// <summary>
    /// شناسهٔ گزینه.
    /// </summary>
    public Guid OptionId { get; init; }

    /// <summary>
    /// تعریف والد در همین schema.
    /// </summary>
    public Guid DefinitionId { get; init; }

    /// <summary>
    /// کد پایدار گزینه (مثلاً black).
    /// </summary>
    public string Code { get; init; } = "";

    /// <summary>
    /// ترتیب نمایش گزینه.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// فعال بودن گزینه برای انتخاب؛ پیش‌فرض true برای BC.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// گزینه می‌سازد.
    /// </summary>
    public static CatalogAttributeOption Create(Guid definitionId, string code, int displayOrder = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        return new CatalogAttributeOption
        {
            OptionId = UuidV7.New(),
            DefinitionId = definitionId,
            Code = code.Trim().ToLowerInvariant(),
            DisplayOrder = displayOrder,
            IsActive = true,
        };
    }
}
