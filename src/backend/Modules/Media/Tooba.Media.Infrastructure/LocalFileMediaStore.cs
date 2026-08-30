using Tooba.BuildingBlocks;
using Tooba.Media.Application;

namespace Tooba.Media.Infrastructure;

/// <summary>ذخیره‌ساز فایل محلی با کلید نسبی امن زیر ریشهٔ پیکربندی‌شده.</summary>
public sealed class LocalFileMediaStore : IMediaObjectStore
{
    private readonly string _root;

    /// <summary>ریشهٔ فیزیکی ذخیره‌سازی را تثبیت می‌کند.</summary>
    public LocalFileMediaStore(string root)
    {
        if (string.IsNullOrWhiteSpace(root))
            throw new PlatformHttpException(503, "ریشهٔ ذخیره‌سازی رسانه پیکربندی نشده است.", "media.storage.unavailable");
        _root = Path.GetFullPath(root);
        Directory.CreateDirectory(_root);
    }

    /// <inheritdoc />
    public async Task SaveAsync(Stream stream, string key, string contentType, CancellationToken cancellationToken)
    {
        _ = contentType;
        var path = ResolveSafePath(key);
        var directory = Path.GetDirectoryName(path)
            ?? throw new PlatformHttpException(503, "مسیر ذخیره‌سازی رسانه نامعتبر است.", "media.storage.unavailable");
        Directory.CreateDirectory(directory);
        await using var file = new FileStream(
            path,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);
        await stream.CopyToAsync(file, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = ResolveSafePath(key);
        if (!File.Exists(path))
            return Task.FromResult<Stream?>(null);
        Stream stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);
        return Task.FromResult<Stream?>(stream);
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(File.Exists(ResolveSafePath(key)));
    }

    /// <inheritdoc />
    public Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = ResolveSafePath(key);
        if (File.Exists(path))
            File.Delete(path);
        return Task.CompletedTask;
    }

    private string ResolveSafePath(string key)
    {
        if (string.IsNullOrWhiteSpace(key)
            || key.Contains("..", StringComparison.Ordinal)
            || Path.IsPathRooted(key)
            || key.Contains(':')
            || key.Contains('\\'))
        {
            throw new PlatformHttpException(400, "کلید ذخیره‌سازی رسانه نامعتبر است.", "media.storage.unavailable");
        }

        var normalized = key.Replace('\\', '/').Trim('/');
        if (normalized.Length == 0 || normalized.StartsWith('/'))
            throw new PlatformHttpException(400, "کلید ذخیره‌سازی رسانه نامعتبر است.", "media.storage.unavailable");

        var full = Path.GetFullPath(Path.Combine(_root, normalized.Replace('/', Path.DirectorySeparatorChar)));
        var rootWithSep = _root.EndsWith(Path.DirectorySeparatorChar)
            ? _root
            : _root + Path.DirectorySeparatorChar;
        if (!full.StartsWith(rootWithSep, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(full, _root, StringComparison.OrdinalIgnoreCase))
        {
            throw new PlatformHttpException(400, "کلید ذخیره‌سازی رسانه از ریشه خارج است.", "media.storage.unavailable");
        }

        return full;
    }
}
