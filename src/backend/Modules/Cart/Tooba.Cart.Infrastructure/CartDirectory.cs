using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tooba.BuildingBlocks;
using Tooba.Cart.Application;
using Tooba.Cart.Domain;
using Tooba.Cart.Infrastructure.Persistence;
using Tooba.Catalog.Application;
using Tooba.Inventory.Application;
using Tooba.Offer.Application;
using Tooba.Offer.Domain;
using Tooba.Pricing.Application;
using Tooba.Pricing.Domain;

namespace Tooba.Cart.Infrastructure;

/// <summary>
/// نگهبان باز موردکاربرد. ماتریس هویت اینجا نیست.
/// </summary>
public sealed class OpenCartUseCaseGuard : ICartUseCaseGuard
{
    /// <inheritdoc />
    public Task EnsureCanMutateAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

/// <summary>
/// نوشتن سبد با قرارداد Offer/Pricing/Inventory. DbContext آن ماژول‌ها لمس نمی‌شود و تراکنش توزیع‌شده نیست.
/// سیاست شکست: اعتبارسنجی موجودی بدون رزرو سخت. رزرو تاریخی سبد فقط آزاد می‌شود و تمدید نمی‌شود.
/// </summary>
public sealed class CartDirectory : ICartDirectory, ICartQueryGateway
{
    private readonly TimeSpan _persistenceTtl;
    private readonly CartDbContext _db;
    private readonly ICartUseCaseGuard _guard;
    private readonly IOfferLookupGateway _offers;
    private readonly IPriceLookupGateway _prices;
    private readonly IInventoryDirectory _inventory;
    private readonly IInventoryAvailabilityGateway _availability;
    private readonly ICatalogLookupGateway? _catalog;
    private readonly IQuantityNormalizer _normalizer;

    /// <summary>
    /// دایرکتوری را به schema Cart و درزهای Offer/Pricing/Inventory وصل می‌کند.
    /// </summary>
    public CartDirectory(
        CartDbContext db,
        ICartUseCaseGuard guard,
        IOfferLookupGateway offers,
        IPriceLookupGateway prices,
        IInventoryDirectory inventory,
        IInventoryAvailabilityGateway availability,
        ICatalogLookupGateway? catalog = null,
        IQuantityNormalizer? normalizer = null,
        IOptions<CartLifetimeOptions>? lifetime = null)
    {
        _db = db;
        _guard = guard;
        _offers = offers;
        _prices = prices;
        _inventory = inventory;
        _availability = availability;
        _catalog = catalog;
        _normalizer = normalizer ?? new QuantityNormalizer();
        var hours = Math.Clamp(lifetime?.Value.PersistenceHours ?? 168, 1, 24 * 90);
        _persistenceTtl = TimeSpan.FromHours(hours);
    }

