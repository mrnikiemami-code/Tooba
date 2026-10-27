using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.Cart.Contracts;
using Tooba.Catalog.Contracts.Checkout;
using Tooba.Inventory.Contracts.Checkout;
using Tooba.Inventory.Contracts.Orders;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Contracts.Ports;
using Tooba.Order.Application.Checkout.Contracts;
using Tooba.Order.Contracts.Fulfillment;
using Tooba.Order.Infrastructure.Checkout.Persistence;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Pricing.Contracts;
using Tooba.Pricing.Contracts.Ports;
using Tooba.Promotion.Contracts.Checkout;
using Tooba.Tax.Contracts.Dtos;
using Tooba.Tax.Contracts.Ports;

namespace Tooba.Order.Tests.Checkout;

/// <summary>
/// هارنس واقعی <see cref="CheckoutDirectory"/> روی درزهای کنترل‌شدهٔ Cart/Offer/Pricing/Promotion/Tax.
/// سبد و درزهای بیرونی fake هستند؛ خود دایرکتوری و مسیر quote واقعی‌اند و هیچ درز بیرونی
/// (رزرو/تبدیل سبد/پرداخت/DbContext) صدا زده نمی‌شود.
/// </summary>
internal sealed class ShippingGroupGoodsFactsHarness : IDisposable
{
    internal const string Currency = "IRR";

    internal readonly Guid CartId = Guid.Parse("0199aaaa-0000-7000-8000-000000000001");
    /// <summary>کاربر عامل همین سبد؛ همان که دسترسی سبد و فرمان پیش‌نمایش باید حمل کنند.</summary>
    internal readonly Guid UserId = Guid.Parse("0199aaaa-0000-7000-8000-0000000000cc");
    internal readonly Guid SellerId = Guid.Parse("0199aaaa-0000-7000-8000-0000000000aa");
    internal readonly Guid OtherSellerId = Guid.Parse("0199aaaa-0000-7000-8000-0000000000bb");

    internal readonly FakeCartGateway Carts = new();
    internal readonly FakeOfferLookup Offers = new();
    internal readonly FakePriceLookup Prices = new();
    internal readonly FakePromotion Promotions = new();
    internal readonly FakeTaxCalculator Taxes = new();
    internal readonly FakeCatalogCheckout Catalog = new();
    internal readonly FakeGuard Guard = new();
    internal readonly NoCallInventoryLifecycle Inventory = new();
    internal readonly NoCallReservationPort Reservation = new();
    internal readonly NoCallCartConversion Conversion = new();

    internal readonly OrderDbContext Db;
    internal readonly CheckoutDirectory Directory;

    internal ShippingGroupGoodsFactsHarness()
    {
        Db = new OrderDbContext(new DbContextOptionsBuilder<OrderDbContext>().Options);
        Directory = new CheckoutDirectory(
            Db,
            Guard,
            Carts,
            Offers,
            Prices,
            Inventory,
            Taxes,
            Promotions,
            Catalog,
            new NoCallCancelGate(),
            new FixedClock(),
            new SequentialIds(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<Tooba.Order.Application.Checkout.Process.CheckoutProcessManager>.Instance,
            inventoryReservation: Reservation,
            cartConversion: Conversion);
    }

    /// <summary>خط سبد با قیمت واحد و ارز یگانه؛ Quoted* همان قرارداد سبد است.</summary>
    internal CartLineSnapshot Line(
        Guid lineId,
        Guid offerId,
        decimal unitPrice,
        decimal quantity = 1m,
        Guid? sellerPartyId = null)
    {
        var seller = sellerPartyId ?? SellerId;
        var variantId = VariantOf(offerId);
        Offers.Register(new OfferReference(offerId, variantId, seller, SalesChannel.Direct, OfferStatus.Active, null));
        var priceId = PriceOf(offerId);
        Prices.Register(new PriceQuote(priceId, offerId, "IR", SalesChannel.Direct, unitPrice, Currency, true, true));
        return new CartLineSnapshot(
            lineId,
            offerId,
            variantId,
            seller,
            quantity,
            ReservationId: null,
            QuotedAmount: unitPrice,
            QuotedCurrency: Currency,
            QuotedTaxExclusive: true,
            PriceId: priceId,
            QuotedAt: new DateTimeOffset(2026, 10, 25, 8, 0, 0, TimeSpan.Zero));
    }

    internal CartSnapshot Cart(params CartLineSnapshot[] lines) => new(
        CartId,
        CartStatus.Active,
        CartAccessKind.Authenticated,
        UserId,
        "IR",
        Currency,
        SalesChannel.Direct,
        DateTimeOffset.UtcNow.AddHours(1),
        CartConversionIntent.OnlinePurchase,
        Version: 7,
        lines);

    internal static Guid VariantOf(Guid offerId)
    {
        var bytes = offerId.ToByteArray();
        bytes[15] ^= 0x5a;
        return new Guid(bytes);
    }

    internal static Guid PriceOf(Guid offerId)
    {
        var bytes = offerId.ToByteArray();
        bytes[14] ^= 0x33;
        return new Guid(bytes);
    }

    internal SubmitCheckoutCommand Command(int expectedVersion = 7, string? coupon = null) => new(
        CartId,
        new CartAccess(Guid.Parse("0199aaaa-0000-7000-8000-0000000000cc"), null),
        expectedVersion,
        OrderMode.OnlinePurchase,
        BuyerPartyId: null,
        PlacedByUserId: Guid.Parse("0199aaaa-0000-7000-8000-0000000000dd"),
        IdempotencyKey: "facts",
        TaxJurisdiction: "IR",
        CouponCode: coupon);

    public void Dispose() => Db.Dispose();
}

internal sealed class FixedClock : IClock
{
    public DateTimeOffset UtcNow { get; } = new(2026, 10, 25, 9, 30, 0, TimeSpan.Zero);
}

internal sealed class SequentialIds : IIdGenerator
{
    private int _next;

