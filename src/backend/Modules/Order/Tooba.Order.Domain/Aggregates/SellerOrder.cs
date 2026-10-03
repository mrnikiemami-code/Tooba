using Tooba.BuildingBlocks;
using Tooba.Order.Domain.Enums;

namespace Tooba.Order.Domain.Aggregates;

/// <summary>
/// سفارش یک فروشنده داخل checkout. چرخهٔ ارسال جدا است.
/// </summary>
public sealed class SellerOrder
{
    /// <summary>
    /// خطوط این فروشنده برای پایداری EF. Navigation کاتالوگ نیست.
    /// </summary>
    public List<OrderLine> Lines { get; } = [];

    /// <summary>
    /// سازندهٔ EF.
    /// </summary>
    private SellerOrder()
    {
    }

    /// <summary>
    /// شناسهٔ داخلی سفارش فروشنده.
    /// </summary>
    public Guid SellerOrderId { get; init; }

    /// <summary>
    /// گروه checkout والد.
    /// </summary>
    public Guid CheckoutId { get; init; }

    /// <summary>
    /// شمارهٔ مرجع قابل‌نمایش؛ مجوز دسترسی نیست.
    /// </summary>
    public string OrderNumber { get; init; } = string.Empty;

    /// <summary>
    /// فروشندهٔ این سفارش.
    /// </summary>
    public Guid SellerPartyId { get; init; }

    /// <summary>
    /// وضعیت این فروشنده، نه کل سبد.
    /// </summary>
    public SellerOrderStatus Status { get; private set; }

    /// <summary>
    /// وضعیت قبل از لغو؛ برای بازگردانی ایمن لازم است.
    /// </summary>
    public SellerOrderStatus? CancelledFromStatus { get; private set; }

    /// <summary>
    /// زمان آخرین بازگردانی از لغو.
    /// </summary>
    public DateTimeOffset? LastRestoredAt { get; private set; }

    /// <summary>
    /// جمع تصویر خطوط.
    /// </summary>
    public decimal SubtotalSnapshot { get; private set; }

    /// <summary>
    /// جمع مالیات تصویر خطوط در لحظهٔ checkout. قاعدهٔ بعدی این عدد را عوض نمی‌کند.
    /// </summary>
    public decimal TaxSnapshot { get; private set; }

    /// <summary>
    /// جمع تخفیف تصویر خطوط در لحظهٔ checkout. پروموشن بعدی این عدد را عوض نمی‌کند.
    /// </summary>
    public decimal DiscountSnapshot { get; private set; }

    /// <summary>
    /// جمع نهایی تصویر.
    /// </summary>
    public decimal GrandTotalSnapshot { get; private set; }

    /// <summary>تعداد ردیف کالا؛ شمارشی صحیح.</summary>
    public int TotalItemCount { get; private set; }

    /// <summary>جمع مقدار کالا؛ اعشاری.</summary>
    public decimal TotalQuantity { get; private set; }

    /// <summary>مبلغ پس از تخفیف و قبل از مالیات.</summary>
    public decimal NetAmountBeforeTax { get; private set; }

    /// <summary>جمع عوارض.</summary>
    public decimal TotalDutyAmount { get; private set; }

    /// <summary>مالیات + عوارض.</summary>
    public decimal TotalTaxAndDutyAmount { get; private set; }

    /// <summary>حالت گرد کردن استفاده‌شده در صدور.</summary>
    public string RoundingModeUsed { get; private set; } = "Nearest";

    /// <summary>دقت پولی استفاده‌شده در صدور.</summary>
    public int MoneyDecimalPlacesUsed { get; private set; }

    /// <summary>
    /// ارز تصویر.
    /// </summary>
    public string Currency { get; init; } = string.Empty;

