using Tooba.BuildingBlocks;
using Tooba.Offer.Domain;

namespace Tooba.Cart.Domain;

/// <summary>
/// وضعیت عمر سبد. سفارش، پرداخت یا موجودی نیست.
/// </summary>
public enum CartStatus
{
    /// <summary>
    /// سبد باز است و خط می‌پذیرد.
    /// </summary>
    Active = 0,

    /// <summary>
    /// سبد به درز تبدیل سفارش آینده رفته؛ Checkout اینجا اجرا نمی‌شود.
    /// </summary>
    Converted = 1,

    /// <summary>
    /// مهلت UTC سبد گذشته و رزروها باید آزاد شوند.
    /// </summary>
    Expired = 2,

    /// <summary>
    /// مالک سبد را رها کرده؛ رزروها آزاد می‌شوند.
    /// </summary>
    Abandoned = 3,
}

/// <summary>
/// گونهٔ دسترسی به سبد. CartId به‌تنهایی مجوز نیست.
/// </summary>
public enum CartAccessKind
{
    /// <summary>
    /// مالک با هویت پایدار User.
    /// </summary>
    Authenticated = 0,

    /// <summary>
    /// مهمان با راز پرمخاطره؛ فقط هش ذخیره می‌شود.
    /// </summary>
    Guest = 1,
}

/// <summary>
/// مسیر تبدیل آینده. هر دو مدل سفارش از همین سبد شروع می‌شوند.
/// </summary>
public enum CartConversionIntent
{
    /// <summary>
    /// هنوز تبدیل نشده.
    /// </summary>
    None = 0,

    /// <summary>
    /// مبدأ سفارش Request-to-Reserve آینده.
    /// </summary>
    RequestToReserve = 1,

    /// <summary>
    /// مبدأ خرید آنلاین آینده.
    /// </summary>
    OnlinePurchase = 2,
}

/// <summary>
/// خط سبد روی Offer فروشنده. Product به‌تنهایی هدف نیست و قیمت حقیقت تسویه نیست.
/// </summary>
public sealed class CartLine
{
    /// <summary>
    /// سازندهٔ EF.
    /// </summary>
    private CartLine()
    {
    }

    /// <summary>
    /// شناسهٔ پایدار خط.
    /// </summary>
    public Guid LineId { get; init; }

    /// <summary>
    /// سبد مالک خط.
    /// </summary>
    public Guid CartId { get; init; }

    /// <summary>
    /// Offer فروشنده؛ موجودی و قیمت جداگانه از قرارداد خوانده می‌شوند.
    /// </summary>
    public Guid OfferId { get; init; }

    /// <summary>
    /// گونهٔ Catalog کپی‌شده از Lookup؛ FK به schema کاتالوگ نیست.
    /// </summary>
    public Guid CatalogVariantId { get; init; }

    /// <summary>
    /// فروشندهٔ Offer؛ چندفروشنده در یک سبد مجاز است.
    /// </summary>
    public Guid SellerPartyId { get; init; }

    /// <summary>
    /// تعداد صحیح مثبت. اعشار نیست.
    /// </summary>
    public int Quantity { get; private set; }

    /// <summary>
    /// رزرو موجودی متعلق به این خط؛ جداول Inventory اینجا join نمی‌شوند.
    /// </summary>
    public Guid? ReservationId { get; private set; }

    /// <summary>
    /// مبلغ نقل‌قول نمایشی. حقیقت تسویه یا مالیات محاسبه‌شده نیست.
    /// </summary>
    public decimal? QuotedAmount { get; private set; }

    /// <summary>
    /// ارز نقل‌قول؛ Locale نیست.
    /// </summary>
    public string? QuotedCurrency { get; private set; }

    /// <summary>
    /// آیا مبلغ نقل‌قول بدون مالیات نوشته شده است.
    /// </summary>
    public bool QuotedTaxExclusive { get; private set; }

    /// <summary>
    /// شناسهٔ قیمت انتخاب‌شده در زمان نقل‌قول.
    /// </summary>
    public Guid? PriceId { get; private set; }