    public Guid NewId() => Guid.Parse($"0199bbbb-0000-7000-8000-{++_next:D12}");
}

internal sealed class FakeGuard : IOrderUseCaseGuard
{
    internal int Calls { get; private set; }

    internal bool Deny { get; set; }

    public Task EnsureCanMutateAsync(CancellationToken cancellationToken)
    {
        Calls++;
        if (Deny)
        {
            throw new ContractOperationException("checkout.forbidden");
        }

        return Task.CompletedTask;
    }
}

internal sealed class FakeCartGateway : ICartQueryGateway
{
    internal int Calls { get; private set; }

    internal CartSnapshot? Cart { get; set; }

    internal CartAccess? LastAccess { get; private set; }

    public Task<CartSnapshot?> GetCartAsync(Guid cartId, CartAccess access, CancellationToken cancellationToken)
    {
        Calls++;
        LastAccess = access;
        return Task.FromResult(Cart);
    }
}

internal sealed class FakeOfferLookup : IOfferLookupGateway
{
    private readonly Dictionary<Guid, OfferReference> _offers = [];

    internal void Register(OfferReference offer) => _offers[offer.OfferId] = offer;

    public Task<OfferReference?> FindOfferAsync(Guid offerId, CancellationToken cancellationToken) =>
        Task.FromResult(_offers.TryGetValue(offerId, out var offer) ? offer : null);

    public Task<IReadOnlyDictionary<Guid, OfferReference>> FindOffersBatchAsync(
        IReadOnlyCollection<Guid> offerIds,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyDictionary<Guid, OfferReference>>(
            offerIds.Where(_offers.ContainsKey).ToDictionary(id => id, id => _offers[id]));

    public Task<IReadOnlyDictionary<Guid, int>> CountOffersByCatalogVariantIdsAsync(
        IReadOnlyCollection<Guid> variantIds,
        CancellationToken cancellationToken) =>
        throw new InvalidOperationException("offer count is not part of this slice");
}

internal sealed class FakePriceLookup : IPriceLookupGateway
{
    private readonly Dictionary<Guid, PriceQuote> _prices = [];

    internal int Calls { get; private set; }

    internal void Register(PriceQuote quote) => _prices[quote.OfferId] = quote;

    public Task<PriceQuote?> ResolvePriceAsync(PriceResolutionQuery query, CancellationToken cancellationToken)
    {
        Calls++;
        return Task.FromResult(_prices.TryGetValue(query.OfferId, out var quote) ? quote : null);
    }

    public Task<IReadOnlyDictionary<Guid, PriceQuote>> ResolvePricesBatchAsync(
        IReadOnlyCollection<Guid> offerIds,
        string market,
        SalesChannel channel,
        string currency,
        DateTimeOffset at,
        CancellationToken cancellationToken) =>
        throw new InvalidOperationException("batch price read is not part of this slice");

    public Task<IReadOnlyDictionary<Guid, PriceQuote>> ResolveCampaignPricesBatchAsync(
        IReadOnlyCollection<Guid> offerIds,
        Guid campaignId,
        string market,
        SalesChannel channel,
        string currency,
        DateTimeOffset at,
        CancellationToken cancellationToken) =>
        throw new InvalidOperationException("campaign price read is not part of this slice");
}

internal sealed class FakePromotion : ICheckoutPromotionPort
{
    internal readonly Dictionary<Guid, decimal> Discounts = [];
    internal int Calls { get; private set; }
    internal CancellationToken? LastToken { get; private set; }
    internal Exception? Failure { get; set; }

