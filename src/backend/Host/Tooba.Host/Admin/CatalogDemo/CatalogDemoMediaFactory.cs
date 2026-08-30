using System.Buffers.Binary;
using System.Text;
using Tooba.Media.Application;

namespace Tooba.Host.Admin.CatalogDemo;

/// <summary>
/// تولید بایت‌های محلی PNG/WebP/SVG و آپلود از طریق Media DAM با نام <c>demo-media-*</c>.
/// تصاویر محصول برای polish تجاری بزرگ‌تر و طرح‌دارند (نه مربع تک‌رنگ ۴۸px).
/// </summary>
public sealed class CatalogDemoMediaFactory
{
    private readonly IMediaDirectory _media;

    /// <summary>دایرکتوری Media را تزریق می‌کند.</summary>
    public CatalogDemoMediaFactory(IMediaDirectory media) => _media = media;

    /// <summary>سه‌تایی تصویر/آیکن/بنر برای یک کلید پایدار می‌سازد یا بازمی‌گرداند.</summary>
    public async Task<(Guid ImageId, Guid IconId, Guid BannerId)> EnsureCategoryMediaAsync(
        string categoryKey,
        CancellationToken cancellationToken)
    {
        var image = await UploadPngAsync(
            $"{CatalogDemoSeam.MediaFilePrefix}{categoryKey}-image-v2.png",
            128,
            128,
            cancellationToken);
        var icon = await UploadPngAsync(
            $"{CatalogDemoSeam.MediaFilePrefix}{categoryKey}-icon-v2.png",
            64,
            64,
            cancellationToken);
        var banner = await UploadWebpAsync($"{CatalogDemoSeam.MediaFilePrefix}{categoryKey}-banner.webp", cancellationToken);
        // SVG برای شواهد سیاست تولید محلی نگه‌داری می‌شود؛ DAM فعلی MIME SVG را نمی‌پذیرد.
        _ = CreateSvgBytes(categoryKey);
        return (image, icon, banner);
    }

    /// <summary>یک تصویر برای L2/L3 در صورت نیاز.</summary>
    public Task<Guid> EnsureSimpleImageAsync(string key, CancellationToken cancellationToken) =>
        UploadPngAsync($"{CatalogDemoSeam.MediaFilePrefix}{key}-image-v2.png", 96, 96, cancellationToken);

    /// <summary>یک آیکن.</summary>
    public Task<Guid> EnsureSimpleIconAsync(string key, CancellationToken cancellationToken) =>
        UploadPngAsync($"{CatalogDemoSeam.MediaFilePrefix}{key}-icon-v2.png", 48, 48, cancellationToken);

    /// <summary>
    /// استخر تصاویر دامنه برای گالری محصول (reuse کنترل‌شده سطح دامنه).
    /// </summary>
    public async Task<IReadOnlyList<Guid>> EnsureProductMediaPoolAsync(
        string domainKey,
        int poolSize,
        CancellationToken cancellationToken)
    {
        poolSize = Math.Clamp(poolSize, 5, 16);
        var ids = new List<Guid>(poolSize);
        for (var i = 0; i < poolSize; i++)
        {
            var fileName = $"{CatalogDemoSeam.MediaFilePrefix}prod-{domainKey}-{i + 1}-v2.png";
            var (r, g, b) = PaletteColor(domainKey, i);
            ids.Add(await UploadPngAsync(fileName, 320, 320, r, g, b, index: i, cancellationToken));
        }

        return ids;
    }

    private async Task<Guid> UploadPngAsync(string fileName, int width, int height, CancellationToken cancellationToken) =>
        await UploadPngAsync(fileName, width, height, 0x2F, 0x6B, 0xA8, index: 0, cancellationToken);

    private async Task<Guid> UploadPngAsync(
        string fileName,
        int width,
        int height,
        byte r,
        byte g,
        byte b,
        int index,
        CancellationToken cancellationToken)
    {
        var existing = await FindExistingAsync(fileName, cancellationToken);
        if (existing is Guid id)
        {
            return id;
        }

        await using var stream = new MemoryStream(CreatePngBytes(width, height, r, g, b, index));
        var uploaded = await _media.UploadAsync(stream, fileName, "image/png", actorUserId: null, cancellationToken);
        return uploaded.MediaAssetId;
    }

