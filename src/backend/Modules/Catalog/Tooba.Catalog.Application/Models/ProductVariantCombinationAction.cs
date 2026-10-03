using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Models;

/// <summary>عمل پیش‌نمایش برای یک ترکیب.</summary>
public enum ProductVariantCombinationAction
{
    /// <summary>ترکیب موجود بدون تغییر.</summary>
    Unchanged = 0,

    /// <summary>ترکیب جدید باید ساخته شود.</summary>
    New = 1,

    /// <summary>ترکیب دیگر انتخاب نشده و باید غیرفعال شود.</summary>
    Deactivate = 2,
}
