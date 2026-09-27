using Tooba.BuildingBlocks;
using Tooba.Content.Contracts.Errors;
using Tooba.Media.Contracts.Ports;
using Tooba.Content.Application.Media.Ports;

namespace Tooba.Content.Infrastructure.Adapters;

/// <summary>Content→Media Contracts readiness adapter; translates Media code to Content-owned code.</summary>
public sealed class ContentMediaAssetValidator(IMediaAssetReadinessPort media) : IContentMediaAssetValidator
{
    /// <inheritdoc />
    public async Task EnsureReadyAssetExistsAsync(Guid mediaAssetId, CancellationToken cancellationToken)
    {
        try
        {
            await media.EnsureReadyAsync(mediaAssetId, cancellationToken);
        }
        catch (ContractOperationException ex) when (ex.Code == MediaAssetContractCodes.AssetMissing)
        {
            throw new ContractOperationException(ContentErrorCodes.MediaNotFound, ex);
        }
    }
}