    /// <summary>
    /// زمان UTC گرفتن نقل‌قول نمایشی.
    /// </summary>
    public DateTimeOffset QuotedAt { get; private set; }

    /// <summary>
    /// خط جدید با تعداد و نقل‌قول می‌سازد.
    /// </summary>
    public static CartLine Open(
        Guid cartId,
        Guid offerId,
        Guid catalogVariantId,
        Guid sellerPartyId,
        int quantity,
        Guid? reservationId,
        decimal quotedAmount,
        string quotedCurrency,
        bool taxExclusive,
        Guid priceId,
        DateTimeOffset quotedAt)
    {
        EnsureQuantity(quantity);
        return new CartLine
        {
            LineId = UuidV7.New(),
            CartId = cartId,
            OfferId = offerId,
            CatalogVariantId = catalogVariantId,
            SellerPartyId = sellerPartyId,
            Quantity = quantity,
            ReservationId = reservationId,
            QuotedAmount = quotedAmount,
            QuotedCurrency = quotedCurrency,
            QuotedTaxExclusive = taxExclusive,
            PriceId = priceId,
            QuotedAt = quotedAt,
        };
    }

    /// <summary>
    /// تعداد و رزرو و نقل‌قول را پس از هم‌ترازسازی موجودی عوض می‌کند.
    /// </summary>
    public void ReplaceHold(
        int quantity,
        Guid? reservationId,
        decimal quotedAmount,
        string quotedCurrency,
        bool taxExclusive,
        Guid priceId,
        DateTimeOffset quotedAt)
    {
        EnsureQuantity(quantity);
        Quantity = quantity;
        ReservationId = reservationId;
        QuotedAmount = quotedAmount;
        QuotedCurrency = quotedCurrency;
        QuotedTaxExclusive = taxExclusive;
        PriceId = priceId;
        QuotedAt = quotedAt;
    }

    /// <summary>
    /// رزرو را پس از آزادسازی از خط جدا می‌کند.
    /// </summary>
    public void ClearReservation() => ReservationId = null;

    /// <summary>
    /// تعداد باید عدد صحیح مثبت و حداکثر ۹۹ باشد.
    /// </summary>
    public static void EnsureQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new InvalidOperationException("تعداد خط سبد باید عدد صحیح مثبت باشد.");
        }

        if (quantity > 99)
        {
            throw new InvalidOperationException("تعداد خط سبد از سقف foundation بیشتر است.");
        }
    }
}

/// <summary>
/// سبد پایدار. سفارش، پرداخت، موجودی و منبع حقیقت قیمت نیست.
/// </summary>
public sealed class ShoppingCart : IHasDomainEvents
{
    private readonly DomainEventCollector _domainEvents = new();

    /// <summary>
    /// سازندهٔ EF.
    /// </summary>
    private ShoppingCart()
    {
    }

    /// <summary>
    /// شناسهٔ پایدار سبد؛ به‌تنهایی Bearer نیست.
    /// </summary>
    public Guid CartId { get; init; }

    /// <summary>
    /// وضعیت عمر سبد.
    /// </summary>
    public CartStatus Status { get; private set; }

    /// <summary>
    /// گونهٔ مالکیت و دسترسی.
    /// </summary>
    public CartAccessKind AccessKind { get; init; }

    /// <summary>
    /// هویت User برای سبد واردشده.
    /// </summary>
    public Guid? OwnerUserId { get; init; }

    /// <summary>
    /// هش SHA-256 راز مهمان؛ راز خام ذخیره نمی‌شود.
    /// </summary>
    public string? GuestCredentialHash { get; init; }

    /// <summary>
    /// بازار تجاری سبد. Locale نیست.
    /// </summary>
    public string Market { get; init; } = string.Empty;

    /// <summary>
    /// ارز زمینهٔ سبد. Locale یا نام نمایشی نیست.
    /// </summary>
    public string Currency { get; init; } = string.Empty;

    /// <summary>
    /// کانال فروش پایدار سبد.
    /// </summary>
    public SalesChannel Channel { get; init; }

    /// <summary>
    /// مهلت UTC رزرو/عمر سبد؛ تایمر مرورگر نیست.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; private set; }

