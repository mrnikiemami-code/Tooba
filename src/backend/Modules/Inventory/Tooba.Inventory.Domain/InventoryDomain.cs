using Tooba.BuildingBlocks;

namespace Tooba.Inventory.Domain;

/// <summary>
/// وضعیت محل نگهداری موجودی. توپولوژی لجستیک کامل اینجا مدل نمی‌شود.
/// </summary>
public enum InventoryLocationStatus
{
    /// <summary>
    /// محل برای دریافت و رزرو قابل‌استفاده است.
    /// </summary>
    Active = 0,

    /// <summary>
    /// محل از گردش خارج شده؛ حذف Catalog نیست.
    /// </summary>
    Retired = 1,
}

/// <summary>
/// وضعیت رزرو موجودی. سبد خرید یا سفارش نیست.
/// </summary>
public enum StockReservationStatus
{
    /// <summary>
    /// مقدار روی موقعیت قفل شده و هنوز مصرف نشده.
    /// </summary>
    Held = 0,

    /// <summary>
    /// قفل آزاد شده و به موجودی قابل‌فروش برگشته.
    /// </summary>
    Released = 1,

    /// <summary>
    /// رزرو به خروج از OnHand تبدیل شده است.
    /// </summary>
    Consumed = 2,
}

/// <summary>
/// گونهٔ اصلاح موجودی. مقدار را مستقیماً از بیرون روی ردیف نمی‌نویسند.
/// </summary>
public enum StockAdjustmentKind
{
    /// <summary>
    /// افزایش OnHand مثل رسید.
    /// </summary>
    Increase = 0,

    /// <summary>
    /// کاهش OnHand مثل ضایعات، بدون عبور از رزرو.
    /// </summary>
    Decrease = 1,

    /// <summary>
    /// تصحیح شمارش؛ دلتا از مقدار فعلی محاسبه می‌شود.
    /// </summary>
    Set = 2,
}

/// <summary>
/// محل نگهداری حداقل. انبار، فروشگاه یا محل مجازی بعداً گسترش می‌یابد.
/// </summary>
public sealed class InventoryLocation : IHasDomainEvents
{
    private readonly DomainEventCollector _domainEvents = new();

    /// <summary>
    /// شناسهٔ پایدار محل.
    /// </summary>
    public Guid LocationId { get; init; }

    /// <summary>
    /// کد کوتاه محل داخل Tenant.
    /// </summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>
    /// نام نمایشی عملیاتی. آدرس لجستیک کامل نیست.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// وضعیت محل.
    /// </summary>
    public InventoryLocationStatus Status { get; private set; }

    /// <summary>
    /// زمان ایجاد UTC.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// زمان آخرین تغییر UTC.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <inheritdoc />
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.Events;

    /// <inheritdoc />
    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>
    /// محل فعال می‌سازد. موجودی Offer را صفر نمی‌کند.
    /// </summary>
    public static InventoryLocation Create(string code, string name, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Trim().Length > 32)
        {
            throw new InvalidOperationException("کد محل باید کوتاه و غیرخالی باشد.");
        }

        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 128)
        {
            throw new InvalidOperationException("نام محل باید غیرخالی باشد.");
        }

        return new InventoryLocation
        {
            LocationId = UuidV7.New(),
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            Status = InventoryLocationStatus.Active,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }
}

/// <summary>
/// موقعیت موجودی یک Offer در یک محل. حقیقت موجودی روی Product نیست.
/// </summary>
public sealed class StockPosition : IHasDomainEvents
{
    private readonly DomainEventCollector _domainEvents = new();

    /// <summary>
    /// شناسهٔ پایدار موقعیت.
    /// </summary>
    public Guid StockItemId { get; init; }

    /// <summary>
    /// Offer هدف. FK به schema offer نیست.
    /// </summary>
    public Guid OfferId { get; init; }

    /// <summary>
    /// گونهٔ Catalog برای جستجوی توصیفی. کلید فروشنده نیست.
    /// </summary>
    public Guid CatalogVariantId { get; init; }

