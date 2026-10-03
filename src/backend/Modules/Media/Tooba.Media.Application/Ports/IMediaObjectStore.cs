namespace Tooba.Media.Application.Ports;

/// <summary>ذخیره‌ساز باینری محلی یا ابری برای کلیدهای نسبی امن.</summary>
public interface IMediaObjectStore
{
    /// <summary>جریان را زیر کلید داده‌شده ذخیره می‌کند.</summary>
    Task SaveAsync(Stream stream, string key, string contentType, CancellationToken cancellationToken);

    /// <summary>جریان خواندن برای کلید موجود برمی‌گرداند؛ در نبود null.</summary>
    Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken);

    /// <summary>وجود کلید را بررسی می‌کند.</summary>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken);

    /// <summary>کلید را در صورت وجود حذف می‌کند.</summary>
    Task DeleteAsync(string key, CancellationToken cancellationToken);
}