    /// <summary>
    /// مسیر تبدیل ثبت‌شده پس از MarkConverted.
    /// </summary>
    public CartConversionIntent ConversionIntent { get; private set; }

    /// <summary>
    /// نسخهٔ خوش‌بینانه برای جهش همزمان خط.
    /// </summary>
    public int Version { get; private set; }

    /// <summary>
    /// زمان ایجاد UTC.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// زمان آخرین تغییر UTC.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// خطوط Offer داخل سبد.
    /// </summary>
    public List<CartLine> Lines { get; } = [];

    /// <inheritdoc />
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.Events;

    /// <inheritdoc />
    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>
    /// سبد واردشده می‌سازد.
    /// </summary>
    public static ShoppingCart CreateAuthenticated(
        Guid userId,
        string market,
        string currency,
        SalesChannel channel,
        DateTimeOffset now,
        DateTimeOffset expiresAt)
    {
        if (userId == Guid.Empty)
        {
            throw new InvalidOperationException("سبد واردشده به UserId پایدار نیاز دارد.");
        }

        var cart = CreateCore(CartAccessKind.Authenticated, userId, null, market, currency, channel, now, expiresAt);
        cart._domainEvents.Add(new CartCreatedDomainEvent(cart.CartId, cart.AccessKind));
        return cart;
    }

    /// <summary>
    /// سبد مهمان می‌سازد؛ فقط هش راز را نگه می‌دارد.
    /// </summary>
    public static ShoppingCart CreateGuest(
        string guestCredentialHash,
        string market,
        string currency,
        SalesChannel channel,
        DateTimeOffset now,
        DateTimeOffset expiresAt)
    {
        if (string.IsNullOrWhiteSpace(guestCredentialHash))
        {
            throw new InvalidOperationException("هش راز مهمان تهی است.");
        }

        var cart = CreateCore(CartAccessKind.Guest, null, guestCredentialHash.Trim(), market, currency, channel, now, expiresAt);
        cart._domainEvents.Add(new CartCreatedDomainEvent(cart.CartId, cart.AccessKind));
        return cart;
    }

    /// <summary>
    /// خط تازه را اضافه می‌کند. ادغام Offer تکراری باید از مسیر تغییر تعداد باشد تا رزرو از مقدار خط بیشتر نشود.
    /// </summary>
    public void AddLine(CartLine line, DateTimeOffset now)
    {
        EnsureActive();
        if (Lines.Any(x => x.OfferId == line.OfferId))
        {
            throw new InvalidOperationException("ادغام خط موجود باید از مسیر تغییر تعداد انجام شود تا رزرو بیش از مقدار خط نماند.");
        }

        Lines.Add(line);
        Touch(now);
        _domainEvents.Add(new CartLineAddedDomainEvent(CartId, line.LineId, line.OfferId, line.Quantity));
    }

    /// <summary>
    /// خط را برای Offer پیدا می‌کند.
    /// </summary>
    public CartLine? FindLineByOffer(Guid offerId) => Lines.SingleOrDefault(x => x.OfferId == offerId);

    /// <summary>
    /// خط را با شناسه پیدا می‌کند.
    /// </summary>
    public CartLine RequireLine(Guid lineId) =>
        Lines.SingleOrDefault(x => x.LineId == lineId)
        ?? throw new InvalidOperationException("خط سبد پیدا نشد.");

    /// <summary>
    /// پس از تغییر تعداد، رویداد و نسخه را جلو می‌برد.
    /// </summary>
    public void RecordLineChanged(Guid lineId, Guid offerId, int quantity, DateTimeOffset now)
    {
        EnsureActive();
        Touch(now);
        _domainEvents.Add(new CartLineChangedDomainEvent(CartId, lineId, offerId, quantity));
    }

    /// <summary>
    /// خط را حذف می‌کند پس از آزادسازی رزرو.
    /// </summary>
    public void RemoveLine(Guid lineId, DateTimeOffset now)
    {
        EnsureActive();
        var line = RequireLine(lineId);
        Lines.Remove(line);
        Touch(now);
        _domainEvents.Add(new CartLineRemovedDomainEvent(CartId, line.LineId, line.OfferId));
    }

