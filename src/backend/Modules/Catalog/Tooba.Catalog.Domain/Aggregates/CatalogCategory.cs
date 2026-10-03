using Tooba.BuildingBlocks;

namespace Tooba.Catalog.Domain.Aggregates;

/// <summary>
/// ردهٔ طبقه‌بندی Catalog. درخت ناوبری فروشگاه عمومی نیست و قیمت ندارد.
/// نام/slug محلی در <see cref="CatalogCategoryTranslation"/> است؛ ستون NameFa/NameEn وجود ندارد.
/// </summary>
public sealed class CatalogCategory
{
    /// <summary>
    /// شناسهٔ پایدار رده داخل schema همین ماژول.
    /// </summary>
    public Guid CategoryId { get; init; }

    /// <summary>
    /// والد اختیاری در همان schema؛ FK به ماژول دیگر نیست.
    /// </summary>
    public Guid? ParentCategoryId { get; set; }

    /// <summary>
    /// انتشار رده برای طبقه‌بندی، نه برای خرید.
    /// </summary>
    public CatalogPublicationStatus Status { get; set; }

    /// <summary>
    /// ترتیب پایدار میان خواهر/برادرها زیر همان والد.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// نمایش‌پذیری در ویترین؛ جدا از Status انتشار Admin.
    /// </summary>
    public bool IsVisible { get; set; }

    /// <summary>
    /// مرجع مات تصویر رده در Media؛ مالکیت باینری اینجا نیست.
    /// </summary>
    public Guid? ImageMediaAssetId { get; set; }

    /// <summary>
    /// مرجع مات آیکون رده در Media.
    /// </summary>
    public Guid? IconMediaAssetId { get; set; }

    /// <summary>
    /// مرجع مات بنر رده در Media؛ مالکیت باینری اینجا نیست.
    /// </summary>
    public Guid? BannerMediaAssetId { get; set; }

    /// <summary>
    /// زمان ایجاد.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// زمان به‌روزرسانی فراداده.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// ردهٔ ریشه یا فرزند می‌سازد. والد نمی‌تواند خودش باشد.
    /// </summary>
    public static CatalogCategory Create(
        Guid? parentCategoryId,
        DateTimeOffset now,
        int sortOrder = 0,
        bool isVisible = true)
    {
        if (parentCategoryId == Guid.Empty)
        {
            parentCategoryId = null;
        }

        return new CatalogCategory
        {
            CategoryId = UuidV7.New(),
            ParentCategoryId = parentCategoryId,
            Status = CatalogPublicationStatus.Draft,
            SortOrder = sortOrder,
            IsVisible = isVisible,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    /// <summary>
    /// والد را عوض می‌کند بدون join بیرون از Catalog.
    /// فراخواننده باید با <see cref="CatalogCategoryTreeRules"/> حلقه را رد کند.
    /// </summary>
    public void Reparent(Guid? parentCategoryId, DateTimeOffset now)
    {
        Move(parentCategoryId, now);
    }

    /// <summary>
    /// جابه‌جایی زیر والد جدید؛ خود-والد ممنوع است. بررسی descendant در لایهٔ سرویس دامنه است.
    /// </summary>
    public void Move(Guid? newParentCategoryId, DateTimeOffset now)
    {
        if (newParentCategoryId == Guid.Empty)
        {
            newParentCategoryId = null;
        }

        if (newParentCategoryId == CategoryId)
        {
            throw new InvalidOperationException("رده نمی‌تواند والد خودش باشد؛ حلقهٔ درخت طبقه‌بندی ممنوع است.");
        }

        ParentCategoryId = newParentCategoryId;
        UpdatedAt = now;
    }

    /// <summary>
    /// فیلدهای غیرمحلی هسته را به‌روز می‌کند؛ Parent از Move می‌آید نه از این متد.
    /// </summary>
    public void SetCoreFields(
        CatalogPublicationStatus? status,
        int? sortOrder,
        bool? isVisible,
        Guid? imageMediaAssetId,
        Guid? iconMediaAssetId,
        Guid? bannerMediaAssetId,
        bool clearImage,
        bool clearIcon,
        bool clearBanner,
        DateTimeOffset now)
    {
        if (status is { } s)
        {
            Status = s;
        }

        if (sortOrder is { } order)
        {
            SortOrder = order;
        }

        if (isVisible is { } visible)
        {
            IsVisible = visible;
        }

        if (clearImage)
        {
            ImageMediaAssetId = null;
        }
        else if (imageMediaAssetId is { } image)
        {
            ImageMediaAssetId = image;
        }

        if (clearIcon)
        {
            IconMediaAssetId = null;
        }
        else if (iconMediaAssetId is { } icon)
        {
            IconMediaAssetId = icon;
        }

        if (clearBanner)
        {
            BannerMediaAssetId = null;
        }
        else if (bannerMediaAssetId is { } banner)
        {
            BannerMediaAssetId = banner;
        }

        UpdatedAt = now;
    }

    /// <summary>
    /// ترتیب خواهر/برادر را تنظیم می‌کند.
    /// </summary>
    public void SetSortOrder(int sortOrder, DateTimeOffset now)
    {
        SortOrder = sortOrder;
        UpdatedAt = now;
    }

    /// <summary>
    /// رده را برای ناوبری منتشر می‌کند. انتشار رده فقط طبقه‌بندی را قابل‌کشف می‌کند و
    /// هیچ قابلیت خریدی نمی‌سازد؛ قیمت و موجودی هرگز به رده تعلق ندارند.
    /// </summary>
    /// <param name="now">زمان UTC سرور برای مهر به‌روزرسانی؛ ساعت کلاینت مرجع نیست.</param>
    public void Publish(DateTimeOffset now)
    {
        Status = CatalogPublicationStatus.Published;
        UpdatedAt = now;
    }

    /// <summary>
    /// آرشیو تحریری رده؛ حذف سخت و cascade تاریخچهٔ slug نیست.
    /// </summary>
    public void Archive(DateTimeOffset now)
    {
        Status = CatalogPublicationStatus.Archived;
        UpdatedAt = now;
    }
}
