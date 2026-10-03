using Tooba.BuildingBlocks;

namespace Tooba.Catalog.Domain.Aggregates;

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

    /// <summary>واحد اندازه‌گیری محصول؛ Variant تکرار نمی‌کند.</summary>
    public Guid UnitOfMeasureId { get; private set; } = CanonicalUnits.Pcs;

    /// <summary>تعداد رقم اعشار مقدار؛ حداکثر ۶.</summary>
    public int QuantityDecimalPlaces { get; private set; }

    /// <summary>گام اختیاری مقدار.</summary>
    public decimal? QuantityStep { get; private set; }

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
            UnitOfMeasureId = CanonicalUnits.Pcs,
            QuantityDecimalPlaces = 0,
            QuantityStep = null,
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

    /// <summary>سیاست مقدار کالا را روی Product می‌گذارد؛ Variant تکرار نمی‌کند.</summary>
    public void SetQuantityPolicy(Guid unitOfMeasureId, int decimalPlaces, decimal? step, DateTimeOffset now)
    {
        if (unitOfMeasureId == Guid.Empty)
        {
            throw new InvalidOperationException("quantity.unit.required");
        }

        if (decimalPlaces is < 0 or > 6)
        {
            throw new InvalidOperationException("quantity.decimal_places.invalid");
        }

        if (step is { } value)
        {
            if (value <= 0)
            {
                throw new InvalidOperationException("quantity.step.invalid");
            }

            var scale = BitConverter.GetBytes(decimal.GetBits(value)[3])[2];
            if (scale > decimalPlaces)
            {
                throw new InvalidOperationException("quantity.step.incompatible_precision");
            }
        }

        UnitOfMeasureId = unitOfMeasureId;
        QuantityDecimalPlaces = decimalPlaces;
        QuantityStep = step;
        UpdatedAt = now;
        _domainEvents.Add(new CatalogProductUpdatedDomainEvent(this));
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