    /// <summary>
    /// سبد را منقضی می‌کند؛ سفارش ساخته نمی‌شود.
    /// </summary>
    public void Expire(DateTimeOffset now)
    {
        if (Status is CartStatus.Converted)
        {
            throw new InvalidOperationException("سبد تبدیل‌شده منقضی نمی‌شود.");
        }

        if (Status == CartStatus.Expired)
        {
            return;
        }

        Status = CartStatus.Expired;
        Touch(now);
        _domainEvents.Add(new CartExpiredDomainEvent(CartId));
    }

    /// <summary>
    /// سبد را رها می‌کند و از حالت فعال خارج می‌کند.
    /// </summary>
    public void Abandon(DateTimeOffset now)
    {
        EnsureActive();
        Status = CartStatus.Abandoned;
        Touch(now);
        _domainEvents.Add(new CartExpiredDomainEvent(CartId));
    }

    /// <summary>
    /// درز تبدیل را بدون ساختن Order ثبت می‌کند.
    /// </summary>
    public void MarkConverted(CartConversionIntent intent, DateTimeOffset now)
    {
        EnsureActive();
        if (intent == CartConversionIntent.None)
        {
            throw new InvalidOperationException("تبدیل سبد به مسیر سفارش نیاز دارد.");
        }

        Status = CartStatus.Converted;
        ConversionIntent = intent;
        Touch(now);
        _domainEvents.Add(new CartConvertedDomainEvent(CartId, intent));
    }

    /// <summary>
    /// مهلت UTC سبد را برای رزروهای جدید هم‌تراز می‌کند.
    /// </summary>
    public void RefreshExpiry(DateTimeOffset expiresAt, DateTimeOffset now)
    {
        EnsureActive();
        if (expiresAt <= now)
        {
            throw new InvalidOperationException("مهلت سبد باید در آینده باشد.");
        }

        ExpiresAt = expiresAt;
        Touch(now);
    }

    /// <summary>
    /// نسخهٔ مورد انتظار کلاینت را با ردیف مقایسه می‌کند.
    /// </summary>
    public void EnsureVersion(int expectedVersion)
    {
        if (expectedVersion != Version)
        {
            throw new InvalidOperationException("نسخهٔ سبد کهنه است؛ جهش همزمان خط رد شد.");
        }
    }

    private static ShoppingCart CreateCore(
        CartAccessKind accessKind,
        Guid? userId,
        string? guestHash,
        string market,
        string currency,
        SalesChannel channel,
        DateTimeOffset now,
        DateTimeOffset expiresAt)
    {
        if (string.IsNullOrWhiteSpace(market))
        {
            throw new InvalidOperationException("بازار سبد اجباری است و Locale نیست.");
        }

        if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3)
        {
            throw new InvalidOperationException("ارز سبد باید کد سه حرفی باشد نه Locale.");
        }

        if (expiresAt <= now)
        {
            throw new InvalidOperationException("مهلت سبد باید بعد از ایجاد باشد.");
        }

