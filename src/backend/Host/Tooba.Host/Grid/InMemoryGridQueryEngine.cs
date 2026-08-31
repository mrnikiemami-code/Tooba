using System.Globalization;
using Tooba.BuildingBlocks.Grid;

namespace Tooba.Host.Grid;

/// <summary>اجرای paging/filter/sort/search روی فهرست in-memory با قرارداد GridQuery.</summary>
public static class InMemoryGridQueryEngine
{
    /// <summary>صفحه‌بندی فهرست flat را اعمال می‌کند.</summary>
    public static GridPageResponse<T> Execute<T>(
        IReadOnlyList<T> source,
        GridQueryRequest request,
        IReadOnlyDictionary<string, InMemoryGridField<T>> fields,
        string? tieBreakerField = null)
    {
        IEnumerable<T> rows = source;

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var q = request.Search.Trim().ToLowerInvariant();
            rows = rows.Where(row => fields.Values.Any(field =>
                field.Searchable && AsString(field.GetValue(row)).ToLowerInvariant().Contains(q, StringComparison.Ordinal)));
        }

        foreach (var filter in request.Filters ?? [])
        {
            if (!fields.TryGetValue(filter.Field, out var field))
            {
                continue;
            }

            rows = rows.Where(row => Matches(field.GetValue(row), field.Kind, filter));
        }

        if (request.AdvancedFilter?.Conditions is { Count: > 0 } conditions)
        {
            rows = rows.Where(row =>
            {
                var conditionResults = conditions
                    .Select(condition =>
                    {
                        if (!fields.TryGetValue(condition.Field, out var field))
                        {
                            return false;
                        }

                        return Matches(
                            field.GetValue(row),
                            field.Kind,
                            new GridFilterRequest(condition.Field, condition.Operator, condition.Value, condition.ValueTo, condition.Values));
                    })
                    .ToList();
                return EvaluateConnectors(conditionResults, request.AdvancedFilter.Connectors ?? []);
            });
        }

