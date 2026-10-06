#pragma warning disable CS1591
namespace Tooba.Media.Contracts.Assets;

/// <summary>
/// Narrow Media Contracts port for the Payment storefront manual-payment proof upload.
/// Implemented by Media Infrastructure; the caller keeps ownership of payment state and only
/// receives the created Media asset identifier.
/// </summary>
public interface IMediaAssetUploadPort
{
    /// <summary>Stores the stream as a Ready Media asset and returns its identifier.</summary>
    Task<Guid> UploadAsync(
        Stream stream,
        string fileName,
        string contentType,
        Guid actorUserId,
        CancellationToken cancellationToken);
}