        return new ShoppingCart
        {
            CartId = UuidV7.New(),
            Status = CartStatus.Active,
            AccessKind = accessKind,
            OwnerUserId = userId,
            GuestCredentialHash = guestHash,
            Market = market.Trim(),
            Currency = currency.Trim().ToUpperInvariant(),
            Channel = channel,
            ExpiresAt = expiresAt,
            ConversionIntent = CartConversionIntent.None,
            Version = 1,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    private void EnsureActive()
    {
        if (Status != CartStatus.Active)
        {
            throw new InvalidOperationException("فقط سبد Active قابل جهش خط است.");
        }
    }

    private void Touch(DateTimeOffset now)
    {
        UpdatedAt = now;
        Version++;
    }
}

/// <summary>
/// رویداد ایجاد سبد.
/// </summary>
public sealed class CartCreatedDomainEvent : IDomainEvent
{
    /// <summary>
    /// رویداد ایجاد را می‌سازد.
    /// </summary>
    public CartCreatedDomainEvent(Guid cartId, CartAccessKind accessKind)
    {
        CartId = cartId;
        AccessKind = accessKind;
        Metadata = EventMetadataFactory.ForDomain("cart.created.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>
    /// سبد ایجادشده.
    /// </summary>
    public Guid CartId { get; }

    /// <summary>
    /// گونهٔ دسترسی.
    /// </summary>
    public CartAccessKind AccessKind { get; }
}

/// <summary>
/// رویداد افزودن خط Offer.
/// </summary>
public sealed class CartLineAddedDomainEvent : IDomainEvent
{
    /// <summary>
    /// رویداد افزودن را می‌سازد.
    /// </summary>
    public CartLineAddedDomainEvent(Guid cartId, Guid lineId, Guid offerId, int quantity)
    {
        CartId = cartId;
        LineId = lineId;
        OfferId = offerId;
        Quantity = quantity;
        Metadata = EventMetadataFactory.ForDomain("cart.line_added.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>
    /// سبد.
    /// </summary>
    public Guid CartId { get; }

    /// <summary>
    /// خط.
    /// </summary>
    public Guid LineId { get; }

    /// <summary>
    /// Offer.
    /// </summary>
    public Guid OfferId { get; }

    /// <summary>
    /// تعداد.
    /// </summary>
    public int Quantity { get; }
}

/// <summary>
/// رویداد تغییر تعداد خط.
/// </summary>
public sealed class CartLineChangedDomainEvent : IDomainEvent
{
    /// <summary>
    /// رویداد تغییر را می‌سازد.
    /// </summary>
    public CartLineChangedDomainEvent(Guid cartId, Guid lineId, Guid offerId, int quantity)
    {
        CartId = cartId;
        LineId = lineId;
        OfferId = offerId;
        Quantity = quantity;
        Metadata = EventMetadataFactory.ForDomain("cart.line_changed.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>
    /// سبد.
    /// </summary>
    public Guid CartId { get; }

    /// <summary>
    /// خط.
    /// </summary>
    public Guid LineId { get; }

    /// <summary>
    /// Offer.
    /// </summary>
    public Guid OfferId { get; }

    /// <summary>
    /// تعداد جدید.
    /// </summary>
    public int Quantity { get; }
}

/// <summary>
/// رویداد حذف خط.
/// </summary>
public sealed class CartLineRemovedDomainEvent : IDomainEvent
{
    /// <summary>
    /// رویداد حذف را می‌سازد.
    /// </summary>
    public CartLineRemovedDomainEvent(Guid cartId, Guid lineId, Guid offerId)
    {
        CartId = cartId;
        LineId = lineId;
        OfferId = offerId;
        Metadata = EventMetadataFactory.ForDomain("cart.line_removed.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>
    /// سبد.
    /// </summary>
    public Guid CartId { get; }

    /// <summary>
    /// خط حذف‌شده.
    /// </summary>
    public Guid LineId { get; }

    /// <summary>
    /// Offer.
    /// </summary>
    public Guid OfferId { get; }
}

/// <summary>
/// رویداد انقضا یا رهاسازی سبد.
/// </summary>
public sealed class CartExpiredDomainEvent : IDomainEvent
{
    /// <summary>
    /// رویداد انقضا را می‌سازد.
    /// </summary>
    public CartExpiredDomainEvent(Guid cartId)
    {
        CartId = cartId;
        Metadata = EventMetadataFactory.ForDomain("cart.expired.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>
    /// سبد منقضی یا رهاشده.
    /// </summary>
    public Guid CartId { get; }
}

/// <summary>
/// رویداد درز تبدیل بدون ساختن Order.
/// </summary>
public sealed class CartConvertedDomainEvent : IDomainEvent
{
    /// <summary>
    /// رویداد تبدیل را می‌سازد.
    /// </summary>
    public CartConvertedDomainEvent(Guid cartId, CartConversionIntent intent)
    {
        CartId = cartId;
        Intent = intent;
        Metadata = EventMetadataFactory.ForDomain("cart.converted.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>
    /// سبد تبدیل‌شده.
    /// </summary>
    public Guid CartId { get; }

    /// <summary>
    /// مسیر سفارش آینده.
    /// </summary>
    public CartConversionIntent Intent { get; }
}
