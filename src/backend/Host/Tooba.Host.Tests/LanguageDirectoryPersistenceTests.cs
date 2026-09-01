using Microsoft.EntityFrameworkCore;
using Xunit;
using Tooba.Localization.Application;
using Tooba.Localization.Domain;
using Tooba.Localization.Infrastructure;
using Tooba.Localization.Infrastructure.Persistence;

namespace Tooba.Host.Tests;

public sealed class LanguageDirectoryPersistenceTests : IDisposable
{
    private readonly LocalizationDbContext _db;
    private readonly LanguageDirectory _directory;

    public LanguageDirectoryPersistenceTests()
    {
        var options = new DbContextOptionsBuilder<LocalizationDbContext>()
            .UseInMemoryDatabase($"localization-tests-{Guid.NewGuid():N}")
            .Options;
        _db = new LocalizationDbContext(options);
        _directory = new LanguageDirectory(_db, new NoLanguageReferences());
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task Bootstrap_is_idempotent_and_seeds_fa_en()
    {
        await _directory.BootstrapAsync(CancellationToken.None);
        await _directory.BootstrapAsync(CancellationToken.None);
        var rows = await _directory.ListAsync(CancellationToken.None);
        Assert.Equal(2, rows.Count);
        Assert.Single(rows.Where(x => x is { Code: "fa-IR", IsDefault: true, IsActive: true }));
        Assert.Contains(rows, x => x.Code == "en-US" && !x.IsDefault);
    }

    [Fact]
    public async Task Cannot_deactivate_default_without_replacement()
    {
        await _directory.BootstrapAsync(CancellationToken.None);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _directory.PatchAsync("fa-IR", new PatchLanguageCommand(false, true, null), CancellationToken.None));
    }

    [Fact]
    public async Task Can_move_default_to_en_US()
    {
        await _directory.BootstrapAsync(CancellationToken.None);
        var updated = await _directory.PatchAsync("en-US", new PatchLanguageCommand(null, true, null), CancellationToken.None);
        Assert.True(updated.IsDefault);
        var rows = await _directory.ListAsync(CancellationToken.None);
        Assert.Single(rows.Where(x => x.IsDefault));
        Assert.Equal("en-US", rows.Single(x => x.IsDefault).Code);
    }

    [Fact]
    public async Task EnsureActiveLanguageCode_rejects_unknown_locale()
    {
        await _directory.BootstrapAsync(CancellationToken.None);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _directory.EnsureActiveLanguageCodeAsync("de-DE", CancellationToken.None));
    }

    [Fact]
    public async Task Code_and_url_prefix_must_be_unique()
    {
        await _directory.BootstrapAsync(CancellationToken.None);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _directory.CreateAsync(
            new CreateLanguageCommand("fa-IR", "fa2", "x", "x", "rtl", "fa-IR", "Jalali", true, false, 2),
            CancellationToken.None));
    }

    private sealed class NoLanguageReferences : ILanguageReferenceGuard
    {
        public Task<bool> IsReferencedAsync(string languageCode, CancellationToken cancellationToken) =>
            Task.FromResult(false);
    }
}
