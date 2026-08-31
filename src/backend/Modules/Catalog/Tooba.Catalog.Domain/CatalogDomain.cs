using Tooba.BuildingBlocks;

namespace Tooba.Catalog.Domain;

/// <summary>
/// وضعیت انتشار توصیفی Catalog. قابل‌خرید بودن Offer نیست و موجودی انبار را نشان نمی‌دهد.
/// </summary>
public enum CatalogPublicationStatus
{
    /// <summary>
    /// پیش‌نویس تحریری؛ وجود محصول با قابل‌فروش بودن Offer یکی نیست.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// منتشرشده در Catalog. هنوز یعنی Offer/قیمت/موجودی آمادهٔ خرید نیست.
    /// </summary>
    Published = 1,

    /// <summary>
    /// بایگانی تحریری؛ حذف Offer یا موجودی نیست.
    /// </summary>
    Archived = 2,
}

/// <summary>
/// درز گونهٔ محصول برای schema ویژگی‌های بعدی. نوع تجاری Offer نیست.
/// </summary>
public enum CatalogProductKind
{
    /// <summary>
    /// کالای فیزیکی توصیفی. حمل و موجودی اینجا مدل نمی‌شود.
    /// </summary>
    PhysicalGood = 0,

    /// <summary>
    /// خدمت توصیفی. قیمت خدمت در Pricing است.
    /// </summary>
    Service = 1,
}

/// <summary>
/// گونهٔ مقدار ویژگی تایپ‌شده. از EAV آزاد بدون نوع جلوگیری می‌کند.
/// </summary>
public enum CatalogAttributeValueKind
{
    /// <summary>
    /// متن آزاد محلی‌سازی‌پذیر در لایهٔ ترجمه، نه قیمت.
    /// </summary>
    Text = 0,

    /// <summary>
    /// عدد اعشاری توصیفی (وزن/اندازه)، نه مبلغ پول.
    /// </summary>
    Number = 1,

    /// <summary>
    /// مقدار بولی مشخصات، نه flag انبار.
    /// </summary>
    Boolean = 2,

    /// <summary>
    /// گزینه از فهرست بسته؛ محور Variant معمولاً از این گونه است.
    /// </summary>
    Enumeration = 3,

    /// <summary>
    /// تاریخ/زمان توصیفی، نه زمان تسویه.
    /// </summary>
    Instant = 4,
}

/// <summary>
/// مالک متن چندزبانه. Locale با Market/Currency قاطی نمی‌شود.
/// </summary>
public enum CatalogLocalizedOwnerKind
{
    /// <summary>
    /// نام/شرح محصول Catalog.
    /// </summary>
    Product = 0,

    /// <summary>
    /// نام ردهٔ طبقه‌بندی.
    /// </summary>
    Category = 1,

    /// <summary>
    /// نام برند تحریری.
    /// </summary>
    Brand = 2,

    /// <summary>
    /// برچسب تعریف ویژگی.
    /// </summary>
    AttributeDefinition = 3,

    /// <summary>
    /// برچسب گزینهٔ شمارشی.
    /// </summary>
    AttributeOption = 4,

    /// <summary>
    /// نام برچسب تاکسونومی Catalog (نه meta keywords).
    /// </summary>
    Tag = 5,
}

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

/// <summary>
/// پیوند چندبه‌چند محصول ↔ برچسب. ذخیرهٔ comma-separated نیست.
/// </summary>
public sealed class CatalogProductTagAssignment
{
    /// <summary>شناسهٔ پیوند.</summary>
    public Guid AssignmentId { get; init; }

    /// <summary>محصول Catalog.</summary>
    public Guid ProductId { get; init; }

    /// <summary>برچسب Catalog.</summary>
    public Guid TagId { get; init; }

    /// <summary>پیوند محصول-برچسب می‌سازد.</summary>
    public static CatalogProductTagAssignment Assign(Guid productId, Guid tagId) =>
        new()
        {
            AssignmentId = UuidV7.New(),
            ProductId = productId,
            TagId = tagId,
        };
}

/// <summary>
/// پیوند چندبه‌چند رده ↔ برچسب. ذخیرهٔ comma-separated نیست.
/// </summary>
public sealed class CatalogCategoryTagAssignment
{
    /// <summary>شناسهٔ پیوند.</summary>
    public Guid AssignmentId { get; init; }

    /// <summary>ردهٔ Catalog.</summary>
    public Guid CategoryId { get; init; }

    /// <summary>برچسب Catalog.</summary>
    public Guid TagId { get; init; }

    /// <summary>پیوند رده-برچسب می‌سازد.</summary>
    public static CatalogCategoryTagAssignment Assign(Guid categoryId, Guid tagId) =>
        new()
        {
            AssignmentId = UuidV7.New(),
            CategoryId = categoryId,
            TagId = tagId,
        };
}

/// <summary>
/// تعریف ویژگی تایپ‌شده. ستون قیمت یا موجودی محصول نیست.
/// </summary>
public sealed class CatalogAttributeDefinition
{
    /// <summary>
    /// شناسهٔ تعریف.
    /// </summary>
    public Guid DefinitionId { get; init; }

    /// <summary>
    /// کد پایدار ماشینی (مثلاً color). برچسب نمایش در جدول ترجمه است.
    /// </summary>
    public string Code { get; init; } = "";

    /// <summary>
    /// نوع مقدار؛ از Dictionary آزاد جلوگیری می‌کند.
    /// </summary>
    public CatalogAttributeValueKind ValueKind { get; init; }

    /// <summary>
    /// اگر true باشد تعریف مجاز است به‌عنوان محور ترکیب Variant انتخاب شود (نه مشخصات سادهٔ محصول).
    /// ستون DB همان <c>IsVariantAxis</c> است؛ معنای معنایی IsVariantAxisAllowed.
    /// </summary>
    public bool IsVariantAxis { get; set; }

    /// <summary>
    /// نام مستعار معنایی برای <see cref="IsVariantAxis"/>؛ در EF نادیده گرفته می‌شود.
    /// </summary>
    public bool IsVariantAxisAllowed => IsVariantAxis;

    /// <summary>
    /// واحد نمایشی اختیاری (مثلاً GB، inch)؛ قیمت نیست.
    /// </summary>
    public string? Unit { get; set; }

    /// <summary>
    /// پیش‌فرض الزام در سطح تعریف؛ override رده می‌تواند سخت‌تر کند.
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// قابل استفاده در فیلتر ویترین آینده.
    /// </summary>
    public bool IsFilterable { get; set; }

    /// <summary>
    /// قابل مقایسه در جدول مقایسهٔ آینده.
    /// </summary>
    public bool IsComparable { get; set; }

    /// <summary>
    /// چندمقداری بودن؛ در foundation فعلی مقدار تکی ذخیره می‌شود.
    /// </summary>
    public bool IsMultivalue { get; set; }

