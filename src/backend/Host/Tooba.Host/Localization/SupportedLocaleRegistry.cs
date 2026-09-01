namespace Tooba.Host.Localization;

/// <summary>سیاست نمایش تقویم — فقط UI؛ ذخیره UTC/NodaTime تغییر نمی‌کند.</summary>
public enum CalendarDisplayPolicy
{
    /// <summary>نمایش جلالی برای فارسی.</summary>
    Jalali,
    /// <summary>نمایش میلادی برای انگلیسی.</summary>
    Gregorian,
}

/// <summary>تعریف زبان/محلیه پشتیبانی‌شده برای Content و ویترین.</summary>
public sealed record SupportedLocaleDefinition(
    string Code,
    string UrlPrefix,
    string DisplayName,
    string NativeName,
    string Direction,
    string Culture,
    CalendarDisplayPolicy CalendarDisplay,
    bool Active,
    bool IsDefault,
    int SortOrder);

/// <summary>به‌روزرسانی امن زبان — کد تغییر نمی‌کند.</summary>
public sealed record SupportedLocalePatch(bool? Active, bool? IsDefault, int? SortOrder);

/// <summary>رجیستری کانونی زبان‌ها — پایهٔ config با overlay درون‌حافظه برای Admin.</summary>
public sealed class SupportedLocaleRegistry
{
    private static readonly SupportedLocaleDefinition[] BaseDefinitions =
    [
        new(
            "fa-IR",
            "fa",
            "Persian (Iran)",
            "فارسی (ایران)",
            "rtl",
            "fa-IR",
            CalendarDisplayPolicy.Jalali,
            Active: true,
            IsDefault: true,
            SortOrder: 0),
        new(
            "en-US",
            "en",
            "English (United States)",
            "English (US)",
            "ltr",
            "en-US",
            CalendarDisplayPolicy.Gregorian,
            Active: true,
            IsDefault: false,
            SortOrder: 1),
    ];

    private readonly Dictionary<string, SupportedLocalePatch> _overrides = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _gate = new();

    /// <summary>همهٔ زبان‌های پیکربندی‌شده.</summary>
    public IReadOnlyList<SupportedLocaleDefinition> List()
    {
        lock (_gate)
        {
            return BaseDefinitions
                .Select(Merge)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Code, StringComparer.Ordinal)
                .ToList();
        }
    }

    /// <summary>زبان فعال پیش‌فرض.</summary>
    public SupportedLocaleDefinition GetDefault()
    {
        var locales = List();
        return locales.FirstOrDefault(x => x is { IsDefault: true, Active: true }) ?? locales[0];
    }

    /// <summary>به‌روزرسانی active/default/sort با قواعد ایمن.</summary>
    public SupportedLocaleDefinition Patch(string code, SupportedLocalePatch patch)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new InvalidOperationException("locale.code_required");
        }

        lock (_gate)
        {
            var baseRow = BaseDefinitions.FirstOrDefault(x =>
                string.Equals(x.Code, code.Trim(), StringComparison.OrdinalIgnoreCase));
            if (baseRow is null)
            {
                throw new InvalidOperationException("locale.not_found");
            }

            var current = Merge(baseRow);
            var nextActive = patch.Active ?? current.Active;
            var nextDefault = patch.IsDefault ?? current.IsDefault;
            if (!nextActive && nextDefault)
            {
                throw new InvalidOperationException("locale.default_must_be_active");
            }

            if (patch.IsDefault == true)
            {
                foreach (var row in BaseDefinitions)
                {
                    if (!string.Equals(row.Code, baseRow.Code, StringComparison.OrdinalIgnoreCase))
                    {
                        var existing = _overrides.GetValueOrDefault(row.Code) ?? new SupportedLocalePatch(null, null, null);
                        _overrides[row.Code] = existing with { IsDefault = false };
                    }
                }
            }

            _overrides[baseRow.Code] = new SupportedLocalePatch(
                patch.Active ?? _overrides.GetValueOrDefault(baseRow.Code)?.Active,
                patch.IsDefault ?? _overrides.GetValueOrDefault(baseRow.Code)?.IsDefault,
                patch.SortOrder ?? _overrides.GetValueOrDefault(baseRow.Code)?.SortOrder);

            var merged = List();
            if (merged.Count(x => x.Active) == 0)
            {
                throw new InvalidOperationException("locale.at_least_one_active");
            }

            if (merged.Count(x => x is { Active: true, IsDefault: true }) != 1)
            {
                throw new InvalidOperationException("locale.exactly_one_default");
            }

            return merged.First(x => string.Equals(x.Code, baseRow.Code, StringComparison.OrdinalIgnoreCase));
        }
    }

    private SupportedLocaleDefinition Merge(SupportedLocaleDefinition baseRow)
    {
        if (!_overrides.TryGetValue(baseRow.Code, out var patch))
        {
            return baseRow;
        }

        return baseRow with
        {
            Active = patch.Active ?? baseRow.Active,
            IsDefault = patch.IsDefault ?? baseRow.IsDefault,
            SortOrder = patch.SortOrder ?? baseRow.SortOrder,
        };
    }
}
