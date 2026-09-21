using Tooba.BuildingBlocks;

namespace Tooba.Pricing.Domain;

/// <summary>
/// قیمت نوشته‌شده برای یک Offer در بازار و کانال و ارز. موجودی و مالیات محاسبه‌شده اینجا نیست.
/// </summary>
public sealed class AuthoredPrice : IHasDomainEvents
{
    private readonly DomainEventCollector _domainEvents = new();

    /// <summary>
    /// شناسهٔ پایدار قیمت.
    /// </summary>
    public Guid PriceId { get; init; }

    /// <summary>
    /// Offer هدف؛ FK به schema offer نیست.
    /// </summary>
    public Guid OfferId { get; init; }

    /// <summary>
    /// بازار تجاری. Locale نیست.
    /// </summary>
    public string Market { get; init; } = string.Empty;

    /// <summary>
    /// کانال فروش همان مفهوم Offer است نه فهرست جدا.
    /// </summary>
    public PriceChannel Channel { get; init; }

    /// <summary>
    /// ارز نوشته‌شده.
    /// </summary>
    public string Currency { get; private set; } = string.Empty;

    /// <summary>
    /// مبلغ بدون مالیات پس از گرد کردن.
    /// </summary>
    public decimal Amount { get; private set; }

    /// <summary>
    /// شروع اعتبار به UTC.
    /// </summary>
    public DateTimeOffset ValidFrom { get; init; }

    /// <summary>
    /// پایان اعتبار اختیاری به UTC. جلالی ذخیره نمی‌شود.
    /// </summary>
    public DateTimeOffset? ValidTo { get; private set; }

    /// <summary>
    /// وضعیت انتخاب.
    /// </summary>
    public PriceStatus Status { get; private set; }

    /// <summary>
    /// درز محدودکننده برای قیمت مشتری/سازمان/قرارداد آینده.
    /// </summary>
    public PriceQualifierKind QualifierKind { get; init; }

    /// <summary>
    /// کلید محدودکنندهٔ آینده؛ برای قیمت پایه تهی است.
    /// </summary>
    public string? QualifierKey { get; init; }

    /// <summary>
    /// زمان ایجاد.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// زمان به‌روزرسانی.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <inheritdoc />
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.Events;

    /// <summary>
    /// قیمت نوشته‌شده می‌سازد. قابل‌خرید بودن و نرخ FX را اعلام نمی‌کند.
    /// </summary>
    public static AuthoredPrice Create(
        Guid priceId,
        Guid offerId,
        string marketCode,
        PriceChannel channel,
        decimal amount,
        string currencyCode,
        DateTimeOffset validFrom,
        DateTimeOffset? validTo,
        DateTimeOffset now) =>
        CreateCore(
            priceId,
            offerId,
            marketCode,
            channel,
            amount,
            currencyCode,
            validFrom,
            validTo,
            now,
            PriceQualifierKind.Base,
            qualifierKey: null);

    /// <summary>
    /// قیمت کمپین مرچندایزینگ می‌سازد؛ QualifierKey همان CampaignId پایدار است.
    /// </summary>
    public static AuthoredPrice CreateMerchandisingCampaign(
        Guid priceId,
        Guid offerId,
        Guid campaignId,
        string marketCode,
        PriceChannel channel,
        decimal amount,
        string currencyCode,
        DateTimeOffset validFrom,
        DateTimeOffset? validTo,
        DateTimeOffset now)
    {
        if (campaignId == Guid.Empty)
        {
            throw new SemanticException(new SemanticError(PricingErrorCodes.CampaignRequired));
        }

        return CreateCore(
            priceId,
            offerId,
            marketCode,
            channel,
            amount,
            currencyCode,
            validFrom,
            validTo,
            now,
            PriceQualifierKind.MerchandisingCampaign,
            campaignId.ToString("D"));
    }

