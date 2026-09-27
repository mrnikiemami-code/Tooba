using Tooba.Media.Application;
using Tooba.Media.Contracts.Ports;

namespace Tooba.Media.Infrastructure.Adapters;

/// <summary>Media-owned readiness port over <see cref="IMediaDirectory"/>.</summary>
public sealed class MediaAssetReadinessBridge(IMediaDirectory media) : IMediaAssetReadinessPort
{
    /// <inheritdoc />
    public async Task EnsureReadyAsync(Guid mediaAssetId, CancellationToken cancellationToken)
    {
        var asset = await media.GetAsync(mediaAssetId, cancellationToken);
        if (asset is null)
            throw new InvalidOperationException("media.asset.missing");
    }
}
