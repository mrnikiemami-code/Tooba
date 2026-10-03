using Tooba.BuildingBlocks;

namespace Tooba.Catalog.Domain.Aggregates;

/// <summary>
/// پیوند محصول به رده. merchandising فروشنده نیست.
/// </summary>
public sealed class CatalogProductCategory
{
    /// <summary>
    /// شناسهٔ پیوند.
    /// </summary>
    public Guid AssignmentId { get; init; }

    /// <summary>
    /// محصول Catalog.
    /// </summary>
    public Guid ProductId { get; init; }

    /// <summary>
    /// ردهٔ Catalog.
    /// </summary>
    public Guid CategoryId { get; init; }

    /// <summary>
    /// نقش پیوند (اصلی / اضافی).
    /// </summary>
    public CatalogProductCategoryRole Role { get; init; }

    /// <summary>
    /// پیوند می‌سازد.
    /// </summary>
    public static CatalogProductCategory Assign(
        Guid productId,
        Guid categoryId,
        CatalogProductCategoryRole role = CatalogProductCategoryRole.Primary) =>
        new()
        {
            AssignmentId = UuidV7.New(),
            ProductId = productId,
            CategoryId = categoryId,
            Role = role,
        };
}
