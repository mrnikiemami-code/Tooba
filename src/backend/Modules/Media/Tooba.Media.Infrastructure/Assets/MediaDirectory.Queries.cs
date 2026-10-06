using Microsoft.EntityFrameworkCore;
using Tooba.Media.Application.Models;
using Tooba.Media.Domain.Enums;

namespace Tooba.Media.Infrastructure.Assets;

/// <summary>Read-only library queries of the Media directory over the Ready asset set.</summary>
public sealed partial class MediaDirectory
{
    /// <inheritdoc />
    public async Task<MediaPagedResult<MediaAssetInfo>> QueryAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken,
        string? contentTypePrefix = null)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = _db.Assets.AsNoTracking()
            .Where(asset => asset.Status == MediaAssetStatus.Ready);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(asset =>
                EF.Functions.ILike(asset.OriginalFileName, $"%{EscapeLike(term)}%")
                || EF.Functions.ILike(asset.ContentType, $"%{EscapeLike(term)}%"));
        }

        if (!string.IsNullOrWhiteSpace(contentTypePrefix))
        {
            var prefix = contentTypePrefix.Trim().ToLowerInvariant();
            query = query.Where(asset => asset.ContentType.StartsWith(prefix));
        }

        var total = await query.LongCountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(asset => asset.CreatedAt)
            .ThenBy(asset => asset.MediaAssetId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return new MediaPagedResult<MediaAssetInfo>(rows.Select(Map).ToList(), page, pageSize, total);
    }

    /// <inheritdoc />
    public async Task<MediaAssetInfo?> GetAsync(Guid mediaAssetId, CancellationToken cancellationToken)
    {
        var asset = await _db.Assets.AsNoTracking()
            .FirstOrDefaultAsync(
                row => row.MediaAssetId == mediaAssetId && row.Status == MediaAssetStatus.Ready,
                cancellationToken);
        return asset is null ? null : Map(asset);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MediaAssetInfo>> GetManyAsync(
        IReadOnlyList<Guid> mediaAssetIds,
        CancellationToken cancellationToken)
    {
        if (mediaAssetIds.Count == 0)
            return [];
        var ids = mediaAssetIds.Distinct().ToArray();
        var rows = await _db.Assets.AsNoTracking()
            .Where(asset => ids.Contains(asset.MediaAssetId) && asset.Status == MediaAssetStatus.Ready)
            .ToListAsync(cancellationToken);
        return rows.Select(Map).ToList();
    }

    /// <inheritdoc />
    public async Task<string?> GetStorageKeyAsync(Guid mediaAssetId, CancellationToken cancellationToken)
    {
        return await _db.Assets.AsNoTracking()
            .Where(asset => asset.MediaAssetId == mediaAssetId && asset.Status == MediaAssetStatus.Ready)
            .Select(asset => asset.StorageKey)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