    private static AuthoredPrice CreateCore(
        Guid priceId,
        Guid offerId,
        string marketCode,
        PriceChannel channel,
        decimal amount,
        string currencyCode,
        DateTimeOffset validFrom,
        DateTimeOffset? validTo,
        DateTimeOffset now,
        PriceQualifierKind qualifierKind,
        string? qualifierKey)
    {
        var market = MarketCode.Parse(marketCode);
        var money = Money.Create(amount, currencyCode);
        if (validTo is { } to && to <= validFrom)
        {
            throw new SemanticException(new SemanticError(PricingErrorCodes.ValidityInverted));
        }

        if (priceId == Guid.Empty)
        {
            throw new InvalidOperationException("pricing.price.id_required");
        }

        var price = new AuthoredPrice
        {
            PriceId = priceId,
            OfferId = offerId,
            Market = market.Value,
            Channel = channel,
            Currency = money.Currency.Value,
            Amount = money.Amount,
            ValidFrom = validFrom,
            ValidTo = validTo,
            Status = PriceStatus.Draft,
            QualifierKind = qualifierKind,
            QualifierKey = qualifierKey,
            CreatedAt = now,
            UpdatedAt = now,
        };
        price._domainEvents.Add(new PriceCreatedDomainEvent(price));
        return price;
    }

    /// <summary>
    /// قیمت را برای انتخاب پایه فعال می‌کند. موجودی را تضمین نمی‌کند.
    /// </summary>
    public void Activate(DateTimeOffset now)
    {
        if (Status == PriceStatus.Retired)
        {
            throw new SemanticException(new SemanticError(PricingErrorCodes.RetiredReactivate));
        }

        Status = PriceStatus.Active;
        UpdatedAt = now;
        _domainEvents.Add(new PriceActivatedDomainEvent(this));
    }

    /// <summary>
    /// مبلغ نوشته‌شده را عوض می‌کند. نتیجهٔ FX را جای حقیقت نمی‌گذارد.
    /// </summary>
    public void ChangeAmount(decimal amount, string currencyCode, DateTimeOffset now)
    {
        if (Status == PriceStatus.Retired)
        {
            throw new SemanticException(new SemanticError(PricingErrorCodes.RetiredImmutable));
        }

        var money = Money.Create(amount, currencyCode);
        if (!string.Equals(money.Currency.Value, Currency, StringComparison.Ordinal))
        {
            throw new SemanticException(new SemanticError(PricingErrorCodes.CurrencyChangeForbidden));
        }

        Amount = money.Amount;
        UpdatedAt = now;
        _domainEvents.Add(new PriceChangedDomainEvent(this));
    }

    /// <summary>
    /// قیمت را از انتخاب خارج می‌کند.
    /// </summary>
    public void Expire(DateTimeOffset now)
    {
        if (Status == PriceStatus.Retired)
        {
            return;
        }

        ValidTo = ValidTo is { } existing && existing < now ? existing : now;
        Status = PriceStatus.Retired;
        UpdatedAt = now;
        _domainEvents.Add(new PriceExpiredDomainEvent(this));
    }

    /// <summary>
    /// آیا در Instant داده‌شده برای انتخاب پایه معتبر است.
    /// </summary>
    public bool IsEffectiveAt(DateTimeOffset at) =>
        Status == PriceStatus.Active
        && at >= ValidFrom
        && (ValidTo is null || at < ValidTo);

    /// <summary>
    /// همپوشانی بازه با رکورد دیگر روی همان کلید انتخاب.
    /// </summary>
    public bool Overlaps(AuthoredPrice other)
    {
        ArgumentNullException.ThrowIfNull(other);
        var thisEnd = ValidTo ?? DateTimeOffset.MaxValue;
        var otherEnd = other.ValidTo ?? DateTimeOffset.MaxValue;
        return ValidFrom < otherEnd && other.ValidFrom < thisEnd;
    }

    /// <inheritdoc />
    public void ClearDomainEvents() => _domainEvents.Clear();
}
