using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tooba.BuildingBlocks;
using Tooba.Cart.Application.Ports;
using Tooba.Cart.Application.Lifetime;
using Tooba.Cart.Contracts.Errors;
using Tooba.Cart.Domain.Aggregates;
using Tooba.Cart.Domain.Entities;
using Tooba.Cart.Domain.ValueObjects;
using CartContract = Tooba.Cart.Contracts;
using Tooba.Cart.Infrastructure.Persistence;
using Tooba.Cart.Infrastructure.Security;
using Tooba.Catalog.Contracts;
using Tooba.Catalog.Contracts.Ports;
using Tooba.Inventory.Contracts.Availability;
using Tooba.Inventory.Contracts.Cart;
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
/// همکارهای تخصصی (quote/expiry/snapshot) در فایل‌های مجاور جدا شده‌اند.
/// </summary>
public sealed class CartDirectory : ICartDirectory, CartContract.ICartQueryGateway
{
    private readonly TimeSpan _persistenceTtl;
    private readonly CartDbContext _db;
    private readonly ICartUseCaseGuard _guard;
    private readonly ICartInventoryHoldPort _inventory;
    private readonly IInventoryAvailabilityGateway _availability;
    private readonly CartQuoteValidator _quote;
    private readonly CartSnapshotProjector _snapshots;
    private readonly CartExpiryScanner _expiry;
    private readonly ICartPersistenceHoursSource? _persistenceHours;
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
        _inventory = inventory;
        _availability = availability;
        _persistenceHours = persistenceHours;
        _commerceContext = commerceContext;
        _clock = clock;
        _ids = ids;
        _quote = new CartQuoteValidator(offers, prices, normalizer, catalog, campaignPrices);
        _snapshots = new CartSnapshotProjector(availability);
        _expiry = new CartExpiryScanner(db, inventory, guard);
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
        string defaultCurrency,
        SalesChannel channel,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        _ = CurrencyCode.Parse(defaultCurrency);
        var now = _clock.UtcNow;
        var ttl = await ResolvePersistenceTtlAsync(cancellationToken).ConfigureAwait(false);
        var cart = ShoppingCart.CreateAuthenticated(
            _ids.NewId(),
            userId, market, defaultCurrency, channel, now, now.Add(ttl));
        _db.Carts.Add(cart);
        await _db.SaveChangesAsync(cancellationToken);
        return await ToSnapshotAsync(cart, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<GuestCartCreated> CreateGuestAsync(
        string market,
        string defaultCurrency,
        SalesChannel channel,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        _ = CurrencyCode.Parse(defaultCurrency);
        var secret = CartCredentialHasher.CreateSecret();
        var now = _clock.UtcNow;
        var ttl = await ResolvePersistenceTtlAsync(cancellationToken).ConfigureAwait(false);
        var cart = ShoppingCart.CreateGuest(
            _ids.NewId(),
            CartCredentialHasher.Hash(secret), market, defaultCurrency, channel, now, now.Add(ttl));
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
        Guid? merchandisingCampaignId = null,
        string? requestedCurrency = null)
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
            // The line's own QuotedCurrency stays authoritative; requestedCurrency never switches it.
            return await ChangeLineCoreAsync(cart, existing, existing.Quantity + quantity, cancellationToken);
        }

        var lineCurrency = CartLineCurrency.ForNewLine(cart, requestedCurrency);
        var now = _clock.UtcNow;
        var (offer, quote, normalized, effectiveCampaignId) = await _quote.ValidateOfferAndQuoteAsync(
            cart,
            offerId,
            quantity,
            lineCurrency,
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
    public Task<int> ExpireDueCartsAsync(DateTimeOffset utcNow, int batchSize, CancellationToken cancellationToken) =>
        _expiry.ExpireDueCartsAsync(utcNow, batchSize, ReleaseAllAsync, SaveCartAsync, cancellationToken);

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
            throw new SemanticException(new SemanticError(CartErrorCodes.UserIdRequired));
        }

