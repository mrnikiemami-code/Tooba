using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tooba.BuildingBlocks;
using Tooba.Cart.Application.Ports;
using Tooba.Cart.Application.Lifetime;
using Tooba.Cart.Domain.Aggregates;
using Tooba.Cart.Domain.Entities;
using Tooba.Cart.Domain.Events;
using Tooba.Cart.Domain.ValueObjects;
using CartContract = Tooba.Cart.Contracts;
using Tooba.Cart.Infrastructure.Persistence;
using Tooba.Cart.Infrastructure.Security;
using Tooba.Catalog.Contracts;
using Tooba.Inventory.Contracts.Availability;
using Tooba.Inventory.Contracts.Cart;
using Tooba.Inventory.Contracts.Checkout;
using Tooba.Inventory.Contracts.Errors;
using Tooba.Inventory.Contracts.Orders;
using Tooba.Inventory.Contracts.Seller;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Contracts.Ports;
using Tooba.Pricing.Contracts;

namespace Tooba.Cart.Infrastructure.Directories;

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
public sealed class CartDirectory : ICartDirectory, CartContract.ICartQueryGateway
{
    private readonly TimeSpan _persistenceTtl;
    private readonly CartDbContext _db;
    private readonly ICartUseCaseGuard _guard;
    private readonly IOfferLookupGateway _offers;
    private readonly IPriceLookupGateway _prices;
    private readonly ICartInventoryHoldPort _inventory;
    private readonly IInventoryAvailabilityGateway _availability;
    private readonly ICatalogCartQuantityPolicyGateway? _catalog;
    private readonly IQuantityNormalizer _normalizer;
    private readonly ICartPersistenceHoursSource? _persistenceHours;
    private readonly ICampaignCartPriceAuthority? _campaignPrices;
    private readonly ICartCommerceContextResolver? _commerceContext;
    private readonly IClock _clock;
    private readonly IIdGenerator _ids;

    /// <summary>
    /// دایرکتوری را به schema Cart و درزهای Offer/Pricing/Inventory وصل می‌کند.
    /// </summary>
    public CartDirectory(
        CartDbContext db,
        ICartUseCaseGuard guard,
        IOfferLookupGateway offers,
        IPriceLookupGateway prices,
        ICartInventoryHoldPort inventory,
        IInventoryAvailabilityGateway availability,
        IQuantityNormalizer normalizer,
        IClock clock,
        IIdGenerator ids,
        ICatalogCartQuantityPolicyGateway? catalog = null,
        IOptions<CartLifetimeOptions>? lifetime = null,
        ICartPersistenceHoursSource? persistenceHours = null,
        ICampaignCartPriceAuthority? campaignPrices = null,
        ICartCommerceContextResolver? commerceContext = null)
    {
        _db = db;
        _guard = guard;
        _offers = offers;
        _prices = prices;
        _inventory = inventory;
        _availability = availability;
        _catalog = catalog;
        _normalizer = normalizer;
        _persistenceHours = persistenceHours;
        _campaignPrices = campaignPrices;
        _commerceContext = commerceContext;
        _clock = clock;
        _ids = ids;
        var hours = CartPersistenceHours.Clamp(lifetime?.Value.PersistenceHours ?? CartPersistenceHours.DefaultHours);
        _persistenceTtl = TimeSpan.FromHours(hours);
    }

