namespace Tooba.Order.Application.Admin.Sellers.Ports;

/// <summary>مرز باریک شمارش سفارش به ازای SellerPartyId (بدون افشای persistence ماژول).</summary>
public interface ISellerOrderCountReader
{
    /// <summary>تعداد SellerOrder به ازای هر شناسهٔ فروشندهٔ داده‌شده.</summary>
    Task<IReadOnlyDictionary<Guid, int>> GetCountsBySellerAsync(
        IReadOnlyList<Guid> sellerPartyIds,
        CancellationToken cancellationToken);
}