        ShoppingCart? guest = null;
        if (anonymousCartId is Guid cartId && cartId != Guid.Empty)
        {
            if (string.IsNullOrWhiteSpace(guestSecret))
            {
                throw new SemanticException(new SemanticError(CartErrorCodes.GuestInvalid));
            }

            guest = await LoadRequiredAsync(cartId, cancellationToken);
            if (guest.AccessKind == CartAccessKind.Authenticated)
            {
                if (guest.OwnerUserId != userId)
                {
                    throw new SemanticException(new SemanticError(CartErrorCodes.AccessDenied));
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
                throw new SemanticException(new SemanticError(CartErrorCodes.CommerceContextUnavailable));
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
        // Existing line requotes in its own authoritative QuotedCurrency; cart default never overrides it.
        var lineCurrency = CartLineCurrency.RequireForExistingLine(line);
        var (_, quote, normalized, effectiveCampaignId) = await _quote.ValidateOfferAndQuoteAsync(
            cart,
            line.OfferId,
            quantity,
            lineCurrency,
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
        string defaultCurrency,
        SalesChannel channel,
        CancellationToken cancellationToken)
    {
        _ = CurrencyCode.Parse(defaultCurrency);
        var now = _clock.UtcNow;
        var ttl = await ResolvePersistenceTtlAsync(cancellationToken).ConfigureAwait(false);
        var cart = ShoppingCart.CreateAuthenticated(
            _ids.NewId(),
            userId, market, defaultCurrency, channel, now, now.Add(ttl));
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
        // Merge currency truth: the target's own line currency, else the source line's currency.
        // cart.DefaultCurrency is never a fallback for an already-quoted line.
        var mergedLineCurrency = existing is not null
            ? CartLineCurrency.RequireForExistingLine(existing)
            : CartLineCurrency.RequireForExistingLine(source);
        decimal quotedAmount = source.QuotedAmount ?? 0;
        var quotedCurrency = mergedLineCurrency;
        var taxExclusive = source.QuotedTaxExclusive;
        var priceId = source.PriceId ?? Guid.Empty;
        try
        {
            var (_, quote, normalized, effectiveCampaign) = await _quote.ValidateOfferAndQuoteAsync(
                target,
                source.OfferId,
                quantity,
                mergedLineCurrency,
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
        catch (SemanticException)
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
            ?? throw new SemanticException(new SemanticError(CartErrorCodes.InventoryInsufficient));
        if (availability.Available < quantity)
        {
            throw new SemanticException(new SemanticError(CartErrorCodes.InventoryInsufficient));
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
        await LoadAsync(cartId, cancellationToken)
            ?? throw new SemanticException(new SemanticError(CartErrorCodes.Missing));

    private static void EnsureAccess(ShoppingCart cart, CartContract.CartAccess access)
    {
        if (cart.AccessKind == CartAccessKind.Authenticated)
        {
            if (access.UserId is null || access.UserId != cart.OwnerUserId)
            {
                throw new SemanticException(new SemanticError(CartErrorCodes.AccessDenied));
            }

            return;
        }

        if (string.IsNullOrWhiteSpace(access.GuestSecret)
            || string.IsNullOrWhiteSpace(cart.GuestCredentialHash)
            || !CartCredentialHasher.Matches(access.GuestSecret, cart.GuestCredentialHash))
        {
            throw new SemanticException(new SemanticError(CartErrorCodes.GuestInvalid));
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
            throw new SemanticException(new SemanticError(CartErrorCodes.VersionConflict));
        }
    }

    private Task<CartContract.CartSnapshot> ToSnapshotAsync(ShoppingCart cart, CancellationToken cancellationToken) =>
        _snapshots.ToSnapshotAsync(cart, cancellationToken);

    /// <summary>
    /// نقل‌قول خطوط کمپین را با واجدشرایطی جاری هم‌تراز می‌کند؛ تخفیف منقضی به Base برمی‌گردد.
    /// </summary>
    private async Task RevalidateCampaignQuotesAsync(ShoppingCart cart, CancellationToken cancellationToken)
    {
        if (_quote.HasCampaignAuthority is false || cart.Status != CartStatus.Active)
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
                var (_, quote, _, effectiveCampaign) = await _quote.ValidateOfferAndQuoteAsync(
                    cart,
                    line.OfferId,
                    line.Quantity,
                    CartLineCurrency.RequireForExistingLine(line),
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
            catch (SemanticException)
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
