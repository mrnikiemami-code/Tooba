namespace Tooba.AccessControl.Contracts.Enums;

/// <summary>انواع scope منبع تایپ‌شده؛ بدون expression آزاد.</summary>
public enum AccessScopeKind
{
    /// <summary>کل محدودهٔ مالک بدون محدودیت منبع.</summary>
    GlobalWithinOwner = 1,

    /// <summary>محدود به دسته.</summary>
    Category = 2,

    /// <summary>محدود به محصول.</summary>
    Product = 3,

    /// <summary>محدود به برند.</summary>
    Brand = 4,

    /// <summary>محدود به انبار.</summary>
    Warehouse = 5,

    /// <summary>محدود به فروشگاه.</summary>
    Store = 6,

    /// <summary>محدود به قطعهٔ سفارش.</summary>
    OrderSegment = 7,
}