    public Task<CheckoutPromotionEvaluationResult> EvaluateForCheckoutAsync(
        CheckoutPromotionEvaluationRequest request,
        CancellationToken cancellationToken)
    {
        Calls++;
        LastToken = cancellationToken;
        if (Failure is not null)
        {
            return Task.FromException<CheckoutPromotionEvaluationResult>(Failure);
        }

        var discount = Discounts.GetValueOrDefault(request.OfferId);
        var postDiscount = request.BaseTaxExclusiveAmount - discount;
        return Task.FromResult(new CheckoutPromotionEvaluationResult(
            discount,
            postDiscount,
            discount == 0m
                ? []
                : [new CheckoutAppliedPromotion(
                    Guid.Parse("0199cccc-0000-7000-8000-000000000001"),
                    "coupon",
                    request.CouponCode,
                    CheckoutPromotionDiscountKind.FixedAmountOff)],
            []));
    }
}

internal sealed class FakeTaxCalculator : ITaxCalculator
{
    internal decimal Rate { get; set; } = 0.10m;
    internal int Calls { get; private set; }
    internal CancellationToken? LastToken { get; private set; }
    internal TaxCalculationResult? Override { get; set; }
    internal Exception? Failure { get; set; }

    public Task<TaxCalculationResult> CalculateAsync(
        TaxCalculationRequest request,
        CancellationToken cancellationToken)
    {
        Calls++;
        LastToken = cancellationToken;
        if (Failure is not null)
        {
            return Task.FromException<TaxCalculationResult>(Failure);
        }

        // نرخ ثابت روی مبلغ بدون مالیات پس از تخفیف؛ همان مسیر واقعی Tax.
        var taxAmount = Math.Round(request.TaxExclusiveAmount * Rate, 2, MidpointRounding.AwayFromZero);
        var result = Override ?? new TaxCalculationResult(
            TaxOutcome.Taxable,
            request.TaxExclusiveAmount,
            Rate,
            taxAmount,
            request.TaxExclusiveAmount + taxAmount,
            request.Currency,
            Guid.Parse("0199cccc-0000-7000-8000-0000000000ff"),
            null,
            request.At);
        return Task.FromResult(result);
    }
}

internal sealed class FakeCatalogCheckout : ICatalogCheckoutLookup
{
    public Task<IReadOnlyDictionary<Guid, Guid?>> GetPrimaryCategoryIdsByVariantIdsAsync(
        IReadOnlyCollection<Guid> variantIds,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyDictionary<Guid, Guid?>>(
            variantIds.ToDictionary(id => id, _ => (Guid?)null));

    public Task<IReadOnlyDictionary<Guid, EffectiveQuantityPolicy>> GetEffectiveQuantityPoliciesForVariantIdsAsync(
        IReadOnlyCollection<Guid> variantIds,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyDictionary<Guid, EffectiveQuantityPolicy>>(
            new Dictionary<Guid, EffectiveQuantityPolicy>());

    public Task<QuantityRoundingMode> GetGlobalRoundingModeAsync(CancellationToken cancellationToken) =>
        Task.FromResult(QuantityRoundingMode.Nearest);
}

internal sealed class NoCallCancelGate : ISellerOrderCancelFulfillmentGate
{
    public Task<SellerOrderCancelFulfillmentSnapshot?> GetAsync(
        Guid sellerOrderId,
        CancellationToken cancellationToken) =>
        throw new InvalidOperationException("cancel gate must not be called by goods facts");
}

internal sealed class NoCallReservationPort : ICheckoutInventoryReservationPort
{
    public Task<CheckoutInventoryReservationResult> ReserveForCheckoutAsync(
        CheckoutInventoryReservationRequest request,
        CancellationToken cancellationToken) =>
        throw new InvalidOperationException("inventory reservation must not be called by goods facts");

    public Task ReleaseAsync(IEnumerable<Guid> reservationIds, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("inventory release must not be called by goods facts");
}

internal sealed class NoCallCartConversion : ICartConversionPort
{
    public Task<CartConversionResult> ConvertForCheckoutAsync(
        CartConversionRequest request,
        CancellationToken cancellationToken) =>
        throw new InvalidOperationException("cart conversion must not be called by goods facts");
}

internal sealed class NoCallInventoryLifecycle : IOrderInventoryLifecyclePort
{
    public Task ReleaseHeldReservationAsync(Guid reservationId, CancellationToken cancellationToken) => NotCalled();

    public Task TryReleaseReservationAsync(Guid reservationId, CancellationToken cancellationToken) => NotCalled();

    public Task<Guid> ReacquireDurableHoldFromPreviousAsync(
        Guid previousReservationId,
        string externalReference,
        string idempotencyKey,
        CancellationToken cancellationToken) => NotCalled<Guid>();

    public Task<Guid> PromoteOrReacquireForManualPaymentReviewAsync(
        Guid reservationId,
        DateTimeOffset reviewExpiresAt,
        string externalReference,
        string idempotencyKey,
        CancellationToken cancellationToken) => NotCalled<Guid>();

    public Task ReleaseIfHeldAsync(Guid reservationId, CancellationToken cancellationToken) => NotCalled();

    public Task<OrderInventoryPaidSupplyResult> EnsurePaidDurableSupplyAsync(
        OrderInventoryPaidSupplyRequest request,
        CancellationToken cancellationToken) => NotCalled<OrderInventoryPaidSupplyResult>();

    public Task<OrderInventorySupplyResult> EnsureUnpaidRetryHoldAsync(
        OrderInventoryUnpaidRetryRequest request,
        CancellationToken cancellationToken) => NotCalled<OrderInventorySupplyResult>();

    public Task<IReadOnlyDictionary<Guid, OrderInventorySupplyStatusSnapshot>> GetSupplyStatusesAsync(
        IReadOnlyDictionary<Guid, IReadOnlyList<OrderInventorySupplyLine>> linesByCheckoutId,
        CancellationToken cancellationToken) => NotCalled<IReadOnlyDictionary<Guid, OrderInventorySupplyStatusSnapshot>>();

    public Task<OrderInventoryReservationView?> FindReservationAsync(
        Guid reservationId,
        CancellationToken cancellationToken) => NotCalled<OrderInventoryReservationView?>();

    public Task<OrderInventoryReservationView> ReserveAsync(
        Guid stockItemId,
        decimal quantity,
        string? externalReference,
        string? idempotencyKey,
        DateTimeOffset? expiresAt,
        CancellationToken cancellationToken) => NotCalled<OrderInventoryReservationView>();

    public Task CommitReservationForPaidOrderAsync(Guid reservationId, CancellationToken cancellationToken) =>
        NotCalled();

    public Task<OrderInventorySupplyStatusDetail> GetSupplyStatusDetailAsync(
        Guid checkoutId,
        IReadOnlyList<OrderInventorySupplyLine> lines,
        CancellationToken cancellationToken) => NotCalled<OrderInventorySupplyStatusDetail>();

    public Task<OrderInventoryEnsureDetailResult> EnsureSupplyDetailAsync(
        OrderInventoryEnsureDetailRequest request,
        CancellationToken cancellationToken) => NotCalled<OrderInventoryEnsureDetailResult>();

    private static Task NotCalled() =>
        throw new InvalidOperationException("inventory lifecycle must not be called by goods facts");

    private static Task<T> NotCalled<T>() =>
        throw new InvalidOperationException("inventory lifecycle must not be called by goods facts");
}

/// <summary>
/// نمونهٔ جایگزین فقط برای اثبات DI: همان قرارداد دایرکتوری و درز فکت‌ها، بدون هیچ منطقی.
/// </summary>
internal sealed class ProbeCheckoutDirectory : ICheckoutDirectory, ICheckoutShippingGroupGoodsFactsReader
{
    public Task<CheckoutSnapshot> SubmitAsync(SubmitCheckoutCommand command, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<CheckoutSnapshot> PreviewAsync(SubmitCheckoutCommand command, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<CheckoutSnapshot?> GetCheckoutAsync(
        Guid checkoutId,
        OrderAccess access,
        CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<SellerOrderSnapshot?> GetSellerOrderByNumberAsync(
        string orderNumber,
        OrderAccess access,
        CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task CancelSellerOrderAsync(
        Guid sellerOrderId,
        OrderAccess access,
        CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task RestoreCancelledCheckoutAsync(
        Guid checkoutId,
        OrderAccess access,
        CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<IReadOnlyList<CheckoutOperationalNoteSnapshot>> ListNotesAsync(
        Guid checkoutId,
        Guid viewerUserId,
        int take,
        CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<CheckoutOperationalNoteSnapshot> AddNoteAsync(
        Guid checkoutId,
        Guid actorUserId,
        string body,
        CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<CheckoutNoteDeleteOutcome> DeleteNoteAsync(
        Guid checkoutId,
        Guid noteId,
        Guid actorUserId,
        CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task RecordAdminViewAsync(Guid checkoutId, Guid viewerUserId, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<CheckoutShippingGroupGoodsFactsResult> ReadAsync(
        CheckoutShippingGroupGoodsFactsRequest request,
        CancellationToken cancellationToken) => throw new NotSupportedException();
}

internal sealed class FakeHostEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = Environments.Development;

    public string ApplicationName { get; set; } = "Tooba.Order.Tests";

    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
