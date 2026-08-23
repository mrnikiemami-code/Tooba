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
}

/// <summary>
/// ردهٔ طبقه‌بندی Catalog. درخت ناوبری فروشگاه عمومی نیست و قیمت ندارد.
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
    public static CatalogCategory Create(Guid? parentCategoryId, DateTimeOffset now)
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
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    /// <summary>
    /// والد را عوض می‌کند بدون join بیرون از Catalog.
    /// </summary>
    public void Reparent(Guid? parentCategoryId, DateTimeOffset now)
    {
        if (parentCategoryId == CategoryId)
        {
            throw new InvalidOperationException("رده نمی‌تواند والد خودش باشد؛ حلقهٔ درخت طبقه‌بندی ممنوع است.");
        }

        ParentCategoryId = parentCategoryId;
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
    /// اگر true باشد محور ترکیب Variant است نه مشخصات سادهٔ محصول.
    /// </summary>
    public bool IsVariantAxis { get; init; }

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
            CreatedAt = now,
        };
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
    /// گزینه می‌سازد.
    /// </summary>
    public static CatalogAttributeOption Create(Guid definitionId, string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        return new CatalogAttributeOption
        {
            OptionId = UuidV7.New(),
            DefinitionId = definitionId,
            Code = code.Trim().ToLowerInvariant(),
        };
    }
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
    /// پیوند می‌سازد.
    /// </summary>
    public static CatalogProductCategory Assign(Guid productId, Guid categoryId) =>
        new()
        {
            AssignmentId = UuidV7.New(),
            ProductId = productId,
            CategoryId = categoryId,
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
    /// مرجع مات می‌سازد.
    /// </summary>
    public static CatalogProductMediaReference Link(Guid productId, Guid mediaAssetId) =>
        new()
        {
            ReferenceId = UuidV7.New(),
            ProductId = productId,
            MediaAssetId = mediaAssetId,
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
            throw new InvalidOperationException("گونه باید حداقل یک محور ویژگی داشته باشد تا با Product ساده قاطی نشود.");
        }

        return string.Join("|", parts);
    }

    /// <summary>
    /// گونه می‌سازد و رویداد ایجاد را برای تصویر Search آینده صف می‌کند نه برای ایندکس کردن همین‌جا.
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
            CreatedAt = now,
            UpdatedAt = now,
        };
        variant._domainEvents.Add(new CatalogVariantCreatedDomainEvent(variant));
        return variant;
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
    /// </summary>
    public void Publish(DateTimeOffset now)
    {
        Status = CatalogPublicationStatus.Published;
        UpdatedAt = now;
        _domainEvents.Add(new CatalogProductPublishedDomainEvent(this));
    }

    /// <summary>
    /// به‌روزرسانی درزهای غیرتجاری.
    /// </summary>
    public void TouchDescriptiveSeams(string? slugSeam, string? seoTitleSeam, Guid? brandId, DateTimeOffset now)
    {
        SlugSeam = string.IsNullOrWhiteSpace(slugSeam) ? SlugSeam : slugSeam.Trim().ToLowerInvariant();
        SeoTitleSeam = string.IsNullOrWhiteSpace(seoTitleSeam) ? SeoTitleSeam : seoTitleSeam.Trim();
        BrandId = brandId ?? BrandId;
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
}
