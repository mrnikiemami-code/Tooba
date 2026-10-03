using Tooba.BuildingBlocks;

namespace Tooba.Catalog.Domain.Aggregates;

/// <summary>
/// برچسب تاکسونومی Catalog برای گروه‌بندی/جستجو/نمایش هدفمند.
/// meta keywords نیست؛ صفحهٔ عمومی خودکار و SEO keyword strategy ندارد.
/// </summary>
public sealed class CatalogTag
{
    /// <summary>شناسهٔ پایدار برچسب.</summary>
    public Guid TagId { get; init; }

    /// <summary>کد پایدار ماشینی (unique)؛ برچسب نمایش در LocalizedText است.</summary>
    public string Code { get; init; } = "";

    /// <summary>درز slug اختیاری برای مسیریابی آینده؛ robots/index اینجا نیست.</summary>
    public string? SlugSeam { get; set; }

    /// <summary>وضعیت انتشار تحریری برچسب.</summary>
    public CatalogPublicationStatus Status { get; set; }

    /// <summary>زمان ایجاد.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>زمان به‌روزرسانی.</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>برچسب تاکسونومی می‌سازد.</summary>
    public static CatalogTag Create(string code, string? slugSeam, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        var normalizedCode = code.Trim().ToLowerInvariant();
        return new CatalogTag
        {
            TagId = UuidV7.New(),
            Code = normalizedCode,
            SlugSeam = string.IsNullOrWhiteSpace(slugSeam)
                ? null
                : slugSeam.Trim().ToLowerInvariant(),
            Status = CatalogPublicationStatus.Draft,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    /// <summary>برچسب را برای استفادهٔ تحریری منتشر می‌کند؛ صفحهٔ عمومی خودکار نمی‌سازد.</summary>
    public void Publish(DateTimeOffset now)
    {
        Status = CatalogPublicationStatus.Published;
        UpdatedAt = now;
    }
}
