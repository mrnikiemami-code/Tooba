using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Tooba.BuildingBlocks;
using Tooba.Media.Application;
using Tooba.Media.Domain;
using Tooba.Media.Infrastructure.Persistence;

namespace Tooba.Media.Infrastructure;

/// <summary>دایرکتوری Media با اعتبارسنجی MIME/اندازه و ذخیره‌سازی امن کلید.</summary>
public sealed class MediaDirectory : IMediaDirectory
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/gif",
    };

    private static readonly Dictionary<string, string> ExtensionByContentType = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
        ["image/gif"] = ".gif",
    };

    private readonly MediaDbContext _db;
    private readonly IMediaObjectStore _store;
    private readonly long _maxUploadBytes;

    /// <summary>وابستگی‌های ذخیره‌سازی و پیکربندی را تزریق می‌کند.</summary>
    public MediaDirectory(MediaDbContext db, IMediaObjectStore store, IConfiguration configuration)
    {
        _db = db;
        _store = store;
        var configured = configuration["Tooba:Media:MaxUploadBytes"];
        _maxUploadBytes = long.TryParse(configured, out var max) && max > 0 ? max : 5_000_000;
    }

    /// <inheritdoc />
    public async Task<MediaAssetInfo> UploadAsync(
        Stream stream,
        string originalFileName,
        string contentType,
        Guid? actorUserId,
        CancellationToken cancellationToken)
    {
        if (stream is null || !stream.CanRead)
            throw new PlatformHttpException(400, "جریان آپلود رسانه نامعتبر است.", "media.upload.failed");

        var normalizedType = NormalizeContentType(contentType);
        if (!AllowedContentTypes.Contains(normalizedType))
            throw new PlatformHttpException(400, "نوع فایل رسانه پشتیبانی نمی‌شود.", "media.type.unsupported");

        var safeName = SanitizeOriginalFileName(originalFileName);
        await using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken);
        if (buffer.Length <= 0)
            throw new PlatformHttpException(400, "فایل رسانه خالی است.", "media.upload.failed");
        if (buffer.Length > _maxUploadBytes)
            throw new PlatformHttpException(400, "حجم فایل رسانه از سقف مجاز بیشتر است.", "media.too_large");

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
        catch (PlatformHttpException)
        {
            throw;
        }
        catch (Exception)
        {
            throw new PlatformHttpException(503, "ذخیره‌سازی رسانه در دسترس نیست.", "media.storage.unavailable");
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
        catch (Exception)
        {
            try { await _store.DeleteAsync(storageKey, cancellationToken); } catch { /* ignore cleanup */ }
            throw new PlatformHttpException(500, "ثبت فرادادهٔ رسانه ناموفق بود.", "media.upload.failed");
        }

        return Map(asset);
    }

    /// <inheritdoc />
    public async Task<MediaPagedResult<MediaAssetInfo>> QueryAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
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

    private static string NormalizeContentType(string contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return string.Empty;
        var trimmed = contentType.Trim();
        var semicolon = trimmed.IndexOf(';');
        if (semicolon >= 0)
            trimmed = trimmed[..semicolon].Trim();
        return trimmed.ToLowerInvariant();
    }

    private static string SanitizeOriginalFileName(string originalFileName)
    {
        var name = string.IsNullOrWhiteSpace(originalFileName) ? "upload" : Path.GetFileName(originalFileName.Trim());
        if (string.IsNullOrWhiteSpace(name))
            name = "upload";
        foreach (var ch in Path.GetInvalidFileNameChars())
            name = name.Replace(ch, '_');
        if (name.Length > MediaAsset.OriginalFileNameMaxLength)
            name = name[..MediaAsset.OriginalFileNameMaxLength];
        return name;
    }

    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);

    private static MediaAssetInfo Map(MediaAsset asset) =>
        new(
            asset.MediaAssetId,
            asset.OriginalFileName,
            asset.ContentType,
            asset.ByteSize,
            asset.Width,
            asset.Height,
            asset.CreatedAt,
            DisplayUrl: $"/v1/storefront/media/{asset.MediaAssetId:D}");
}