        var materialized = rows.ToList();
        var sorted = SortRows(materialized, request.Sort ?? [], fields, tieBreakerField);
        var total = sorted.Count;
        var start = (request.Page - 1) * request.PageSize;
        var pageItems = sorted.Skip(start).Take(request.PageSize).ToList();
        return new GridPageResponse<T>(pageItems, request.Page, request.PageSize, total);
    }

    private static List<T> SortRows<T>(
        List<T> rows,
        IReadOnlyList<GridSortRequest> sorts,
        IReadOnlyDictionary<string, InMemoryGridField<T>> fields,
        string? tieBreakerField)
    {
        if (sorts.Count == 0)
        {
            return rows;
        }

        IOrderedEnumerable<T>? ordered = null;
        foreach (var sort in sorts)
        {
            if (!fields.TryGetValue(sort.Field, out var field) || !field.Sortable)
            {
                continue;
            }

            ordered = ordered is null
                ? (sort.Direction == "asc"
                    ? rows.OrderBy(row => CompareKey(field.GetValue(row), field.Kind), Comparer<object?>.Default)
                    : rows.OrderByDescending(row => CompareKey(field.GetValue(row), field.Kind), Comparer<object?>.Default))
                : (sort.Direction == "asc"
                    ? ordered.ThenBy(row => CompareKey(field.GetValue(row), field.Kind), Comparer<object?>.Default)
                    : ordered.ThenByDescending(row => CompareKey(field.GetValue(row), field.Kind), Comparer<object?>.Default));
        }

        if (ordered is null)
        {
            return rows;
        }

        if (!string.IsNullOrWhiteSpace(tieBreakerField)
            && fields.TryGetValue(tieBreakerField, out var tieField)
            && sorts.All(s => !string.Equals(s.Field, tieBreakerField, StringComparison.Ordinal)))
        {
            ordered = ordered.ThenBy(row => CompareKey(tieField.GetValue(row), tieField.Kind), Comparer<object?>.Default);
        }

        return ordered.ToList();
    }

    private static object? CompareKey(object? value, InMemoryGridFieldKind kind) =>
        kind switch
        {
            InMemoryGridFieldKind.Number => AsDecimal(value),
            InMemoryGridFieldKind.Date => AsDate(value),
            _ => AsString(value).ToLowerInvariant(),
        };

    private static bool EvaluateConnectors(IReadOnlyList<bool> results, IReadOnlyList<string> connectors)
    {
        if (results.Count == 0)
        {
            return true;
        }

        var acc = results[0];
        for (var index = 1; index < results.Count; index++)
        {
            var connector = index - 1 < connectors.Count ? connectors[index - 1] : "and";
            acc = string.Equals(connector, "or", StringComparison.OrdinalIgnoreCase)
                ? acc || results[index]
                : acc && results[index];
        }

        return acc;
    }

    private static bool Matches(object? cell, InMemoryGridFieldKind kind, GridFilterRequest filter)
    {
        var op = (filter.Operator ?? string.Empty).Trim();
        return kind switch
        {
            InMemoryGridFieldKind.Text => MatchText(cell, op, filter.Value),
            InMemoryGridFieldKind.Number => MatchNumber(cell, op, filter.Value, filter.ValueTo),
            InMemoryGridFieldKind.Date => MatchDate(cell, op, filter.Value, filter.ValueTo),
            InMemoryGridFieldKind.Enum => MatchEnum(cell, op, filter.Value, filter.Values),
            _ => true,
        };
    }

    private static bool MatchText(object? cell, string op, string? value)
    {
        var hay = AsString(cell).ToLowerInvariant();
        return op switch
        {
            "blank" => string.IsNullOrWhiteSpace(hay),
            "notBlank" => !string.IsNullOrWhiteSpace(hay),
            "equals" => hay == (value ?? string.Empty).Trim().ToLowerInvariant(),
            "notEqual" => hay != (value ?? string.Empty).Trim().ToLowerInvariant(),
            "startsWith" => hay.StartsWith((value ?? string.Empty).Trim().ToLowerInvariant(), StringComparison.Ordinal),
            "endsWith" => hay.EndsWith((value ?? string.Empty).Trim().ToLowerInvariant(), StringComparison.Ordinal),
            "notContains" => !hay.Contains((value ?? string.Empty).Trim().ToLowerInvariant(), StringComparison.Ordinal),
            _ => hay.Contains((value ?? string.Empty).Trim().ToLowerInvariant(), StringComparison.Ordinal),
        };
    }

    private static bool MatchNumber(object? cell, string op, string? value, string? valueTo)
    {
        var n = AsDecimal(cell);
        return op switch
        {
            "blank" => cell is null,
            "notBlank" => cell is not null,
            "equals" => n == ParseDecimal(value),
            "notEqual" => n != ParseDecimal(value),
            "greaterThan" => n > ParseDecimal(value),
            "greaterThanOrEqual" => n >= ParseDecimal(value),
            "lessThan" => n < ParseDecimal(value),
            "lessThanOrEqual" => n <= ParseDecimal(value),
            "between" => n >= ParseDecimal(value) && n <= ParseDecimal(valueTo ?? value),
            _ => true,
        };
    }

    private static bool MatchDate(object? cell, string op, string? value, string? valueTo)
    {
        var iso = AsDate(cell);
        var target = ParseDate(value);
        var targetTo = ParseDate(valueTo ?? value);
        return op switch
        {
            "blank" => iso is null,
            "notBlank" => iso is not null,
            "on" => iso == target,
            "before" => iso is not null && target is not null && string.Compare(iso, target, StringComparison.Ordinal) < 0,
            "after" => iso is not null && target is not null && string.Compare(iso, target, StringComparison.Ordinal) > 0,
            "between" => iso is not null && target is not null && targetTo is not null
                && string.Compare(iso, target, StringComparison.Ordinal) >= 0
                && string.Compare(iso, targetTo, StringComparison.Ordinal) <= 0,
            _ => true,
        };
    }

    private static bool MatchEnum(object? cell, string op, string? value, IReadOnlyList<string>? values)
    {
        var hay = AsString(cell);
        var set = values?.Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase)
            ?? [];
        if (set.Count == 0 && !string.IsNullOrWhiteSpace(value))
        {
            set.Add(value.Trim());
        }

        return op switch
        {
            "blank" => string.IsNullOrWhiteSpace(hay),
            "notBlank" => !string.IsNullOrWhiteSpace(hay),
            "equals" => string.Equals(hay, value, StringComparison.OrdinalIgnoreCase),
            "notEqual" => !string.Equals(hay, value, StringComparison.OrdinalIgnoreCase),
            "in" => set.Contains(hay),
            "notIn" => !set.Contains(hay),
            _ => set.Count == 0 || set.Contains(hay),
        };
    }

    private static string AsString(object? value) =>
        value switch
        {
            null => string.Empty,
            DateTimeOffset dto => dto.ToString("O", CultureInfo.InvariantCulture),
            DateTime dt => dt.ToString("O", CultureInfo.InvariantCulture),
            Enum e => e.ToString() ?? string.Empty,
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty,
        };

    private static decimal AsDecimal(object? value)
    {
        if (value is decimal d)
        {
            return d;
        }

        if (value is int i)
        {
            return i;
        }

        if (value is long l)
        {
            return l;
        }

        return decimal.TryParse(AsString(value), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0m;
    }

    private static decimal ParseDecimal(string? value) =>
        decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0m;

    private static string? AsDate(object? value)
    {
        if (value is DateTimeOffset dto)
        {
            return dto.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        if (value is DateTime dt)
        {
            return dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        var text = AsString(value);
        if (DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
        {
            return parsed.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        return string.IsNullOrWhiteSpace(text) ? null : text.Length >= 10 ? text[..10] : text;
    }

    private static string? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim().Length >= 10 ? value.Trim()[..10] : value.Trim();
    }
}