    private static (byte R, byte G, byte B) PaletteColor(string domain, int index)
    {
        var seed = Math.Abs(HashCode.Combine(domain, index));
        return (
            (byte)(40 + (seed % 180)),
            (byte)(50 + ((seed / 7) % 160)),
            (byte)(60 + ((seed / 13) % 150)));
    }

    private async Task<Guid> UploadWebpAsync(string fileName, CancellationToken cancellationToken)
    {
        var existing = await FindExistingAsync(fileName, cancellationToken);
        if (existing is Guid id)
        {
            return id;
        }

        await using var stream = new MemoryStream(CreateMinimalWebpBytes());
        var uploaded = await _media.UploadAsync(stream, fileName, "image/webp", actorUserId: null, cancellationToken);
        return uploaded.MediaAssetId;
    }

    private async Task<Guid?> FindExistingAsync(string fileName, CancellationToken cancellationToken)
    {
        var page = await _media.QueryAsync(fileName, page: 1, pageSize: 20, cancellationToken);
        var match = page.Items.FirstOrDefault(x =>
            string.Equals(x.OriginalFileName, fileName, StringComparison.OrdinalIgnoreCase));
        return match?.MediaAssetId;
    }

    /// <summary>PNG طرح‌دار فشرده‌شده با رنگ پایه.</summary>
    public static byte[] CreatePngBytes(int width, int height) =>
        CreatePngBytes(width, height, 0x2F, 0x6B, 0xA8, index: 0);

    /// <summary>PNG طرح‌دار با رنگ RGB و تنوع اندیس (گرادیان/قاب/دایره).</summary>
    public static byte[] CreatePngBytes(int width, int height, byte r, byte g, byte b) =>
        CreatePngBytes(width, height, r, g, b, index: 0);

    /// <summary>PNG طرح‌دار با رنگ RGB و تنوع اندیس (گرادیان/قاب/دایره).</summary>
    public static byte[] CreatePngBytes(int width, int height, byte r, byte g, byte b, int index)
    {
        width = Math.Clamp(width, 1, 512);
        height = Math.Clamp(height, 1, 512);
        var rawStride = 1 + (width * 3);
        var raw = new byte[rawStride * height];
        var cx = (width - 1) / 2.0;
        var cy = (height - 1) / 2.0;
        var radius = Math.Min(width, height) * (0.28 + ((index % 3) * 0.04));
        var border = Math.Max(2, Math.Min(width, height) / 32);

        for (var y = 0; y < height; y++)
        {
            var row = y * rawStride;
            raw[row] = 0; // filter None
            var gy = y / (double)Math.Max(1, height - 1);
            for (var x = 0; x < width; x++)
            {
                var gx = x / (double)Math.Max(1, width - 1);
                var inBorder = x < border || y < border || x >= width - border || y >= height - border;
                var dx = x - cx;
                var dy = y - cy;
                var inDisc = (dx * dx) + (dy * dy) <= radius * radius;
                var checker = ((x / 16) + (y / 16) + index) % 2 == 0;

                byte pr;
                byte pg;
                byte pb;
                if (inBorder)
                {
                    pr = (byte)Math.Clamp(r / 2, 0, 255);
                    pg = (byte)Math.Clamp(g / 2, 0, 255);
                    pb = (byte)Math.Clamp(b / 2, 0, 255);
                }
                else if (inDisc)
                {
                    pr = (byte)Math.Clamp(r + 55, 0, 255);
                    pg = (byte)Math.Clamp(g + 45, 0, 255);
                    pb = (byte)Math.Clamp(b + 35, 0, 255);
                }
                else
                {
                    var mix = 0.55 + (0.35 * gx) + (0.10 * (1.0 - gy));
                    if (checker)
                    {
                        mix *= 0.92;
                    }

                    pr = (byte)Math.Clamp((int)(r * mix), 0, 255);
                    pg = (byte)Math.Clamp((int)(g * mix), 0, 255);
                    pb = (byte)Math.Clamp((int)(b * mix), 0, 255);
                }

                var i = row + 1 + (x * 3);
                raw[i] = pr;
                raw[i + 1] = pg;
                raw[i + 2] = pb;
            }
        }

        using var ms = new MemoryStream();
        WritePngSignature(ms);
        WriteChunk(ms, "IHDR", BuildIhdr(width, height));
        WriteChunk(ms, "IDAT", ZlibCompress(raw));
        WriteChunk(ms, "IEND", ReadOnlySpan<byte>.Empty);
        return ms.ToArray();
    }

