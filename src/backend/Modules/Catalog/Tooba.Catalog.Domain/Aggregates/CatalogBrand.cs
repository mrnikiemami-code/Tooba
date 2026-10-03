using Tooba.BuildingBlocks;

namespace Tooba.Catalog.Domain.Aggregates;

/// <summary>
/// برند تحریری Catalog. مالکیت فروشنده و تسویه نیست.
/// </summary>
public sealed class CatalogBrand
{
    /// <summary>
    /// شناسهٔ پایدار برند.
    /// </summary>
    public Guid BrandId { get; init; }

    /// <summary>
    /// درز slug برای SEO بعدی؛ robots/index اینجا نیست.
    /// </summary>
    public string? SlugSeam { get; set; }

    /// <summary>
    /// وضعیت انتشار برند.
    /// </summary>
    public CatalogPublicationStatus Status { get; set; }

    /// <summary>
    /// شناسهٔ مرجع مات لوگوی برند در Media؛ اختیاری و فقط برای نمایش ویترین.
    /// </summary>
    public Guid? LogoMediaAssetId { get; set; }

    /// <summary>
    /// زمان ایجاد.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// زمان به‌روزرسانی.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// برند توصیفی می‌سازد بدون SellerId.
    /// </summary>
    public static CatalogBrand Create(string? slugSeam, DateTimeOffset now) =>
        new()
        {
            BrandId = UuidV7.New(),
            SlugSeam = string.IsNullOrWhiteSpace(slugSeam) ? null : slugSeam.Trim().ToLowerInvariant(),
            Status = CatalogPublicationStatus.Draft,
            CreatedAt = now,
            UpdatedAt = now,
        };

    /// <summary>
    /// برند را برای سطوح عمومی برند منتشر می‌کند. انتشار برند صرفاً تحریری است و
    /// نه مالکیت فروشنده می‌سازد و نه ادعای بازاریابی؛ Offer و قیمت بیرون از Catalog می‌مانند.
    /// </summary>
    /// <param name="now">زمان UTC سرور برای مهر به‌روزرسانی؛ ساعت کلاینت مرجع نیست.</param>
    public void Publish(DateTimeOffset now)
    {
        Status = CatalogPublicationStatus.Published;
        UpdatedAt = now;
    }
}
