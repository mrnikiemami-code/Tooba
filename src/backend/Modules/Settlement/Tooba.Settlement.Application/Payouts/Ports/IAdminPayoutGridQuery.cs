using Tooba.BuildingBlocks.Grid;
using Tooba.Settlement.Application.Payouts.Models;

namespace Tooba.Settlement.Application.Payouts.Ports;

/// <summary>پرس‌وجوی DB-native صف payout Admin.</summary>
public interface IAdminPayoutGridQuery
{
    /// <summary>صفحه‌بندی server-side صف Pending|Failed.</summary>
    Task<GridPageResponse<AdminPayoutListItem>> QueryAsync(
        GridQueryRequest request,
        CancellationToken cancellationToken);
}