    /// <inheritdoc />
    public async Task<CartSnapshot?> GetCartAsync(Guid cartId, CartAccess access, CancellationToken cancellationToken)
    {
        var cart = await LoadAsync(cartId, cancellationToken);
        if (cart is null)
        {
            return null;
        }

        EnsureAccess(cart, access);
        return await ToSnapshotAsync(cart, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<CartSnapshot> CreateAuthenticatedAsync(
        Guid userId,
        string market,
        string currency,
        SalesChannel channel,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        _ = CurrencyCode.Parse(currency);
        var now = DateTimeOffset.UtcNow;
        var cart = ShoppingCart.CreateAuthenticated(userId, market, currency, channel, now, now.Add(_persistenceTtl));
        _db.Carts.Add(cart);
        await _db.SaveChangesAsync(cancellationToken);
        return await ToSnapshotAsync(cart, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<GuestCartCreated> CreateGuestAsync(
        string market,
        string currency,
        SalesChannel channel,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        _ = CurrencyCode.Parse(currency);
        var secret = CartCredentialHasher.CreateSecret();
        var now = DateTimeOffset.UtcNow;
        var cart = ShoppingCart.CreateGuest(CartCredentialHasher.Hash(secret), market, currency, channel, now, now.Add(_persistenceTtl));
        _db.Carts.Add(cart);
        await _db.SaveChangesAsync(cancellationToken);
        return new GuestCartCreated(await ToSnapshotAsync(cart, cancellationToken), secret);
    }

    /// <inheritdoc />
    public async Task<CartSnapshot> AddOrIncreaseLineAsync(
        Guid cartId,
        CartAccess access,
        int expectedVersion,
        Guid offerId,
        decimal quantity,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var cart = await LoadRequiredAsync(cartId, cancellationToken);
        EnsureAccess(cart, access);
        cart.EnsureVersion(expectedVersion);
        var existing = cart.FindLineByOffer(offerId);
        if (existing is not null)
        {
            return await ChangeLineCoreAsync(cart, existing, existing.Quantity + quantity, cancellationToken);
        }

        var now = DateTimeOffset.UtcNow;
        var (offer, quote, normalized) = await ValidateOfferAndQuoteAsync(cart, offerId, quantity, now, cancellationToken);
        quantity = normalized;
        var line = CartLine.Open(
            cart.CartId,
            offer.OfferId,
            offer.CatalogVariantId,
            offer.SellerPartyId,
            quantity,
            null,
            quote.Amount,
            quote.Currency,
            quote.TaxExclusive,
            quote.PriceId,
            now);
        await EnsureSellableAsync(offer.OfferId, quantity, cancellationToken);
        cart.RefreshExpiry(now.Add(_persistenceTtl), now);
        cart.AddLine(line, now);
        await SaveCartAsync(cancellationToken);
        return await ToSnapshotAsync(cart, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<CartSnapshot> ChangeLineQuantityAsync(
        Guid cartId,
        CartAccess access,
        int expectedVersion,
        Guid lineId,
        decimal quantity,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var cart = await LoadRequiredAsync(cartId, cancellationToken);
        EnsureAccess(cart, access);
        cart.EnsureVersion(expectedVersion);
        var line = cart.RequireLine(lineId);
        if (quantity == 0)
        {
            return await RemoveLineCoreAsync(cart, line, cancellationToken);
        }

        return await ChangeLineCoreAsync(cart, line, quantity, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<CartSnapshot> RemoveLineAsync(
        Guid cartId,
        CartAccess access,
        int expectedVersion,
        Guid lineId,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var cart = await LoadRequiredAsync(cartId, cancellationToken);
        EnsureAccess(cart, access);
        cart.EnsureVersion(expectedVersion);
        return await RemoveLineCoreAsync(cart, cart.RequireLine(lineId), cancellationToken);
    }

    /// <inheritdoc />
    public async Task AbandonAsync(Guid cartId, CartAccess access, int expectedVersion, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var cart = await LoadRequiredAsync(cartId, cancellationToken);
        EnsureAccess(cart, access);
        cart.EnsureVersion(expectedVersion);
        await ReleaseAllAsync(cart, cancellationToken);
        cart.Abandon(DateTimeOffset.UtcNow);
        await SaveCartAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> ExpireDueCartsAsync(DateTimeOffset utcNow, int batchSize, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var limit = Math.Max(1, batchSize);
        var total = 0;
        while (true)
        {
            var expired = await ExpireDueBatchAsync(utcNow, limit, cancellationToken).ConfigureAwait(false);
            total += expired;
            if (expired < limit)
            {
                break;
            }
        }

        await _inventory.ReleaseExpiredHoldsAsync(utcNow, limit, cancellationToken).ConfigureAwait(false);
        return total;
    }

    private async Task<int> ExpireDueBatchAsync(DateTimeOffset utcNow, int batchSize, CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var ids = await _db.Database
            .SqlQuery<Guid>(
                $"""
                 SELECT c.cart_id AS "Value"
                 FROM cart.carts AS c
                 WHERE c.status = 'Active'
                   AND c.expires_at IS NOT NULL
                   AND c.expires_at <= {utcNow}
                 ORDER BY c.expires_at
                 LIMIT {batchSize}
                 FOR UPDATE SKIP LOCKED
                 """)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        if (ids.Count == 0)
        {
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return 0;
        }

        var due = await _db.Carts
            .Include(x => x.Lines)
            .Where(x => ids.Contains(x.CartId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        foreach (var cart in due)
        {
            await ReleaseAllAsync(cart, cancellationToken).ConfigureAwait(false);
            cart.Expire(utcNow);
        }

        await SaveCartAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return due.Count;
    }

    /// <inheritdoc />
    public async Task<CartSnapshot> ConvertAsync(
        Guid cartId,
        CartAccess access,
        int expectedVersion,
        CartConversionIntent intent,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var cart = await LoadRequiredAsync(cartId, cancellationToken);
        EnsureAccess(cart, access);
        if (cart.Status == CartStatus.Converted)
        {
            return await ToSnapshotAsync(cart, cancellationToken);
        }

        cart.EnsureVersion(expectedVersion);
        cart.MarkConverted(intent, DateTimeOffset.UtcNow);
        await SaveCartAsync(cancellationToken);
        return await ToSnapshotAsync(cart, cancellationToken);
    }

    private async Task<CartSnapshot> ChangeLineCoreAsync(ShoppingCart cart, CartLine line, decimal quantity, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var (_, quote, normalized) = await ValidateOfferAndQuoteAsync(cart, line.OfferId, quantity, now, cancellationToken);
        quantity = normalized;
        await EnsureSellableAsync(line.OfferId, quantity, cancellationToken);
        if (line.ReservationId is { } oldId)
        {
            await _inventory.ReleaseAsync(oldId, cancellationToken);
            line.ClearReservation();
        }

        line.ReplaceHold(quantity, null, quote.Amount, quote.Currency, quote.TaxExclusive, quote.PriceId, now);
        cart.RefreshExpiry(now.Add(_persistenceTtl), now);
        cart.RecordLineChanged(line.LineId, line.OfferId, quantity, now);
        await SaveCartAsync(cancellationToken);
        return await ToSnapshotAsync(cart, cancellationToken);
    }

    private async Task<CartSnapshot> RemoveLineCoreAsync(ShoppingCart cart, CartLine line, CancellationToken cancellationToken)
    {
        if (line.ReservationId is { } reservationId)
        {
            await _inventory.ReleaseAsync(reservationId, cancellationToken);
            line.ClearReservation();
        }

        cart.RemoveLine(line.LineId, DateTimeOffset.UtcNow);
        await SaveCartAsync(cancellationToken);
        return await ToSnapshotAsync(cart, cancellationToken);
    }

    private async Task<(OfferReference Offer, PriceQuote Quote, decimal Quantity)> ValidateOfferAndQuoteAsync(
        ShoppingCart cart,
        Guid offerId,
        decimal quantity,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var offer = await _offers.FindOfferAsync(offerId, cancellationToken)
            ?? throw new InvalidOperationException("Offer از قرارداد Lookup پیدا نشد؛ DbContext Offer خوانده نشد.");
        if (offer.Status != OfferStatus.Active)
        {
            throw new InvalidOperationException("Offer غیرفعال یا بایگانی‌شده به سبد اضافه نمی‌شود.");
        }

        if (offer.Channel != cart.Channel)
        {
            throw new InvalidOperationException("کانال Offer با زمینهٔ سبد یکی نیست.");
        }

        if (_catalog is not null)
        {
            var policy = await _catalog.GetEffectiveQuantityPolicyForVariantAsync(offer.CatalogVariantId, cancellationToken)
                ?? throw new InvalidOperationException("سیاست مقدار گونه از Catalog پیدا نشد.");
            quantity = _normalizer.Normalize(quantity, policy);
        }

        CartLine.EnsureQuantity(quantity);
        if (offer.MinimumOrderQuantity is { } min && quantity < min)
        {
            throw new InvalidOperationException("offer.min_quantity.not_met");
        }

        if (offer.MaximumOrderQuantity is { } max && quantity > max)
        {
            throw new InvalidOperationException("offer.max_quantity.exceeded");
        }

        var quote = await _prices.ResolvePriceAsync(
            new PriceResolutionQuery(offerId, cart.Market, cart.Channel, cart.Currency, now, null, null, quantity),
            cancellationToken)
            ?? throw new InvalidOperationException("نقل‌قول قیمت از قرارداد Pricing پیدا نشد؛ مبلغ روی Product/Offer نیست.");
        return (offer, quote, quantity);
    }

    private async Task EnsureSellableAsync(Guid offerId, decimal quantity, CancellationToken cancellationToken)
    {
        var availability = await _availability.GetAvailabilityAsync(offerId, cancellationToken)
            ?? throw new InvalidOperationException("موجودی Offer از قرارداد Inventory پیدا نشد؛ جدول Inventory اینجا join نشد.");
        if (availability.Available < quantity)
        {
            throw new InvalidOperationException("موجودی قابل‌فروش برای خط سبد کافی نیست.");
        }
    }

    private async Task ReleaseAllAsync(ShoppingCart cart, CancellationToken cancellationToken)
    {
        foreach (var line in cart.Lines.ToList())
        {
            if (line.ReservationId is { } reservationId)
            {
                await _inventory.ReleaseAsync(reservationId, cancellationToken);
                line.ClearReservation();
            }
        }
    }

    private async Task<ShoppingCart?> LoadAsync(Guid cartId, CancellationToken cancellationToken) =>
        await _db.Carts.Include(x => x.Lines).SingleOrDefaultAsync(x => x.CartId == cartId, cancellationToken);

    private async Task<ShoppingCart> LoadRequiredAsync(Guid cartId, CancellationToken cancellationToken) =>
        await LoadAsync(cartId, cancellationToken) ?? throw new InvalidOperationException("سبد پیدا نشد.");

    private static void EnsureAccess(ShoppingCart cart, CartAccess access)
    {
        if (cart.AccessKind == CartAccessKind.Authenticated)
        {
            if (access.UserId is null || access.UserId != cart.OwnerUserId)
            {
                throw new InvalidOperationException("سبد واردشده بدون UserId مطابق قابل‌دسترسی نیست؛ CartId Bearer نیست.");
            }

            return;
        }

        if (string.IsNullOrWhiteSpace(access.GuestSecret)
            || string.IsNullOrWhiteSpace(cart.GuestCredentialHash)
            || !CartCredentialHasher.Matches(access.GuestSecret, cart.GuestCredentialHash))
        {
            throw new InvalidOperationException("راز مهمان نامعتبر است؛ CartId به‌تنهایی مجوز نیست و راز خام در پایگاه نیست.");
        }
    }

    private async Task SaveCartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InvalidOperationException("نسخهٔ سبد کهنه است؛ جهش همزمان خط رد شد.");
        }
    }

    private async Task<CartSnapshot> ToSnapshotAsync(ShoppingCart cart, CancellationToken cancellationToken)
    {
        var offerIds = cart.Lines.Select(x => x.OfferId).Distinct().ToArray();
        var availability = offerIds.Length == 0
            ? new Dictionary<Guid, InventoryAvailability>()
            : await _availability.GetAvailabilityBatchAsync(offerIds, cancellationToken);
        return new CartSnapshot(
            cart.CartId,
            cart.Status,
            cart.AccessKind,
            cart.OwnerUserId,
            cart.Market,
            cart.Currency,
            cart.Channel,
            cart.ExpiresAt,
            cart.ConversionIntent,
            cart.Version,
            cart.Lines.Select(line =>
            {
                availability.TryGetValue(line.OfferId, out var stock);
                var available = stock?.Available ?? 0;
                var kind = available >= line.Quantity
                    ? CartLineAvailabilityKind.Available
                    : available > 0
                        ? CartLineAvailabilityKind.LimitedQuantity
                        : CartLineAvailabilityKind.Unavailable;
                return new CartLineSnapshot(
                    line.LineId,
                    line.OfferId,
                    line.CatalogVariantId,
                    line.SellerPartyId,
                    line.Quantity,
                    line.ReservationId,
                    line.QuotedAmount,
                    line.QuotedCurrency,
                    line.QuotedTaxExclusive,
                    line.PriceId,
                    line.QuotedAt,
                    kind);
            }).ToList());
    }
}
