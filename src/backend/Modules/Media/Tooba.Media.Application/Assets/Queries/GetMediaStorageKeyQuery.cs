using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Media.Application.Composition;
using Tooba.Media.Application.Ports;

namespace Tooba.Media.Application.Assets.Queries;

/// <summary>Resolve internal object-store key for binary serving.</summary>
public sealed record GetMediaStorageKeyQuery(Guid MediaAssetId) : IRequest<Result<string?>>;

/// <summary>Handler for <see cref="GetMediaStorageKeyQuery"/>.</summary>
public sealed class GetMediaStorageKeyQueryHandler(IMediaDirectory directory)
    : IRequestHandler<GetMediaStorageKeyQuery, Result<string?>>
{
    /// <inheritdoc />
    public Task<Result<string?>> Handle(
        GetMediaStorageKeyQuery request,
        CancellationToken cancellationToken) =>
        MediaOperation.ExecuteAsync(() =>
            directory.GetStorageKeyAsync(request.MediaAssetId, cancellationToken));
}
