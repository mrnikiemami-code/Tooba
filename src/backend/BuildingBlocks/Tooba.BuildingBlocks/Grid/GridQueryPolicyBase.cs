namespace Tooba.BuildingBlocks.Grid;

/// <summary>مجموعهٔ عملگرهای استاندارد GridQuery — برای reuse در policyهای ماژول.</summary>
public static class GridQueryOperators
{
    /// <summary>عملگرهای متنی مجاز.</summary>
    public static readonly HashSet<string> Text =
    [
        "contains", "equals", "startsWith", "notContains", "notEqual", "endsWith", "blank", "notBlank",
    ];

    /// <summary>عملگرهای عددی مجاز.</summary>
    public static readonly HashSet<string> Number =
    [
        "equals", "notEqual", "greaterThan", "greaterThanOrEqual", "lessThan", "lessThanOrEqual", "between", "blank", "notBlank",
    ];

    /// <summary>عملگرهای تاریخ مجاز.</summary>
    public static readonly HashSet<string> Date =
    [
        "on", "before", "after", "between", "blank", "notBlank",
    ];

    /// <summary>عملگرهای enum/set مجاز.</summary>
    public static readonly HashSet<string> Enum =
    [
        "equals", "notEqual", "in", "notIn", "blank", "notBlank",
    ];
}

/// <summary>
/// اعتبارسنجی ساختاری مشترک GridQuery — بدون whitelist فیلد.
/// ماژول‌ها پس از این مرحله فیلتر/مرتب‌سازی دامنه‌ای خود را اعمال می‌کنند.
/// </summary>
public static class GridQueryPolicyBase
{
    /// <summary>حداکثر pageSize پیش‌فرض canonical.</summary>
    public const int DefaultMaxPageSize = 1000;

    /// <summary>pageSize پیش‌فرض canonical.</summary>
    public const int DefaultDefaultPageSize = 20;

    /// <summary>حداکثر طول search.</summary>
    public const int DefaultMaxSearchLength = 200;

    /// <summary>حداکثر تعداد sort همزمان.</summary>
    public const int DefaultMaxSortCount = 3;

    /// <summary>page و pageSize را clamp می‌کند.</summary>
    public static (int Page, int PageSize) NormalizePaging(
        int page,
        int pageSize,
        int maxPageSize = DefaultMaxPageSize,
        int defaultPageSize = DefaultDefaultPageSize)
    {
        var normalizedPage = page < 1 ? 1 : page;
        var normalizedPageSize = pageSize < 1 ? defaultPageSize : Math.Min(pageSize, maxPageSize);
        return (normalizedPage, normalizedPageSize);
    }

    /// <summary>search را trim و محدود به maxLength می‌کند.</summary>
    public static string? NormalizeSearch(string? search, int maxLength = DefaultMaxSearchLength)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return null;
        }

        var trimmed = search.Trim();
        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }

    /// <summary>یک GridFilterRequest را normalize می‌کند.</summary>
    public static GridFilterRequest NormalizeFilter(GridFilterRequest filter) =>
        new(
            filter.Field,
            (filter.Operator ?? string.Empty).Trim(),
            NormalizeScalar(filter.Value),
            NormalizeScalar(filter.ValueTo),
            filter.Values?
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v.Trim())
                .Distinct(StringComparer.Ordinal)
                .Take(20)
                .ToList());

    /// <summary>مقدار scalar فیلتر را trim می‌کند.</summary>
    public static string? NormalizeScalar(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>تعداد connectorهای advanced filter را validate می‌کند.</summary>
    public static void ValidateAdvancedConnectors(int conditionCount, IReadOnlyList<string>? connectors)
    {
        var expectedConnectors = Math.Max(conditionCount - 1, 0);
        var rawConnectors = connectors ?? [];
        if (rawConnectors.Count != expectedConnectors)
        {
            throw GridQueryValidationException.ConnectorCount();
        }

        foreach (var connector in rawConnectors)
        {
            var normalized = connector.ToLowerInvariant();
            if (normalized is not ("and" or "or"))
            {
                throw GridQueryValidationException.ConnectorInvalid();
            }
        }
    }
}

/// <summary>خطای اعتبارسنجی GridQuery — در Host به PlatformHttpException map می‌شود.</summary>
public sealed class GridQueryValidationException : Exception
{
    /// <summary>کد HTTP پیشنهادی.</summary>
    public int StatusCode { get; }

    /// <summary>کد خطای application-owned.</summary>
    public string ErrorCode { get; }

    private GridQueryValidationException(int statusCode, string message, string errorCode)
        : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }

    /// <summary>connector count mismatch.</summary>
    public static GridQueryValidationException ConnectorCount() =>
        new(400, "تعداد اتصال‌دهندهٔ فیلتر پیشرفته نامعتبر است.", "grid.advancedFilter.connector.count");

    /// <summary>connector نامعتبر.</summary>
    public static GridQueryValidationException ConnectorInvalid() =>
        new(400, "اتصال‌دهندهٔ فیلتر پیشرفته مجاز نیست.", "grid.advancedFilter.connector.invalid");

    /// <summary>فیلد فیلتر نامعتبر.</summary>
    public static GridQueryValidationException FilterFieldInvalid() =>
        new(400, "فیلد فیلتر مجاز نیست.", "grid.filter.field.invalid");

    /// <summary>عملگر فیلتر نامعتبر.</summary>
    public static GridQueryValidationException FilterOperatorInvalid() =>
        new(400, "عملگر فیلتر مجاز نیست.", "grid.filter.operator.invalid");

    /// <summary>فیلد advanced filter نامعتبر.</summary>
    public static GridQueryValidationException AdvancedFieldInvalid() =>
        new(400, "فیلد فیلتر پیشرفته مجاز نیست.", "grid.advancedFilter.field.invalid");
}
