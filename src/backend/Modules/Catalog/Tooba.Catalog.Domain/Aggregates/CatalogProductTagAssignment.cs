using Tooba.BuildingBlocks;

namespace Tooba.Catalog.Domain.Aggregates;

/// <summary>
/// پیوند چندبه‌چند محصول ↔ برچسب. ذخیرهٔ comma-separated نیست.
/// </summary>
public sealed class CatalogProductTagAssignment
{
    /// <summary>شناسهٔ پیوند.</summary>
    public Guid AssignmentId { get; init; }

    /// <summary>محصول Catalog.</summary>
    public Guid ProductId { get; init; }

    /// <summary>برچسب Catalog.</summary>
    public Guid TagId { get; init; }

    /// <summary>پیوند محصول-برچسب می‌سازد.</summary>
    public static CatalogProductTagAssignment Assign(Guid productId, Guid tagId) =>
        new()
        {
            AssignmentId = UuidV7.New(),
            ProductId = productId,
            TagId = tagId,
        };
}