    /// <summary>
    /// محل نگهداری.
    /// </summary>
    public Guid LocationId { get; init; }

    /// <summary>
    /// موجودی فیزیکی. اعشار شناور نیست.
    /// </summary>
    public int OnHand { get; private set; }

    /// <summary>
    /// مقدار قفل‌شده برای رزروهای Held.
    /// </summary>
    public int Reserved { get; private set; }

    /// <summary>
    /// زمان ایجاد UTC.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// زمان آخرین تغییر UTC.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// موجودی قابل‌فروش مشتق؛ ستون جدا ذخیره نمی‌شود تا منحرف نشود.
    /// </summary>
    public int Available => OnHand - Reserved;

    /// <inheritdoc />
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.Events;

    /// <inheritdoc />
    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>
    /// موقعیت خالی می‌سازد. قابل‌خرید بودن Offer را اعلام نمی‌کند.
    /// </summary>
    public static StockPosition Open(Guid offerId, Guid catalogVariantId, Guid locationId, DateTimeOffset now)
    {
        if (offerId == Guid.Empty || catalogVariantId == Guid.Empty || locationId == Guid.Empty)
        {
            throw new InvalidOperationException("Offer و گونه و محل باید شناسهٔ پایدار داشته باشند.");
        }

        var position = new StockPosition
        {
            StockItemId = UuidV7.New(),
            OfferId = offerId,
            CatalogVariantId = catalogVariantId,
            LocationId = locationId,
            OnHand = 0,
            Reserved = 0,
            CreatedAt = now,
            UpdatedAt = now,
        };
        position._domainEvents.Add(new StockAvailabilityChangedDomainEvent(position.StockItemId, position.OfferId, 0, 0, 0));
        return position;
    }

    /// <summary>
    /// پس از به‌روزرسانی اتمی پایگاه، مقادیر خوانده‌شده را با رویداد هم‌تراز می‌کند.
    /// </summary>
    public void SyncQuantities(int onHand, int reserved, DateTimeOffset now)
    {
        EnsureLegal(onHand, reserved);
        OnHand = onHand;
        Reserved = reserved;
        UpdatedAt = now;
        _domainEvents.Add(new StockAvailabilityChangedDomainEvent(StockItemId, OfferId, OnHand, Reserved, Available));
    }

    /// <summary>
    /// رویداد اصلاح را ثبت می‌کند. مقدار را جداگانه با SQL اتمی عوض می‌کنند.
    /// </summary>
    public void RecordAdjustment(StockAdjustmentKind kind, int delta, string reason)
    {
        _domainEvents.Add(new StockAdjustedDomainEvent(StockItemId, OfferId, kind, delta, reason));
    }

    /// <summary>
    /// رویداد رزرو موفق را ثبت می‌کند.
    /// </summary>
    public void RecordReserved(Guid reservationId, int quantity)
    {
        _domainEvents.Add(new StockReservedDomainEvent(reservationId, StockItemId, OfferId, quantity));
    }

    /// <summary>
    /// رویداد آزادسازی رزرو را ثبت می‌کند.
    /// </summary>
    public void RecordReleased(Guid reservationId, int quantity)
    {
        _domainEvents.Add(new StockReleasedDomainEvent(reservationId, StockItemId, OfferId, quantity));
    }

    /// <summary>
    /// رویداد مصرف رزرو را ثبت می‌کند.
    /// </summary>
    public void RecordConsumed(Guid reservationId, int quantity)
    {
        _domainEvents.Add(new StockReservationConsumedDomainEvent(reservationId, StockItemId, OfferId, quantity));
    }

    /// <summary>
    /// حالت غیرممکن موجودی را رد می‌کند.
    /// </summary>
    public static void EnsureLegal(int onHand, int reserved)
    {
        if (onHand < 0 || reserved < 0 || reserved > onHand)
        {
            throw new InvalidOperationException("OnHand و Reserved نمی‌توانند منفی باشند یا Reserved از OnHand بیشتر شود.");
        }
    }
}