    /// <summary>
    /// ترتیب نمایش پیش‌فرض تعریف.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// حداقل عددی اختیاری برای Number.
    /// </summary>
    public decimal? ValidationMin { get; set; }

    /// <summary>
    /// حداکثر عددی اختیاری برای Number.
    /// </summary>
    public decimal? ValidationMax { get; set; }

    /// <summary>
    /// حداکثر طول متن اختیاری برای Text.
    /// </summary>
    public int? ValidationMaxLength { get; set; }

    /// <summary>
    /// فعال بودن تعریف برای schema authoring؛ پیش‌فرض true برای BC.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// زمان ایجاد.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// تعریف ویژگی می‌سازد.
    /// </summary>
    public static CatalogAttributeDefinition Create(string code, CatalogAttributeValueKind valueKind, bool isVariantAxis, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        return new CatalogAttributeDefinition
        {
            DefinitionId = UuidV7.New(),
            Code = code.Trim().ToLowerInvariant(),
            ValueKind = valueKind,
            IsVariantAxis = isVariantAxis,
            Unit = null,
            IsRequired = false,
            IsFilterable = false,
            IsComparable = false,
            IsMultivalue = false,
            DisplayOrder = 0,
            ValidationMin = null,
            ValidationMax = null,
            ValidationMaxLength = null,
            IsActive = true,
            CreatedAt = now,
        };
    }

    /// <summary>
    /// فرادادهٔ schema را بدون تغییر Code/ValueKind/IsVariantAxis به‌روز می‌کند.
    /// </summary>
    public void UpdateMetadata(
        string? unit,
        bool isRequired,
        bool isFilterable,
        bool isComparable,
        bool isMultivalue,
        int displayOrder,
        decimal? validationMin,
        decimal? validationMax,
        int? validationMaxLength,
        bool isActive)
    {
        if (validationMin is not null && validationMax is not null && validationMin > validationMax)
        {
            throw new InvalidOperationException("حداقل اعتبارسنجی نمی‌تواند از حداکثر بزرگ‌تر باشد.");
        }

        Unit = string.IsNullOrWhiteSpace(unit) ? null : unit.Trim();
        IsRequired = isRequired;
        IsFilterable = isFilterable;
        IsComparable = isComparable;
        IsMultivalue = isMultivalue;
        DisplayOrder = displayOrder;
        ValidationMin = validationMin;
        ValidationMax = validationMax;
        ValidationMaxLength = validationMaxLength is < 0
            ? throw new InvalidOperationException("حداکثر طول نمی‌تواند منفی باشد.")
            : validationMaxLength;
        IsActive = isActive;
    }

    /// <summary>
    /// قابلیت محور تنوع را به‌روز می‌کند؛ bindingهای رده را خودکار تغییر نمی‌دهد.
    /// </summary>
    public void SetVariantAxisAllowed(bool enabled)
    {
        if (enabled)
        {
            CatalogCategoryAttributeAssignmentRules.ValidateVariantAxisCapabilityEnable(ValueKind);
        }

        IsVariantAxis = enabled;
    }
}

/// <summary>
/// گزینهٔ شمارشی یک تعریف. هویت Offer فروشنده نیست.
/// </summary>
public sealed class CatalogAttributeOption
{
    /// <summary>
    /// شناسهٔ گزینه.
    /// </summary>
    public Guid OptionId { get; init; }

    /// <summary>
    /// تعریف والد در همین schema.
    /// </summary>
    public Guid DefinitionId { get; init; }

    /// <summary>
    /// کد پایدار گزینه (مثلاً black).
    /// </summary>
    public string Code { get; init; } = "";

    /// <summary>
    /// ترتیب نمایش گزینه.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// فعال بودن گزینه برای انتخاب؛ پیش‌فرض true برای BC.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// گزینه می‌سازد.
    /// </summary>
    public static CatalogAttributeOption Create(Guid definitionId, string code, int displayOrder = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        return new CatalogAttributeOption
        {
            OptionId = UuidV7.New(),
            DefinitionId = definitionId,
            Code = code.Trim().ToLowerInvariant(),
            DisplayOrder = displayOrder,
            IsActive = true,
        };
    }
}

/// <summary>
/// پیوند تعریف ویژگی به رده برای schema مؤثر. SQL بیرون از Catalog نیست.
/// </summary>
public sealed class CatalogCategoryAttributeBinding
{
    /// <summary>
    /// شناسهٔ پیوند.
    /// </summary>
    public Guid BindingId { get; init; }

    /// <summary>
    /// ردهٔ مالک schema.
    /// </summary>
    public Guid CategoryId { get; init; }

    /// <summary>
    /// تعریف ویژگی.
    /// </summary>
    public Guid DefinitionId { get; init; }

    /// <summary>
    /// ترتیب نمایش در schema همین رده.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// الزام در همین رده (assignment-level).
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// نمایش در فیلتر محصولات برای همین رده.
    /// </summary>
    public bool IsFilterable { get; set; }

    /// <summary>
    /// استفاده به‌عنوان محور تنوع در همین رده (نیاز به IsVariantAxisAllowed روی تعریف).
    /// </summary>
    public bool IsVariantAxis { get; set; }

    /// <summary>
    /// نمایش در مقایسه محصولات برای همین رده.
    /// </summary>
    public bool IsComparable { get; set; }

    /// <summary>
    /// زمان ایجاد.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// پیوند رده-تعریف می‌سازد.
    /// </summary>
    public static CatalogCategoryAttributeBinding Bind(
        Guid categoryId,
        Guid definitionId,
        int displayOrder,
        bool isRequired,
        bool isFilterable,
        bool isVariantAxis,
        bool isComparable,
        DateTimeOffset now) =>
        new()
        {
            BindingId = UuidV7.New(),
            CategoryId = categoryId,
            DefinitionId = definitionId,
            DisplayOrder = displayOrder,
            IsRequired = isRequired,
            IsFilterable = isFilterable,
            IsVariantAxis = isVariantAxis,
            IsComparable = isComparable,
            CreatedAt = now,
        };
}

/// <summary>
/// قواعد اعتبارسنجی assignment رفتار category-specific.
/// </summary>
public static class CatalogCategoryAttributeAssignmentRules
{
    /// <summary>
    /// آیا نوع مقدار ذاتاً از محور تنوع پشتیبانی می‌کند.
    /// </summary>
    public static bool ValueKindSupportsVariantAxis(CatalogAttributeValueKind valueKind) =>
        valueKind is CatalogAttributeValueKind.Enumeration or CatalogAttributeValueKind.Number;

    /// <summary>
    /// فعال‌سازی capability محور تنوع را در برابر ValueKind بررسی می‌کند.
    /// </summary>
    public static void ValidateVariantAxisCapabilityEnable(CatalogAttributeValueKind valueKind)
    {
        if (!ValueKindSupportsVariantAxis(valueKind))
        {
            throw new InvalidOperationException("catalog.attribute.variant_axis.value_kind.invalid");
        }
    }

