using Tooba.BuildingBlocks.Grid;

namespace Tooba.Party.Contracts.Ports;

/// <summary>ردیف فروشنده Admin از Party + شمارنده‌های Offer/Order.</summary>
public sealed record AdminSellerListItem(
    Guid SellerPartyId,
    string DisplayName,
    string Status,
    int ActiveOffers,
    int OrderCount);

/// <summary>Admin sellers grid — Contracts-only metrics; Infrastructure implements.</summary>
public interface IAdminSellersGridPort
{
    /// <summary>صفحه‌بندی server-side گرید فروشندگان Admin.</summary>
    Task<GridPageResponse<AdminSellerListItem>> QueryAsync(
        GridQueryRequest request,
        CancellationToken cancellationToken);
}
