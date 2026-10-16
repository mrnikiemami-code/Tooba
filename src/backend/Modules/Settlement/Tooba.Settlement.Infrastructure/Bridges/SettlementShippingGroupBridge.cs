using Tooba.BuildingBlocks;
using Tooba.Order.Contracts.Settlement;
using Tooba.Payment.Contracts.Settlement;
using Tooba.Settlement.Application.Payouts.Ports;
using Tooba.Settlement.Contracts.Errors;

namespace Tooba.Settlement.Infrastructure.Bridges;

/// <summary>
/// پل هزینهٔ ارسال per-group برای Settlement: هویت و charge پذیرفته‌شده از Order.Contracts و مبلغ
/// پرداخت‌شدهٔ مشتری از Payment.Contracts. تنها charge مشتری عبور می‌کند؛ هیچ سهم carrier/فروشنده/
/// فروشگاه در این درز ساخته نمی‌شود.
/// </summary>
public sealed class SettlementShippingGroupBridge : ISettlementShippingGroupReader
{
    private readonly IOrderShippingGroupSettlementReader _groups;
    private readonly IPaymentSettlementReader _payments;

    /// <summary>پل را به Contracts سفارش و پرداخت وصل می‌کند.</summary>
    public SettlementShippingGroupBridge(
        IOrderShippingGroupSettlementReader groups,
        IPaymentSettlementReader payments)
    {
        _groups = groups;
        _payments = payments;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SettlementShippingGroupFeeSnapshot>> GetForPaymentAsync(
        Guid paymentId,
        IReadOnlyList<Guid> sellerOrderIds,
        CancellationToken cancellationToken)
    {
        var accepted = await _groups.GetForSellerOrdersAsync(sellerOrderIds, cancellationToken);
        if (accepted.Count == 0)
        {
            return [];
        }

        var paid = await _payments.GetShippingGroupAllocationsAsync(paymentId, cancellationToken);
        var paidByGroup = paid
            .GroupBy(x => x.ShippingGroupId)
            .ToDictionary(
                group => group.Key,
                group => group.Aggregate(
                    (amount: 0m, currency: string.Empty),
                    (acc, item) => (acc.amount + item.AllocatedAmount, item.Currency)));

        var snapshots = new List<SettlementShippingGroupFeeSnapshot>(accepted.Count);
        foreach (var group in accepted.OrderBy(x => x.ShippingGroupId))
        {
            if (group.ShippingCharge < 0)
            {
                throw new ContractOperationException(SettlementErrorCodes.ShippingGroupChargeInvalid);
            }

            if (group.ShippingCharge == 0)
            {
                // گروه بدون هزینهٔ ارسال مشتری هیچ سطر تسویه‌ای ندارد و باز conciliation لازم نیست.
                continue;
            }

            if (!paidByGroup.TryGetValue(group.ShippingGroupId, out var allocation))
            {
                throw new ContractOperationException(SettlementErrorCodes.ShippingGroupChargeMismatch);
            }

            if (!string.Equals(allocation.currency, group.Currency, StringComparison.OrdinalIgnoreCase)
                || allocation.amount != group.ShippingCharge)
            {
                throw new ContractOperationException(SettlementErrorCodes.ShippingGroupChargeMismatch);
            }

            snapshots.Add(new SettlementShippingGroupFeeSnapshot(
                group.ShippingGroupId,
                group.CheckoutId,
                group.SellerOrderId,
                group.SellerPartyId,
                group.MethodCode,
                group.ServiceCode,
                group.OptionCode,
                group.AcceptedQuoteVersion,
                group.ShippingCharge,
                allocation.amount,
                group.Currency));
        }

        return snapshots;
    }
}
