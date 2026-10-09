using Tooba.Settlement.Domain.ValueObjects;

namespace Tooba.Settlement.Application.Payouts.Ports;

/// <summary>
/// برش دفتر تسویه برای انتساب FIFO واریز تکمیل‌شده به سفارش فروشنده.
/// </summary>
public readonly record struct RestoreSettlementLedgerSlice(
    Guid? SellerOrderId,
    EntryType EntryType,
    decimal NetAmount,
    DateTimeOffset PostedAt,
    Guid EntryId);

/// <summary>
/// سیاست انتساب واریز تکمیل‌شده به سفارش. مبلغ فقط accrued/payable مسدود نمی‌کند.
/// </summary>
public static class SellerOrderRestoreSettlementPolicy
{
    /// <summary>
    /// آیا واریز Succeeded سهم خالص این سفارش را مصرف کرده است.
    /// </summary>
    public static bool HasCompletedPayoutEffect(
        IReadOnlyList<RestoreSettlementLedgerSlice> sellerLedger,
        decimal completedPayoutAmount,
        Guid sellerOrderId)
    {
        if (completedPayoutAmount <= 0 || sellerLedger.Count == 0)
        {
            return false;
        }

        var netForOrder = sellerLedger
            .Where(x => x.SellerOrderId == sellerOrderId)
            .Sum(SignedNet);
        if (netForOrder <= 0)
        {
            return false;
        }

        var remaining = new Dictionary<Guid, decimal>();
        foreach (var slice in sellerLedger.OrderBy(x => x.PostedAt).ThenBy(x => x.EntryId))
        {
            if (slice.SellerOrderId is not Guid id)
            {
                continue;
            }

            remaining.TryGetValue(id, out var current);
            remaining[id] = current + SignedNet(slice);
        }

        var fifo = sellerLedger
            .Where(x => x.EntryType == EntryType.Credit && x.SellerOrderId is not null)
            .OrderBy(x => x.PostedAt)
            .ThenBy(x => x.EntryId)
            .Select(x => x.SellerOrderId!.Value)
            .Distinct()
            .Where(id => remaining.GetValueOrDefault(id) > 0)
            .ToList();

        var payoutLeft = completedPayoutAmount;
        foreach (var id in fifo)
        {
            if (payoutLeft <= 0)
            {
                break;
            }

            var take = Math.Min(remaining[id], payoutLeft);
            if (take > 0 && id == sellerOrderId)
            {
                return true;
            }

            payoutLeft -= take;
        }

        return false;
    }

    private static decimal SignedNet(RestoreSettlementLedgerSlice slice) =>
        slice.EntryType == EntryType.Credit ? slice.NetAmount : -slice.NetAmount;
}
