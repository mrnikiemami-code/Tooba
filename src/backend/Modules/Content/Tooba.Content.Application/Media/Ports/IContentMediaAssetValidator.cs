namespace Tooba.Content.Application.Media.Ports;

/// <summary>اعتبارسنجی وجود دارایی DAM بدون وابستگی مستقیم Content به Media.</summary>
public interface IContentMediaAssetValidator
{
    Task EnsureReadyAssetExistsAsync(Guid mediaAssetId, CancellationToken cancellationToken);
}
