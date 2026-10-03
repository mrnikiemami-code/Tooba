using Tooba.BuildingBlocks;

namespace Tooba.Catalog.Domain.Aggregates;

/// <summary>
/// گونهٔ فروش‌پذیر توصیفی متعلق به یک Product. SKU Catalog با هویت Offer فروشنده یکی نیست.
/// </summary>
public sealed class CatalogVariant : IHasDomainEvents
{
    private readonly DomainEventCollector _domainEvents = new();

    /// <summary>
    /// شناسهٔ پایدار گونه در Catalog.
    /// </summary>
    public Guid VariantId { get; init; }

    /// <summary>
    /// محصول والد. بدون Product گونه معنا ندارد.
    /// </summary>
    public Guid ProductId { get; init; }

    /// <summary>
    /// کد کاتالوگ اختیاری؛ کد SKU اختصاصی فروشنده در Offer آینده است.
    /// </summary>
    public string? CatalogCodeSeam { get; set; }

    /// <summary>
    /// اثرانگشت قطعی ترکیب محورها برای یکتایی داخل Product.
    /// </summary>
    public string CombinationFingerprint { get; init; } = "";

    /// <summary>
    /// انتشار گونه. قابل‌خرید بودن Offer نیست.
    /// </summary>
    public CatalogPublicationStatus Status { get; set; }

    /// <summary>
    /// ترتیب نمایش پایدار تنوع داخل محصول.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// تنوع پیش‌فرض محصول؛ حداکثر یکی میان غیرآرشیو.
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// زمان ایجاد.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// زمان به‌روزرسانی.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// مقادیر محور بارگذاری‌شده توسط EF.
    /// </summary>
    public List<CatalogVariantAttributeValue> AttributeValues { get; } = [];

    /// <inheritdoc />
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.Events;

    /// <summary>
    /// اثرانگشت پایدار از جفت تعریف/مقدار مرتب‌شده.
    /// </summary>
    public static string ComputeFingerprint(IEnumerable<(Guid DefinitionId, string CanonicalValue)> axes)
    {
        ArgumentNullException.ThrowIfNull(axes);
        var parts = axes
            .Select(x => $"{x.DefinitionId:N}={x.CanonicalValue.Trim().ToLowerInvariant()}")
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        if (parts.Length == 0)
        {
            throw new InvalidOperationException("تنوع باید حداقل یک محور ویژگی داشته باشد تا با Product ساده قاطی نشود.");
        }

        return string.Join("|", parts);
    }

    /// <summary>
    /// تنوع می‌سازد و رویداد ایجاد را برای تصویر Search آینده صف می‌کند نه برای ایندکس کردن همین‌جا.
    /// </summary>
    public static CatalogVariant Create(
        Guid productId,
        string combinationFingerprint,
        string? catalogCodeSeam,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(combinationFingerprint);
        var variant = new CatalogVariant
        {
            VariantId = UuidV7.New(),
            ProductId = productId,
            CombinationFingerprint = combinationFingerprint,
            CatalogCodeSeam = string.IsNullOrWhiteSpace(catalogCodeSeam) ? null : catalogCodeSeam.Trim(),
            Status = CatalogPublicationStatus.Draft,
            SortOrder = 0,
            IsDefault = false,
            CreatedAt = now,
            UpdatedAt = now,
        };
        variant._domainEvents.Add(new CatalogVariantCreatedDomainEvent(variant));
        return variant;
    }

    /// <summary>
    /// وضعیت انتشار تنوع را بدون تغییر اثرانگشت ترکیب به‌روز می‌کند.
    /// </summary>
    public void SetStatus(CatalogPublicationStatus status, DateTimeOffset now)
    {
        Status = status;
        if (status == CatalogPublicationStatus.Archived)
        {
            IsDefault = false;
        }

        UpdatedAt = now;
    }

    /// <summary>
    /// ترتیب نمایش تنوع را به‌روز می‌کند.
    /// </summary>
    public void SetSortOrder(int sortOrder, DateTimeOffset now)
    {
        SortOrder = sortOrder;
        UpdatedAt = now;
    }

    /// <summary>
    /// پرچم پیش‌فرض را روی این موجودیت تنظیم می‌کند؛ یکتایی در دایرکتوری اعمال می‌شود.
    /// </summary>
    public void SetDefault(bool isDefault, DateTimeOffset now)
    {
        if (isDefault && Status == CatalogPublicationStatus.Archived)
        {
            throw new InvalidOperationException("تنوع بایگانی‌شده نمی‌تواند پیش‌فرض باشد.");
        }

        IsDefault = isDefault;
        UpdatedAt = now;
    }

    /// <summary>
    /// کد کاتالوگ را بدون تغییر اثرانگشت ترکیب به‌روز می‌کند.
    /// </summary>
    public void UpdateCatalogCodeSeam(string? catalogCodeSeam, DateTimeOffset now)
    {
        CatalogCodeSeam = string.IsNullOrWhiteSpace(catalogCodeSeam) ? null : catalogCodeSeam.Trim();
        UpdatedAt = now;
    }

    /// <inheritdoc />
    public void ClearDomainEvents() => _domainEvents.Clear();
}
