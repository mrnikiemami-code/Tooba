using Tooba.BuildingBlocks;

namespace Tooba.Order.Domain.Checkout;

/// <summary>
/// مادی‌ساز قطعی گروه‌های ارسال سفارش. تمام ناوردایی‌های مالکیت و پوشش خطوط اینجا اعمال می‌شوند
/// و هر نقض با کد پایدار <see cref="OrderShippingGroupFault"/> پیش از هر اثر جانبی متوقف می‌شود.
/// </summary>
public static class OrderShippingGroupMaterializer
{
    /// <summary>
    /// گروه‌های پایدار را از پلن پیش‌نویس می‌سازد.
    /// ناوردایی‌ها: هر خط سفارش دقیقاً در یک گروه؛ فروشندهٔ گروه همان فروشندهٔ سفارش/خط؛
    /// نقشهٔ خط سبد به خط سفارش کامل؛ یک گروه برای هر (فروشنده، روش).
    /// </summary>
    /// <param name="plans">پلن گروه‌های پیش‌نویس (خالی = مسیر قدیمی تک‌روشی بدون گروه).</param>
    /// <param name="orderLines">خطوط سفارش ساخته‌شده با مالکیت فروشنده.</param>
    /// <param name="sellerOrderIdBySellerPartyId">نگاشت فروشنده به سفارش فروشنده در همین checkout.</param>
    /// <param name="orderLineIdByCartLineId">نقشهٔ قطعی خط سبد به خط سفارش در همین تبدیل.</param>
    /// <param name="checkoutId">checkout مالک.</param>
    /// <param name="now">زمان snapshot.</param>
    public static IReadOnlyList<OrderShippingGroup> Materialize(
        IReadOnlyList<OrderShippingGroupPlan> plans,
        IReadOnlyList<(Guid OrderLineId, Guid SellerPartyId)> orderLines,
        IReadOnlyDictionary<Guid, Guid> sellerOrderIdBySellerPartyId,
        IReadOnlyDictionary<Guid, Guid> orderLineIdByCartLineId,
        Guid checkoutId,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(plans);
        ArgumentNullException.ThrowIfNull(orderLines);
        ArgumentNullException.ThrowIfNull(sellerOrderIdBySellerPartyId);
        ArgumentNullException.ThrowIfNull(orderLineIdByCartLineId);

        if (plans.Count == 0)
        {
            // Legacy single-method orders written before M2 carry no group rows; nothing to freeze.
            return [];
        }

        var sellerByOrderLine = orderLines.ToDictionary(x => x.OrderLineId, x => x.SellerPartyId);
        var assigned = new HashSet<Guid>();
        var seenSellerMethod = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var groups = new List<OrderShippingGroup>(plans.Count);

        foreach (var plan in plans)
        {
            if (!sellerOrderIdBySellerPartyId.TryGetValue(plan.SellerPartyId, out var sellerOrderId))
            {
                throw new ContractOperationException(OrderShippingGroupFault.SellerMismatch);
            }

            // TB240-R3: "one group per (seller, method)" is the LEGACY rule. A plan carrying COMPLETE captured
            // evidence is an authoritative commitment (one seller may be Platform-fulfilled for one line set and
            // Seller-fulfilled for another over the same one-level catalog method), so its uniqueness key is the
            // whole commitment — seller + canonical method + exact whole-line membership. This mirrors the partial
            // unique indexes on order.cart_shipping_groups/order.order_shipping_groups and the planner seam; a
            // repeated LINE is still refused below, so a genuine duplicate can never pass.
            var sellerMethodKey = plan.AcceptedEvidence is null
                ? $"{plan.SellerPartyId:N}|{(plan.MethodCode ?? string.Empty).Trim()}"
                : $"{plan.SellerPartyId:N}|{(plan.MethodCode ?? string.Empty).Trim()}|{string.Join(',', (plan.CartLineIds ?? []).OrderBy(x => x).Select(x => x.ToString("N")))}";

            if (!seenSellerMethod.Add(sellerMethodKey))
            {
                throw new ContractOperationException(OrderShippingGroupFault.DuplicateSellerMethod);
            }

            var group = OrderShippingGroup.Create(plan, checkoutId, sellerOrderId, now);

            if (plan.CartLineIds is null || plan.CartLineIds.Count == 0)
            {
                throw new ContractOperationException(OrderShippingGroupFault.GroupWithoutLines);
            }

            foreach (var cartLineId in plan.CartLineIds)
            {
                if (!orderLineIdByCartLineId.TryGetValue(cartLineId, out var orderLineId))
                {
                    throw new ContractOperationException(OrderShippingGroupFault.CartLineMappingMissing);
                }

                if (!sellerByOrderLine.TryGetValue(orderLineId, out var lineSeller))
                {
                    throw new ContractOperationException(OrderShippingGroupFault.CartLineMappingMissing);
                }

                if (lineSeller != plan.SellerPartyId)
                {
                    throw new ContractOperationException(OrderShippingGroupFault.SellerMismatch);
                }

                if (!assigned.Add(orderLineId))
                {
                    throw new ContractOperationException(OrderShippingGroupFault.DuplicateLineMembership);
                }

                group.AddLine(orderLineId);
            }

            groups.Add(group);
        }

        if (assigned.Count != orderLines.Count || orderLines.Any(x => !assigned.Contains(x.OrderLineId)))
        {
            throw new ContractOperationException(OrderShippingGroupFault.MissingLineMembership);
        }

        return groups;
    }

    /// <summary>جمع قطعی هزینهٔ ارسال سفارش از snapshot گروه‌ها (فقط یک بار).</summary>
    public static decimal SumShippingCharge(IReadOnlyList<OrderShippingGroup> groups)
    {
        ArgumentNullException.ThrowIfNull(groups);
        return groups.Sum(x => x.ShippingChargeSnapshot);
    }
}
