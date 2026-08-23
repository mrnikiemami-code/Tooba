using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Cart.Application;
using Tooba.Cart.Domain;
using Tooba.Inventory.Application;
using Tooba.Offer.Application;
using Tooba.Offer.Domain;
using Tooba.Order.Application;
using Tooba.Order.Domain;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Pricing.Application;

namespace Tooba.Order.Infrastructure;

/// <summary>
/// نگهبان باز موردکاربرد Order. ماتریس هویت اینجا نیست.
/// </summary>
public sealed class OpenOrderUseCaseGuard : IOrderUseCaseGuard
{
    /// <inheritdoc />
    public Task EnsureCanMutateAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

/// <summary>
/// ارکستراسیون checkout: سبد از قرارداد Cart، قیمت از Pricing، Offer از Lookup، رزرو از Inventory.
/// تراکنش توزیع‌شده نیست. نقل‌قول سبد حقیقت تسویه نیست؛ اختلاف قیمت با PRICE_CHANGED شکست می‌خورد.
/// </summary>
public sealed class CheckoutDirectory : ICheckoutDirectory
{
    private readonly OrderDbContext _db;
    private readonly IOrderUseCaseGuard _guard;
    private readonly ICartQueryGateway _carts;
    private readonly ICartDirectory _cartMutations;
    private readonly IOfferLookupGateway _offers;
    private readonly IPriceLookupGateway _prices;
    private readonly IInventoryDirectory _inventory;

    /// <summary>
    /// دایرکتوری را به schema order و درزهای ماژول‌های دیگر وصل می‌کند.
    /// </summary>
    public CheckoutDirectory(
        OrderDbContext db,
        IOrderUseCaseGuard guard,
        ICartQueryGateway carts,
        ICartDirectory cartMutations,
        IOfferLookupGateway offers,
        IPriceLookupGateway prices,
        IInventoryDirectory inventory)
    {
        _db = db;
        _guard = guard;
        _carts = carts;
        _cartMutations = cartMutations;
        _offers = offers;
        _prices = prices;
        _inventory = inventory;
    }

    /// <inheritdoc />
    public async Task<CheckoutSnapshot> SubmitAsync(SubmitCheckoutCommand command, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var existing = await _db.Checkouts
            .Include(x => x.SellerOrders)
            .ThenInclude(x => x.Lines)
            .SingleOrDefaultAsync(x => x.IdempotencyKey == command.IdempotencyKey.Trim(), cancellationToken);
        if (existing is not null)
        {
            EnsureAccess(existing, new OrderAccess(command.BuyerPartyId, command.PlacedByUserId));
            return ToSnapshot(existing);
        }

        var cart = await _carts.GetCartAsync(command.CartId, command.CartAccess, cancellationToken)
            ?? throw new InvalidOperationException("سبد برای checkout پیدا نشد؛ CartId Bearer نیست.");
        if (cart.Status != CartStatus.Active)
        {
            throw new InvalidOperationException("فقط سبد Active به سفارش تبدیل می‌شود.");
        }

        if (cart.Version != command.ExpectedCartVersion)
        {
            throw new InvalidOperationException("نسخهٔ سبد کهنه است؛ checkout همزمان رد شد.");
        }

        if (cart.Lines.Count == 0)
        {
            throw new InvalidOperationException("سبد خالی به سفارش تبدیل نمی‌شود.");
        }

        var now = DateTimeOffset.UtcNow;
        var checkoutId = UuidV7.New();
        var sellerOrders = new List<SellerOrder>();
        var sequence = 0;
        foreach (var sellerGroup in cart.Lines.GroupBy(x => x.SellerPartyId))
        {
            sequence++;
            var sellerOrderId = UuidV7.New();
            var lines = new List<OrderLine>();
            foreach (var cartLine in sellerGroup)
            {
                var offer = await _offers.FindOfferAsync(cartLine.OfferId, cancellationToken)
                    ?? throw new InvalidOperationException("Offer از قرارداد Lookup پیدا نشد؛ DbContext Offer خوانده نشد.");
                if (offer.Status != OfferStatus.Active)
                {
                    throw new InvalidOperationException("Offer غیرفعال در checkout پذیرفته نمی‌شود.");
                }

                if (offer.SellerPartyId != cartLine.SellerPartyId)
                {
                    throw new InvalidOperationException("فروشندهٔ Offer با خط سبد یکی نیست.");
                }

                var quote = await _prices.ResolvePriceAsync(
                    new PriceResolutionQuery(
                        cartLine.OfferId,
                        cart.Market,
                        cart.Channel,
                        cart.Currency,
                        now,
                        command.BuyerPartyId,
                        null,
                        cartLine.Quantity),
                    cancellationToken)
                    ?? throw new InvalidOperationException("نقل‌قول قیمت از قرارداد Pricing پیدا نشد.");

                if (cartLine.QuotedAmount is null
                    || cartLine.QuotedAmount != quote.Amount
                    || !string.Equals(cartLine.QuotedCurrency, quote.Currency, StringComparison.Ordinal)
                    || cartLine.QuotedTaxExclusive != quote.TaxExclusive)
                {
                    throw new InvalidOperationException("PRICE_CHANGED");
                }

                if (cartLine.ReservationId is null)
                {
                    throw new InvalidOperationException("خط سبد بدون رزرو موجودی به سفارش تبدیل نمی‌شود.");
                }

                lines.Add(OrderLine.FromCheckout(
                    sellerOrderId,
                    cartLine.OfferId,
                    cartLine.CatalogVariantId,
                    cartLine.SellerPartyId,
                    cartLine.Quantity,
                    quote.Amount,
                    quote.Currency,
                    quote.TaxExclusive,
                    quote.PriceId,
                    cartLine.ReservationId));
            }

            sellerOrders.Add(SellerOrder.Open(
                checkoutId,
                sellerGroup.Key,
                BuildOrderNumber(now, sequence),
                command.Mode,
                cart.Currency,
                lines));
        }

        var group = CheckoutGroup.Submit(
            checkoutId,
            command.IdempotencyKey,
            cart.CartId,
            command.Mode,
            command.BuyerPartyId,
            command.PlacedByUserId,
            cart.Market,
            cart.Currency,
            cart.Channel,
            sellerOrders,
            now);
        _db.Checkouts.Add(group);
        await _db.SaveChangesAsync(cancellationToken);

        var intent = command.Mode == OrderMode.RequestToReserve
            ? CartConversionIntent.RequestToReserve
            : CartConversionIntent.OnlinePurchase;
        await _cartMutations.ConvertAsync(cart.CartId, command.CartAccess, cart.Version, intent, cancellationToken);
        return ToSnapshot(group);
    }

