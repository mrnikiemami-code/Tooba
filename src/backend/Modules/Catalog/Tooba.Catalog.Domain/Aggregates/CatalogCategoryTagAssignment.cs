using Tooba.BuildingBlocks;

namespace Tooba.Catalog.Domain.Aggregates;

/// <summary>
/// پیوند چندبه‌چند رده ↔ برچسب. ذخیرهٔ comma-separated نیست.
/// </summary>
public sealed class CatalogCategoryTagAssignment
{
    /// <summary>شناسهٔ پیوند.</summary>
    public Guid AssignmentId { get; init; }

    /// <summary>ردهٔ Catalog.</summary>
    public Guid CategoryId { get; init; }

    /// <summary>برچسب Catalog.</summary>
    public Guid TagId { get; init; }

    /// <summary>پیوند رده-برچسب می‌سازد.</summary>
    public static CatalogCategoryTagAssignment Assign(Guid categoryId, Guid tagId) =>
        new()
        {
            AssignmentId = UuidV7.New(),
            CategoryId = categoryId,
            TagId = tagId,
        };
}
