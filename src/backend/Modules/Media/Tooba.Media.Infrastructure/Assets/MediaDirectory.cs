using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Tooba.Media.Application.Models;
using Tooba.Media.Application.Ports;
using Tooba.Media.Domain.Aggregates;
using Tooba.Media.Infrastructure.Persistence;

namespace Tooba.Media.Infrastructure.Assets;

/// <summary>
/// Shared seam of the Media directory: configuration bounds, the accepted MIME policy and the
/// mapping from the persisted <see cref="MediaAsset"/> aggregate to the module DTO.
/// The upload pipeline lives in <c>MediaDirectory.Upload.cs</c> and the read-only library queries in
/// <c>MediaDirectory.Queries.cs</c>; no responsibility is duplicated across the partials.
/// </summary>
public sealed partial class MediaDirectory : IMediaDirectory
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/gif",
        "application/pdf",
        "video/mp4",
        "video/webm",
    };

    private static readonly Dictionary<string, string> ExtensionByContentType = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
        ["image/gif"] = ".gif",
        ["application/pdf"] = ".pdf",
        ["video/mp4"] = ".mp4",
        ["video/webm"] = ".webm",
    };

    private readonly MediaDbContext _db;
    private readonly IMediaObjectStore _store;
    private readonly long _maxUploadBytes;

    /// <summary>وابستگی‌های ذخیره‌سازی و پیکربندی را تزریق می‌کند.</summary>
    public MediaDirectory(MediaDbContext db, IMediaObjectStore store, IConfiguration configuration)
    {
        _db = db;
        _store = store;
        // Default 50MB so video/pdf can upload; tests override via Tooba:Media:MaxUploadBytes.
        var configured = configuration["Tooba:Media:MaxUploadBytes"];
        _maxUploadBytes = long.TryParse(configured, out var max) && max > 0 ? max : 50_000_000;
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
            DisplayUrl: $"/v1/storefront/media/{asset.MediaAssetId:D}",
            FocalPointX: asset.FocalPointX,
            FocalPointY: asset.FocalPointY);
}
