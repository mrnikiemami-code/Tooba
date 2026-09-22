#pragma warning disable CS1591
namespace Tooba.Media.Contracts.Assets;

public interface IMediaAssetUploadPort
{
    Task<Guid> UploadAsync(Stream stream, string fileName, string contentType, Guid actorUserId, CancellationToken cancellationToken);
}