    /// <summary>
    /// فعال‌سازی محور تنوع را در برابر capability/type تعریف بررسی می‌کند.
    /// </summary>
    public static void ValidateVariantAxis(CatalogAttributeDefinition definition, bool isVariantAxisEnabled)
    {
        if (!isVariantAxisEnabled)
        {
            return;
        }

        if (!definition.IsVariantAxisAllowed)
        {
            throw new InvalidOperationException("catalog.attribute.variant_axis.capability_disabled");
        }

        if (!ValueKindSupportsVariantAxis(definition.ValueKind))
        {
            throw new InvalidOperationException("catalog.attribute.variant_axis.value_kind.invalid");
        }
    }
}

/// <summary>
/// نوع نمایش فیلتر در PLP — نه نام کامپوننت frontend.
/// </summary>
public enum CatalogFacetDisplayType
{
    /// <summary>چندانتخابی با checkbox.</summary>
    CheckboxList = 0,

    /// <summary>انتخاب با جستجو.</summary>
    SearchableSelect = 1,

    /// <summary>بازهٔ عددی.</summary>
    Range = 2,

    /// <summary>نمایش رنگ (نیاز به متادیتای رنگ گزینه).</summary>
    ColorSwatch = 3,

    /// <summary>کلید روشن/خاموش برای بولی.</summary>
    BooleanToggle = 4,
}

/// <summary>
/// پیکربندی نمایش فیلتر PLP برای یک ویژگی در یک رده.
/// </summary>
public sealed class CatalogCategoryFacetConfiguration
{
    /// <summary>
    /// شناسهٔ پیکربندی facet.
    /// </summary>
    public Guid FacetConfigurationId { get; init; }

    /// <summary>
    /// ردهٔ مالک پیکربندی.
    /// </summary>
    public Guid CategoryId { get; init; }

    /// <summary>
    /// تعریف ویژگیٔ فیلتر.
    /// </summary>
    public Guid DefinitionId { get; init; }

    /// <summary>
    /// نوع نمایش فیلتر در PLP.
    /// </summary>
    public CatalogFacetDisplayType DisplayType { get; set; }

    /// <summary>
    /// ترتیب نمایش در بین facetهای محلی این رده.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// آیا فیلتر در PLP نمایش داده شود.
    /// </summary>
    public bool IsVisible { get; set; }

    /// <summary>
    /// آیا گزینه‌ها قابل جستجو باشند.
    /// </summary>
    public bool IsSearchable { get; set; }

    /// <summary>
    /// آیا فیلتر پیش‌فرض بسته باشد.
    /// </summary>
    public bool IsCollapsedByDefault { get; set; }

    /// <summary>
    /// آیا تعداد محصول کنار گزینه نمایش داده شود.
    /// </summary>
    public bool ShowCounts { get; set; }

    /// <summary>
    /// زمان ایجاد.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// ایجاد پیکربندی facet برای یک ویژگی در رده.
    /// </summary>
    public static CatalogCategoryFacetConfiguration Create(
        Guid categoryId,
        Guid definitionId,
        CatalogFacetDisplayType displayType,
        int sortOrder,
        bool isVisible,
        bool isSearchable,
        bool isCollapsedByDefault,
        bool showCounts,
        DateTimeOffset now) =>
        new()
        {
            FacetConfigurationId = UuidV7.New(),
            CategoryId = categoryId,
            DefinitionId = definitionId,
            DisplayType = displayType,
            SortOrder = sortOrder,
            IsVisible = isVisible,
            IsSearchable = isSearchable,
            IsCollapsedByDefault = isCollapsedByDefault,
            ShowCounts = showCounts,
            CreatedAt = now,
        };
}

/// <summary>
/// ردیف میانی facet مؤثر.
/// </summary>
public sealed record CatalogEffectiveFacetBinding(
    Guid DefinitionId,
    CatalogFacetDisplayType DisplayType,
    int SortOrder,
    bool IsVisible,
    bool IsSearchable,
    bool IsCollapsedByDefault,
    bool ShowCounts,
    Guid SourceCategoryId,
    CatalogAttributeDefinition Definition);

/// <summary>
/// اعتبارسنجی نوع نمایش facet بر اساس ValueKind.
/// </summary>
public static class CatalogCategoryFacetRules
{
    /// <summary>
    /// پیشنهاد نوع نمایش بر اساس ValueKind.
    /// </summary>
    public static CatalogFacetDisplayType SuggestDisplayType(CatalogAttributeValueKind valueKind) =>
        valueKind switch
        {
            CatalogAttributeValueKind.Boolean => CatalogFacetDisplayType.BooleanToggle,
            CatalogAttributeValueKind.Number => CatalogFacetDisplayType.Range,
            CatalogAttributeValueKind.Enumeration => CatalogFacetDisplayType.CheckboxList,
            CatalogAttributeValueKind.Text => CatalogFacetDisplayType.SearchableSelect,
            _ => CatalogFacetDisplayType.CheckboxList,
        };

    /// <summary>
    /// اعتبارسنجی ترکیب ValueKind و DisplayType.
    /// </summary>
    public static void ValidateDisplayType(CatalogAttributeDefinition definition, CatalogFacetDisplayType displayType)
    {
        if (displayType == CatalogFacetDisplayType.ColorSwatch)
        {
            throw new InvalidOperationException("نمایش رنگ هنوز به متادیتای رنگ گزینه نیاز دارد؛ از چندانتخابی استفاده کنید.");
        }

        switch (definition.ValueKind)
        {
            case CatalogAttributeValueKind.Boolean:
                if (displayType != CatalogFacetDisplayType.BooleanToggle)
                {
                    throw new InvalidOperationException("برای ویژگی بولی فقط کلید روشن/خاموش مجاز است.");
                }

                break;
            case CatalogAttributeValueKind.Number:
                if (displayType != CatalogFacetDisplayType.Range)
                {
                    throw new InvalidOperationException("برای ویژگی عددی فقط بازه مجاز است.");
                }

                break;
            case CatalogAttributeValueKind.Text:
                if (displayType is CatalogFacetDisplayType.Range or CatalogFacetDisplayType.BooleanToggle or CatalogFacetDisplayType.ColorSwatch)
                {
                    throw new InvalidOperationException("نوع نمایش برای متن مجاز نیست.");
                }

                break;
            case CatalogAttributeValueKind.Enumeration:
                if (displayType is CatalogFacetDisplayType.Range or CatalogFacetDisplayType.BooleanToggle)
                {
                    throw new InvalidOperationException("نوع نمایش برای فهرست گزینه‌ها مجاز نیست.");
                }

                break;
            case CatalogAttributeValueKind.Instant:
                throw new InvalidOperationException("فیلتر برای این نوع تاریخ/زمان هنوز پشتیبانی نمی‌شود.");
        }

        if (displayType == CatalogFacetDisplayType.BooleanToggle && definition.ValueKind != CatalogAttributeValueKind.Boolean)
        {
            throw new InvalidOperationException("کلید روشن/خاموش فقط برای بولی است.");
        }
    }

