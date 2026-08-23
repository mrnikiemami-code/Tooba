namespace Tooba.BuildingBlocks;

/// <summary>
/// درز حل اتصال پایگاه‌داده: <see cref="ConnectionReference"/> را به connection string قابل‌استفاده تبدیل می‌کند.
/// پیاده‌سازی Host است؛ ماژول‌ها نباید credential خام را از پیکربندی بخوانند یا لاگ کنند.
/// </summary>
/// <remarks>
/// شکست باید fail-closed باشد (مثلاً 503 با کد <c>platform.connection.unconfigured</c>) نه افشای جزئیات اتصال.
/// </remarks>
public interface IDatabaseConnectionResolver
{
    /// <summary>
    /// مقدار connection string متناظر با مرجع را برمی‌گرداند.
    /// </summary>
    /// <param name="reference">شناسهٔ منطقی اتصال، نه خودِ credential.</param>
    /// <returns>رشتهٔ اتصال معتبر برای Npgsql؛ هرگز برای لاگ فنی در نظر گرفته نشود.</returns>
    /// <exception cref="PlatformHttpException">وقتی مرجع خالی، ناشناخته، یا از نظر نحوی نامعتبر است.</exception>
    string Resolve(ConnectionReference reference);
}

/// <summary>
/// تولید UUID نسخهٔ ۷ (زمان‌محور RFC 9562) برای کلیدهای پایدار موجودیت‌ها.
/// روی .NET 8 از Guid big-endian استفاده می‌شود تا ترتیب بایتری با نسخهٔ ۷ سازگار بماند.
/// </summary>
public static class UuidV7
{
    /// <summary>
    /// یک شناسهٔ UUID v7 جدید تولید می‌کند. نسخه در nibble بالای بایت ۶ برابر ۷ است.
    /// </summary>
    public static Guid New()
    {
        Span<byte> bytes = stackalloc byte[16];
        var unixMs = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        System.Buffers.Binary.BinaryPrimitives.WriteUInt64BigEndian(bytes, unixMs << 16);
        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x70);
        Random.Shared.NextBytes(bytes[8..]);
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);
        return new Guid(bytes, bigEndian: true);
    }
}