    private static byte[] BuildIhdr(int width, int height)
    {
        var buf = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(buf.AsSpan(0, 4), width);
        BinaryPrimitives.WriteInt32BigEndian(buf.AsSpan(4, 4), height);
        buf[8] = 8; // bit depth
        buf[9] = 2; // RGB
        buf[10] = 0;
        buf[11] = 0;
        buf[12] = 0;
        return buf;
    }

    /// <summary>WebP سادهٔ VP8L تک‌پیکسل (lossy-ish minimal RIFF).</summary>
    public static byte[] CreateMinimalWebpBytes()
    {
        // حداقل WebP Lossy معتبر ۱×۱ (از نمونهٔ canonical کوچک).
        return
        [
            0x52, 0x49, 0x46, 0x46, 0x24, 0x00, 0x00, 0x00, 0x57, 0x45, 0x42, 0x50, 0x56, 0x50, 0x38, 0x20,
            0x18, 0x00, 0x00, 0x00, 0x30, 0x01, 0x00, 0x9D, 0x01, 0x2A, 0x01, 0x00, 0x01, 0x00, 0x02, 0x00,
            0x34, 0x25, 0xA4, 0x00, 0x03, 0x70, 0x00, 0xFE, 0xFB, 0xFD, 0x50, 0x00
        ];
    }

    /// <summary>SVG برداری کوچک برای سیاست تولید محلی (آپلود DAM اختیاری).</summary>
    public static byte[] CreateSvgBytes(string label)
    {
        var safe = System.Security.SecurityElement.Escape(label) ?? "demo";
        var svg =
            $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"64\" height=\"64\" viewBox=\"0 0 64 64\">" +
            $"<rect width=\"64\" height=\"64\" fill=\"#2F6BA8\"/>" +
            $"<text x=\"32\" y=\"36\" text-anchor=\"middle\" fill=\"#fff\" font-size=\"10\">{safe}</text></svg>";
        return Encoding.UTF8.GetBytes(svg);
    }

    private static void WritePngSignature(Stream stream) =>
        stream.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);

    private static void WriteChunk(Stream stream, string type, ReadOnlySpan<byte> payload)
    {
        Span<byte> lenBytes = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(lenBytes, payload.Length);
        stream.Write(lenBytes);
        var typeBytes = Encoding.ASCII.GetBytes(type);
        stream.Write(typeBytes);
        if (!payload.IsEmpty)
        {
            stream.Write(payload);
        }

        var crc = Crc32(typeBytes, payload);
        Span<byte> crcBytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(crcBytes, crc);
        stream.Write(crcBytes);
    }

    private static byte[] ZlibCompress(byte[] raw)
    {
        using var output = new MemoryStream();
        // zlib header
        output.WriteByte(0x78);
        output.WriteByte(0x01);
        using (var deflate = new System.IO.Compression.DeflateStream(output, System.IO.Compression.CompressionLevel.Fastest, leaveOpen: true))
        {
            deflate.Write(raw);
        }

        var adler = Adler32(raw);
        Span<byte> checksum = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(checksum, adler);
        output.Write(checksum);
        return output.ToArray();
    }

    private static uint Adler32(ReadOnlySpan<byte> data)
    {
        uint a = 1, b = 0;
        foreach (var t in data)
        {
            a = (a + t) % 65521;
            b = (b + a) % 65521;
        }

        return (b << 16) | a;
    }

    private static uint Crc32(ReadOnlySpan<byte> type, ReadOnlySpan<byte> payload)
    {
        uint crc = 0xFFFFFFFF;
        foreach (var t in type)
        {
            crc = CrcTable[(crc ^ t) & 0xFF] ^ (crc >> 8);
        }

        foreach (var p in payload)
        {
            crc = CrcTable[(crc ^ p) & 0xFF] ^ (crc >> 8);
        }

        return crc ^ 0xFFFFFFFF;
    }

    private static readonly uint[] CrcTable = CreateCrcTable();

    private static uint[] CreateCrcTable()
    {
        var table = new uint[256];
        for (uint n = 0; n < 256; n++)
        {
            var c = n;
            for (var k = 0; k < 8; k++)
            {
                c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
            }

            table[n] = c;
        }

        return table;
    }
}
