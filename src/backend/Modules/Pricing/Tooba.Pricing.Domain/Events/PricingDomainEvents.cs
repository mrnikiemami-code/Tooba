using Tooba.BuildingBlocks;

namespace Tooba.Pricing.Domain;

/// <summary>
/// رویداد ایجاد قیمت نوشته‌شده.
/// </summary>
public sealed class PriceCreatedDomainEvent : IDomainEvent
{
    /// <summary>
    /// از ریشه می‌سازد.
    /// </summary>
    public PriceCreatedDomainEvent(AuthoredPrice price)
    {
        ArgumentNullException.ThrowIfNull(price);
        PriceId = price.PriceId;
        OfferId = price.OfferId;
        Metadata = EventMetadataFactory.ForDomain("pricing.price_created.domain");
    }

    /// <summary>
    /// قیمت ایجادشده.
    /// </summary>
    public Guid PriceId { get; }

    /// <summary>
    /// Offer هدف.
    /// </summary>
    public Guid OfferId { get; }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }
}

/// <summary>
/// رویداد فعال‌سازی قیمت پایه.
/// </summary>
public sealed class PriceActivatedDomainEvent : IDomainEvent
{
    /// <summary>
    /// از ریشه می‌سازد.
    /// </summary>
    public PriceActivatedDomainEvent(AuthoredPrice price)
    {
        ArgumentNullException.ThrowIfNull(price);
        PriceId = price.PriceId;
        Metadata = EventMetadataFactory.ForDomain("pricing.price_activated.domain");
    }

    /// <summary>
    /// قیمت فعال‌شده.
    /// </summary>
    public Guid PriceId { get; }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }
}

/// <summary>
/// رویداد تغییر مبلغ نوشته‌شده.
/// </summary>
public sealed class PriceChangedDomainEvent : IDomainEvent
{
    /// <summary>
    /// از ریشه می‌سازد.
    /// </summary>
    public PriceChangedDomainEvent(AuthoredPrice price)
    {
        ArgumentNullException.ThrowIfNull(price);
        PriceId = price.PriceId;
        Metadata = EventMetadataFactory.ForDomain("pricing.price_changed.domain");
    }

    /// <summary>
    /// قیمت تغییر یافته.
    /// </summary>
    public Guid PriceId { get; }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }
}

/// <summary>
/// رویداد خروج قیمت از انتخاب.
/// </summary>
public sealed class PriceExpiredDomainEvent : IDomainEvent
{
    /// <summary>
    /// از ریشه می‌سازد.
    /// </summary>
    public PriceExpiredDomainEvent(AuthoredPrice price)
    {
        ArgumentNullException.ThrowIfNull(price);
        PriceId = price.PriceId;
        Metadata = EventMetadataFactory.ForDomain("pricing.price_expired.domain");
    }

    /// <summary>
    /// قیمت منقضی.
    /// </summary>
    public Guid PriceId { get; }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }
}
