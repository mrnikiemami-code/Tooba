using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Contracts.Operations;
using Tooba.Order.Application.Admin.Operations.Models;
using Tooba.Order.Application.Admin.Operations.Policies;
using Tooba.Order.Application.Admin.Operations.Ports;
using Tooba.Order.Contracts.Admin.Operations;
using Tooba.Order.Contracts.Fulfillment;
using Tooba.Order.Contracts.Payments;
using Tooba.Order.Domain;
using Tooba.Payment.Contracts.Admin;
using Tooba.Returns.Contracts.Operations;
using Tooba.Settlement.Contracts.Operations;

using Tooba.Order.Application.Checkout.Abuse;
using Tooba.Order.Application.Checkout.Contracts;
using Tooba.Order.Application.Checkout.Policies;
using Tooba.Order.Application.Checkout.Process;
using Tooba.Order.Application.ReservationCycle.Contracts;
using Tooba.Order.Application.ReservationCycle.Policies;
using Tooba.Order.Application.ReservationCycle.Services;
using Tooba.Order.Application.Seller.Policies;

namespace Tooba.Order.Application.Admin.Operations.Services;

/// <summary>
/// حذف تکرار اقدام‌های کل‌سفارش؛ seller-scoped دست نخورده می‌ماند.
/// </summary>
public static class AdminOrderWholeOrderActions
{
    private static readonly HashSet<string> WholeOrderCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "cancel",
        "confirm_deposit",
        "reject_deposit",
        "restore_deposit",
        "unconfirm_deposit",
        "recover_inventory_reservation",
        "restore_cancelled_order",
    };

    /// <summary>هر کد کل‌سفارش حداکثر یک‌بار؛ ترجیح با sellerOrderId خالی.</summary>
    public static List<AdminOrderOperationAction> Collapse(IReadOnlyList<AdminOrderOperationAction> actions)
    {
        var preferred = new Dictionary<string, AdminOrderOperationAction>(StringComparer.OrdinalIgnoreCase);
        foreach (var action in actions)
        {
            if (!WholeOrderCodes.Contains(action.Code))
            {
                continue;
            }

            if (!preferred.TryGetValue(action.Code, out var existing) || action.SellerOrderId is null)
            {
                preferred[action.Code] = action;
            }
            else
            {
                _ = existing;
            }
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<AdminOrderOperationAction>(actions.Count);
        foreach (var action in actions)
        {
            if (WholeOrderCodes.Contains(action.Code))
            {
                if (!seen.Add(action.Code))
                {
                    continue;
                }

                result.Add(preferred[action.Code]);
                continue;
            }

            result.Add(action);
        }

        return result;
    }
}