    /// <summary>
    /// آیا IsSearchable برای این DisplayType مجاز است.
    /// </summary>
    public static bool IsSearchableAllowed(CatalogFacetDisplayType displayType) =>
        displayType is CatalogFacetDisplayType.CheckboxList or CatalogFacetDisplayType.SearchableSelect;
}

/// <summary>
/// حل facet مؤثر رده با ارث والدین؛ eligibility از schema مؤثر IsFilterable.
/// </summary>
public static class CatalogCategoryFacetResolver
{
    /// <summary>
    /// حل facet مؤثر رده با ارث والدین و eligibility از schema.
    /// </summary>
    public static IReadOnlyList<CatalogEffectiveFacetBinding> ResolveEffectiveFacets(
        Guid categoryId,
        IReadOnlyDictionary<Guid, CatalogCategory> categoriesById,
        IReadOnlyList<CatalogCategoryFacetConfiguration> allConfigurations,
        IReadOnlyList<CatalogEffectiveSchemaBinding> effectiveSchema,
        IReadOnlyDictionary<Guid, CatalogAttributeDefinition> definitionsById)
    {
        ArgumentNullException.ThrowIfNull(categoriesById);
        ArgumentNullException.ThrowIfNull(allConfigurations);
        ArgumentNullException.ThrowIfNull(effectiveSchema);
        ArgumentNullException.ThrowIfNull(definitionsById);

        if (!categoriesById.ContainsKey(categoryId))
        {
            throw new InvalidOperationException("رده برای حل facet در Catalog این Tenant نیست.");
        }

        var filterable = effectiveSchema.Where(x => x.IsFilterable).ToDictionary(x => x.DefinitionId);
        if (filterable.Count == 0)
        {
            return Array.Empty<CatalogEffectiveFacetBinding>();
        }

        var ancestry = WalkAncestry(categoryId, categoriesById);
        var merged = new Dictionary<Guid, CatalogCategoryFacetConfiguration>();
        var sourceCategory = new Dictionary<Guid, Guid>();
        foreach (var ancestorId in ancestry)
        {
            foreach (var config in allConfigurations
                         .Where(c => c.CategoryId == ancestorId)
                         .OrderBy(c => c.SortOrder)
                         .ThenBy(c => c.FacetConfigurationId))
            {
                if (!filterable.ContainsKey(config.DefinitionId))
                {
                    continue;
                }

                merged[config.DefinitionId] = config;
                sourceCategory[config.DefinitionId] = ancestorId;
            }
        }

        return merged.Values
            .Select(config =>
            {
                if (!definitionsById.TryGetValue(config.DefinitionId, out var definition))
                {
                    throw new InvalidOperationException("تعریف ویژگی facet در Catalog نیست.");
                }

                return new CatalogEffectiveFacetBinding(
                    config.DefinitionId,
                    config.DisplayType,
                    config.SortOrder,
                    config.IsVisible,
                    config.IsSearchable,
                    config.IsCollapsedByDefault,
                    config.ShowCounts,
                    sourceCategory[config.DefinitionId],
                    definition);
            })
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Definition.Code, StringComparer.Ordinal)
            .ToList();
    }

    private static List<Guid> WalkAncestry(Guid categoryId, IReadOnlyDictionary<Guid, CatalogCategory> categoriesById)
    {
        var chain = new List<Guid>();
        var seen = new HashSet<Guid>();
        var current = categoryId;
        while (true)
        {
            if (!seen.Add(current))
            {
                throw new InvalidOperationException("حلقه در درخت ردهٔ Catalog تشخیص داده شد؛ facet قابل حل نیست.");
            }

            chain.Add(current);
            if (!categoriesById.TryGetValue(current, out var category) || category.ParentCategoryId is not Guid parent)
            {
                break;
            }

            current = parent;
        }

        chain.Reverse();
        return chain;
    }
}

/// <summary>
/// نوع آیتم مگامنو — T009 فقط Category-backed را در Admin Category Workspace مدیریت می‌کند.
/// </summary>
public enum CatalogMegaMenuItemType
{
    /// <summary>پیوند به ردهٔ Catalog.</summary>
    Category = 0,
}

/// <summary>
/// آیتم presentation مگامنو؛ hierarchy منو از ParentMegaMenuItemId جدا از taxonomy رده است.
/// </summary>
public sealed class CatalogMegaMenuItem
{
    /// <summary>شناسهٔ آیتم منو.</summary>
    public Guid MegaMenuItemId { get; init; }

    /// <summary>نوع آیتم.</summary>
    public CatalogMegaMenuItemType ItemType { get; init; }

    /// <summary>ردهٔ مقصد (برای Category-backed).</summary>
    public Guid CategoryId { get; init; }

    /// <summary>والد presentation در درخت منو (نه ParentCategoryId).</summary>
    public Guid? ParentMegaMenuItemId { get; set; }

    /// <summary>ترتیب بین خواهران presentation.</summary>
    public int SortOrder { get; set; }

    /// <summary>نمایش در مگامنو.</summary>
    public bool IsVisible { get; set; }

    /// <summary>برجسته در منو.</summary>
    public bool IsFeatured { get; set; }

    /// <summary>تصویر تبلیغاتی اختیاری.</summary>
    public Guid? ImageMediaAssetId { get; set; }

    /// <summary>آیکن اختیاری.</summary>
    public Guid? IconMediaAssetId { get; set; }

    /// <summary>زمان ایجاد.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>ایجاد آیتم منو برای یک رده.</summary>
    public static CatalogMegaMenuItem BindCategory(
        Guid categoryId,
        Guid? parentMegaMenuItemId,
        int sortOrder,
        bool isVisible,
        bool isFeatured,
        Guid? imageMediaAssetId,
        Guid? iconMediaAssetId,
        DateTimeOffset now) =>
        new()
        {
            MegaMenuItemId = UuidV7.New(),
            ItemType = CatalogMegaMenuItemType.Category,
            CategoryId = categoryId,
            ParentMegaMenuItemId = parentMegaMenuItemId,
            SortOrder = sortOrder,
            IsVisible = isVisible,
            IsFeatured = isFeatured,
            ImageMediaAssetId = imageMediaAssetId,
            IconMediaAssetId = iconMediaAssetId,
            CreatedAt = now,
        };
}

/// <summary>
/// override نمایشی محلی برای آیتم مگامنو.
/// </summary>
public sealed class CatalogMegaMenuItemTranslation
{
    /// <summary>شناسهٔ ترجمه.</summary>
    public Guid MegaMenuItemTranslationId { get; init; }