/// <summary>
/// رزرو مقدار روی یک موقعیت. Cart/Order اینجا نوع دامنه نیستند.
/// </summary>
public sealed class StockReservation
{
    /// <summary>
    /// شناسهٔ پایدار رزرو برای درز آیندهٔ سبد/سفارش.
    /// </summary>
    public Guid ReservationId { get; init; }

    /// <summary>
    /// موقعیت موجودی.
    /// </summary>
    public Guid StockItemId { get; init; }

    /// <summary>
    /// مقدار قفل‌شده.
    /// </summary>
    public int Quantity { get; init; }

    /// <summary>
    /// وضعیت چرخهٔ رزرو.
    /// </summary>
    public StockReservationStatus Status { get; private set; }

    /// <summary>
    /// مرجع خارجی اختیاری مثل کلید سبد آینده؛ نوع Cart نیست.
    /// </summary>
    public string? ExternalReference { get; init; }

    /// <summary>
    /// کلید تکرارناپذیری اختیاری.
    /// </summary>
    public string? IdempotencyKey { get; private set; }

    /// <summary>
    /// زمان ایجاد UTC.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// زمان آخرین تغییر UTC.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// مهلت UTC آزادسازی خودکار؛ تهی یعنی بدون انقضای زمانی در این رزرو.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>
    /// رزرو Held می‌سازد.
    /// </summary>
    public static StockReservation Hold(
        Guid stockItemId,
        int quantity,
        string? externalReference,
        string? idempotencyKey,
        DateTimeOffset now,
        DateTimeOffset? expiresAt)
    {
        if (quantity <= 0)
        {
            throw new InvalidOperationException("مقدار رزرو باید مثبت باشد.");
        }

        if (expiresAt is { } expiry && expiry <= now)
        {
            throw new InvalidOperationException("مهلت رزرو باید بعد از زمان ایجاد باشد.");
        }

        return new StockReservation
        {
            ReservationId = UuidV7.New(),
            StockItemId = stockItemId,
            Quantity = quantity,
            Status = StockReservationStatus.Held,
            ExternalReference = string.IsNullOrWhiteSpace(externalReference) ? null : externalReference.Trim(),
            IdempotencyKey = string.IsNullOrWhiteSpace(idempotencyKey) ? null : idempotencyKey.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
            ExpiresAt = expiresAt,
        };
    }

    /// <summary>
    /// وضعیت را عوض می‌کند.
    /// </summary>
    public void MoveTo(StockReservationStatus status, DateTimeOffset now)
    {
        if (Status != StockReservationStatus.Held)
        {
            throw new InvalidOperationException("فقط رزرو Held قابل آزادسازی یا مصرف است.");
        }

        Status = status;
        UpdatedAt = now;
        IdempotencyKey = null;
    }
}

