namespace Tooba.Order.Domain;

/// <summary>
/// LOCK-INVOICE-002 / LOCK-QTY-001: تعداد ردیف صحیح است؛ جمع مقدار اعشاری جدا می‌ماند.
/// </summary>
public static class InvoiceHeaderSemantics
{
    /// <summary>تعداد ردیف یک سفارش فروشنده.</summary>
    public static int LineCount(SellerOrder order)
    {
        ArgumentNullException.ThrowIfNull(order);
        return order.TotalItemCount > 0 ? order.TotalItemCount : order.Lines.Count;
    }

    /// <summary>تعداد ردیف همهٔ سفارش‌های فروشنده.</summary>
    public static int LineCount(IEnumerable<SellerOrder> orders)
    {
        ArgumentNullException.ThrowIfNull(orders);
        return orders.Sum(LineCount);
    }

    /// <summary>
    /// درست است اگر همهٔ خطوط یک واحد اندازه‌گیری داشته باشند؛ فقط در این حالت جمع مقدار معنا دارد.
    /// </summary>
    public static bool HasSharedUnit(IEnumerable<SellerOrder> orders)
    {
        ArgumentNullException.ThrowIfNull(orders);
        var units = orders
            .SelectMany(order => order.Lines)
            .Select(UnitKey)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(2)
            .ToList();
        return units.Count <= 1;
    }

    private static string UnitKey(OrderLine line) =>
        !string.IsNullOrWhiteSpace(line.UnitCodeSnapshot)
            ? line.UnitCodeSnapshot.Trim()
            : line.UnitOfMeasureIdSnapshot?.ToString("N") ?? string.Empty;
}