    /// <summary>آیتم منو.</summary>
    public Guid MegaMenuItemId { get; init; }

    /// <summary>locale نرمال‌شده.</summary>
    public string Locale { get; init; } = string.Empty;

    /// <summary>عنوان متفاوت در مگامنو.</summary>
    public string? TitleOverride { get; set; }

    /// <summary>متن badge اختیاری.</summary>
    public string? BadgeText { get; set; }

    /// <summary>برچسب کوتاه اختیاری.</summary>
    public string? ShortLabel { get; set; }

    /// <summary>ایجاد یا به‌روزرسانی override.</summary>
    public static CatalogMegaMenuItemTranslation Create(
        Guid megaMenuItemId,
        string locale,
        string? titleOverride,
        string? badgeText,
        string? shortLabel) =>
        new()
        {
            MegaMenuItemTranslationId = UuidV7.New(),
            MegaMenuItemId = megaMenuItemId,
            Locale = locale.Trim(),
            TitleOverride = string.IsNullOrWhiteSpace(titleOverride) ? null : titleOverride.Trim(),
            BadgeText = string.IsNullOrWhiteSpace(badgeText) ? null : badgeText.Trim(),
            ShortLabel = string.IsNullOrWhiteSpace(shortLabel) ? null : shortLabel.Trim(),
        };
}

/// <summary>
/// اعتبارسنجی placement مگامنو — جدا از درخت taxonomy.
/// </summary>
public static class CatalogMegaMenuTreeRules
{
    /// <summary>حداکثر عمق presentation (L1/L2/L3).</summary>
    public const int MaxPresentationDepth = 3;

    /// <summary>
    /// والد و عمق presentation را بررسی می‌کند.
    /// </summary>
    public static void ValidatePlacement(
        Guid megaMenuItemId,
        Guid? parentMegaMenuItemId,
        IReadOnlyDictionary<Guid, CatalogMegaMenuItem> itemsById)
    {
        if (parentMegaMenuItemId is null)
        {
            return;
        }

        if (parentMegaMenuItemId == megaMenuItemId)
        {
            throw new InvalidOperationException("آیتم منو نمی‌تواند والد خودش باشد.");
        }

        if (!itemsById.ContainsKey(parentMegaMenuItemId.Value))
        {
            throw new InvalidOperationException("والد presentation در مگامنو یافت نشد.");
        }

        var depth = 1;
        var current = parentMegaMenuItemId.Value;
        var seen = new HashSet<Guid> { megaMenuItemId };
        while (true)
        {
            if (!seen.Add(current))
            {
                throw new InvalidOperationException("حلقه در درخت presentation مگامنو.");
            }

            depth++;
            if (depth > MaxPresentationDepth)
            {
                throw new InvalidOperationException("حداکثر سه سطح در مگامنو پشتیبانی می‌شود.");
            }

            if (!itemsById.TryGetValue(current, out var parent) || parent.ParentMegaMenuItemId is not Guid next)
            {
                break;
            }

            current = next;
        }
    }
}

/// <summary>
/// حل عنوان و eligibility آیتم مگامنو برای locale.
/// </summary>
public static class CatalogMegaMenuComposer
{
    /// <summary>
    /// آیتم‌های قابل نمایش در ویترین را فیلتر و مرتب می‌کند.
    /// </summary>
    public static IReadOnlyList<CatalogMegaMenuRenderableItem> ComposeStorefrontMenu(
        IReadOnlyList<CatalogMegaMenuItem> items,
        IReadOnlyDictionary<Guid, CatalogCategory> categoriesById,
        IReadOnlyDictionary<Guid, CatalogCategoryTranslation> translationsByCategoryId,
        IReadOnlyDictionary<Guid, CatalogMegaMenuItemTranslation> overridesByItemId,
        string locale,
        string uiLocaleSegment)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(categoriesById);
        ArgumentNullException.ThrowIfNull(translationsByCategoryId);
        ArgumentNullException.ThrowIfNull(overridesByItemId);

        var normalizedLocale = locale.Trim();
        var result = new List<CatalogMegaMenuRenderableItem>();
        foreach (var item in items.Where(x => x.IsVisible && x.ItemType == CatalogMegaMenuItemType.Category))
        {
            if (!categoriesById.TryGetValue(item.CategoryId, out var category))
            {
                continue;
            }

            if (category.Status != CatalogPublicationStatus.Published || !category.IsVisible)
            {
                continue;
            }

            if (!translationsByCategoryId.TryGetValue(item.CategoryId, out var translation)
                || string.IsNullOrWhiteSpace(translation.Slug))
            {
                continue;
            }

            overridesByItemId.TryGetValue(item.MegaMenuItemId, out var overrideRow);
            var title = overrideRow?.TitleOverride ?? translation.Name;
            if (string.IsNullOrWhiteSpace(title))
            {
                continue;
            }

            var destination = $"/{uiLocaleSegment}/category/{translation.Slug.Trim()}";
            result.Add(new CatalogMegaMenuRenderableItem(
                item.MegaMenuItemId,
                item.ParentMegaMenuItemId,
                item.CategoryId,
                title,
                destination,
                item.IsFeatured,
                item.IconMediaAssetId ?? category.IconMediaAssetId,
                item.ImageMediaAssetId ?? category.ImageMediaAssetId,
                item.SortOrder));
        }

        return result
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Title, StringComparer.Ordinal)
            .ToList();
    }
}

/// <summary>ردیف renderable مگامنو برای Storefront.</summary>
public sealed record CatalogMegaMenuRenderableItem(
    Guid MegaMenuItemId,
    Guid? ParentMegaMenuItemId,
    Guid CategoryId,
    string Title,
    string Destination,
    bool IsFeatured,
    Guid? IconMediaAssetId,
    Guid? ImageMediaAssetId,
    int SortOrder);

/// <summary>
/// محورهای Variant انتخاب‌شده برای یک محصول. ماتریس کامل ترکیبی اینجا تولید نمی‌شود.
/// </summary>
public sealed class CatalogProductVariantAxis
{
    /// <summary>
    /// شناسهٔ ردیف.
    /// </summary>
    public Guid AxisId { get; init; }

    /// <summary>
    /// محصول مالک محورهای انتخاب‌شده.
    /// </summary>
    public Guid ProductId { get; init; }

    /// <summary>
    /// تعریف مجاز محور (باید IsVariantAxis=true باشد).
    /// </summary>
    public Guid DefinitionId { get; init; }

    /// <summary>
    /// ترتیب محور در هویت ترکیب.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// ردیف محور محصول می‌سازد.
    /// </summary>
    public static CatalogProductVariantAxis Create(Guid productId, Guid definitionId, int displayOrder) =>
        new()
        {
            AxisId = UuidV7.New(),
            ProductId = productId,
            DefinitionId = definitionId,
            DisplayOrder = displayOrder,
        };
}

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

