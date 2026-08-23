using Tooba.BuildingBlocks;

namespace Tooba.Offer.Domain;

/// <summary>
/// وضعیت تجاری Offer. موجودی، اعتبار قیمت، و انتشار Catalog را نشان نمی‌دهد.
/// </summary>
public enum OfferStatus
{
    /// <summary>
    /// پیش‌نویس listing. قابل‌خرید بودن را تضمین نمی‌کند.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Offer برای کانال فعال است؛ هنوز Price/Stock جدا هستند.
    /// </summary>
    Active = 1,

    /// <summary>
    /// تعلیق تجاری فروشنده/کانال. موجودی صفر نیست.
    /// </summary>
    Suspended = 2,

    /// <summary>
    /// بایگانی listing. حذف Product نیست.
    /// </summary>
    Archived = 3,
}

/// <summary>
/// کانال فروش پایدار. رشتهٔ UI آزاد نیست.
/// </summary>
public enum SalesChannel
{
    /// <summary>
    /// فروش مستقیم فروشگاه.
    /// </summary>
    Direct = 0,

    /// <summary>
    /// کانال Marketplace چندفروشنده.
    /// </summary>
    Marketplace = 1,

    /// <summary>
    /// کانال نمایندگی.
    /// </summary>
    Agency = 2,

    /// <summary>
    /// کانال سازمانی.
    /// </summary>
    Corporate = 3,

    /// <summary>
    /// کانال همکاری در فروش.
    /// </summary>
    Affiliate = 4,

    /// <summary>
    /// کانال API یکپارچه.
    /// </summary>
    Api = 5,
}

/// <summary>
/// listing تجاری فروشنده روی یک Variant Catalog. قیمت و موجودی ندارد.
/// </summary>
public sealed class SellerOffer : IHasDomainEvents
{
    private readonly DomainEventCollector _domainEvents = new();

    /// <summary>
    /// شناسهٔ پایدار Offer.
    /// </summary>
    public Guid OfferId { get; init; }

    /// <summary>
    /// شناسهٔ مات Variant Catalog؛ FK بین‌ماژولی نیست.
    /// </summary>
    public Guid CatalogVariantId { get; init; }

    /// <summary>
    /// Party سازمان فروشنده. UserId ورود نیست.
    /// </summary>
    public Guid SellerPartyId { get; init; }

    /// <summary>
    /// SKU اختصاصی فروشنده؛ کد Variant Catalog نیست.
    /// </summary>
    public string? SellerSku { get; set; }

    /// <summary>
    /// وضعیت listing.
    /// </summary>
    public OfferStatus Status { get; set; }

    /// <summary>
    /// کانال فروش.
    /// </summary>
    public SalesChannel Channel { get; init; }

    /// <summary>
    /// زمان ایجاد.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// زمان به‌روزرسانی.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <inheritdoc />
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.Events;

    /// <summary>
    /// Offer می‌سازد بدون مبلغ و موجودی.
    /// </summary>
    public static SellerOffer Create(
        Guid catalogVariantId,
        Guid sellerPartyId,
        SalesChannel channel,
        string? sellerSku,
        DateTimeOffset now)
    {
        var offer = new SellerOffer
        {
            OfferId = UuidV7.New(),
            CatalogVariantId = catalogVariantId,
            SellerPartyId = sellerPartyId,
            Channel = channel,
            SellerSku = string.IsNullOrWhiteSpace(sellerSku) ? null : sellerSku.Trim(),
            Status = OfferStatus.Draft,
            CreatedAt = now,
            UpdatedAt = now,
        };
        offer._domainEvents.Add(new OfferCreatedDomainEvent(offer));
        return offer;
    }

    /// <summary>
    /// listing را فعال می‌کند. اعتبار Price/Stock را اعلام نمی‌کند.
    /// </summary>
    public void Activate(DateTimeOffset now)
    {
        if (Status == OfferStatus.Archived)
        {
            throw new InvalidOperationException("Offer بایگانی‌شده دوباره فعال نمی‌شود؛ listing جدید بسازید.");
        }

        Status = OfferStatus.Active;
        UpdatedAt = now;
        _domainEvents.Add(new OfferActivatedDomainEvent(this));
    }

    /// <summary>
    /// listing را معلق می‌کند.
    /// </summary>
    public void Suspend(DateTimeOffset now)
    {
        Status = OfferStatus.Suspended;
        UpdatedAt = now;
        _domainEvents.Add(new OfferSuspendedDomainEvent(this));
    }

    /// <summary>
    /// listing را بایگانی می‌کند تا جای همان فروشنده+گونه+کانال آزاد شود.
    /// </summary>
    public void Archive(DateTimeOffset now)
    {
        Status = OfferStatus.Archived;
        UpdatedAt = now;
        _domainEvents.Add(new OfferArchivedDomainEvent(this));
    }

    /// <inheritdoc />
    public void ClearDomainEvents() => _domainEvents.Clear();
}

/// <summary>
/// رویداد ایجاد Offer.
/// </summary>
public sealed class OfferCreatedDomainEvent : IDomainEvent
{
    /// <summary>
    /// از ریشه می‌سازد.
    /// </summary>
    public OfferCreatedDomainEvent(SellerOffer offer)
    {
        ArgumentNullException.ThrowIfNull(offer);
        OfferId = offer.OfferId;
        CatalogVariantId = offer.CatalogVariantId;
        SellerPartyId = offer.SellerPartyId;
        Metadata = EventMetadataFactory.ForDomain("offer.created.domain");
    }

    /// <summary>
    /// Offer ایجادشده.
    /// </summary>
    public Guid OfferId { get; }

    /// <summary>
    /// Variant هدف.
    /// </summary>
    public Guid CatalogVariantId { get; }

    /// <summary>
    /// فروشندهٔ Party.
    /// </summary>
    public Guid SellerPartyId { get; }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }
}

/// <summary>
/// رویداد فعال‌سازی listing.
/// </summary>
public sealed class OfferActivatedDomainEvent : IDomainEvent
{
    /// <summary>
    /// از ریشه می‌سازد.
    /// </summary>
    public OfferActivatedDomainEvent(SellerOffer offer)
    {
        ArgumentNullException.ThrowIfNull(offer);
        OfferId = offer.OfferId;
        Metadata = EventMetadataFactory.ForDomain("offer.activated.domain");
    }

    /// <summary>
    /// Offer فعال‌شده.
    /// </summary>
    public Guid OfferId { get; }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }
}

/// <summary>
/// رویداد تعلیق listing.
/// </summary>
public sealed class OfferSuspendedDomainEvent : IDomainEvent
{
    /// <summary>
    /// از ریشه می‌سازد.
    /// </summary>
    public OfferSuspendedDomainEvent(SellerOffer offer)
    {
        ArgumentNullException.ThrowIfNull(offer);
        OfferId = offer.OfferId;
        Metadata = EventMetadataFactory.ForDomain("offer.suspended.domain");
    }

    /// <summary>
    /// Offer معلق.
    /// </summary>
    public Guid OfferId { get; }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }
}

/// <summary>
/// رویداد بایگانی listing.
/// </summary>
public sealed class OfferArchivedDomainEvent : IDomainEvent
{
    /// <summary>
    /// از ریشه می‌سازد.
    /// </summary>
    public OfferArchivedDomainEvent(SellerOffer offer)
    {
        ArgumentNullException.ThrowIfNull(offer);
        OfferId = offer.OfferId;
        Metadata = EventMetadataFactory.ForDomain("offer.archived.domain");
    }

    /// <summary>
    /// Offer بایگانی‌شده.
    /// </summary>
    public Guid OfferId { get; }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }
}
