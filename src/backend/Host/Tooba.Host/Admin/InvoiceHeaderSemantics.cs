using Tooba.Order.Domain;

namespace Tooba.Host.Admin;

/// <summary>
/// LOCK-INVOICE-002 / LOCK-QTY-001: تعداد ردیف صحیح است؛ جمع مقدار اعشاری جدا می‌ماند.
/// </summary>
internal static class InvoiceHeaderSemantics
{
    public static int LineCount(SellerOrder order) =>
        order.TotalItemCount > 0 ? order.TotalItemCount : order.Lines.Count;

    public static int LineCount(IEnumerable<SellerOrder> orders) =>
        orders.Sum(LineCount);

    public static bool HasSharedUnit(IEnumerable<SellerOrder> orders)
    {
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
