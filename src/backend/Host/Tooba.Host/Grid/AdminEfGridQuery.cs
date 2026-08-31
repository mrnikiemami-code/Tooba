using System.Globalization;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks.Grid;

namespace Tooba.Host.Grid;

/// <summary>
/// کمک‌های مشترک فیلتر/مرتب‌سازی اسکالر روی IQueryable برای گریدهای Admin غیرساده.
/// فقط عملگرهای قابل‌ترجمه EF؛ بدون ToList قبل از Skip/Take.
/// </summary>
internal static class AdminEfGridQuery
{
    public static IQueryable<T> ApplyTextFilter<T>(
        IQueryable<T> source,
        Expression<Func<T, string?>> selector,
        GridFilterRequest filter)
    {
        var op = (filter.Operator ?? string.Empty).Trim();
        var value = (filter.Value ?? string.Empty).Trim();
        return op switch
        {
            "blank" => source.Where(BuildNullOrWhiteSpace(selector, isBlank: true)),
            "notBlank" => source.Where(BuildNullOrWhiteSpace(selector, isBlank: false)),
            "equals" => source.Where(BuildStringEquals(selector, value)),
            "notEqual" => source.Where(BuildStringNotEquals(selector, value)),
            "startsWith" => source.Where(BuildStringStartsWith(selector, value)),
            "endsWith" => source.Where(BuildStringEndsWith(selector, value)),
            "notContains" => source.Where(BuildStringNotContains(selector, value)),
            _ => source.Where(BuildStringContains(selector, value)),
        };
    }

    public static IQueryable<T> ApplySearchAny<T>(
        IQueryable<T> source,
        string search,
        params Expression<Func<T, string?>>[] selectors)
    {
        if (string.IsNullOrWhiteSpace(search) || selectors.Length == 0)
        {
            return source;
        }

        var term = search.Trim().ToLower();
        Expression? body = null;
        var parameter = Expression.Parameter(typeof(T), "row");
        foreach (var selector in selectors)
        {
            var replaced = new ParameterReplaceVisitor(selector.Parameters[0], parameter).Visit(selector.Body)!;
            var coalesced = Expression.Coalesce(replaced, Expression.Constant(string.Empty));
            var lower = Expression.Call(coalesced, nameof(string.ToLower), Type.EmptyTypes);
            var contains = Expression.Call(lower, nameof(string.Contains), Type.EmptyTypes, Expression.Constant(term));
            body = body is null ? contains : Expression.OrElse(body, contains);
        }

        var lambda = Expression.Lambda<Func<T, bool>>(body!, parameter);
        return source.Where(lambda);
    }

    public static IQueryable<T> ApplyEnumFilter<T, TEnum>(
        IQueryable<T> source,
        Expression<Func<T, TEnum>> selector,
        GridFilterRequest filter)
        where TEnum : struct, Enum
    {
        var op = (filter.Operator ?? string.Empty).Trim();
        var values = (filter.Values ?? [])
            .Concat(string.IsNullOrWhiteSpace(filter.Value) ? [] : [filter.Value!])
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (op is "blank")
        {
            return source;
        }

        if (op is "notBlank")
        {
            return source;
        }

        var parsed = values
            .Select(v => Enum.TryParse<TEnum>(v, ignoreCase: true, out var e) ? (TEnum?)e : null)
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .ToList();

        if (parsed.Count == 0)
        {
            return source.Where(_ => false);
        }

        return op switch
        {
            "notEqual" or "notIn" => source.Where(BuildEnumNotIn(selector, parsed)),
            _ => source.Where(BuildEnumIn(selector, parsed)),
        };
    }

