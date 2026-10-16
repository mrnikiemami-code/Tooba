using Microsoft.EntityFrameworkCore;
using Tooba.Fulfillment.Application.Fulfillments.Models;
using Tooba.Fulfillment.Domain.Aggregates;
using Tooba.Fulfillment.Infrastructure.Persistence;
using Tooba.Order.Contracts.Fulfillment;

namespace Tooba.Fulfillment.Infrastructure.Directories;

/// <summary>
/// بخش گروه ارسال پذیرفته‌شدهٔ <see cref="FulfillmentDirectory"/>: نگاشت snapshot سفارش به
/// گروه‌های مالک Fulfillment، ذخیرهٔ افزودنی آن‌ها و ساخت snapshot خواندنی گروه‌ها.
/// این partial فقط درز گروه است و سیاست قیمت/تخصیص carrier نمی‌سازد.
/// </summary>
public sealed partial class FulfillmentDirectory
{
    /// <summary>
    /// گروه‌های ارسال پذیرفته‌شدهٔ سفارش را به طرح انتقال‌دهندهٔ Fulfillment تبدیل می‌کند.
    /// سفارش بدون گروه‌بندی (legacy) null برمی‌گرداند تا مسیر تک‌روشی دست‌نخورده بماند.
    /// </summary>
    private static IReadOnlyList<FulfillmentShippingGroupPlan>? MapShippingGroupPlans(
        OrderFulfillmentHandoffSnapshot handoff)
    {
        if (handoff.ShippingGroups is null || handoff.ShippingGroups.Count == 0)
        {
            return null;
        }

        return handoff.ShippingGroups
            .Select(group => new FulfillmentShippingGroupPlan(
                group.ShippingGroupId,
                group.SellerPartyId,
                group.MethodCode,
                group.ServiceCode,
                group.OptionCode,
                group.MethodLabel,
                group.OriginContext,
                group.MinimumDeliveryDate,
                group.RequestedDeliveryDate,
                group.RequestedDeliveryTimeWindow,
                group.LeadDays,
                group.OrderLineIds))
            .ToArray();
    }

    /// <summary>گروه‌های ساخته‌شدهٔ واحد را در همان تراکنش درج می‌کند (بدون درج تکراری).</summary>
    private void PersistShippingGroups(FulfillmentUnit unit)
    {
        if (unit.ShippingGroups.Count > 0)
        {
            _db.ShippingGroups.AddRange(unit.ShippingGroups);
        }
    }
}
