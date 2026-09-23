namespace Tooba.Order.Application.Admin.Sellers.Models;

/// <summary>نقشهٔ شمارش سفارش به ازای SellerPartyId.</summary>
public sealed record SellerOrderCountMap(IReadOnlyDictionary<Guid, int> CountsBySellerPartyId);
