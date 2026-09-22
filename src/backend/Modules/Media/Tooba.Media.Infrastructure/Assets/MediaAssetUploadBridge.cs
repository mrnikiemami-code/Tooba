#pragma warning disable CS1591
using Tooba.Media.Application;
using Tooba.Media.Contracts.Assets;

namespace Tooba.Media.Infrastructure.Assets;

public sealed class MediaAssetUploadBridge(IMediaDirectory media) : IMediaAssetUploadPort
{
    public async Task<Guid> UploadAsync(
        Stream stream, string fileName, string contentType, Guid actorUserId,
        CancellationToken cancellationToken) =>
        (await media.UploadAsync(stream, fileName, contentType, actorUserId, cancellationToken)).MediaAssetId;
}
