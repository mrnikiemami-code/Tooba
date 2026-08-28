namespace Tooba.Host.Grid;

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

/// <summary>
/// درخواست صفحه‌بندی/مرتب‌سازی/فیلتر گرید.
/// </summary>
public sealed record GridQueryRequest(
    int Page,
    int PageSize,
    string? Search,
    IReadOnlyList<GridSortRequest> Sort,
    IReadOnlyList<GridFilterRequest> Filters);

/// <summary>
/// پاسخ صفحه‌بندی‌شدهٔ گرید.
/// </summary>
public sealed record GridPageResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount);
