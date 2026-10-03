using Tooba.BuildingBlocks;

namespace Tooba.Catalog.Domain.Aggregates;

/// <summary>
/// پیکربندی نمایش فیلتر PLP برای یک ویژگی در یک رده.
/// </summary>
public sealed class CatalogCategoryFacetConfiguration
{
    /// <summary>
    /// شناسهٔ پیکربندی facet.
    /// </summary>
    public Guid FacetConfigurationId { get; init; }

    /// <summary>
    /// ردهٔ مالک پیکربندی.
    /// </summary>
    public Guid CategoryId { get; init; }

    /// <summary>
    /// تعریف ویژگیٔ فیلتر.
    /// </summary>
    public Guid DefinitionId { get; init; }

    /// <summary>
    /// نوع نمایش فیلتر در PLP.
    /// </summary>
    public CatalogFacetDisplayType DisplayType { get; set; }

    /// <summary>
    /// ترتیب نمایش در بین facetهای محلی این رده.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// آیا فیلتر در PLP نمایش داده شود.
    /// </summary>
    public bool IsVisible { get; set; }

    /// <summary>
    /// آیا گزینه‌ها قابل جستجو باشند.
    /// </summary>
    public bool IsSearchable { get; set; }

    /// <summary>
    /// آیا فیلتر پیش‌فرض بسته باشد.
    /// </summary>
    public bool IsCollapsedByDefault { get; set; }

    /// <summary>
    /// آیا تعداد محصول کنار گزینه نمایش داده شود.
    /// </summary>
    public bool ShowCounts { get; set; }

    /// <summary>
    /// زمان ایجاد.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// ایجاد پیکربندی facet برای یک ویژگی در رده.
    /// </summary>
    public static CatalogCategoryFacetConfiguration Create(
        Guid categoryId,
        Guid definitionId,
        CatalogFacetDisplayType displayType,
        int sortOrder,
        bool isVisible,
        bool isSearchable,
        bool isCollapsedByDefault,
        bool showCounts,
        DateTimeOffset now) =>
        new()
        {
            FacetConfigurationId = UuidV7.New(),
            CategoryId = categoryId,
            DefinitionId = definitionId,
            DisplayType = displayType,
            SortOrder = sortOrder,
            IsVisible = isVisible,
            IsSearchable = isSearchable,
            IsCollapsedByDefault = isCollapsedByDefault,
            ShowCounts = showCounts,
            CreatedAt = now,
        };
}
