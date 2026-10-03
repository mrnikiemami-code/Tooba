using Tooba.BuildingBlocks;

namespace Tooba.Catalog.Domain.Enums;

/// <summary>نتیجهٔ اعتبارسنجی placement مگامنو (بدون exception برای expected failure).</summary>
public enum CatalogMegaMenuPlacementViolation
{
    /// <summary>معتبر.</summary>
    None = 0,

    /// <summary>آیتم نمی‌تواند والد خودش باشد.</summary>
    SelfParent = 1,

    /// <summary>والد presentation یافت نشد.</summary>
    ParentMissing = 2,

    /// <summary>حلقه در درخت presentation.</summary>
    Cycle = 3,

    /// <summary>عمق از حداکثر مجاز بیشتر است.</summary>
    MaxDepthExceeded = 4,
}
