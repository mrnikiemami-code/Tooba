namespace Tooba.Content.Application;

/// <summary>اعتبارسنجی وجود دارایی DAM بدون وابستگی مستقیم Content به Media.</summary>
public interface IContentMediaAssetValidator
{
    /// <summary>اطمینان از وجود دارایی آماده در DAM.</summary>
    Task EnsureReadyAssetExistsAsync(Guid mediaAssetId, CancellationToken cancellationToken);
}
