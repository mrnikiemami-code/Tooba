using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Media.Application.Composition;
using Tooba.Media.Application.Models;
using Tooba.Media.Application.Ports;

namespace Tooba.Media.Application.Assets.Queries;

/// <summary>Paged Media library query with optional search and content-type filter.</summary>
public sealed record QueryMediaAssetsQuery(
    string? Search,
    string? ContentTypePrefix,
    int Page,
    int PageSize) : IRequest<Result<MediaPagedResult<MediaAssetInfo>>>;

/// <summary>Handler for <see cref="QueryMediaAssetsQuery"/>.</summary>
public sealed class QueryMediaAssetsQueryHandler(IMediaDirectory directory)
    : IRequestHandler<QueryMediaAssetsQuery, Result<MediaPagedResult<MediaAssetInfo>>>
{
    /// <inheritdoc />
    public Task<Result<MediaPagedResult<MediaAssetInfo>>> Handle(
        QueryMediaAssetsQuery request,
        CancellationToken cancellationToken) =>
        MediaOperation.ExecuteAsync(() =>
            directory.QueryAsync(
                request.Search,
                request.Page,
                request.PageSize,
                cancellationToken,
                request.ContentTypePrefix));
}
