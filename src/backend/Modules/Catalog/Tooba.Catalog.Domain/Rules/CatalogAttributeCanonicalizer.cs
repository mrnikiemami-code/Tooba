using Tooba.BuildingBlocks;

namespace Tooba.Catalog.Domain.Rules;

/// <summary>
/// نرمال‌سازی مقدار ویژگی طبق نوع تعریف تا Type safety حفظ شود.
/// </summary>
public static class CatalogAttributeCanonicalizer
{
    /// <summary>
    /// مقدار خام را به شکل پایدار تبدیل می‌کند یا رد می‌کند.
    /// </summary>
    public static string Canonicalize(CatalogAttributeValueKind kind, string raw, Guid? enumOptionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(raw);
        return kind switch
        {
            CatalogAttributeValueKind.Text => raw.Trim(),
            CatalogAttributeValueKind.Number => decimal.Parse(raw.Trim(), System.Globalization.CultureInfo.InvariantCulture)
                .ToString(System.Globalization.CultureInfo.InvariantCulture),
            CatalogAttributeValueKind.Boolean => bool.Parse(raw.Trim()).ToString(),
            CatalogAttributeValueKind.Instant => DateTimeOffset.Parse(raw.Trim(), System.Globalization.CultureInfo.InvariantCulture)
                .ToString("O"),
            CatalogAttributeValueKind.Enumeration => (enumOptionId ?? throw new InvalidOperationException("گزینهٔ شمارشی باید شناسه داشته باشد."))
                .ToString("N"),
            _ => throw new InvalidOperationException("گونهٔ ویژگی پشتیبانی نمی‌شود."),
        };
    }

    /// <summary>
    /// محدودیت‌های typed تعریف را پس از canonicalization اعمال می‌کند؛ JSON آزاد نیست.
    /// </summary>
    public static void EnforceValidationBounds(CatalogAttributeDefinition definition, string canonicalValue)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentException.ThrowIfNullOrWhiteSpace(canonicalValue);
        if (definition.ValueKind == CatalogAttributeValueKind.Number)
        {
            var number = decimal.Parse(canonicalValue, System.Globalization.CultureInfo.InvariantCulture);
            if (definition.ValidationMin is decimal min && number < min)
            {
                throw new InvalidOperationException("مقدار عددی از حداقل تعریف کوچک‌تر است.");
            }

            if (definition.ValidationMax is decimal max && number > max)
            {
                throw new InvalidOperationException("مقدار عددی از حداکثر تعریف بزرگ‌تر است.");
            }
        }

        if (definition.ValueKind == CatalogAttributeValueKind.Text
            && definition.ValidationMaxLength is int maxLength
            && canonicalValue.Length > maxLength)
        {
            throw new InvalidOperationException("طول متن از حداکثر تعریف بیشتر است.");
        }
    }
}