    public static IQueryable<T> ApplyNumberFilter<T>(
        IQueryable<T> source,
        Expression<Func<T, decimal>> selector,
        GridFilterRequest filter)
    {
        var op = (filter.Operator ?? string.Empty).Trim();
        if (op is "blank")
        {
            return source.Where(_ => false);
        }

        if (op is "notBlank")
        {
            return source;
        }

        if (!decimal.TryParse(filter.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var n))
        {
            return source.Where(_ => false);
        }

        decimal? nTo = decimal.TryParse(filter.ValueTo, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;

        return op switch
        {
            "equals" => source.Where(BuildDecimalCompare(selector, n, ExpressionType.Equal)),
            "notEqual" => source.Where(BuildDecimalCompare(selector, n, ExpressionType.NotEqual)),
            "greaterThan" => source.Where(BuildDecimalCompare(selector, n, ExpressionType.GreaterThan)),
            "greaterThanOrEqual" => source.Where(BuildDecimalCompare(selector, n, ExpressionType.GreaterThanOrEqual)),
            "lessThan" => source.Where(BuildDecimalCompare(selector, n, ExpressionType.LessThan)),
            "lessThanOrEqual" => source.Where(BuildDecimalCompare(selector, n, ExpressionType.LessThanOrEqual)),
            "between" => source.Where(BuildDecimalBetween(selector, n, nTo ?? n)),
            _ => source,
        };
    }

    public static IQueryable<T> ApplyIntFilter<T>(
        IQueryable<T> source,
        Expression<Func<T, int>> selector,
        GridFilterRequest filter)
    {
        var asDecimal = Expression.Lambda<Func<T, decimal>>(
            Expression.Convert(selector.Body, typeof(decimal)),
            selector.Parameters);
        return ApplyNumberFilter(source, asDecimal, filter);
    }

    public static IQueryable<T> ApplyDateFilter<T>(
        IQueryable<T> source,
        Expression<Func<T, DateTimeOffset>> selector,
        GridFilterRequest filter)
    {
        var op = (filter.Operator ?? string.Empty).Trim();
        if (op is "blank")
        {
            return source.Where(_ => false);
        }

        if (op is "notBlank")
        {
            return source;
        }

        if (!TryParseDate(filter.Value, out var from))
        {
            return source.Where(_ => false);
        }

        var to = TryParseDate(filter.ValueTo ?? filter.Value, out var parsedTo) ? parsedTo : from;
        var fromDto = new DateTimeOffset(from.Year, from.Month, from.Day, 0, 0, 0, TimeSpan.Zero);
        var toExclusive = new DateTimeOffset(to.Year, to.Month, to.Day, 0, 0, 0, TimeSpan.Zero).AddDays(1);

        return op switch
        {
            "on" => source.Where(BuildDateOn(selector, fromDto, toExclusive)),
            "before" => source.Where(BuildDateBefore(selector, fromDto)),
            "after" => source.Where(BuildDateAfter(selector, toExclusive.AddDays(-1))),
            "between" => source.Where(BuildDateBetween(selector, fromDto, toExclusive)),
            _ => source,
        };
    }

    public static async Task<GridPageResponse<TItem>> PageAsync<TRow, TItem>(
        IQueryable<TRow> filtered,
        GridQueryRequest request,
        Func<IQueryable<TRow>, IQueryable<TRow>> order,
        Func<List<TRow>, CancellationToken, Task<IReadOnlyList<TItem>>> materializePage,
        CancellationToken cancellationToken)
    {
        var total = await filtered.CountAsync(cancellationToken);
        if (total == 0)
        {
            return new GridPageResponse<TItem>([], request.Page, request.PageSize, 0);
        }

        var pageRows = await order(filtered)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);
        var items = await materializePage(pageRows, cancellationToken);
        return new GridPageResponse<TItem>(items, request.Page, request.PageSize, total);
    }

