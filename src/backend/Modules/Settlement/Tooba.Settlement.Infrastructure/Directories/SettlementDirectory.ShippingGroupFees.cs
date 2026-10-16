using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Settlement.Contracts.Errors;
using Tooba.Settlement.Domain.Aggregates;
using Tooba.Settlement.Domain.ValueObjects;

namespace Tooba.Settlement.Infrastructure.Directories;

/// <summary>
/// حساب‌داری هزینهٔ ارسال per-group: ثبت idempotent charge مشتری از نقل‌قول پذیرفتهٔ Order و مبلغ
/// پرداخت‌شدهٔ Payment. هیچ سهم carrier/فروشنده/فروشگاه ساخته نمی‌شود، هیچ سطر ledger فروشنده و هیچ
/// payout ایجاد نمی‌شود و مسیر قدیمی StoreShipping دست‌نخورده می‌ماند.
/// </summary>
public sealed partial class SettlementDirectory
{
    /// <summary>
    /// charge پذیرفته‌شدهٔ هر گروه ارسال پایدار پرداخت را (در صورت KNOWN بودن) ثبت می‌کند. پیش‌شرط:
    /// مبلغ پرداخت‌شدهٔ گروه با charge نقل‌قول پذیرفته برابر باشد؛ در غیر این صورت fail-closed است.
    /// </summary>
    public async Task RecordShippingGroupFeesAsync(
        Guid paymentId,
        IReadOnlyList<Guid> sellerOrderIds,
        CancellationToken cancellationToken)
    {
        var groups = await _shippingGroups.GetForPaymentAsync(paymentId, sellerOrderIds, cancellationToken);
        if (groups.Count == 0)
        {
            return;
        }

        var now = _clock.UtcNow;
        var recorded = false;
        foreach (var group in groups)
        {
            var idempotencyKey = $"shipping-group-fee:{paymentId:N}:{group.ShippingGroupId:N}";
            if (await _db.ShippingGroupFees.AnyAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken))
            {
                continue;
            }

            if (await _db.ShippingGroupFees.AnyAsync(
                    x => x.PaymentId == paymentId
                        && x.ShippingGroupId == group.ShippingGroupId
                        && x.ReversalOfShippingGroupFeeId == null,
                    cancellationToken))
            {
                continue;
            }

            var fee = ShippingGroupFeeAllocation.RecordAcceptedCharge(
                _ids.NewId(),
                paymentId,
                group.ShippingGroupId,
                group.CheckoutId,
                group.SellerOrderId,
                group.SellerPartyId,
                group.MethodCode,
                group.ServiceCode,
                group.OptionCode,
                group.AcceptedQuoteVersion,
                group.CustomerCharge,
                group.Currency,
                idempotencyKey,
                now);
            _db.ShippingGroupFees.Add(fee);
            recorded = true;
        }

        if (recorded)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    /// <summary>
    /// reversal یک charge گروه پایدار را ثبت می‌کند؛ سطر charge اصلی بازنویسی نمی‌شود. سطر reversal هم
    /// PendingAllocation است و هیچ مقدار مالی حمل نمی‌کند.
    /// </summary>
    public async Task ReverseShippingGroupFeeAsync(
        Guid paymentId,
        Guid shippingGroupId,
        CancellationToken cancellationToken)
    {
        var idempotencyKey = $"shipping-group-fee-reversal:{paymentId:N}:{shippingGroupId:N}";
        if (await _db.ShippingGroupFees.AnyAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken))
        {
            return;
        }

        var original = await _db.ShippingGroupFees.AsNoTracking()
            .Where(x => x.PaymentId == paymentId
                && x.ShippingGroupId == shippingGroupId
                && x.RecordKind == ShippingGroupFeeRecordKind.Charge)
            .OrderBy(x => x.RecordedAt)
            .ThenBy(x => x.ShippingGroupFeeId)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ContractOperationException(SettlementErrorCodes.ShippingGroupIdentityRequired);

        var reversal = ShippingGroupFeeAllocation.ReverseAcceptedCharge(
            _ids.NewId(),
            original,
            idempotencyKey,
            _clock.UtcNow);
        _db.ShippingGroupFees.Add(reversal);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// سطرهای هزینهٔ ارسال per-group یک پرداخت را برمی‌گرداند (فقط‌خواندنی).
    /// </summary>
    public async Task<IReadOnlyList<ShippingGroupFeeAllocation>> ListShippingGroupFeesAsync(
        Guid paymentId,
        CancellationToken cancellationToken) =>
        await _db.ShippingGroupFees.AsNoTracking()
            .Where(x => x.PaymentId == paymentId)
            .OrderBy(x => x.RecordedAt)
            .ThenBy(x => x.ShippingGroupFeeId)
            .ToListAsync(cancellationToken);
}
