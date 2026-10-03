using Tooba.BuildingBlocks;

namespace Tooba.Catalog.Domain.Aggregates;

/// <summary>
/// مرجع رسانهٔ مات. FK به جدول Media ماژول دیگر نیست و باینری ذخیره نمی‌شود.
/// </summary>
public sealed class CatalogProductMediaReference
{
    /// <summary>
    /// شناسهٔ ردیف مرجع.
    /// </summary>
    public Guid ReferenceId { get; init; }

    /// <summary>
    /// محصول مالک مرجع.
    /// </summary>
    public Guid ProductId { get; init; }

    /// <summary>
    /// شناسهٔ مات دارایی در قابلیت Media آینده.
    /// </summary>
    public Guid MediaAssetId { get; init; }

    /// <summary>
    /// ترتیب نمایش گالری داخل محصول.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// تصویر اصلی فهرست/بند انگشتی.
    /// </summary>
    public bool IsPrimary { get; set; }

    /// <summary>
    /// متن جایگزین دسترس‌پذیری؛ باینری نیست.
    /// </summary>
    public string? AltText { get; set; }

    /// <summary>
    /// مرجع مات می‌سازد.
    /// </summary>
    public static CatalogProductMediaReference Link(
        Guid productId,
        Guid mediaAssetId,
        int displayOrder = 0,
        bool isPrimary = false,
        string? altText = null) =>
        new()
        {
            ReferenceId = UuidV7.New(),
            ProductId = productId,
            MediaAssetId = mediaAssetId,
            DisplayOrder = displayOrder,
            IsPrimary = isPrimary,
            AltText = string.IsNullOrWhiteSpace(altText) ? null : altText.Trim(),
        };
}
