using Tooba.BuildingBlocks;

namespace Tooba.Catalog.Domain.Aggregates;

/// <summary>
/// متن محلی‌سازی‌شده. Locale با Market یکی نیست.
/// </summary>
public sealed class CatalogLocalizedText
{
    /// <summary>
    /// شناسهٔ ردیف ترجمه.
    /// </summary>
    public Guid TextId { get; init; }

    /// <summary>
    /// نوع مالک داخل Catalog.
    /// </summary>
    public CatalogLocalizedOwnerKind OwnerKind { get; init; }

    /// <summary>
    /// شناسهٔ مالک.
    /// </summary>
    public Guid OwnerId { get; init; }

    /// <summary>
    /// فیلد منطقی مثل name یا description.
    /// </summary>
    public string FieldKey { get; init; } = "";

    /// <summary>
    /// برچسب زبان BCP-47؛ کد ارز نیست.
    /// </summary>
    public string Locale { get; init; } = "";

    /// <summary>
    /// مقدار نمایشی.
    /// </summary>
    public string Value { get; set; } = "";

    /// <summary>
    /// ردیف ترجمه می‌سازد.
    /// </summary>
    public static CatalogLocalizedText Create(
        CatalogLocalizedOwnerKind ownerKind,
        Guid ownerId,
        string fieldKey,
        string locale,
        string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(locale);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return new CatalogLocalizedText
        {
            TextId = UuidV7.New(),
            OwnerKind = ownerKind,
            OwnerId = ownerId,
            FieldKey = fieldKey.Trim().ToLowerInvariant(),
            Locale = locale.Trim(),
            Value = value.Trim(),
        };
    }
}
