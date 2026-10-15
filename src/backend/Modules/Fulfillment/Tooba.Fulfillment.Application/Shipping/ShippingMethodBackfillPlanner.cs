using Tooba.Fulfillment.Contracts.Shipping;

namespace Tooba.Fulfillment.Application.Shipping;

/// <summary>کد روش موجود و پیوند قدیمی آن؛ ورودی برنامه‌ریزی backfill.</summary>
public sealed record ShippingMethodExistingRow(
    Guid ShippingMethodId,
    string MethodCode,
    string LegacyServiceCode,
    string? LegacyOptionCode,
    bool IsActive);

/// <summary>یک روش مطلوب که از کاتالوگ دوسطحی موجود استخراج شده است.</summary>
public sealed record ShippingMethodDesiredRow(
    string MethodCode,
    string ProviderKind,
    string LegacyServiceCode,
    string? LegacyOptionCode,
    int SortOrder,
    bool IsActive,
    IReadOnlyList<Guid> LanguageIds,
    string DisplayName,
    string? Description);

/// <summary>تصمیم backfill برای یک روش مطلوب.</summary>
public enum ShippingMethodPlanAction
{
    /// <summary>روش موجود نیست و باید ساخته شود.</summary>
    Create = 0,

    /// <summary>روش با همان کد و همان پیوند موجود است؛ هیچ نوشتنی لازم نیست (idempotent).</summary>
    Skip = 1,

    /// <summary>روش موجود است اما فعال/غیرفعال بودن آن باید هم‌راستا شود.</summary>
    AlignActivation = 2,
}

/// <summary>یک قلم برنامهٔ backfill.</summary>
public sealed record ShippingMethodPlanItem(
    ShippingMethodPlanAction Action,
    ShippingMethodDesiredRow Desired,
    Guid? ExistingShippingMethodId);

/// <summary>برنامهٔ کامل backfill همراه با تخلف‌های کشف‌شده.</summary>
public sealed record ShippingMethodBackfillPlan(
    IReadOnlyList<ShippingMethodPlanItem> Items,
    IReadOnlyList<string> Violations)
{
    /// <summary>آیا برنامه بدون تخلف و قابل اجرا است.</summary>
    public bool IsExecutable => Violations.Count == 0;
}

/// <summary>
/// برنامه‌ریز نگاشت کاتالوگ دوسطحی موجود به کاتالوگ یک‌سطحی.
/// </summary>
/// <remarks>
/// <para>
/// این کلاس یک تابع خالص است: هیچ DbContext و هیچ نوشتنی ندارد. برای همان ورودی، همان خروجی را
/// می‌دهد، پس اجرای مکرر آن idempotent است (<see cref="ShippingMethodPlanAction.Skip"/>).
/// </para>
/// <para>
/// قواعد حفاظتی: کد روش باید در <see cref="Domain.Aggregates.ShippingMethod.MaxCodeLength"/> جا شود؛
/// برخورد بدون‌توجه‌به‌حروف بزرگ/کوچک، جداکنندهٔ مبهم و نگاشت تکراری مجاز نیست؛ سرویس بدون گزینه یک
/// روش با <c>LegacyOptionCode = null</c> می‌سازد؛ یک سیاست fallback در سطح سرویس روش فروشی دوم
/// محسوب نمی‌شود. فعال/غیرفعال بودن موجود بازنویسی نمی‌شود مگر برای هم‌راستاسازی صریح.
/// </para>
/// </remarks>
public static class ShippingMethodBackfillPlanner
{
    /// <summary>برنامهٔ backfill را از کاتالوگ موجود می‌سازد.</summary>
    public static ShippingMethodBackfillPlan Plan(
        IReadOnlyList<ShippingCatalogServiceSnapshot> services,
        IReadOnlyList<ShippingMethodExistingRow> existing)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(existing);

