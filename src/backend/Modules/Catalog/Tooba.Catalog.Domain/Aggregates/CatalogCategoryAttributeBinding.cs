using Tooba.BuildingBlocks;

namespace Tooba.Catalog.Domain.Aggregates;

/// <summary>
/// پیوند تعریف ویژگی به رده برای schema مؤثر. SQL بیرون از Catalog نیست.
/// </summary>
public sealed class CatalogCategoryAttributeBinding
{
    /// <summary>
    /// شناسهٔ پیوند.
    /// </summary>
    public Guid BindingId { get; init; }

    /// <summary>
    /// ردهٔ مالک schema.
    /// </summary>
    public Guid CategoryId { get; init; }

    /// <summary>
    /// تعریف ویژگی.
    /// </summary>
    public Guid DefinitionId { get; init; }

    /// <summary>
    /// ترتیب نمایش در schema همین رده.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// الزام در همین رده (assignment-level).
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// نمایش در فیلتر محصولات برای همین رده.
    /// </summary>
    public bool IsFilterable { get; set; }

    /// <summary>
    /// استفاده به‌عنوان محور تنوع در همین رده (نیاز به IsVariantAxisAllowed روی تعریف).
    /// </summary>
    public bool IsVariantAxis { get; set; }

    /// <summary>
    /// نمایش در مقایسه محصولات برای همین رده.
    /// </summary>
    public bool IsComparable { get; set; }

    /// <summary>
    /// زمان ایجاد.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// پیوند رده-تعریف می‌سازد.
    /// </summary>
    public static CatalogCategoryAttributeBinding Bind(
        Guid categoryId,
        Guid definitionId,
        int displayOrder,
        bool isRequired,
        bool isFilterable,
        bool isVariantAxis,
        bool isComparable,
        DateTimeOffset now) =>
        new()
        {
            BindingId = UuidV7.New(),
            CategoryId = categoryId,
            DefinitionId = definitionId,
            DisplayOrder = displayOrder,
            IsRequired = isRequired,
            IsFilterable = isFilterable,
            IsVariantAxis = isVariantAxis,
            IsComparable = isComparable,
            CreatedAt = now,
        };
}
