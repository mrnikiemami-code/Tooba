namespace Tooba.Media.Contracts.Assets;

/// <summary>
/// Narrow Media Contracts port for Catalog (and similar) Development demo media upload/reset.
/// </summary>
public interface IMediaAssetDemoPort
{
    /// <summary>Uploads bytes as a Ready asset (idempotent callers should Find first).</summary>
    Task<Guid> UploadAsync(
        Stream stream,
        string originalFileName,
        string contentType,
        CancellationToken cancellationToken);

    /// <summary>Finds a Ready asset by exact OriginalFileName (case-insensitive).</summary>
    Task<Guid?> FindByOriginalFileNameAsync(string originalFileName, CancellationToken cancellationToken);

    /// <summary>Deletes Ready assets whose OriginalFileName starts with the prefix (storage + metadata).</summary>
    Task<int> DeleteByOriginalFileNamePrefixAsync(string originalFileNamePrefix, CancellationToken cancellationToken);
}
