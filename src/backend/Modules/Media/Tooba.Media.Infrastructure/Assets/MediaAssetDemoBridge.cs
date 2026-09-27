using Microsoft.EntityFrameworkCore;
using Tooba.Media.Application;
using Tooba.Media.Contracts.Assets;
using Tooba.Media.Infrastructure.Persistence;

namespace Tooba.Media.Infrastructure.Assets;

/// <summary>Media-owned bridge for Development demo media upload/find/delete-by-prefix.</summary>
public sealed class MediaAssetDemoBridge(
    IMediaDirectory media,
    MediaDbContext db,
    IMediaObjectStore store) : IMediaAssetDemoPort
{
    /// <inheritdoc />
    public async Task<Guid> UploadAsync(
        Stream stream,
        string originalFileName,
        string contentType,
        CancellationToken cancellationToken)
    {
        var uploaded = await media.UploadAsync(
            stream, originalFileName, contentType, actorUserId: null, cancellationToken);
        return uploaded.MediaAssetId;
    }

    /// <inheritdoc />
    public async Task<Guid?> FindByOriginalFileNameAsync(
        string originalFileName,
        CancellationToken cancellationToken)
    {
        var page = await media.QueryAsync(originalFileName, page: 1, pageSize: 20, cancellationToken);
        var match = page.Items.FirstOrDefault(x =>
            string.Equals(x.OriginalFileName, originalFileName, StringComparison.OrdinalIgnoreCase));
        return match?.MediaAssetId;
    }

    /// <inheritdoc />
    public async Task<int> DeleteByOriginalFileNamePrefixAsync(
        string originalFileNamePrefix,
        CancellationToken cancellationToken)
    {
        var assets = await db.Assets
            .Where(a => a.OriginalFileName.StartsWith(originalFileNamePrefix))
            .ToListAsync(cancellationToken);
        if (assets.Count == 0)
        {
            return 0;
        }

        foreach (var asset in assets)
        {
            try
            {
                await store.DeleteAsync(asset.StorageKey, cancellationToken);
            }
            catch
            {
                // Object may already be gone on disk.
            }
        }

        db.Assets.RemoveRange(assets);
        await db.SaveChangesAsync(cancellationToken);
        return assets.Count;
    }
}
