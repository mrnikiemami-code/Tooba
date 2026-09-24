using Tooba.Offer.Contracts.Dtos;
using Tooba.BuildingBlocks;
using Tooba.Cart.Domain.Entities;
using Tooba.Cart.Domain.ValueObjects;
using Tooba.Cart.Domain.Events;

namespace Tooba.Cart.Domain.Aggregates;

/// <summary>
/// Domain type.
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
    public CartAccessKind AccessKind { get; private set; }

    /// <summary>
    /// هویت User برای سبد واردشده.
    /// </summary>
    public Guid? OwnerUserId { get; private set; }

    /// <summary>
    /// هش SHA-256 راز مهمان؛ راز خام ذخیره نمی‌شود.
    /// </summary>
    public string? GuestCredentialHash { get; private set; }

    /// <summary>
    /// بازار تجاری سبد. Locale نیست.
    /// </summary>
    public string Market { get; init; } = string.Empty;

    /// <summary>
    /// ارز پیش‌فرض انتخاب خط تازه. Locale یا نام نمایشی نیست و ارز تراکنش نیست:
    /// این مقدار فقط انتخاب اولیهٔ سبد است و ارز حقیقی هر خط از نقل‌قول Pricing روی همان خط می‌آید.
    /// </summary>
    public string DefaultCurrency { get; init; } = string.Empty;

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
    /// سبد واردشده می‌سازد. <paramref name="defaultCurrency"/> فقط انتخاب پیش‌فرض خط تازه است.
    /// </summary>
    public static ShoppingCart CreateAuthenticated(
        Guid cartId,
        Guid userId,
        string market,
        string defaultCurrency,
        SalesChannel channel,
        DateTimeOffset now,
        DateTimeOffset expiresAt)
    {
        if (userId == Guid.Empty)
        {
            throw new InvalidOperationException("cart.user_id.required");
        }

        var cart = CreateCore(cartId, CartAccessKind.Authenticated, userId, null, market, defaultCurrency, channel, now, expiresAt);
        cart._domainEvents.Add(new CartCreatedDomainEvent(cart.CartId, cart.AccessKind));
        return cart;
    }

    /// <summary>
    /// سبد مهمان می‌سازد؛ فقط هش راز را نگه می‌دارد. <paramref name="defaultCurrency"/> فقط انتخاب پیش‌فرض خط تازه است.
    /// </summary>
    public static ShoppingCart CreateGuest(
        Guid cartId,
        string guestCredentialHash,
        string market,
        string defaultCurrency,
        SalesChannel channel,
        DateTimeOffset now,
        DateTimeOffset expiresAt)
    {
        if (string.IsNullOrWhiteSpace(guestCredentialHash))
        {
            throw new InvalidOperationException("cart.guest_secret.hash_required");
        }

        var cart = CreateCore(cartId, CartAccessKind.Guest, null, guestCredentialHash.Trim(), market, defaultCurrency, channel, now, expiresAt);
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
            throw new InvalidOperationException("cart.line.merge_via_quantity");
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
        ?? throw new InvalidOperationException("cart.line.missing");

    /// <summary>
    /// پس از تغییر تعداد، رویداد و نسخه را جلو می‌برد.
    /// </summary>
    public void RecordLineChanged(Guid lineId, Guid offerId, decimal quantity, DateTimeOffset now)
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
            throw new InvalidOperationException("cart.converted.not_expirable");
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
            throw new InvalidOperationException("cart.convert.order_required");
        }

        Status = CartStatus.Converted;
        ConversionIntent = intent;
        Touch(now);
        _domainEvents.Add(new CartConvertedDomainEvent(CartId, intent));
    }

    /// <summary>
    /// سبد مهمان را به مالک احرازشده منتسب می‌کند و راز مهمان را باطل می‌کند.
    /// </summary>
    public void AdoptAuthenticatedOwner(Guid userId, DateTimeOffset now)
    {
        EnsureActive();
        if (userId == Guid.Empty)
        {
            throw new InvalidOperationException("cart.user_id.required");
        }

        if (AccessKind != CartAccessKind.Guest)
        {
            throw new InvalidOperationException("cart.assign.guest_only");
        }

        AccessKind = CartAccessKind.Authenticated;
        OwnerUserId = userId;
        GuestCredentialHash = null;
        Touch(now);
    }

    /// <summary>
    /// مهلت UTC سبد را برای رزروهای جدید هم‌تراز می‌کند.
    /// </summary>
    public void RefreshExpiry(DateTimeOffset expiresAt, DateTimeOffset now)
    {
        EnsureActive();
        if (expiresAt <= now)
        {
            throw new InvalidOperationException("cart.expiry.future_required");
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
            throw new InvalidOperationException("cart.version.stale");
        }
    }

    private static ShoppingCart CreateCore(
        Guid cartId,
        CartAccessKind accessKind,
        Guid? userId,
        string? guestHash,
        string market,
        string defaultCurrency,
        SalesChannel channel,
        DateTimeOffset now,
        DateTimeOffset expiresAt)
    {
        if (string.IsNullOrWhiteSpace(market))
        {
            throw new InvalidOperationException("cart.market.required");
        }

        // Default selection shape only. This is NOT the currency invariant of all CartLines;
        // each line's authoritative currency comes from its own Pricing quote.
        if (string.IsNullOrWhiteSpace(defaultCurrency) || defaultCurrency.Trim().Length != 3)
        {
            throw new InvalidOperationException("cart.currency.invalid");
        }

        if (expiresAt <= now)
        {
            throw new InvalidOperationException("cart.expiry.after_created");
        }

        return new ShoppingCart
        {
            CartId = cartId,
            Status = CartStatus.Active,
            AccessKind = accessKind,
            OwnerUserId = userId,
            GuestCredentialHash = guestHash,
            Market = market.Trim(),
            DefaultCurrency = defaultCurrency.Trim().ToUpperInvariant(),
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
            throw new InvalidOperationException("cart.line.requires_active");
        }
    }

    private void Touch(DateTimeOffset now)
    {
        UpdatedAt = now;
        Version++;
    }
}