/// <summary>
/// نقش پیوند محصول↔رده: دسته اصلی (schema) یا اضافی (کشف/PLP).
/// </summary>
public enum CatalogProductCategoryRole : byte
{
    /// <summary>دسته اصلی — منبع schema و breadcrumb.</summary>
    Primary = 0,

    /// <summary>دسته اضافی — فقط کشف/ناوبری/PLP.</summary>
    Additional = 1,
}

/// <summary>
/// پیوند محصول به رده. merchandising فروشنده نیست.
/// </summary>
public sealed class CatalogProductCategory
{
    /// <summary>
    /// شناسهٔ پیوند.
    /// </summary>
    public Guid AssignmentId { get; init; }

    /// <summary>
    /// محصول Catalog.
    /// </summary>
    public Guid ProductId { get; init; }

    /// <summary>
    /// ردهٔ Catalog.
    /// </summary>
    public Guid CategoryId { get; init; }

    /// <summary>
    /// نقش پیوند (اصلی / اضافی).
    /// </summary>
    public CatalogProductCategoryRole Role { get; init; }

    /// <summary>
    /// پیوند می‌سازد.
    /// </summary>
    public static CatalogProductCategory Assign(
        Guid productId,
        Guid categoryId,
        CatalogProductCategoryRole role = CatalogProductCategoryRole.Primary) =>
        new()
        {
            AssignmentId = UuidV7.New(),
            ProductId = productId,
            CategoryId = categoryId,
            Role = role,
        };
}

/// <summary>
/// مرجع رسانهٔ مات. FK به جدول Media ماژول دیگر نیست و باینری ذخیره نمی‌شود.
/// </summary>
public sealed class CatalogProductMediaReference
{
    /// <summary>
    /// شناسهٔ ردیف مرجع.
    /// </summary>
    public Guid ReferenceId { get; init; }

    /// <summary>
    /// محصول مالک مرجع.
    /// </summary>
    public Guid ProductId { get; init; }

    /// <summary>
    /// شناسهٔ مات دارایی در قابلیت Media آینده.
    /// </summary>
    public Guid MediaAssetId { get; init; }

    /// <summary>
    /// ترتیب نمایش گالری داخل محصول.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// تصویر اصلی فهرست/بند انگشتی.
    /// </summary>
    public bool IsPrimary { get; set; }

    /// <summary>
    /// متن جایگزین دسترس‌پذیری؛ باینری نیست.
    /// </summary>
    public string? AltText { get; set; }

    /// <summary>
    /// مرجع مات می‌سازد.
    /// </summary>
    public static CatalogProductMediaReference Link(
        Guid productId,
        Guid mediaAssetId,
        int displayOrder = 0,
        bool isPrimary = false,
        string? altText = null) =>
        new()
        {
            ReferenceId = UuidV7.New(),
            ProductId = productId,
            MediaAssetId = mediaAssetId,
            DisplayOrder = displayOrder,
            IsPrimary = isPrimary,
            AltText = string.IsNullOrWhiteSpace(altText) ? null : altText.Trim(),
        };
}

/// <summary>
/// مقدار ویژگی روی محصول (غیرمحور Variant). مبلغ و موجودی نیست.
/// </summary>
public sealed class CatalogProductAttributeValue
{
    /// <summary>
    /// شناسهٔ مقدار.
    /// </summary>
    public Guid ValueId { get; init; }

    /// <summary>
    /// محصول.
    /// </summary>
    public Guid ProductId { get; init; }

    /// <summary>
    /// تعریف ویژگی.
    /// </summary>
    public Guid DefinitionId { get; init; }

    /// <summary>
    /// مقدار نرمال‌شده برای مقایسهٔ نوعی.
    /// </summary>
    public string CanonicalValue { get; init; } = "";

    /// <summary>
    /// مقدار محصول می‌سازد.
    /// </summary>
    public static CatalogProductAttributeValue Create(Guid productId, Guid definitionId, string canonicalValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(canonicalValue);
        return new CatalogProductAttributeValue
        {
            ValueId = UuidV7.New(),
            ProductId = productId,
            DefinitionId = definitionId,
            CanonicalValue = canonicalValue.Trim(),
        };
    }
}

/// <summary>
/// مقدار محور Variant. ترکیب این مقادیر هویت Offer فروشنده نیست.
/// </summary>
public sealed class CatalogVariantAttributeValue
{
    /// <summary>
    /// شناسهٔ مقدار.
    /// </summary>
    public Guid ValueId { get; init; }

    /// <summary>
    /// گونهٔ Catalog.
    /// </summary>
    public Guid VariantId { get; init; }

    /// <summary>
    /// تعریف محور.
    /// </summary>
    public Guid DefinitionId { get; init; }

    /// <summary>
    /// مقدار نرمال ترکیب.
    /// </summary>
    public string CanonicalValue { get; init; } = "";

    /// <summary>
    /// مقدار محور می‌سازد.
    /// </summary>
    public static CatalogVariantAttributeValue Create(Guid variantId, Guid definitionId, string canonicalValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(canonicalValue);
        return new CatalogVariantAttributeValue
        {
            ValueId = UuidV7.New(),
            VariantId = variantId,
            DefinitionId = definitionId,
            CanonicalValue = canonicalValue.Trim(),
        };
    }
}

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

/// <summary>
/// محصول توصیفی Catalog. قیمت، موجودی، فروشنده و Offer داخل این ریشه نیستند.
/// </summary>
public sealed class CatalogProduct : IHasDomainEvents
{
    private readonly DomainEventCollector _domainEvents = new();

    /// <summary>
    /// شناسهٔ پایدار محصول توصیفی.
    /// </summary>
    public Guid ProductId { get; init; }

    /// <summary>
    /// درز گونه برای schema ویژگی.
    /// </summary>
    public CatalogProductKind Kind { get; init; }

    /// <summary>
    /// وضعیت انتشار Catalog نه قابلیت خرید Offer.
    /// </summary>
    public CatalogPublicationStatus Status { get; set; }

    /// <summary>
    /// برند اختیاری تحریری.
    /// </summary>
    public Guid? BrandId { get; set; }

    /// <summary>
    /// درز slug برای مسیر SEO بعدی؛ سیاست index/robots اینجا نیست.
    /// </summary>
    public string? SlugSeam { get; set; }

    /// <summary>
    /// درز عنوان SEO محتوایی؛ موتور SEO جدا است.
    /// </summary>
    public string? SeoTitleSeam { get; set; }

    /// <summary>
    /// زمان ایجاد.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// زمان به‌روزرسانی.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// انتساب رده‌ها.
    /// </summary>
    public List<CatalogProductCategory> CategoryAssignments { get; } = [];

