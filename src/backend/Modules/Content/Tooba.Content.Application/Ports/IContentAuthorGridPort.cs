using Tooba.BuildingBlocks.Grid;
using Tooba.Content.Application.Models;

namespace Tooba.Content.Application.Ports;

/// <summary>Admin author grid — DB-native paging; Infrastructure implements.</summary>
public interface IContentAuthorGridPort
{
    Task<GridPageResponse<ContentAuthorGridRowDto>> QueryAsync(GridQueryRequest request, CancellationToken cancellationToken);
}
