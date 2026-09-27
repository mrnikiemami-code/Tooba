using Tooba.BuildingBlocks.Grid;
using Tooba.Content.Application.Models;

namespace Tooba.Content.Application.Ports;

/// <summary>Admin article grid — DB-native paging; Infrastructure implements.</summary>
public interface IContentArticleGridPort
{
    Task<GridPageResponse<AdminArticleSnapshot>> QueryAsync(GridQueryRequest request, CancellationToken cancellationToken);
}
