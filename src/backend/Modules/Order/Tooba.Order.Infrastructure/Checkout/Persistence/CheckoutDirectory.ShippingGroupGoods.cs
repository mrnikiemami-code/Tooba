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
        var membership = ValidateMembership(request.Groups, cart);

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

        var linesByCartLineId = BuildAuthoritativeLinesByCartLineId(snapshot, cart);
        var groups = new List<CheckoutShippingGroupGoodsFacts>(membership.Count);
        foreach (var group in membership)
        {
            groups.Add(BuildGroupFacts(group, linesByCartLineId, cartCurrency));
        }

        return new CheckoutShippingGroupGoodsFactsResult(
            cart.CartId,
            cart.Version,
            cartCurrency,
            groups);
    }

    /// <summary>
    /// افراز کامل خطوط سبد را می‌سنجد: کلید گروه خالی/تکراری، گروه خالی، خط ناشناخته/تکراری/مشترک،
    /// خط جاافتاده و فروشندهٔ مختلط رد می‌شوند. کوچک‌کردن بی‌صدا فهرست مجاز نیست.
    /// </summary>
    private static List<(CheckoutShippingGroupMembership Group, Guid SellerPartyId)> ValidateMembership(
        IReadOnlyList<CheckoutShippingGroupMembership>? groups,
        CartSnapshot cart)
    {
        if (groups is null || groups.Count == 0)
        {
            throw new ContractOperationException(GroupPartitionInvalid);
        }

        var sellerByCartLineId = cart.Lines.ToDictionary(line => line.LineId, line => line.SellerPartyId);
        var seenGroupKeys = new HashSet<string>(StringComparer.Ordinal);
        var seenCartLineIds = new HashSet<Guid>();
        var validated = new List<(CheckoutShippingGroupMembership, Guid)>(groups.Count);

        foreach (var group in groups)
        {
            if (group is null || string.IsNullOrWhiteSpace(group.GroupKey))
            {
                throw new ContractOperationException(GroupPartitionInvalid);
            }

            if (group.GroupKey.Length != group.GroupKey.Trim().Length
                || !seenGroupKeys.Add(group.GroupKey))
            {
                throw new ContractOperationException(GroupPartitionInvalid);
            }

            if (group.CartLineIds is null || group.CartLineIds.Count == 0)
            {
                throw new ContractOperationException(GroupPartitionInvalid);
            }

            Guid? sellerPartyId = null;
            foreach (var cartLineId in group.CartLineIds)
            {
                if (!sellerByCartLineId.TryGetValue(cartLineId, out var lineSeller))
                {
                    throw new ContractOperationException(GroupPartitionInvalid);
                }

                if (!seenCartLineIds.Add(cartLineId))
                {
                    throw new ContractOperationException(GroupPartitionInvalid);
                }

                if (sellerPartyId is null)
                {
                    sellerPartyId = lineSeller;
                }
                else if (sellerPartyId.Value != lineSeller)
                {
                    // گروه ارسال نمی‌تواند خطوط چند فروشنده را در یک مبلغ ترکیب کند.
                    throw new ContractOperationException(GroupPartitionInvalid);
                }
            }

            validated.Add((group, sellerPartyId!.Value));
        }

        if (seenCartLineIds.Count != cart.Lines.Count)
        {
            // خط جاافتاده = فکت ناقص؛ هرگز با فهرست کوچک‌تر ادامه نمی‌دهیم.
            throw new ContractOperationException(GroupPartitionInvalid);
        }

        return validated;
    }

    /// <summary>
    /// خطوط authoritative همان پیش‌نمایش را با نقشهٔ دقیق خط سبد→خط سفارش تطبیق می‌دهد؛ هرگز با
    /// OfferId/مقدار حدس زده نمی‌شود.
    /// </summary>
    private Dictionary<Guid, OrderLineSnapshot> BuildAuthoritativeLinesByCartLineId(
        CheckoutSnapshot snapshot,
        CartSnapshot cart)
    {
        var lines = snapshot.SellerOrders.SelectMany(order => order.Lines).ToList();
        var result = new Dictionary<Guid, OrderLineSnapshot>(cart.Lines.Count);
        foreach (var cartLine in cart.Lines)
        {
            if (!_orderLineIdByCartLineId.TryGetValue(cartLine.LineId, out var orderLineId))
            {
                throw new ContractOperationException(GroupPartitionInvalid);
            }

            var matches = lines.Where(line => line.LineId == orderLineId).ToList();
            if (matches.Count != 1
                || matches[0].OfferId != cartLine.OfferId
                || matches[0].SellerPartyId != cartLine.SellerPartyId
                || matches[0].Quantity != cartLine.Quantity)
            {
                throw new ContractOperationException(GroupPartitionInvalid);
            }

            result[cartLine.LineId] = matches[0];
        }

        if (result.Count != lines.Count)
        {
            // خط سفارش بدون خط سبد یا برعکس = نگاشت ناقص/متناقض.
            throw new ContractOperationException(GroupPartitionInvalid);
        }

        return result;
    }

    /// <summary>
    /// جمع مقدار نهایی اعضای یک گروه؛ مبالغ فقط خوانده می‌شوند و نه دوباره مالیات/تخفیف می‌گیرند.
    /// </summary>
    private static CheckoutShippingGroupGoodsFacts BuildGroupFacts(
        (CheckoutShippingGroupMembership Group, Guid SellerPartyId) group,
        IReadOnlyDictionary<Guid, OrderLineSnapshot> linesByCartLineId,
        string cartCurrency)
    {
        var contributions = new List<CheckoutShippingGroupGoodsContribution>(group.Group.CartLineIds.Count);
        decimal finalAmount = 0m;
        decimal quantityTotal = 0m;

        foreach (var cartLineId in group.Group.CartLineIds)
        {
            var line = linesByCartLineId[cartLineId];
            if (!string.Equals(line.Currency, cartCurrency, StringComparison.Ordinal)
                || line.TaxInclusiveSnapshot < 0m
                || line.DiscountAmountSnapshot < 0m
                || line.PostDiscountTaxExclusiveSnapshot < 0m
                || line.TaxAmountSnapshot < 0m)
            {
                throw new ContractOperationException(GroupGoodsAmountInvalid);
            }

            finalAmount += line.TaxInclusiveSnapshot;
            quantityTotal += line.Quantity;
            contributions.Add(new CheckoutShippingGroupGoodsContribution(
                cartLineId,
                line.LineId,
                line.OfferId,
                line.Quantity,
                line.UnitPriceSnapshot,
                line.PreDiscountTaxExclusiveSnapshot,
                line.DiscountAmountSnapshot,
                line.PostDiscountTaxExclusiveSnapshot,
                line.TaxAmountSnapshot,
                line.TaxInclusiveSnapshot,
                line.Currency));
        }

        return new CheckoutShippingGroupGoodsFacts(
            group.Group.GroupKey,
            group.SellerPartyId,
            cartCurrency,
            finalAmount,
            quantityTotal,
            // اندازه‌گیری authoritative در این برش وجود ندارد؛ عدد ساختگی ساخته نمی‌شود.
            GroupWeightGrams: null,
            PhysicalItemCount: null,
            contributions);
    }
}
