using Tooba.BuildingBlocks;
using Tooba.Cart.Contracts;
using Tooba.Order.Application.Checkout.Contracts;
using Tooba.Order.Application.Storefront.Services;

namespace Tooba.Order.Infrastructure.Checkout.Persistence;

/// <summary>
/// سازندهٔ مشترک فکت‌های مالی گروه ارسال از <b>خطوط سفارش واقعی</b> یک نقل‌قول مشخص.
/// </summary>
/// <remarks>
/// <para>
/// Preview (۲۱۷، از پیش‌نمایش authoritative) و ثبت نهایی (TB-233-R1، از همان خطوط
/// <c>SellerOrder</c> همین submit) هر دو از همین یک سازنده استفاده می‌کنند تا ریاضیات تخفیف/مالیات
/// هیچ‌گاه کپی یا دوباره‌سازی نشود.
/// </para>
/// <para>
/// مبلغ گروه = جمع <c>TaxInclusiveSnapshot</c> همان اعضا؛ هیچ سهمی از جمع کل سبد و هیچ مبلغ ارسالی
/// فراخوان پذیرفته نمی‌شود. وزن/تعداد فیزیکی منبع authoritative ندارند و تهی می‌مانند.
/// </para>
/// </remarks>
internal static class ShippingGroupGoodsFactsBuilder
{
    /// <summary>
    /// همان فیلدهای لازم از یک خط authoritative؛ چه از snapshot پیش‌نمایش و چه از خط
    /// <c>SellerOrder</c> همین submit ساخته شود، ریاضیات یکی است.
    /// </summary>
    internal readonly record struct FactsLine(
        Guid LineId,
        Guid OfferId,
        Guid SellerPartyId,
        decimal Quantity,
        string Currency,
        decimal UnitPriceSnapshot,
        decimal PreDiscountTaxExclusive,
        decimal DiscountAmount,
        decimal PostDiscountTaxExclusive,
        decimal TaxAmount,
        decimal TaxInclusive);

    /// <summary>کد خطای افراز نامعتبر گروه‌های ارسال.</summary>
    internal const string GroupPartitionInvalid = CheckoutDirectory.GroupPartitionInvalid;

    /// <summary>کد خطای ناسازگاری ارز یا مبلغ غیرمجاز در فکت گروه.</summary>
    internal const string GroupGoodsAmountInvalid = CheckoutDirectory.GroupGoodsAmountInvalid;

    /// <summary>
    /// افراز کامل خطوط سبد را می‌سنجد: کلید گروه خالی/تکراری، گروه خالی، خط ناشناخته/تکراری/مشترک،
    /// خط جاافتاده و فروشندهٔ مختلط رد می‌شوند.
    /// </summary>
    internal static List<(CheckoutShippingGroupMembership Group, Guid SellerPartyId)> ValidateMembership(
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
    /// خطوط authoritative یک نقل‌قول را با نقشهٔ دقیق خط سبد→خط سفارش تطبیق می‌دهد؛ هرگز با
    /// OfferId/مقدار حدس زده نمی‌شود.
    /// </summary>
    internal static Dictionary<Guid, FactsLine> BuildAuthoritativeLinesByCartLineId(
        IReadOnlyList<OrderLineSnapshot> lines,
        CartSnapshot cart,
        IReadOnlyDictionary<Guid, Guid> orderLineIdByCartLineId) =>
        Match(
            lines.Select(line => new FactsLine(
                line.LineId,
                line.OfferId,
                line.SellerPartyId,
                line.Quantity,
                line.Currency,
                line.UnitPriceSnapshot,
                line.PreDiscountTaxExclusiveSnapshot,
                line.DiscountAmountSnapshot,
                line.PostDiscountTaxExclusiveSnapshot,
                line.TaxAmountSnapshot,
                line.TaxInclusiveSnapshot)).ToList(),
            cart,
            orderLineIdByCartLineId);

    /// <summary>همان تطبیق، از خطوط <c>SellerOrder</c> همین submit (TB-233-R1).</summary>
    internal static Dictionary<Guid, FactsLine> BuildAuthoritativeLinesByCartLineId(
        IReadOnlyList<Tooba.Order.Domain.Aggregates.OrderLine> lines,
        CartSnapshot cart,
        IReadOnlyDictionary<Guid, Guid> orderLineIdByCartLineId) =>
        Match(
            lines.Select(line => new FactsLine(
                line.LineId,
                line.OfferId,
                line.SellerPartyId,
                line.Quantity,
                line.Currency,
                line.UnitPriceSnapshot,
                line.PreDiscountTaxExclusiveSnapshot,
                line.DiscountAmountSnapshot,
                line.PostDiscountTaxExclusiveSnapshot,
                line.TaxAmountSnapshot,
                line.TaxInclusiveSnapshot)).ToList(),
            cart,
            orderLineIdByCartLineId);

    private static Dictionary<Guid, FactsLine> Match(
        IReadOnlyList<FactsLine> lines,
        CartSnapshot cart,
        IReadOnlyDictionary<Guid, Guid> orderLineIdByCartLineId)
    {
        var result = new Dictionary<Guid, FactsLine>(cart.Lines.Count);
        foreach (var cartLine in cart.Lines)
        {
            if (!orderLineIdByCartLineId.TryGetValue(cartLine.LineId, out var orderLineId))
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

    /// <summary>جمع مقدار نهایی اعضای یک گروه؛ مبالغ فقط خوانده می‌شوند و نه دوباره مالیات/تخفیف می‌گیرند.</summary>
    internal static CheckoutShippingGroupGoodsFacts BuildGroupFacts(
        (CheckoutShippingGroupMembership Group, Guid SellerPartyId) group,
        IReadOnlyDictionary<Guid, FactsLine> linesByCartLineId,
        string cartCurrency)
    {
        var contributions = new List<CheckoutShippingGroupGoodsContribution>(group.Group.CartLineIds.Count);
        decimal finalAmount = 0m;
        decimal quantityTotal = 0m;

        foreach (var cartLineId in group.Group.CartLineIds)
        {
            var line = linesByCartLineId[cartLineId];
            if (!string.Equals(line.Currency, cartCurrency, StringComparison.Ordinal)
                || line.TaxInclusive < 0m
                || line.DiscountAmount < 0m
                || line.PostDiscountTaxExclusive < 0m
                || line.TaxAmount < 0m)
            {
                throw new ContractOperationException(GroupGoodsAmountInvalid);
            }

            finalAmount += line.TaxInclusive;
            quantityTotal += line.Quantity;
            contributions.Add(new CheckoutShippingGroupGoodsContribution(
                cartLineId,
                line.LineId,
                line.OfferId,
                line.Quantity,
                line.UnitPriceSnapshot,
                line.PreDiscountTaxExclusive,
                line.DiscountAmount,
                line.PostDiscountTaxExclusive,
                line.TaxAmount,
                line.TaxInclusive,
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
