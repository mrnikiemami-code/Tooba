using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Media.Contracts.Assets;
using Tooba.Media.Contracts.Errors;
using Tooba.Media.Domain.Aggregates;
using Tooba.Media.Domain.Enums;

namespace Tooba.Media.Infrastructure.Assets;

/// <summary>Story-facing Media ownership and seller upload seam.</summary>
public sealed partial class MediaDirectory : IStoryMediaAssetPort
{
    /// <inheritdoc />
    public async Task<StoryMediaAssetAuthorization> AuthorizeForStoryAsync(
        Guid mediaAssetId,
        Guid tenantId,
        Guid? sellerPartyId,
        bool allowUnscopedAdminAssets,
        CancellationToken cancellationToken)
    {
        var asset = await _db.Assets.AsNoTracking()
            .FirstOrDefaultAsync(
                row => row.MediaAssetId == mediaAssetId && row.Status == MediaAssetStatus.Ready,
                cancellationToken)
            ?? throw new ContractOperationException(MediaErrorCodes.Missing);

        if (!IsImageOrVideo(asset.ContentType))
            throw new ContractOperationException(MediaErrorCodes.TypeUnsupported);

        var binding = await _db.AssetOwnerBindings.AsNoTracking()
            .FirstOrDefaultAsync(row => row.MediaAssetId == mediaAssetId, cancellationToken);

        if (binding is null)
        {
            if (!allowUnscopedAdminAssets)
                throw new ContractOperationException(MediaErrorCodes.OwnerForbidden);
        }
        else
        {
            if (binding.TenantId != tenantId)
                throw new ContractOperationException(MediaErrorCodes.OwnerForbidden);
            if (sellerPartyId is Guid shop && shop != Guid.Empty && binding.SellerPartyId != shop)
                throw new ContractOperationException(MediaErrorCodes.OwnerForbidden);
        }

        return MapStory(asset, binding?.SellerPartyId);
    }

    /// <inheritdoc />
    public async Task<StoryMediaAssetAuthorization> UploadForSellerAsync(
        Stream stream,
        string fileName,
        string contentType,
        Guid tenantId,
        Guid sellerPartyId,
        Guid actorUserId,
        string? occurrenceKey,
        CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty || sellerPartyId == Guid.Empty || actorUserId == Guid.Empty)
            throw new ContractOperationException(MediaErrorCodes.OwnerForbidden);
        if (string.IsNullOrWhiteSpace(occurrenceKey))
            throw new ContractOperationException(MediaErrorCodes.ValidationFailed);

        var normalizedType = NormalizeContentType(contentType);
        if (!IsImageOrVideo(normalizedType))
            throw new ContractOperationException(MediaErrorCodes.TypeUnsupported);

        await using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken);
        var bytes = buffer.ToArray();
        var digest = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var intentHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            $"{tenantId:D}|{sellerPartyId:D}|{normalizedType}|{fileName.Trim()}|{digest}"))).ToLowerInvariant();
        var key = occurrenceKey.Trim();

        var prior = await _db.AssetOwnerOccurrences.AsNoTracking()
            .FirstOrDefaultAsync(
                row => row.OccurrenceKey == key
                    && row.TenantId == tenantId
                    && row.SellerPartyId == sellerPartyId,
                cancellationToken);
        if (prior is not null)
        {
            if (!string.Equals(prior.IntentHash, intentHash, StringComparison.Ordinal))
                throw new ContractOperationException(MediaErrorCodes.OwnerForbidden);
            var existing = await _db.Assets.AsNoTracking()
                .FirstOrDefaultAsync(row => row.MediaAssetId == prior.MediaAssetId, cancellationToken)
                ?? throw new ContractOperationException(MediaErrorCodes.Missing);
            return MapStory(existing, sellerPartyId);
        }

        buffer.Position = 0;
        var uploaded = await UploadAsync(buffer, fileName, contentType, actorUserId, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        _db.AssetOwnerBindings.Add(MediaAssetOwnerBinding.Create(
            uploaded.MediaAssetId, tenantId, sellerPartyId, actorUserId, now));
        _db.AssetOwnerOccurrences.Add(new MediaAssetOwnerOccurrence
        {
            OccurrenceKey = key,
            TenantId = tenantId,
            SellerPartyId = sellerPartyId,
            MediaAssetId = uploaded.MediaAssetId,
            ActorUserId = actorUserId,
            IntentHash = intentHash,
            CreatedAt = now,
        });

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Binding failed after object-store save: refuse returning an unscoped Ready asset.
            var orphan = await _db.Assets.FirstOrDefaultAsync(row => row.MediaAssetId == uploaded.MediaAssetId, cancellationToken);
            if (orphan is not null)
            {
                try { await _store.DeleteAsync(orphan.StorageKey, cancellationToken); } catch { /* best-effort */ }
                _db.Assets.Remove(orphan);
                try { await _db.SaveChangesAsync(cancellationToken); } catch { /* best-effort */ }
            }
            throw new ContractOperationException(MediaErrorCodes.UploadFailed, ex);
        }

        var asset = await _db.Assets.AsNoTracking()
            .FirstAsync(row => row.MediaAssetId == uploaded.MediaAssetId, cancellationToken);
        return MapStory(asset, sellerPartyId);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StoryMediaAssetAuthorization>> ListSellerAssetsAsync(
        Guid tenantId,
        Guid sellerPartyId,
        int take,
        CancellationToken cancellationToken)
    {
        var limit = Math.Clamp(take, 1, 100);
        var ids = await _db.AssetOwnerBindings.AsNoTracking()
            .Where(row => row.TenantId == tenantId && row.SellerPartyId == sellerPartyId)
            .OrderByDescending(row => row.CreatedAt)
            .Select(row => row.MediaAssetId)
            .Take(limit)
            .ToListAsync(cancellationToken);
        if (ids.Count == 0)
            return [];

        var assets = await _db.Assets.AsNoTracking()
            .Where(row => ids.Contains(row.MediaAssetId) && row.Status == MediaAssetStatus.Ready)
            .ToListAsync(cancellationToken);
        var byId = assets.ToDictionary(row => row.MediaAssetId);
        return ids
            .Where(byId.ContainsKey)
            .Select(id => MapStory(byId[id], sellerPartyId))
            .Where(row => IsImageOrVideo(row.ContentType))
            .ToList();
    }

    private static bool IsImageOrVideo(string contentType) =>
        contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
        || contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase);

    private static StoryMediaAssetAuthorization MapStory(MediaAsset asset, Guid? sellerPartyId) =>
        new(
            asset.MediaAssetId,
            asset.ContentType,
            $"/v1/storefront/media/{asset.MediaAssetId:D}",
            asset.ContentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase),
            sellerPartyId);
}

/// <summary>Replay row for seller media uploads.</summary>
public sealed class MediaAssetOwnerOccurrence
{
    /// <summary>Occurrence key.</summary>
    public string OccurrenceKey { get; init; } = string.Empty;
    /// <summary>Tenant.</summary>
    public Guid TenantId { get; init; }
    /// <summary>Seller party.</summary>
    public Guid SellerPartyId { get; init; }
    /// <summary>Media asset.</summary>
    public Guid MediaAssetId { get; init; }
    /// <summary>Actor.</summary>
    public Guid ActorUserId { get; init; }
    /// <summary>SHA-256 of upload intent (type|name|content digest).</summary>
    public string IntentHash { get; init; } = string.Empty;
    /// <summary>Created at.</summary>
    public DateTimeOffset CreatedAt { get; init; }
}
