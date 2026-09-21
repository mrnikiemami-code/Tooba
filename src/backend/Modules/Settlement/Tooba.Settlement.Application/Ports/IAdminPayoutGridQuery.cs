using Tooba.BuildingBlocks.Grid;
using Tooba.Settlement.Application.Models;

namespace Tooba.Settlement.Application.Ports;

/// <summary>پرس‌وجوی DB-native صف payout Admin.</summary>
public interface IAdminPayoutGridQuery
{
    /// <summary>صفحه‌بندی server-side صف Pending|Failed.</summary>
    Task<GridPageResponse<AdminPayoutListItem>> QueryAsync(
        GridQueryRequest request,
        CancellationToken cancellationToken);
}
