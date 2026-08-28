namespace Tooba.BuildingBlocks.Grid;

/// <summary>
/// قرارداد پرس‌وجوی گرید سمت سرور. AG Grid یا هر UI دیگر نباید مستقیماً به Host برسد.
/// </summary>
/// <summary>مرتب‌سازی whitelist‌شده.</summary>
public sealed record GridSortRequest(string Field, string Direction);

/// <summary>فیلتر تایپ‌شده با فیلد whitelist‌شده.</summary>
public sealed record GridFilterRequest(
    string Field,
    string Operator,
    string? Value,
    string? ValueTo,
    IReadOnlyList<string>? Values);

/// <summary>یک شرط فیلتر پیشرفته — application-owned، نه AG Grid.</summary>
public sealed record GridAdvancedFilterCondition(
    string Id,
    string Field,
    string Operator,
    string? Value,
    string? ValueTo,
    IReadOnlyList<string>? Values);

/// <summary>عبارت فیلتر پیشرفته با AND/OR صریح — left-to-right ارزیابی می‌شود.</summary>
public sealed record GridAdvancedFilterExpression(
    IReadOnlyList<GridAdvancedFilterCondition> Conditions,
    IReadOnlyList<string> Connectors);

/// <summary>درخواست صفحه‌بندی/مرتب‌سازی/فیلتر گرید.</summary>
public sealed record GridQueryRequest(
    int Page,
    int PageSize,
    string? Search,
    IReadOnlyList<GridSortRequest> Sort,
    IReadOnlyList<GridFilterRequest> Filters,
    GridAdvancedFilterExpression? AdvancedFilter = null);

/// <summary>پاسخ صفحه‌بندی‌شدهٔ گرید.</summary>
public sealed record GridPageResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount);