    /// <summary>
    /// گونه‌ها.
    /// </summary>
    public List<CatalogVariant> Variants { get; } = [];

    /// <inheritdoc />
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.Events;

    /// <summary>
    /// محصول توصیفی می‌سازد بدون فیلد تجاری.
    /// </summary>
    public static CatalogProduct Create(CatalogProductKind kind, string? slugSeam, DateTimeOffset now)
    {
        var product = new CatalogProduct
        {
            ProductId = UuidV7.New(),
            Kind = kind,
            Status = CatalogPublicationStatus.Draft,
            SlugSeam = string.IsNullOrWhiteSpace(slugSeam) ? null : slugSeam.Trim().ToLowerInvariant(),
            CreatedAt = now,
            UpdatedAt = now,
        };
        product._domainEvents.Add(new CatalogProductCreatedDomainEvent(product));
        return product;
    }

    /// <summary>
    /// انتشار تحریری. Offer را قابل‌خرید نمی‌کند.
    /// فقط از پیش‌نویس؛ تکرار روی Published بی‌اثر است؛ Archived -> Published ممنوع است.
    /// </summary>
    public void Publish(DateTimeOffset now)
    {
        if (Status == CatalogPublicationStatus.Published)
        {
            return;
        }

        if (Status == CatalogPublicationStatus.Archived)
        {
            throw new InvalidOperationException(ProductPublishRules.MessageRestoreBeforePublishFa);
        }

        Status = CatalogPublicationStatus.Published;
        UpdatedAt = now;
        _domainEvents.Add(new CatalogProductPublishedDomainEvent(this));
    }

    /// <summary>
    /// لغو انتشار تحریری به پیش‌نویس. آرشیو جدا می‌ماند.
    /// </summary>
    public void Unpublish(DateTimeOffset now)
    {
        if (Status == CatalogPublicationStatus.Archived)
        {
            throw new InvalidOperationException("محصول آرشیو شده را با لغو انتشار به پیش‌نویس برنمی‌گردانیم.");
        }

        if (Status == CatalogPublicationStatus.Draft)
        {
            return;
        }

        Status = CatalogPublicationStatus.Draft;
        UpdatedAt = now;
        _domainEvents.Add(new CatalogProductUpdatedDomainEvent(this));
    }

    /// <summary>
    /// آرشیو تحریری. حذف سخت نیست و با Offer قاطی نمی‌شود.
    /// </summary>
    public void Archive(DateTimeOffset now)
    {
        if (Status == CatalogPublicationStatus.Archived)
        {
            return;
        }

        Status = CatalogPublicationStatus.Archived;
        UpdatedAt = now;
        _domainEvents.Add(new CatalogProductUpdatedDomainEvent(this));
    }

    /// <summary>
    /// بازیابی صریح از بایگانی به پیش‌نویس؛ حذف سخت نیست و Offer را جهش نمی‌دهد.
    /// </summary>
    public void RestoreFromArchive(DateTimeOffset now)
    {
        if (Status != CatalogPublicationStatus.Archived)
        {
            throw new InvalidOperationException("فقط محصول بایگانی‌شده را می‌توان به پیش‌نویس بازگرداند.");
        }

        Status = CatalogPublicationStatus.Draft;
        UpdatedAt = now;
        _domainEvents.Add(new CatalogProductUpdatedDomainEvent(this));
    }

    /// <summary>
    /// به‌روزرسانی درزهای غیرتجاری (slug/SEO). Brand از مسیر اختصاصی AssignBrand تنظیم می‌شود.
    /// </summary>
    public void TouchDescriptiveSeams(string? slugSeam, string? seoTitleSeam, Guid? brandId, DateTimeOffset now)
    {
        SlugSeam = string.IsNullOrWhiteSpace(slugSeam) ? SlugSeam : slugSeam.Trim().ToLowerInvariant();
        SeoTitleSeam = string.IsNullOrWhiteSpace(seoTitleSeam) ? SeoTitleSeam : seoTitleSeam.Trim();
        BrandId = brandId ?? BrandId;
        UpdatedAt = now;
        _domainEvents.Add(new CatalogProductUpdatedDomainEvent(this));
    }

    /// <summary>
    /// انتساب یا حذف برند Catalog برای محصول (شامل پاک‌کردن با null).
    /// </summary>
    public void AssignBrand(Guid? brandId, DateTimeOffset now)
    {
        BrandId = brandId;
        UpdatedAt = now;
        _domainEvents.Add(new CatalogProductUpdatedDomainEvent(this));
    }

    /// <inheritdoc />
    public void ClearDomainEvents() => _domainEvents.Clear();
}

/// <summary>
/// رویداد دامنهٔ ایجاد محصول. ایندکس Search اینجا اجرا نمی‌شود.
/// </summary>
public sealed class CatalogProductCreatedDomainEvent : IDomainEvent
{
    /// <summary>
    /// رویداد را از ریشه می‌سازد.
    /// </summary>
    public CatalogProductCreatedDomainEvent(CatalogProduct product)
    {
        ArgumentNullException.ThrowIfNull(product);
        ProductId = product.ProductId;
        Metadata = EventMetadataFactory.ForDomain("catalog.product_created.domain");
    }

    /// <summary>
    /// محصول ایجادشده.
    /// </summary>
    public Guid ProductId { get; }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }
}

/// <summary>
/// رویداد انتشار Catalog. خرید Offer را تضمین نمی‌کند.
/// </summary>
public sealed class CatalogProductPublishedDomainEvent : IDomainEvent
{
    /// <summary>
    /// رویداد انتشار.
    /// </summary>
    public CatalogProductPublishedDomainEvent(CatalogProduct product)
    {
        ArgumentNullException.ThrowIfNull(product);
        ProductId = product.ProductId;
        Metadata = EventMetadataFactory.ForDomain("catalog.product_published.domain");
    }

    /// <summary>
    /// محصول منتشرشده در Catalog.
    /// </summary>
    public Guid ProductId { get; }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }
}

/// <summary>
/// رویداد به‌روزرسانی توصیفی برای تصویر بعدی Search.
/// </summary>
public sealed class CatalogProductUpdatedDomainEvent : IDomainEvent
{
    /// <summary>
    /// رویداد به‌روزرسانی.
    /// </summary>
    public CatalogProductUpdatedDomainEvent(CatalogProduct product)
    {
        ArgumentNullException.ThrowIfNull(product);
        ProductId = product.ProductId;
        Metadata = EventMetadataFactory.ForDomain("catalog.product_updated.domain");
    }

    /// <summary>
    /// محصول تغییر یافته.
    /// </summary>
    public Guid ProductId { get; }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }
}