/// <summary>
/// رویداد اصلاح موجودی. قیمت را عوض نمی‌کند.
/// </summary>
public sealed class StockAdjustedDomainEvent : IDomainEvent
{
    /// <summary>
    /// رویداد اصلاح را می‌سازد.
    /// </summary>
    public StockAdjustedDomainEvent(Guid stockItemId, Guid offerId, StockAdjustmentKind kind, int delta, string reason)
    {
        StockItemId = stockItemId;
        OfferId = offerId;
        Kind = kind;
        Delta = delta;
        Reason = reason;
        Metadata = EventMetadataFactory.ForDomain("inventory.adjusted.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>
    /// موقعیت اصلاح‌شده.
    /// </summary>
    public Guid StockItemId { get; }

    /// <summary>
    /// Offer هدف.
    /// </summary>
    public Guid OfferId { get; }

    /// <summary>
    /// گونهٔ اصلاح.
    /// </summary>
    public StockAdjustmentKind Kind { get; }

    /// <summary>
    /// تغییر OnHand.
    /// </summary>
    public int Delta { get; }

    /// <summary>
    /// دلیل عملیاتی.
    /// </summary>
    public string Reason { get; }
}

/// <summary>
/// رویداد رزرو موفق.
/// </summary>
public sealed class StockReservedDomainEvent : IDomainEvent
{
    /// <summary>
    /// رویداد رزرو را می‌سازد.
    /// </summary>
    public StockReservedDomainEvent(Guid reservationId, Guid stockItemId, Guid offerId, int quantity)
    {
        ReservationId = reservationId;
        StockItemId = stockItemId;
        OfferId = offerId;
        Quantity = quantity;
        Metadata = EventMetadataFactory.ForDomain("inventory.reserved.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>
    /// رزرو ایجادشده.
    /// </summary>
    public Guid ReservationId { get; }

    /// <summary>
    /// موقعیت.
    /// </summary>
    public Guid StockItemId { get; }

    /// <summary>
    /// Offer.
    /// </summary>
    public Guid OfferId { get; }

    /// <summary>
    /// مقدار قفل‌شده.
    /// </summary>
    public int Quantity { get; }
}

/// <summary>
/// رویداد آزادسازی رزرو.
/// </summary>
public sealed class StockReleasedDomainEvent : IDomainEvent
{
    /// <summary>
    /// رویداد آزادسازی را می‌سازد.
    /// </summary>
    public StockReleasedDomainEvent(Guid reservationId, Guid stockItemId, Guid offerId, int quantity)
    {
        ReservationId = reservationId;
        StockItemId = stockItemId;
        OfferId = offerId;
        Quantity = quantity;
        Metadata = EventMetadataFactory.ForDomain("inventory.released.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>
    /// رزرو آزادشده.
    /// </summary>
    public Guid ReservationId { get; }

    /// <summary>
    /// موقعیت.
    /// </summary>
    public Guid StockItemId { get; }

    /// <summary>
    /// Offer.
    /// </summary>
    public Guid OfferId { get; }

    /// <summary>
    /// مقدار برگشتی.
    /// </summary>
    public int Quantity { get; }
}

/// <summary>
/// رویداد مصرف رزرو.
/// </summary>
public sealed class StockReservationConsumedDomainEvent : IDomainEvent
{
    /// <summary>
    /// رویداد مصرف را می‌سازد.
    /// </summary>
    public StockReservationConsumedDomainEvent(Guid reservationId, Guid stockItemId, Guid offerId, int quantity)
    {
        ReservationId = reservationId;
        StockItemId = stockItemId;
        OfferId = offerId;
        Quantity = quantity;
        Metadata = EventMetadataFactory.ForDomain("inventory.reservation_consumed.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>
    /// رزرو مصرف‌شده.
    /// </summary>
    public Guid ReservationId { get; }

    /// <summary>
    /// موقعیت.
    /// </summary>
    public Guid StockItemId { get; }

    /// <summary>
    /// Offer.
    /// </summary>
    public Guid OfferId { get; }

    /// <summary>
    /// مقدار کسرشده از OnHand.
    /// </summary>
    public int Quantity { get; }
}

/// <summary>
/// رویداد تغییر موجودی قابل‌مشاهده. به‌تنهایی قابل‌خرید بودن نیست.
/// </summary>
public sealed class StockAvailabilityChangedDomainEvent : IDomainEvent
{
    /// <summary>
    /// رویداد تغییر موجودی را می‌سازد.
    /// </summary>
    public StockAvailabilityChangedDomainEvent(Guid stockItemId, Guid offerId, int onHand, int reserved, int available)
    {
        StockItemId = stockItemId;
        OfferId = offerId;
        OnHand = onHand;
        Reserved = reserved;
        Available = available;
        Metadata = EventMetadataFactory.ForDomain("inventory.availability_changed.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>
    /// موقعیت.
    /// </summary>
    public Guid StockItemId { get; }

    /// <summary>
    /// Offer.
    /// </summary>
    public Guid OfferId { get; }

    /// <summary>
    /// موجودی فیزیکی.
    /// </summary>
    public int OnHand { get; }

    /// <summary>
    /// مقدار رزرو.
    /// </summary>
    public int Reserved { get; }

    /// <summary>
    /// موجودی قابل‌فروش مشتق.
    /// </summary>
    public int Available { get; }
}