    /// <inheritdoc />
    public async Task<CheckoutSnapshot?> GetCheckoutAsync(Guid checkoutId, OrderAccess access, CancellationToken cancellationToken)
    {
        var group = await _db.Checkouts
            .Include(x => x.SellerOrders)
            .ThenInclude(x => x.Lines)
            .SingleOrDefaultAsync(x => x.CheckoutId == checkoutId, cancellationToken);
        if (group is null)
        {
            return null;
        }

        if (!group.CanBeViewedBy(access.BuyerPartyId, access.PlacedByUserId))
        {
            return null;
        }

        return ToSnapshot(group);
    }

    /// <inheritdoc />
    public async Task<SellerOrderSnapshot?> GetSellerOrderByNumberAsync(string orderNumber, OrderAccess access, CancellationToken cancellationToken)
    {
        var order = await _db.SellerOrders
            .Include(x => x.Lines)
            .SingleOrDefaultAsync(x => x.OrderNumber == orderNumber, cancellationToken);
        if (order is null)
        {
            return null;
        }

        var group = await _db.Checkouts.SingleAsync(x => x.CheckoutId == order.CheckoutId, cancellationToken);
        if (!group.CanBeViewedBy(access.BuyerPartyId, access.PlacedByUserId))
        {
            return null;
        }

        return ToSellerSnapshot(order);
    }

    /// <inheritdoc />
    public async Task CancelSellerOrderAsync(Guid sellerOrderId, OrderAccess access, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var order = await _db.SellerOrders
            .Include(x => x.Lines)
            .SingleOrDefaultAsync(x => x.SellerOrderId == sellerOrderId, cancellationToken)
            ?? throw new InvalidOperationException("سفارش فروشنده پیدا نشد.");
        var group = await _db.Checkouts.SingleAsync(x => x.CheckoutId == order.CheckoutId, cancellationToken);
        EnsureAccess(group, access);
        foreach (var line in order.Lines)
        {
            if (line.ReservationId is { } reservationId)
            {
                await _inventory.ReleaseAsync(reservationId, cancellationToken);
            }
        }

        order.Cancel();
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static void EnsureAccess(CheckoutGroup group, OrderAccess access)
    {
        if (!group.CanBeViewedBy(access.BuyerPartyId, access.PlacedByUserId))
        {
            throw new InvalidOperationException("دسترسی به سفارش بدون هویت خریدار یا کاربر عامل رد شد؛ شمارهٔ سفارش Bearer نیست.");
        }
    }

    private static string BuildOrderNumber(DateTimeOffset now, int sequence) =>
        $"TB-{now.UtcDateTime:yyyyMMddHHmmss}-{sequence:D2}-{Guid.NewGuid().ToString("N")[..6]}";

    private static CheckoutSnapshot ToSnapshot(CheckoutGroup group) =>
        new(
            group.CheckoutId,
            group.CartId,
            group.Mode,
            group.BuyerPartyId,
            group.PlacedByUserId,
            group.Market,
            group.Currency,
            group.Channel,
            group.SubmittedAt,
            group.SellerOrders.Select(ToSellerSnapshot).ToList());

    private static SellerOrderSnapshot ToSellerSnapshot(SellerOrder order) =>
        new(
            order.SellerOrderId,
            order.OrderNumber,
            order.SellerPartyId,
            order.Status,
            order.SubtotalSnapshot,
            order.TaxSnapshot,
            order.DiscountSnapshot,
            order.GrandTotalSnapshot,
            order.Currency,
            order.Lines.Select(line => new OrderLineSnapshot(
                line.LineId,
                line.OfferId,
                line.CatalogVariantId,
                line.SellerPartyId,
                line.Quantity,
                line.UnitPriceSnapshot,
                line.LineTotalSnapshot,
                line.Currency,
                line.TaxExclusive,
                line.PriceId,
                line.ReservationId)).ToList());
}
