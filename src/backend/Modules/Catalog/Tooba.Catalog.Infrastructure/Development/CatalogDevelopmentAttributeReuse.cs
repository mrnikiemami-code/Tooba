#pragma warning disable CS1591
using Microsoft.EntityFrameworkCore;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.Models;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;

namespace Tooba.Catalog.Infrastructure.Development;

/// <summary>
/// درز کوچک و Development-only برای «خواندن-یا-ساختن» ویژگی‌های دانهٔ Development کاتالوگ.
/// <para>
/// دو دانهٔ Development کاتالوگ (schema ویژگی‌ها و دموی legacy Workspace) می‌توانند کد ویژگی مشترکی
/// بخواهند. پیش از این، دانهٔ دوم کورکورانه <see cref="ICatalogDirectory.CreateAttributeDefinitionAsync"/>
/// را صدا می‌زد و روی پایگاه‌دادهٔ خالی با رد شدن <c>catalog.attribute.code.duplicate</c> متوقف می‌شد.
/// </para>
/// <para>
/// این درز رفتار عمومی ساخت ویژگی را تغییر نمی‌دهد: یکتایی کد، اعتبارسنجی‌ها و سیاست رد کردن در مالکیت
/// کاتالوگ دست‌نخورده می‌ماند. تنها کاری که می‌کند این است که اگر تعریفی با همان کد از قبل وجود داشته
/// باشد، آن را فقط زمانی بازاستفاده می‌کند که تمام معناهای موردنیاز مسیر پایین‌دستی (نوع مقدار، مشارکت
/// محور تنوع، فعال بودن) یکسان باشند؛ در غیر این صورت با یک پیام پایدار و قابل‌تشخیص که فقط کد و قاعده(های)
/// ناسازگار را نام می‌برد متوقف می‌شود و هیچ نوشتنی انجام نمی‌دهد.
/// </para>
/// </summary>
internal static class CatalogDevelopmentAttributeReuse
{
    /// <summary>پیشوند پایدار خطای ناسازگاریِ بازاستفاده؛ فقط کد و قاعده(های) ناسازگار را نام می‌برد.</summary>
    internal const string IncompatibleReuseCode = "catalog-dev.attribute.incompatible-reuse";

    private const string ValueKindRule = "value-kind";
    private const string VariantAxisRule = "variant-axis";
    private const string ActiveRule = "active";

    /// <summary>
    /// تعریف ویژگی را با همان کد برمی‌گرداند و تنها در صورت نبودن می‌سازد.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// تعریف موجودی با همان کد هست اما معنای موردنیاز مسیر پایین‌دستی را ندارد.
    /// </exception>
    internal static async Task<Guid> EnsureCompatibleDefinitionAsync(
        ICatalogDirectory catalog,
        string code,
        CatalogAttributeValueKind valueKind,
        bool isVariantAxis,
        IReadOnlyDictionary<string, string> localizedNames,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        var normalized = Normalize(code);
        var definitions = await catalog.ListAttributeDefinitionsAsync(cancellationToken);
        var existing = definitions.FirstOrDefault(x => Normalize(x.Code) == normalized);
        if (existing is null)
        {
            return await catalog.CreateAttributeDefinitionAsync(
                code,
                valueKind,
                isVariantAxis,
                localizedNames,
                cancellationToken);
        }

        var mismatch = DescribeIncompatibility(code, existing, valueKind, isVariantAxis);
        if (mismatch is not null)
        {
            throw new InvalidOperationException(mismatch);
        }

        return existing.DefinitionId;
    }

    /// <summary>
    /// گزینهٔ شمارشی را با همان کد زیر همان تعریف برمی‌گرداند و تنها در صورت نبودن می‌سازد.
    /// هویت گزینه همان (تعریف، کد) است؛ بازاستفاده هیچ معنای دیگری را تغییر نمی‌دهد.
    /// </summary>
    internal static async Task<Guid> EnsureCompatibleOptionAsync(
        ICatalogDirectory catalog,
        CatalogDbContext catalogDb,
        Guid definitionId,
        string code,
        IReadOnlyDictionary<string, string> localizedNames,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(catalogDb);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        var normalized = Normalize(code);
        var existing = await catalogDb.AttributeOptions.AsNoTracking()
            .FirstOrDefaultAsync(x => x.DefinitionId == definitionId && x.Code == normalized, cancellationToken);
        if (existing is not null)
        {
            return existing.OptionId;
        }

        return await catalog.AddAttributeOptionAsync(definitionId, code, localizedNames, cancellationToken);
    }

    /// <summary>
    /// پیام پایدار ناسازگاری را می‌سازد؛ اگر تعریف موجود سازگار باشد <see langword="null"/> برمی‌گرداند.
    /// پیام فقط کد و قاعده(های) ناسازگار را نام می‌برد و هیچ دادهٔ دیگری را افشا نمی‌کند.
    /// </summary>
    internal static string? DescribeIncompatibility(
        string code,
        AttributeDefinitionView existing,
        CatalogAttributeValueKind valueKind,
        bool isVariantAxis)
    {
        ArgumentNullException.ThrowIfNull(existing);

        var rules = new List<string>(3);
        if (existing.ValueKind != valueKind)
        {
            rules.Add(ValueKindRule);
        }

        if (existing.IsVariantAxisAllowed != isVariantAxis)
        {
            rules.Add(VariantAxisRule);
        }

        if (!existing.IsActive)
        {
            rules.Add(ActiveRule);
        }

        return rules.Count == 0
            ? null
            : $"{IncompatibleReuseCode}: code={Normalize(code)}; rules={string.Join(',', rules)}";
    }

    private static string Normalize(string code) => code.Trim().ToLowerInvariant();
}