    /// <inheritdoc />
    public async Task<CartContract.CartSnapshot?> GetCartAsync(Guid cartId, CartContract.CartAccess access, CancellationToken cancellationToken)
    {
        var cart = await LoadAsync(cartId, cancellationToken);
        if (cart is null)
        {
            return null;
        }

        EnsureAccess(cart, access);
        await RevalidateCampaignQuotesAsync(cart, cancellationToken);
        return await ToSnapshotAsync(cart, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<CartContract.CartSnapshot> CreateAuthenticatedAsync(
        Guid userId,
        string market,
        string currency,
        SalesChannel channel,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        _ = CurrencyCode.Parse(currency);
        var now = _clock.UtcNow;
        var ttl = await ResolvePersistenceTtlAsync(cancellationToken).ConfigureAwait(false);
        var cart = ShoppingCart.CreateAuthenticated(
            _ids.NewId(),
            userId, market, currency, channel, now, now.Add(ttl));
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
        var now = _clock.UtcNow;
        var ttl = await ResolvePersistenceTtlAsync(cancellationToken).ConfigureAwait(false);
        var cart = ShoppingCart.CreateGuest(
            _ids.NewId(),
            CartCredentialHasher.Hash(secret), market, currency, channel, now, now.Add(ttl));
        _db.Carts.Add(cart);
        await _db.SaveChangesAsync(cancellationToken);
        return new GuestCartCreated(await ToSnapshotAsync(cart, cancellationToken), secret);
    }

    /// <inheritdoc />
    public async Task<CartContract.CartSnapshot> AddOrIncreaseLineAsync(
        Guid cartId,
        CartContract.CartAccess access,
        int expectedVersion,
        Guid offerId,
        decimal quantity,
        CancellationToken cancellationToken,
        Guid? merchandisingCampaignId = null)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var cart = await LoadRequiredAsync(cartId, cancellationToken);
        EnsureAccess(cart, access);
        cart.EnsureVersion(expectedVersion);
        var existing = cart.FindLineByOffer(offerId);
        if (existing is not null)
        {
            // Prefer surviving campaign context when merging quantity into an existing line.
            var mergedCampaign = merchandisingCampaignId ?? existing.MerchandisingCampaignId;
            existing.SetMerchandisingCampaignId(mergedCampaign);
            return await ChangeLineCoreAsync(cart, existing, existing.Quantity + quantity, cancellationToken);
        }

        var now = _clock.UtcNow;
        var (offer, quote, normalized, effectiveCampaignId) = await ValidateOfferAndQuoteAsync(
            cart,
            offerId,
            quantity,
            now,
            merchandisingCampaignId,
            cancellationToken);
        quantity = normalized;
        var line = CartLine.Open(
            _ids.NewId(),
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
            now,
            effectiveCampaignId);
        await EnsureSellableAsync(offer.OfferId, quantity, cancellationToken);
        var ttl = await ResolvePersistenceTtlAsync(cancellationToken).ConfigureAwait(false);
        cart.RefreshExpiry(now.Add(ttl), now);
        cart.AddLine(line, now);
        await SaveCartAsync(cancellationToken);
        return await ToSnapshotAsync(cart, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<CartContract.CartSnapshot> ChangeLineQuantityAsync(
        Guid cartId,
        CartContract.CartAccess access,
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
    public async Task<CartContract.CartSnapshot> RemoveLineAsync(
        Guid cartId,
        CartContract.CartAccess access,
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
    public async Task AbandonAsync(Guid cartId, CartContract.CartAccess access, int expectedVersion, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var cart = await LoadRequiredAsync(cartId, cancellationToken);
        EnsureAccess(cart, access);
        cart.EnsureVersion(expectedVersion);
        await ReleaseAllAsync(cart, cancellationToken);
        cart.Abandon(_clock.UtcNow);
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
    public async Task<CartMergeResult> MergeAnonymousAfterLoginAsync(
        Guid userId,
        Guid? anonymousCartId,
        string? guestSecret,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        if (userId == Guid.Empty)
        {
            throw new InvalidOperationException("cart.user_id.required");
        }

        ShoppingCart? guest = null;
        if (anonymousCartId is Guid cartId && cartId != Guid.Empty)
        {
            if (string.IsNullOrWhiteSpace(guestSecret))
            {
                throw new InvalidOperationException("cart.guest_secret.invalid");
            }

            guest = await LoadRequiredAsync(cartId, cancellationToken);
            if (guest.AccessKind == CartAccessKind.Authenticated)
            {
                if (guest.OwnerUserId != userId)
                {
                    throw new InvalidOperationException("cart.access.denied");
                }

                return new CartMergeResult(await ToSnapshotAsync(guest, cancellationToken), false, (await ToSnapshotAsync(guest, cancellationToken)).Lines);
            }

            EnsureAccess(guest, new CartContract.CartAccess(null, guestSecret));
        }

        var authenticated = await _db.Carts
            .Include(x => x.Lines)
            .Where(x => x.OwnerUserId == userId && x.Status == CartStatus.Active && x.AccessKind == CartAccessKind.Authenticated)
            .OrderByDescending(x => x.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (guest is null)
        {
            if (_commerceContext is null)
            {
                throw new InvalidOperationException("cart.commerce.context_unavailable");
            }

            var context = _commerceContext.Resolve();
            authenticated ??= await CreateAuthenticatedCoreAsync(
                userId, context.Market, context.DefaultCurrency, context.Channel, cancellationToken);
            var empty = await ToSnapshotAsync(authenticated, cancellationToken);
            return new CartMergeResult(empty, false, empty.Lines);
        }

        if (authenticated is null || (authenticated.Lines.Count == 0 && guest.Lines.Count > 0))
        {
            if (authenticated is not null && authenticated.CartId != guest.CartId)
            {
                authenticated.Abandon(_clock.UtcNow);
            }

            guest.AdoptAuthenticatedOwner(userId, _clock.UtcNow);
            await SaveCartAsync(cancellationToken);
            var adopted = await ToSnapshotAsync(guest, cancellationToken);
            return new CartMergeResult(adopted, true, adopted.Lines);
        }

        foreach (var line in guest.Lines.ToList())
        {
            await MergeLineWithoutReservationAsync(authenticated, line, cancellationToken);
        }

        if (guest.CartId != authenticated.CartId)
        {
            await ReleaseAllAsync(guest, cancellationToken);
            guest.Abandon(_clock.UtcNow);
        }

        await SaveCartAsync(cancellationToken);
        var merged = await ToSnapshotAsync(authenticated, cancellationToken);
        return new CartMergeResult(merged, false, merged.Lines);
    }

    /// <inheritdoc />
    public async Task<CartContract.CartSnapshot?> FindActiveAuthenticatedAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            return null;
        }

        var cart = await _db.Carts
            .Include(x => x.Lines)
            .Where(x => x.OwnerUserId == userId && x.Status == CartStatus.Active && x.AccessKind == CartAccessKind.Authenticated)
            .OrderByDescending(x => x.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        return cart is null ? null : await ToSnapshotAsync(cart, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<CartContract.CartSnapshot> ConvertAsync(
        Guid cartId,
        CartContract.CartAccess access,
        int expectedVersion,
        CartContract.CartConversionIntent intent,
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
        cart.MarkConverted((CartConversionIntent)(int)intent, _clock.UtcNow);
        await SaveCartAsync(cancellationToken);
        return await ToSnapshotAsync(cart, cancellationToken);
    }

    private async Task<CartContract.CartSnapshot> ChangeLineCoreAsync(ShoppingCart cart, CartLine line, decimal quantity, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var (_, quote, normalized, effectiveCampaignId) = await ValidateOfferAndQuoteAsync(
            cart,
            line.OfferId,
            quantity,
            now,
            line.MerchandisingCampaignId,
            cancellationToken);
        quantity = normalized;
        await EnsureSellableAsync(line.OfferId, quantity, cancellationToken);
        if (line.ReservationId is { } oldId)
        {
            await _inventory.ReleaseAsync(oldId, cancellationToken);
            line.ClearReservation();
        }

        line.ReplaceHold(
            quantity,
            null,
            quote.Amount,
            quote.Currency,
            quote.TaxExclusive,
            quote.PriceId,
            now,
            effectiveCampaignId);
        var ttl = await ResolvePersistenceTtlAsync(cancellationToken).ConfigureAwait(false);
        cart.RefreshExpiry(now.Add(ttl), now);
        cart.RecordLineChanged(line.LineId, line.OfferId, quantity, now);
        await SaveCartAsync(cancellationToken);
        return await ToSnapshotAsync(cart, cancellationToken);
    }

    private async Task<CartContract.CartSnapshot> RemoveLineCoreAsync(ShoppingCart cart, CartLine line, CancellationToken cancellationToken)
    {
        if (line.ReservationId is { } reservationId)
        {
            await _inventory.ReleaseAsync(reservationId, cancellationToken);
            line.ClearReservation();
        }

        cart.RemoveLine(line.LineId, _clock.UtcNow);
        await SaveCartAsync(cancellationToken);
        return await ToSnapshotAsync(cart, cancellationToken);
    }

    private async Task<(OfferReference Offer, PriceQuote Quote, decimal Quantity, Guid? EffectiveCampaignId)> ValidateOfferAndQuoteAsync(
        ShoppingCart cart,
        Guid offerId,
        decimal quantity,
        DateTimeOffset now,
        Guid? merchandisingCampaignId,
        CancellationToken cancellationToken)
    {
        var offer = await _offers.FindOfferAsync(offerId, cancellationToken)
            ?? throw new InvalidOperationException("cart.offer.missing");
        if (offer.Status != OfferStatus.Active)
        {
            throw new InvalidOperationException("cart.offer.inactive");
        }

        if (offer.Channel != cart.Channel)
        {
            throw new InvalidOperationException("cart.offer.channel_mismatch");
        }

        if (_catalog is not null)
        {
            var policy = await _catalog.GetEffectiveQuantityPolicyForVariantAsync(offer.CatalogVariantId, cancellationToken)
                ?? throw new InvalidOperationException("cart.quantity_policy.missing");
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

        Guid? effectiveCampaignId = null;
        PriceQuote? campaignQuote = null;
        if (merchandisingCampaignId is Guid campaignId
            && campaignId != Guid.Empty
            && _campaignPrices is not null)
        {
            campaignQuote = await _campaignPrices.TryResolveEligibleCampaignPriceAsync(
                campaignId,
                offerId,
                cart.Market,
                cart.Channel,
                cart.Currency,
                now,
                cancellationToken);
            if (campaignQuote is not null)
            {
                effectiveCampaignId = campaignId;
            }
        }

        if (campaignQuote is not null)
        {
            return (offer, campaignQuote, quantity, effectiveCampaignId);
        }

        var quote = await _prices.ResolvePriceAsync(
            new PriceResolutionQuery(offerId, cart.Market, cart.Channel, cart.Currency, now, null, null, quantity),
            cancellationToken)
            ?? throw new InvalidOperationException("cart.pricing.quote_missing");
        return (offer, quote, quantity, null);
    }

    private async Task<TimeSpan> ResolvePersistenceTtlAsync(CancellationToken cancellationToken)
    {
        if (_persistenceHours is null)
        {
            return _persistenceTtl;
        }

        var hours = await _persistenceHours.ResolvePersistenceHoursAsync(cancellationToken).ConfigureAwait(false);
        return TimeSpan.FromHours(CartPersistenceHours.Clamp(hours));
    }

    private async Task<ShoppingCart> CreateAuthenticatedCoreAsync(
        Guid userId,
        string market,
        string currency,
        SalesChannel channel,
        CancellationToken cancellationToken)
    {
        _ = CurrencyCode.Parse(currency);
        var now = _clock.UtcNow;
        var ttl = await ResolvePersistenceTtlAsync(cancellationToken).ConfigureAwait(false);
        var cart = ShoppingCart.CreateAuthenticated(
            _ids.NewId(),
            userId, market, currency, channel, now, now.Add(ttl));
        _db.Carts.Add(cart);
        await _db.SaveChangesAsync(cancellationToken);
        return cart;
    }

    private async Task MergeLineWithoutReservationAsync(
        ShoppingCart target,
        CartLine source,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var existing = target.FindLineByOffer(source.OfferId);
        var quantity = (existing?.Quantity ?? 0) + source.Quantity;
        decimal quotedAmount = source.QuotedAmount ?? 0;
        var quotedCurrency = source.QuotedCurrency ?? target.Currency;
        var taxExclusive = source.QuotedTaxExclusive;
        var priceId = source.PriceId ?? Guid.Empty;
        try
        {
            var (_, quote, normalized, effectiveCampaign) = await ValidateOfferAndQuoteAsync(
                target,
                source.OfferId,
                quantity,
                now,
                source.MerchandisingCampaignId ?? existing?.MerchandisingCampaignId,
                cancellationToken);
            quantity = normalized;
            quotedAmount = quote.Amount;
            quotedCurrency = quote.Currency;
            taxExclusive = quote.TaxExclusive;
            priceId = quote.PriceId;
            if (existing is not null)
            {
                existing.SetMerchandisingCampaignId(effectiveCampaign);
            }
            else
            {
                source.SetMerchandisingCampaignId(effectiveCampaign);
            }
        }
        catch (InvalidOperationException)
        {
            // خط ادغام‌شده حذف نمی‌شود؛ موجودی/قیمت در snapshot بازتاب می‌شود.
        }

        if (existing is not null)
        {
            if (existing.ReservationId is { } oldId)
            {
                await _inventory.ReleaseAsync(oldId, cancellationToken);
                existing.ClearReservation();
            }

            existing.ReplaceHold(
                quantity,
                null,
                quotedAmount,
                quotedCurrency,
                taxExclusive,
                priceId,
                now,
                existing.MerchandisingCampaignId);
            target.RecordLineChanged(existing.LineId, existing.OfferId, quantity, now);
            return;
        }

        var line = CartLine.Open(
            _ids.NewId(),
            target.CartId,
            source.OfferId,
            source.CatalogVariantId,
            source.SellerPartyId,
            quantity,
            null,
            quotedAmount,
            quotedCurrency,
            taxExclusive,
            priceId,
            now,
            source.MerchandisingCampaignId);
        target.AddLine(line, now);
    }

    private async Task EnsureSellableAsync(Guid offerId, decimal quantity, CancellationToken cancellationToken)
    {
        var availability = await _availability.GetAvailabilityAsync(offerId, cancellationToken)
            ?? throw new InvalidOperationException("cart.inventory.missing");
        if (availability.Available < quantity)
        {
            throw new InvalidOperationException("cart.inventory.insufficient");
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
        await LoadAsync(cartId, cancellationToken) ?? throw new InvalidOperationException("cart.missing");

    private static void EnsureAccess(ShoppingCart cart, CartContract.CartAccess access)
    {
        if (cart.AccessKind == CartAccessKind.Authenticated)
        {
            if (access.UserId is null || access.UserId != cart.OwnerUserId)
            {
                throw new InvalidOperationException("cart.access.denied");
            }

            return;
        }

        if (string.IsNullOrWhiteSpace(access.GuestSecret)
            || string.IsNullOrWhiteSpace(cart.GuestCredentialHash)
            || !CartCredentialHasher.Matches(access.GuestSecret, cart.GuestCredentialHash))
        {
            throw new InvalidOperationException("cart.guest_secret.invalid");
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
            throw new InvalidOperationException("cart.version.stale");
        }
    }

    private async Task<CartContract.CartSnapshot> ToSnapshotAsync(ShoppingCart cart, CancellationToken cancellationToken)
    {
        var offerIds = cart.Lines.Select(x => x.OfferId).Distinct().ToArray();
        var availability = offerIds.Length == 0
            ? new Dictionary<Guid, InventoryAvailability>()
            : await _availability.GetAvailabilityBatchAsync(offerIds, cancellationToken);
        return new CartContract.CartSnapshot(
            cart.CartId,
            (CartContract.CartStatus)(int)cart.Status,
            (CartContract.CartAccessKind)(int)cart.AccessKind,
            cart.OwnerUserId,
            cart.Market,
            cart.Currency,
            cart.Channel,
            cart.ExpiresAt,
            (CartContract.CartConversionIntent)(int)cart.ConversionIntent,
            cart.Version,
            cart.Lines.Select(line =>
            {
                availability.TryGetValue(line.OfferId, out var stock);
                var available = stock?.Available ?? 0;
                var kind = available >= line.Quantity
                    ? CartContract.CartLineAvailabilityKind.Available
                    : available > 0
                        ? CartContract.CartLineAvailabilityKind.LimitedQuantity
                        : CartContract.CartLineAvailabilityKind.Unavailable;
                return new CartContract.CartLineSnapshot(
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
                    kind,
                    line.MerchandisingCampaignId);
            }).ToList());
    }

    /// <summary>
    /// نقل‌قول خطوط کمپین را با واجدشرایطی جاری هم‌تراز می‌کند؛ تخفیف منقضی به Base برمی‌گردد.
    /// </summary>
    private async Task RevalidateCampaignQuotesAsync(ShoppingCart cart, CancellationToken cancellationToken)
    {
        if (_campaignPrices is null || cart.Status != CartStatus.Active)
        {
            return;
        }

        var now = _clock.UtcNow;
        var changed = false;
        foreach (var line in cart.Lines.ToList())
        {
            if (line.MerchandisingCampaignId is null)
            {
                continue;
            }

            try
            {
                var (_, quote, _, effectiveCampaign) = await ValidateOfferAndQuoteAsync(
                    cart,
                    line.OfferId,
                    line.Quantity,
                    now,
                    line.MerchandisingCampaignId,
                    cancellationToken);
                if (line.QuotedAmount != quote.Amount
                    || !string.Equals(line.QuotedCurrency, quote.Currency, StringComparison.Ordinal)
                    || line.PriceId != quote.PriceId
                    || line.MerchandisingCampaignId != effectiveCampaign)
                {
                    line.ReplaceHold(
                        line.Quantity,
                        line.ReservationId,
                        quote.Amount,
                        quote.Currency,
                        quote.TaxExclusive,
                        quote.PriceId,
                        now,
                        effectiveCampaign);
                    cart.RecordLineChanged(line.LineId, line.OfferId, line.Quantity, now);
                    changed = true;
                }
            }
            catch (InvalidOperationException)
            {
                // leave line; availability snapshot will reflect issues
            }
        }

        if (changed)
        {
            await SaveCartAsync(cancellationToken);
        }
    }
}
