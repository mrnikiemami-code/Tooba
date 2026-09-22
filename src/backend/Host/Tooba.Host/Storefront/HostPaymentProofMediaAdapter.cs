#pragma warning disable CS1591
using Tooba.Media.Application;
using Tooba.Payment.Application.Ports;

namespace Tooba.Host.Storefront;

/// <summary>Host-neutral media upload adapter for Payment proof (no Payment business logic).</summary>
public sealed class HostPaymentProofMediaAdapter(IMediaDirectory media) : IPaymentProofMediaPort
{
    public async Task<Guid> UploadAsync(
        Stream stream,
        string fileName,
        string contentType,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var asset = await media.UploadAsync(stream, fileName, contentType, actorUserId, cancellationToken);
        return asset.MediaAssetId;
    }
}