/// <summary>
/// رویداد ایجاد گونه. هویت Offer فروشنده نیست.
/// </summary>
public sealed class CatalogVariantCreatedDomainEvent : IDomainEvent
{
    /// <summary>
    /// رویداد ایجاد گونه.
    /// </summary>
    public CatalogVariantCreatedDomainEvent(CatalogVariant variant)
    {
        ArgumentNullException.ThrowIfNull(variant);
        VariantId = variant.VariantId;
        ProductId = variant.ProductId;
        Metadata = EventMetadataFactory.ForDomain("catalog.variant_created.domain");
    }

    /// <summary>
    /// گونهٔ ایجادشده.
    /// </summary>
    public Guid VariantId { get; }

    /// <summary>
    /// محصول والد.
    /// </summary>
    public Guid ProductId { get; }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }
}

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

/// <summary>
/// ردیف میانی حل schema مؤثر قبل از DTO لایهٔ Application.
/// </summary>
public sealed record CatalogEffectiveSchemaBinding(
    Guid DefinitionId,
    int DisplayOrder,
    bool IsRequired,
    bool IsFilterable,
    bool IsVariantAxis,
    bool IsComparable,
    Guid InheritedFromCategoryId,
    CatalogAttributeDefinition Definition,
    /// <summary>اگر فرزند override محلی دارد، نزدیک‌ترین والدِ منبع قبل از override.</summary>
    Guid? OverriddenFromCategoryId = null);

/// <summary>
/// گزارش تأثیر تغییر رده بدون حذف خاموش مقادیر.
/// </summary>
public sealed record CatalogCategoryChangeImpactReport(
    IReadOnlyList<(Guid DefinitionId, string CanonicalValue)> OrphanAttributeValues,
    IReadOnlyList<Guid> InvalidVariantAxisDefinitionIds);

/// <summary>
/// حل schema مؤثر رده با ارث از والدین؛ حلقهٔ درخت تشخیص داده می‌شود.
/// </summary>
public static class CatalogCategorySchemaResolver
{
    /// <summary>
    /// از ردهٔ هدف به ریشه راه می‌رود، پیوندها را ادغام می‌کند (فرزند روی همان DefinitionId غالب است)،
    /// و فهرست مرتب با فرادادهٔ تعریف برمی‌گرداند.
    /// </summary>
    public static IReadOnlyList<CatalogEffectiveSchemaBinding> ResolveEffectiveSchema(
        Guid categoryId,
        IReadOnlyDictionary<Guid, CatalogCategory> categoriesById,
        IReadOnlyList<CatalogCategoryAttributeBinding> allBindings,
        IReadOnlyDictionary<Guid, CatalogAttributeDefinition> definitionsById)
    {
        ArgumentNullException.ThrowIfNull(categoriesById);
        ArgumentNullException.ThrowIfNull(allBindings);
        ArgumentNullException.ThrowIfNull(definitionsById);

        if (!categoriesById.ContainsKey(categoryId))
        {
            throw new InvalidOperationException("رده برای حل schema در Catalog این Tenant نیست.");
        }

        var ancestry = WalkAncestry(categoryId, categoriesById);
        // از ریشه به فرزند: فرزند override می‌کند.
        var merged = new Dictionary<Guid, CatalogCategoryAttributeBinding>();
        var inheritedFrom = new Dictionary<Guid, Guid>();
        var overriddenFrom = new Dictionary<Guid, Guid?>();
        foreach (var ancestorId in ancestry)
        {
            foreach (var binding in allBindings.Where(b => b.CategoryId == ancestorId)
                         .OrderBy(b => b.DisplayOrder)
                         .ThenBy(b => b.BindingId))
            {
                if (merged.ContainsKey(binding.DefinitionId))
                {
                    overriddenFrom[binding.DefinitionId] = inheritedFrom[binding.DefinitionId];
                }
                else if (!overriddenFrom.ContainsKey(binding.DefinitionId))
                {
                    overriddenFrom[binding.DefinitionId] = null;
                }

                merged[binding.DefinitionId] = binding;
                inheritedFrom[binding.DefinitionId] = ancestorId;
            }
        }

        return merged.Values
            .Select(binding =>
            {
                if (!definitionsById.TryGetValue(binding.DefinitionId, out var definition))
                {
                    throw new InvalidOperationException("تعریف ویژگی پیوندشده در Catalog نیست.");
                }

                var isVariantAxis = binding.IsVariantAxis && definition.IsVariantAxisAllowed;
                var sourceCategoryId = inheritedFrom[binding.DefinitionId];
                var priorAncestor = overriddenFrom.GetValueOrDefault(binding.DefinitionId);
                // override محلی فقط وقتی منبع نهایی خودِ رده است و قبلاً از والد آمده.
                var localOverrideFrom = sourceCategoryId == categoryId ? priorAncestor : null;
                return new CatalogEffectiveSchemaBinding(
                    binding.DefinitionId,
                    binding.DisplayOrder,
                    binding.IsRequired,
                    binding.IsFilterable,
                    isVariantAxis,
                    binding.IsComparable,
                    sourceCategoryId,
                    definition,
                    localOverrideFrom);
            })
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Definition.Code, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// مقادیر محصول و محورهایی که در schema جدید جایی ندارند را فهرست می‌کند؛ حذف نمی‌کند.
    /// </summary>
    public static CatalogCategoryChangeImpactReport PreviewCategoryChange(
        IReadOnlyList<CatalogProductAttributeValue> productValues,
        IReadOnlyList<CatalogProductVariantAxis> productAxes,
        IReadOnlyList<CatalogEffectiveSchemaBinding> newEffectiveSchema)
    {
        ArgumentNullException.ThrowIfNull(productValues);
        ArgumentNullException.ThrowIfNull(productAxes);
        ArgumentNullException.ThrowIfNull(newEffectiveSchema);

        var allowed = newEffectiveSchema.Select(x => x.DefinitionId).ToHashSet();
        var orphans = productValues
            .Where(v => !allowed.Contains(v.DefinitionId))
            .Select(v => (v.DefinitionId, v.CanonicalValue))
            .ToList();
        var invalidAxes = productAxes
            .Where(a => !allowed.Contains(a.DefinitionId))
            .Select(a => a.DefinitionId)
            .Distinct()
            .ToList();
        return new CatalogCategoryChangeImpactReport(orphans, invalidAxes);
    }

    private static List<Guid> WalkAncestry(Guid categoryId, IReadOnlyDictionary<Guid, CatalogCategory> categoriesById)
    {
        var chain = new List<Guid>();
        var seen = new HashSet<Guid>();
        var current = categoryId;
        while (true)
        {
            if (!seen.Add(current))
            {
                throw new InvalidOperationException("حلقه در درخت ردهٔ Catalog تشخیص داده شد؛ schema قابل حل نیست.");
            }

            chain.Add(current);
            if (!categoriesById.TryGetValue(current, out var category) || category.ParentCategoryId is not Guid parent)
            {
                break;
            }

            current = parent;
        }

        chain.Reverse();
        return chain;
    }
}
