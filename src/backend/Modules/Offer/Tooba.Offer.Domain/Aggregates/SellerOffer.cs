using Tooba.Offer.Domain.Events;
using Tooba.BuildingBlocks;
using Tooba.Offer.Contracts;
using Tooba.Offer.Domain;

namespace Tooba.Offer.Domain.Aggregates;

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

    /// <summary>
    /// انتخاب سیاست مرجوعی: Default | Custom | NonReturnable.
    /// </summary>
    public string ReturnPolicyChoice { get; private set; } = "Default";

    /// <summary>
    /// مهلت اختصاصی (روز) وقتی Choice=Custom.
    /// </summary>
    public int? CustomReturnWindowDays { get; private set; }

    /// <summary>حداقل مقدار خرید فروشنده؛ اختیاری.</summary>
    public decimal? MinimumOrderQuantity { get; private set; }

    /// <summary>حداکثر مقدار خرید فروشنده؛ اختیاری.</summary>
    public decimal? MaximumOrderQuantity { get; private set; }

    /// <inheritdoc />
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.Events;

    /// <summary>
    /// Offer می‌سازد بدون مبلغ و موجودی. شناسه از Application/`IIdGenerator` می‌آید.
    /// </summary>
    public static SellerOffer Create(
        Guid offerId,
        Guid catalogVariantId,
        Guid sellerPartyId,
        SalesChannel channel,
        string? sellerSku,
        DateTimeOffset now)
    {
        var offer = new SellerOffer
        {
            OfferId = offerId,
            CatalogVariantId = catalogVariantId,
            SellerPartyId = sellerPartyId,
            Channel = channel,
            SellerSku = string.IsNullOrWhiteSpace(sellerSku) ? null : sellerSku.Trim(),
            Status = OfferStatus.Draft,
            CreatedAt = now,
            UpdatedAt = now,
            ReturnPolicyChoice = "Default",
            CustomReturnWindowDays = null,
        };
        offer._domainEvents.Add(new OfferCreatedDomainEvent(offer));
        return offer;
    }

    /// <summary>
    /// سیاست مرجوعی listing را تنظیم می‌کند (اعتبارسنجی حاکمیت در لایهٔ Application/Host).
    /// </summary>
    public void SetReturnPolicy(string choice, int? customReturnWindowDays, DateTimeOffset now)
    {
        var normalized = string.IsNullOrWhiteSpace(choice) ? "Default" : choice.Trim();
        ReturnPolicyChoice = normalized switch
        {
            "Custom" => "Custom",
            "NonReturnable" => "NonReturnable",
            _ => "Default",
        };
        CustomReturnWindowDays = ReturnPolicyChoice == "Custom" ? customReturnWindowDays : null;
        UpdatedAt = now;
    }

    /// <summary>حداقل/حداکثر مقدار خرید listing را تنظیم می‌کند.</summary>
    public void SetOrderQuantityLimits(decimal? minimum, decimal? maximum, DateTimeOffset now)
    {
        if (minimum is { } min && min <= 0)
        {
            throw new SemanticException(new SemanticError(OfferErrorCodes.MinQuantityInvalid));
        }

        if (maximum is { } max && max <= 0)
        {
            throw new SemanticException(new SemanticError(OfferErrorCodes.MaxQuantityInvalid));
        }

        if (minimum is { } a && maximum is { } b && a > b)
        {
            throw new SemanticException(new SemanticError(OfferErrorCodes.MinQuantityExceedsMax));
        }

        MinimumOrderQuantity = minimum;
        MaximumOrderQuantity = maximum;
        UpdatedAt = now;
    }

    /// <summary>
    /// listing را فعال می‌کند. اعتبار Price/Stock را اعلام نمی‌کند.
    /// </summary>
    public void Activate(DateTimeOffset now)
    {
        if (Status == OfferStatus.Archived)
        {
            throw new SemanticException(new SemanticError(OfferErrorCodes.ArchivedCannotActivate));
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