    private static bool TryParseDate(string? value, out DateTime date)
    {
        date = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var text = value.Trim();
        if (text.Length >= 10 && DateTime.TryParse(text[..10], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out date))
        {
            return true;
        }

        return DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dto)
            && (date = dto.UtcDateTime) == date;
    }

    private static Expression<Func<T, bool>> BuildNullOrWhiteSpace<T>(Expression<Func<T, string?>> selector, bool isBlank)
    {
        var parameter = selector.Parameters[0];
        var body = selector.Body;
        var isNull = Expression.Equal(body, Expression.Constant(null, typeof(string)));
        var trim = Expression.Condition(
            isNull,
            Expression.Constant(string.Empty),
            Expression.Call(body, nameof(string.Trim), Type.EmptyTypes));
        var isEmpty = Expression.Equal(trim, Expression.Constant(string.Empty));
        var predicate = isBlank ? Expression.OrElse(isNull, isEmpty) : Expression.AndAlso(Expression.Not(isNull), Expression.Not(isEmpty));
        return Expression.Lambda<Func<T, bool>>(predicate, parameter);
    }

    private static Expression<Func<T, bool>> BuildStringContains<T>(Expression<Func<T, string?>> selector, string value)
    {
        var parameter = selector.Parameters[0];
        var coalesced = Expression.Coalesce(selector.Body, Expression.Constant(string.Empty));
        var lower = Expression.Call(coalesced, nameof(string.ToLower), Type.EmptyTypes);
        var call = Expression.Call(lower, nameof(string.Contains), Type.EmptyTypes, Expression.Constant(value.ToLowerInvariant()));
        return Expression.Lambda<Func<T, bool>>(call, parameter);
    }

    private static Expression<Func<T, bool>> BuildStringNotContains<T>(Expression<Func<T, string?>> selector, string value)
    {
        var contains = BuildStringContains(selector, value);
        return Expression.Lambda<Func<T, bool>>(Expression.Not(contains.Body), contains.Parameters);
    }

    private static Expression<Func<T, bool>> BuildStringEquals<T>(Expression<Func<T, string?>> selector, string value)
    {
        var parameter = selector.Parameters[0];
        var coalesced = Expression.Coalesce(selector.Body, Expression.Constant(string.Empty));
        var lower = Expression.Call(coalesced, nameof(string.ToLower), Type.EmptyTypes);
        var eq = Expression.Equal(lower, Expression.Constant(value.ToLowerInvariant()));
        return Expression.Lambda<Func<T, bool>>(eq, parameter);
    }

    private static Expression<Func<T, bool>> BuildStringNotEquals<T>(Expression<Func<T, string?>> selector, string value)
    {
        var eq = BuildStringEquals(selector, value);
        return Expression.Lambda<Func<T, bool>>(Expression.Not(eq.Body), eq.Parameters);
    }

    private static Expression<Func<T, bool>> BuildStringStartsWith<T>(Expression<Func<T, string?>> selector, string value)
    {
        var parameter = selector.Parameters[0];
        var coalesced = Expression.Coalesce(selector.Body, Expression.Constant(string.Empty));
        var lower = Expression.Call(coalesced, nameof(string.ToLower), Type.EmptyTypes);
        var call = Expression.Call(lower, nameof(string.StartsWith), Type.EmptyTypes, Expression.Constant(value.ToLowerInvariant()));
        return Expression.Lambda<Func<T, bool>>(call, parameter);
    }

    private static Expression<Func<T, bool>> BuildStringEndsWith<T>(Expression<Func<T, string?>> selector, string value)
    {
        var parameter = selector.Parameters[0];
        var coalesced = Expression.Coalesce(selector.Body, Expression.Constant(string.Empty));
        var lower = Expression.Call(coalesced, nameof(string.ToLower), Type.EmptyTypes);
        var call = Expression.Call(lower, nameof(string.EndsWith), Type.EmptyTypes, Expression.Constant(value.ToLowerInvariant()));
        return Expression.Lambda<Func<T, bool>>(call, parameter);
    }

    private static Expression<Func<T, bool>> BuildEnumIn<T, TEnum>(Expression<Func<T, TEnum>> selector, List<TEnum> values)
        where TEnum : struct, Enum
    {
        var parameter = selector.Parameters[0];
        var constant = Expression.Constant(values);
        var contains = Expression.Call(
            typeof(Enumerable),
            nameof(Enumerable.Contains),
            [typeof(TEnum)],
            constant,
            selector.Body);
        return Expression.Lambda<Func<T, bool>>(contains, parameter);
    }

    private static Expression<Func<T, bool>> BuildEnumNotIn<T, TEnum>(Expression<Func<T, TEnum>> selector, List<TEnum> values)
        where TEnum : struct, Enum
    {
        var included = BuildEnumIn(selector, values);
        return Expression.Lambda<Func<T, bool>>(Expression.Not(included.Body), included.Parameters);
    }

    private static Expression<Func<T, bool>> BuildDecimalCompare<T>(
        Expression<Func<T, decimal>> selector,
        decimal value,
        ExpressionType type)
    {
        var parameter = selector.Parameters[0];
        var compare = Expression.MakeBinary(type, selector.Body, Expression.Constant(value));
        return Expression.Lambda<Func<T, bool>>(compare, parameter);
    }

    private static Expression<Func<T, bool>> BuildDecimalBetween<T>(
        Expression<Func<T, decimal>> selector,
        decimal from,
        decimal to)
    {
        var parameter = selector.Parameters[0];
        var ge = Expression.GreaterThanOrEqual(selector.Body, Expression.Constant(from));
        var le = Expression.LessThanOrEqual(selector.Body, Expression.Constant(to));
        return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(ge, le), parameter);
    }

    private static Expression<Func<T, bool>> BuildDateOn<T>(
        Expression<Func<T, DateTimeOffset>> selector,
        DateTimeOffset from,
        DateTimeOffset toExclusive)
    {
        var parameter = selector.Parameters[0];
        var ge = Expression.GreaterThanOrEqual(selector.Body, Expression.Constant(from));
        var lt = Expression.LessThan(selector.Body, Expression.Constant(toExclusive));
        return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(ge, lt), parameter);
    }

    private static Expression<Func<T, bool>> BuildDateBefore<T>(
        Expression<Func<T, DateTimeOffset>> selector,
        DateTimeOffset boundary)
    {
        var parameter = selector.Parameters[0];
        var lt = Expression.LessThan(selector.Body, Expression.Constant(boundary));
        return Expression.Lambda<Func<T, bool>>(lt, parameter);
    }

    private static Expression<Func<T, bool>> BuildDateAfter<T>(
        Expression<Func<T, DateTimeOffset>> selector,
        DateTimeOffset boundaryInclusiveDay)
    {
        var parameter = selector.Parameters[0];
        var gt = Expression.GreaterThanOrEqual(selector.Body, Expression.Constant(boundaryInclusiveDay.AddDays(1)));
        return Expression.Lambda<Func<T, bool>>(gt, parameter);
    }

    private static Expression<Func<T, bool>> BuildDateBetween<T>(
        Expression<Func<T, DateTimeOffset>> selector,
        DateTimeOffset from,
        DateTimeOffset toExclusive)
    {
        return BuildDateOn(selector, from, toExclusive);
    }

    private sealed class ParameterReplaceVisitor : ExpressionVisitor
    {
        private readonly ParameterExpression _from;
        private readonly ParameterExpression _to;

        public ParameterReplaceVisitor(ParameterExpression from, ParameterExpression to)
        {
            _from = from;
            _to = to;
        }

        protected override Expression VisitParameter(ParameterExpression node) =>
            node == _from ? _to : base.VisitParameter(node);
    }
}
