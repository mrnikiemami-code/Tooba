using Tooba.Catalog.Contracts;
using Tooba.Order.Domain;
using Tooba.Party.Contracts;

namespace Tooba.Order.Application.Admin.Completeness.History;

/// <summary>
/// نام فروشنده و عنوان کالای خطوط سفارش را یک‌بار حل می‌کند تا ترکیب تاریخچه N+1 نشود.
/// </summary>
internal sealed class AdminOrderHistoryScope
{
    private readonly Dictionary<Guid, string> _sellerNames;
    private readonly Dictionary<Guid, OrderLine> _linesById;
    private readonly IReadOnlyDictionary<Guid, string> _titlesByVariant;

    private AdminOrderHistoryScope(
        Dictionary<Guid, string> sellerNames,
        Dictionary<Guid, OrderLine> linesById,
        IReadOnlyDictionary<Guid, string> titlesByVariant)
    {
        _sellerNames = sellerNames;
        _linesById = linesById;
        _titlesByVariant = titlesByVariant;
    }

    public static async Task<AdminOrderHistoryScope> LoadAsync(
        CheckoutGroup group,
        IPartyLookup parties,
        ICatalogVariantLookup catalog,
        CancellationToken cancellationToken)
    {
        var sellerPartyIds = group.SellerOrders.Select(x => x.SellerPartyId).Distinct().ToList();
        var displayNames = sellerPartyIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await parties.GetDisplayNamesAsync(sellerPartyIds, cancellationToken);
        var sellerNames = sellerPartyIds.ToDictionary(
            id => id,
            id => displayNames.TryGetValue(id, out var name) && !string.IsNullOrWhiteSpace(name)
                ? name
                : AdminOrderHistoryFormatting.FallbackSellerName);

        var linesById = group.SellerOrders.SelectMany(x => x.Lines).ToDictionary(x => x.LineId);
        var variantIds = linesById.Values.Select(x => x.CatalogVariantId).Distinct().ToList();
        var titles = variantIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await catalog.GetVariantTitlesAsync(variantIds, cancellationToken);

        return new AdminOrderHistoryScope(sellerNames, linesById, titles);
    }

    public string SellerName(Guid sellerPartyId) =>
        _sellerNames.TryGetValue(sellerPartyId, out var name)
            ? name
            : AdminOrderHistoryFormatting.FallbackSellerName;

    public string LineTitle(Guid orderLineId) =>
        _linesById.TryGetValue(orderLineId, out var line)
        && _titlesByVariant.TryGetValue(line.CatalogVariantId, out var title)
        && !string.IsNullOrWhiteSpace(title)
            ? title
            : AdminOrderHistoryFormatting.FallbackProductTitle;

    /// <summary>خلاصهٔ «کالا — تعداد» برای یک یا چند خط؛ رشتهٔ خالی یعنی محدودهٔ قابل نمایش نیست.</summary>
    public string FormatLineQtyScope(IReadOnlyList<(Guid OrderLineId, decimal Quantity)> items)
    {
        var list = items.Where(x => x.Quantity > 0).ToList();
        if (list.Count == 0)
        {
            return string.Empty;
        }

        if (list.Count == 1)
        {
            return AdminOrderHistoryFormatting.FormatProductQtyScopeFa(
                LineTitle(list[0].OrderLineId),
                list[0].Quantity);
        }

        var total = list.Sum(x => x.Quantity);
        return $"{LineTitle(list[0].OrderLineId)} و {AdminOrderHistoryFormatting.ToFaDigits(list.Count - 1)} کالای دیگر"
            + $" — تعداد {AdminOrderHistoryFormatting.ToFaDigits(total)}";
    }
}
