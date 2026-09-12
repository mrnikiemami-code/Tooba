namespace Tooba.Order.Application;

/// <summary>
/// مبلغ قابل‌پرداخت سفارش: جمع کالای فروشنده + StoreShipping.
/// تصویر Paid در Order این معادله را بدون باز کردن PaymentDbContext بازسازی می‌کند.
/// </summary>
public static class CheckoutPayableInvariant
{
    /// <summary>
    /// Payable = Σ seller merchandise + max(0, StoreShipping).
    /// تخصیص ارسال به فروشنده اضافه نمی‌شود.
    /// </summary>
    public static decimal CanonicalAmount(IEnumerable<decimal> sellerMerchandiseTotals, decimal shippingAmount)
    {
        ArgumentNullException.ThrowIfNull(sellerMerchandiseTotals);
        var merchandise = 0m;
        foreach (var total in sellerMerchandiseTotals)
        {
            merchandise += total;
        }

        return merchandise + Math.Max(0m, shippingAmount);
    }
}