    /// <summary>
    /// سفارش فروشنده را می‌سازد.
    /// </summary>
    public static SellerOrder Open(
        Guid checkoutId,
        Guid sellerPartyId,
        string orderNumber,
        OrderMode mode,
        string currency,
        IReadOnlyList<OrderLine> lines,
        QuantityRoundingMode roundingMode = QuantityRoundingMode.Nearest,
        int? moneyDecimalPlaces = null)
    {
        if (lines.Count == 0)
        {
            throw new ContractOperationException("order.seller_order.empty");
        }

        var order = new SellerOrder
        {
            SellerOrderId = lines[0].SellerOrderId,
            CheckoutId = checkoutId,
            SellerPartyId = sellerPartyId,
            OrderNumber = orderNumber,
            Currency = currency,
            Status = mode == OrderMode.OnlinePurchase
                ? SellerOrderStatus.PendingPayment
                : SellerOrderStatus.ReservationRequested,
        };
        foreach (var line in lines)
        {
            order.Lines.Add(line);
        }

        order.SubtotalSnapshot = lines.Sum(x => x.LineTotalSnapshot);
        order.TaxSnapshot = lines.Sum(x => x.TaxAmountSnapshot);
        order.DiscountSnapshot = lines.Sum(x => x.DiscountAmountSnapshot);
        order.TotalDutyAmount = lines.Sum(x => x.DutyAmountSnapshot);
        order.TotalItemCount = lines.Count;
        order.TotalQuantity = lines.Sum(x => x.Quantity);
        order.NetAmountBeforeTax = order.SubtotalSnapshot - order.DiscountSnapshot;
        order.TotalTaxAndDutyAmount = order.TaxSnapshot + order.TotalDutyAmount;
        order.GrandTotalSnapshot = order.NetAmountBeforeTax + order.TotalTaxAndDutyAmount;
        order.RoundingModeUsed = roundingMode.ToString();
        order.MoneyDecimalPlacesUsed = moneyDecimalPlaces ?? FinancialRounder.MoneyPlaces(currency);
        return order;
    }

    /// <summary>
    /// لغو سفارش باز (قبل از Paid). مسیر authoritative؛ caller نباید فقط به UI تکیه کند.
    /// </summary>
    public void Cancel()
    {
        if (Status == SellerOrderStatus.Cancelled)
        {
            return;
        }

        if (Status is SellerOrderStatus.PendingPayment
            or SellerOrderStatus.Submitted
            or SellerOrderStatus.ReservationRequested)
        {
            CancelledFromStatus = Status;
            Status = SellerOrderStatus.Cancelled;
            return;
        }

        throw new ContractOperationException("order.cancel.forbidden");
    }

    /// <summary>
    /// لغو سفارش Paid فقط وقتی application ثابت کرده هنوز محموله/ارسال مسدودکننده ندارد.
    /// </summary>
    public void CancelPaidBeforeShipment()
    {
        if (Status == SellerOrderStatus.Cancelled)
        {
            return;
        }

        if (Status != SellerOrderStatus.Paid)
        {
            throw new ContractOperationException("order.cancel.forbidden");
        }

        CancelledFromStatus = Status;
        Status = SellerOrderStatus.Cancelled;
    }

    /// <summary>
    /// سفارش لغوشده را به وضعیت ذخیره‌شدهٔ قبل از لغو برمی‌گرداند.
    /// </summary>
    public void RestoreFromCancellation(DateTimeOffset at)
    {
        if (Status != SellerOrderStatus.Cancelled)
        {
            throw new ContractOperationException("order.restore.invalid_state");
        }

        if (CancelledFromStatus is null)
        {
            throw new ContractOperationException("order.restore.missing_snapshot");
        }

        Status = CancelledFromStatus.Value;
        CancelledFromStatus = null;
        LastRestoredAt = at;
    }

    /// <summary>
    /// پرداخت تأییدشده را روی سفارش خرید آنلاین ثبت می‌کند. متن callback این متد را صدا نمی‌زند.
    /// </summary>
    public void RecordVerifiedPayment()
    {
        if (Status == SellerOrderStatus.Paid)
        {
            return;
        }

        if (Status != SellerOrderStatus.PendingPayment)
        {
            throw new ContractOperationException("order.payment.confirm.invalid_state");
        }

        Status = SellerOrderStatus.Paid;
    }

    /// <summary>
    /// برگشت Paid به انتظار پرداخت وقتی تأیید واریز دستی لغو می‌شود.
    /// </summary>
    public void RevertVerifiedPayment()
    {
        if (Status == SellerOrderStatus.PendingPayment)
        {
            return;
        }

        if (Status != SellerOrderStatus.Paid)
        {
            throw new ContractOperationException("order.payment.unconfirm.invalid_state");
        }

        Status = SellerOrderStatus.PendingPayment;
    }
}

