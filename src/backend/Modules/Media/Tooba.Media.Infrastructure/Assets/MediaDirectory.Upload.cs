using System.Security.Cryptography;
using Tooba.BuildingBlocks;
using Tooba.Media.Application.Models;
using Tooba.Media.Contracts.Errors;
using Tooba.Media.Domain.Aggregates;

namespace Tooba.Media.Infrastructure.Assets;

/// <summary>Upload pipeline of the Media directory: MIME/size policy, hashing, storage and persist.</summary>
public sealed partial class MediaDirectory
{
    /// <inheritdoc />
    public async Task<MediaAssetInfo> UploadAsync(
        Stream stream,
        string originalFileName,
        string contentType,
        Guid? actorUserId,
        CancellationToken cancellationToken)
    {
        if (stream is null || !stream.CanRead)
            throw new ContractOperationException(MediaErrorCodes.UploadFailed);

        var normalizedType = NormalizeContentType(contentType);
        if (!AllowedContentTypes.Contains(normalizedType))
            throw new ContractOperationException(MediaErrorCodes.TypeUnsupported);

        var safeName = SanitizeOriginalFileName(originalFileName);
        await using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken);
        if (buffer.Length <= 0)
            throw new ContractOperationException(MediaErrorCodes.UploadFailed);
        if (buffer.Length > _maxUploadBytes)
            throw new ContractOperationException(MediaErrorCodes.TooLarge);

        buffer.Position = 0;
        var checksum = Convert.ToHexString(await SHA256.HashDataAsync(buffer, cancellationToken)).ToLowerInvariant();
        buffer.Position = 0;

        var now = DateTimeOffset.UtcNow;
        var assetId = UuidV7.New();
        var extension = ExtensionByContentType[normalizedType];
        var storageKey = $"{now:yyyy}/{now:MM}/{assetId:N}{extension}";

        try
        {
            await _store.SaveAsync(buffer, storageKey, normalizedType, cancellationToken);
        }
        catch (ContractOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ContractOperationException(MediaErrorCodes.StorageUnavailable, ex);
        }

        // Width/Height: بدون وابستگی سنگین ImageSharp در این نسخه null می‌ماند.
        var asset = MediaAsset.CreateReady(
            storageKey,
            safeName,
            normalizedType,
            buffer.Length,
            checksum,
            width: null,
            height: null,
            actorUserId,
            now,
            assetId);

        _db.Assets.Add(asset);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            try { await _store.DeleteAsync(storageKey, cancellationToken); } catch { /* ignore cleanup */ }
            throw new ContractOperationException(MediaErrorCodes.UploadFailed, ex);
        }

        return Map(asset);
    }
}
