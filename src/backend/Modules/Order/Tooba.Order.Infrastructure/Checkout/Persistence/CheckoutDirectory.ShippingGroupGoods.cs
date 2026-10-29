using Tooba.BuildingBlocks;
using Tooba.Cart.Contracts;
using Tooba.Order.Application.Checkout.Contracts;
using Tooba.Order.Application.Storefront.Services;

namespace Tooba.Order.Infrastructure.Checkout.Persistence;

/// <summary>
/// درز فقط-خواندنی فکت‌های مالی گروه ارسال روی <b>همان</b> پیش‌نمایش authoritative checkout.
/// مبلغ هر گروه از جمع مقدار نهایی خطوط <b>همان گروه</b> می‌آید، نه از مبلغ ارسالی مرورگر، نه از
/// جمع کل سبد و نه از بازمحاسبهٔ دوبارهٔ تخفیف/مالیات.
/// </summary>
/// <remarks>
/// منطق افراز/تطبیق/جمع در <see cref="ShippingGroupGoodsFactsBuilder"/> است و همین سازنده در ثبت نهایی
/// TB-233-R1 روی خطوط <c>SellerOrder</c> همان submit هم به کار می‌رود؛ هیچ ریاضیات کپی نمی‌شود.
/// </remarks>
public sealed partial class CheckoutDirectory : ICheckoutShippingGroupGoodsFactsReader
{
    /// <summary>کد خطای افراز نامعتبر گروه‌های ارسال.</summary>
    public const string GroupPartitionInvalid = "checkout.shipping_groups.invalid";

    /// <summary>کد خطای ناسازگاری ارز یا مبلغ غیرمجاز در فکت گروه.</summary>
    public const string GroupGoodsAmountInvalid = "checkout.shipping_groups.amount_invalid";

    /// <inheritdoc />
    public async Task<CheckoutShippingGroupGoodsFactsResult> ReadAsync(
        CheckoutShippingGroupGoodsFactsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var command = request.Command ?? throw new ContractOperationException(GroupPartitionInvalid);

        // دسترسی/وضعیت/خالی‌بودن سبد اولین قدم است؛ هیچ quote‌ای پیش از آن صدا زده نمی‌شود.
        var cart = await LoadPreviewCartAsync(command, cancellationToken);
        if (cart.Version != command.ExpectedCartVersion)
        {
            throw new ContractOperationException("PRICE_CHANGED");
        }

        // افراز کامل و بدون ابهام خطوط سبد، پیش از هر Pricing/Promotion/Tax.
        var membership = ShippingGroupGoodsFactsBuilder.ValidateMembership(request.Groups, cart);

        var goodsCommand = command with
        {
            // مبلغ ارسال/انتخاب گروه در این فکت‌ها سهمی ندارد و نباید باعث ماندگاری سفارش شود.
            ShippingAmount = 0m,
            ShippingGroups = null,
        };
        var snapshot = await QuotePreviewSnapshotAsync(cart, goodsCommand, cancellationToken);
        var cartCurrency = StorefrontCartCurrencyCompatibility.ResolveSoleCurrency(cart);
        if (!string.Equals(snapshot.Currency, cartCurrency, StringComparison.Ordinal))
        {
            throw new ContractOperationException(GroupGoodsAmountInvalid);
        }

        var linesByCartLineId = ShippingGroupGoodsFactsBuilder.BuildAuthoritativeLinesByCartLineId(
            snapshot.SellerOrders.SelectMany(order => order.Lines).ToList(),
            cart,
            _orderLineIdByCartLineId);

        var weightByVariant = await _catalog.GetShippingWeightGramsByVariantIdsAsync(
            cart.Lines.Select(line => line.CatalogVariantId).Distinct().ToArray(),
            cancellationToken);

        var groups = new List<CheckoutShippingGroupGoodsFacts>(membership.Count);
        foreach (var group in membership)
        {
            var facts = ShippingGroupGoodsFactsBuilder.BuildGroupFacts(group, linesByCartLineId, cartCurrency);
            groups.Add(facts with
            {
                GroupWeightGrams = ResolveGroupWeightGrams(cart, group.Group.CartLineIds, weightByVariant),
            });
        }

        return new CheckoutShippingGroupGoodsFactsResult(
            cart.CartId,
            cart.Version,
            cartCurrency,
            groups);
    }

    /// <summary>
    /// وزن گروه از اندازه‌گیری authoritative هر خط؛ اگر هر خط وزن نداشته باشد تهی می‌ماند.
    /// </summary>
    private static long? ResolveGroupWeightGrams(
        CartSnapshot cart,
        IReadOnlyList<Guid> cartLineIds,
        IReadOnlyDictionary<Guid, long?> weightByVariant)
    {
        long total = 0;
        foreach (var cartLineId in cartLineIds)
        {
            var line = cart.Lines.First(candidate => candidate.LineId == cartLineId);
            if (!weightByVariant.TryGetValue(line.CatalogVariantId, out var unitGrams) || unitGrams is null)
            {
                return null;
            }

            var lineGrams = (decimal)unitGrams.Value * line.Quantity;
            if (lineGrams <= 0m || lineGrams != decimal.Truncate(lineGrams) || lineGrams > long.MaxValue)
            {
                return null;
            }

            total += (long)lineGrams;
        }

        return total > 0 ? total : null;
    }
}