        var violations = new List<string>();
        var byCode = new Dictionary<string, ShippingMethodExistingRow>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in existing)
        {
            if (!byCode.TryAdd(row.MethodCode.Trim(), row))
            {
                violations.Add($"duplicate_existing_code:{row.MethodCode.Trim()}");
            }
        }

        var desired = new List<ShippingMethodDesiredRow>();
        var seenBindings = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var service in services.OrderBy(x => x.SortOrder).ThenBy(x => x.Code, StringComparer.Ordinal))
        {
            var serviceCode = service.Code.Trim().ToLowerInvariant();
            // همهٔ گزینه‌ها نگاشت می‌شوند، فعال و غیرفعال: یک کد روش پرداخت‌شده/تاریخی ممکن است
            // غیرفعال شده باشد و باید هویت خود را حفظ کند. غیرفعال بودن، فعال‌سازی ناخواسته نیست.
            var options = service.Options
                .OrderBy(x => x.SortOrder).ThenBy(x => x.Code, StringComparer.Ordinal)
                .ToArray();

            if (options.Length == 0)
            {
                // سرویس بدون گزینه: یک روش تک‌کدی. برای سرویس غیرفعال هم یک ردیف غیرفعال
                // ثبت می‌شود تا نگاشت کامل و پایدار بماند (بدون فعال‌سازی ناخواسته).
                AddDesired(
                    desired,
                    seenBindings,
                    violations,
                    serviceCode,
                    service.ProviderKind,
                    serviceCode,
                    null,
                    service.SortOrder,
                    service.IsActive,
                    service.Translations,
                    serviceCode);
                continue;
            }

            var optionOrder = service.SortOrder;
            foreach (var option in options)
            {
                AddDesired(
                    desired,
                    seenBindings,
                    violations,
                    $"{serviceCode}:{option.Code.Trim().ToLowerInvariant()}",
                    service.ProviderKind,
                    serviceCode,
                    option.Code.Trim().ToLowerInvariant(),
                    optionOrder,
                    service.IsActive && option.IsActive,
                    option.Translations
                        .Select(t => new ShippingCatalogTranslationSnapshot(t.LanguageId, t.Name, null))
                        .ToArray(),
                    $"{serviceCode}:{option.Code.Trim().ToLowerInvariant()}");
                optionOrder += 1;
            }
        }

        var items = new List<ShippingMethodPlanItem>(desired.Count);
        foreach (var row in desired)
        {
            if (!byCode.TryGetValue(row.MethodCode, out var current))
            {
                items.Add(new ShippingMethodPlanItem(ShippingMethodPlanAction.Create, row, null));
                continue;
            }

            if (!string.Equals(current.LegacyServiceCode, row.LegacyServiceCode, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(current.LegacyOptionCode, row.LegacyOptionCode, StringComparison.OrdinalIgnoreCase))
            {
                violations.Add($"binding_mismatch:{row.MethodCode}");
                continue;
            }

            items.Add(current.IsActive == row.IsActive
                ? new ShippingMethodPlanItem(ShippingMethodPlanAction.Skip, row, current.ShippingMethodId)
                : new ShippingMethodPlanItem(ShippingMethodPlanAction.AlignActivation, row, current.ShippingMethodId));
        }

        return new ShippingMethodBackfillPlan(items, violations);
    }

    private static void AddDesired(
        List<ShippingMethodDesiredRow> desired,
        HashSet<string> seenBindings,
        List<string> violations,
        string methodCode,
        string providerKind,
        string legacyServiceCode,
        string? legacyOptionCode,
        int sortOrder,
        bool isActive,
        IReadOnlyList<ShippingCatalogTranslationSnapshot> translations,
        string displayName)
    {
        var normalized = Domain.Aggregates.ShippingMethod.TryNormalizeCode(methodCode);
        if (normalized is null)
        {
            violations.Add($"invalid_method_code:{methodCode}");
            return;
        }

        var binding = $"{legacyServiceCode}|{legacyOptionCode ?? string.Empty}";
        if (!seenBindings.Add(binding))
        {
            violations.Add($"duplicate_binding:{binding}");
            return;
        }

        if (desired.Any(x => string.Equals(x.MethodCode, normalized, StringComparison.OrdinalIgnoreCase)))
        {
            violations.Add($"duplicate_method_code:{normalized}");
            return;
        }

        var primary = translations.FirstOrDefault();
        desired.Add(new ShippingMethodDesiredRow(
            normalized,
            providerKind,
            legacyServiceCode,
            legacyOptionCode,
            sortOrder,
            isActive,
            translations.Select(t => t.LanguageId).ToArray(),
            string.IsNullOrWhiteSpace(primary?.Name) ? displayName : primary!.Name,
            primary?.Description));
    }
}
