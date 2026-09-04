using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;
using Tooba.BuildingBlocks;
using Tooba.Host.Media;
using Tooba.Media.Application;
using Tooba.Media.Infrastructure;
using Tooba.Media.Infrastructure.Persistence;
using Tooba.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>TB-P07-T029: آپلود DAM، رد MIME/حجم، صفحه کتابخانه و ارائهٔ باینری.</summary>
[Collection("PostgresSerial")]
public sealed class MediaDamTests : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private bool _dockerAvailable;
    private string? _tempRoot;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "tooba-media-dam-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
        try
        {
            _container = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("tooba_media")
                .WithUsername("tooba")
                .WithPassword("dev-placeholder")
                .Build();
            await _container.StartAsync();
            _dockerAvailable = true;
        }
        catch (Exception)
        {
            _dockerAvailable = false;
        }
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        if (_container is not null)
            await _container.DisposeAsync();
        if (_tempRoot is not null && Directory.Exists(_tempRoot))
        {
            try { Directory.Delete(_tempRoot, recursive: true); } catch { /* ignore */ }
        }
    }

    [Fact]
    public void Media_module_boundary_static_checks()
    {
        Assert.Equal("media", MediaDbContext.Schema);
        Assert.NotNull(typeof(IMediaDirectory).GetMethod(nameof(IMediaDirectory.UploadAsync)));
        Assert.NotNull(typeof(IMediaDirectory).GetMethod(nameof(IMediaDirectory.QueryAsync)));
        Assert.NotNull(typeof(IMediaObjectStore).GetMethod(nameof(IMediaObjectStore.SaveAsync)));
    }

    [SkippableFact]
    public async Task Upload_jpeg_success_rejects_plain_and_oversized_and_pages_library()
    {
        Skip.If(!_dockerAvailable || _container is null || _tempRoot is null,
            "Docker/Testcontainers PostgreSQL is not available.");

        await using var db = CreateDb(_container.GetConnectionString());
        await db.Database.MigrateAsync();
        var store = new LocalFileMediaStore(_tempRoot);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Tooba:Media:MaxUploadBytes"] = "5000",
            })
            .Build();
        var directory = new MediaDirectory(db, store, config);

        await using var jpeg = new MemoryStream(MinimalJpegBytes());
        var uploaded = await directory.UploadAsync(jpeg, "hero.jpg", "image/jpeg", UuidV7.New(), CancellationToken.None);
        Assert.Equal("image/jpeg", uploaded.ContentType);
        Assert.True(uploaded.ByteSize > 0);
        Assert.NotEqual(Guid.Empty, uploaded.MediaAssetId);

        await using var plain = new MemoryStream("not-an-image"u8.ToArray());
        var unsupported = await Assert.ThrowsAsync<PlatformHttpException>(() =>
            directory.UploadAsync(plain, "notes.txt", "text/plain", null, CancellationToken.None));
        Assert.Equal("media.type.unsupported", unsupported.ErrorCode);

        await using var pdf = new MemoryStream("%PDF-1.4 minimal"u8.ToArray());
        var pdfUploaded = await directory.UploadAsync(pdf, "doc.pdf", "application/pdf", null, CancellationToken.None);
        Assert.Equal("application/pdf", pdfUploaded.ContentType);
        Assert.True(pdfUploaded.ByteSize > 0);

        await using var oversized = new MemoryStream(new byte[5001]);
        var tooLarge = await Assert.ThrowsAsync<PlatformHttpException>(() =>
            directory.UploadAsync(oversized, "big.jpg", "image/jpeg", null, CancellationToken.None));
        Assert.Equal("media.too_large", tooLarge.ErrorCode);

        await using var second = new MemoryStream(MinimalJpegBytes());
        await directory.UploadAsync(second, "second.jpg", "image/jpeg", null, CancellationToken.None);

        var page1 = await directory.QueryAsync(null, page: 1, pageSize: 1, CancellationToken.None);
        Assert.Single(page1.Items);
        Assert.Equal(3, page1.TotalCount);
        var page2 = await directory.QueryAsync(null, page: 2, pageSize: 1, CancellationToken.None);
        Assert.Single(page2.Items);
        Assert.NotEqual(page1.Items[0].MediaAssetId, page2.Items[0].MediaAssetId);

        // همان helper که /v1/storefront/media/{id} و /v1/media/{id} استفاده می‌کنند.
        var served = await MediaEndpoints.TryServeStoredMediaAsync(
            uploaded.MediaAssetId, directory, store, CancellationToken.None);
        var file = Assert.IsType<FileStreamHttpResult>(served);
        Assert.Equal("image/jpeg", file.ContentType);
    }

    private static MediaDbContext CreateDb(string connectionString)
    {
        var options = new DbContextOptionsBuilder<MediaDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, connectionString, MediaDbContext.Schema, typeof(MediaDbContext));
        return new MediaDbContext(options.Options);
    }

    /// <summary>حداقل JPEG معتبر ۱×۱.</summary>
    private static byte[] MinimalJpegBytes() =>
    [
        0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01, 0x01, 0x00, 0x00, 0x01,
        0x00, 0x01, 0x00, 0x00, 0xFF, 0xDB, 0x00, 0x43, 0x00, 0x08, 0x06, 0x06, 0x07, 0x06, 0x05, 0x08,
        0x07, 0x07, 0x07, 0x09, 0x09, 0x08, 0x0A, 0x0C, 0x14, 0x0D, 0x0C, 0x0B, 0x0B, 0x0C, 0x19, 0x12,
        0x13, 0x0F, 0x14, 0x1D, 0x1A, 0x1F, 0x1E, 0x1D, 0x1A, 0x1C, 0x1C, 0x20, 0x24, 0x2E, 0x27, 0x20,
        0x22, 0x2C, 0x23, 0x1C, 0x1C, 0x28, 0x37, 0x29, 0x2C, 0x30, 0x31, 0x34, 0x34, 0x34, 0x1F, 0x27,
        0x39, 0x3D, 0x38, 0x32, 0x3C, 0x2E, 0x33, 0x34, 0x32, 0xFF, 0xC0, 0x00, 0x0B, 0x08, 0x00, 0x01,
        0x00, 0x01, 0x01, 0x01, 0x11, 0x00, 0xFF, 0xC4, 0x00, 0x14, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x03, 0xFF, 0xC4, 0x00, 0x14,
        0x10, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0xFF, 0xDA, 0x00, 0x08, 0x01, 0x01, 0x00, 0x00, 0x3F, 0x00, 0x7F, 0xFF, 0xD9
    ];
}
