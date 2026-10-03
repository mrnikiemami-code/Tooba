using Microsoft.EntityFrameworkCore;
using Xunit;
using Tooba.BuildingBlocks;
using Tooba.Localization.Application.Models;
using Tooba.Localization.Application.Ports;
using Tooba.Localization.Contracts.Errors;
using Tooba.Localization.Contracts.Ports;
using Tooba.Localization.Infrastructure.Languages;
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
        Assert.Contains(rows, x => x.Code == "fa-IR");
        Assert.Contains(rows, x => x.Code == "en-US");
    }

    [Fact]
    public async Task EnsureActiveLanguageCode_rejects_unknown_locale()
    {
        await _directory.BootstrapAsync(CancellationToken.None);
        var ex = await Assert.ThrowsAsync<SemanticException>(() =>
            _directory.EnsureActiveLanguageCodeAsync("de-DE", CancellationToken.None));
        Assert.Equal(LanguageErrorCodes.Inactive, ex.Error.Code);
    }

    [Fact]
    public async Task Code_and_url_prefix_must_be_unique()
    {
        await _directory.BootstrapAsync(CancellationToken.None);
        var ex = await Assert.ThrowsAsync<SemanticException>(() => _directory.CreateAsync(
            new CreateLanguageSpec("fa-IR", "fa2", "x", "x", "rtl", "fa-IR", "Jalali", true, false, 2),
            CancellationToken.None));
        Assert.Equal(LanguageErrorCodes.CodeDuplicate, ex.Error.Code);
    }

    [Fact]
    public async Task Referenced_language_rejects_code_change()
    {
        await _directory.BootstrapAsync(CancellationToken.None);
        var referenced = new ReferencedLanguageGuard(["fa-IR"]);
        var directory = new LanguageDirectory(_db, referenced);
        var ex = await Assert.ThrowsAsync<SemanticException>(() => directory.UpdateAsync(
            "fa-IR",
            new UpdateLanguageSpec("fa-IR-NEW", "fa", "فارسی", "فارسی", "rtl", "fa-IR", "Jalali", true, true, 0),
            CancellationToken.None));
        Assert.Equal(LanguageErrorCodes.CodeInUse, ex.Error.Code);
    }

    [Fact]
    public async Task Referenced_language_rejects_url_prefix_change()
    {
        await _directory.BootstrapAsync(CancellationToken.None);
        var referenced = new ReferencedLanguageGuard(["fa-IR"]);
        var directory = new LanguageDirectory(_db, referenced);
        var ex = await Assert.ThrowsAsync<SemanticException>(() => directory.UpdateAsync(
            "fa-IR",
            new UpdateLanguageSpec("fa-IR", "fa2", "فارسی", "فارسی", "rtl", "fa-IR", "Jalali", true, true, 0),
            CancellationToken.None));
        Assert.Equal(LanguageErrorCodes.UrlPrefixInUse, ex.Error.Code);
    }

    [Fact]
    public async Task Unreferenced_language_allows_identity_update()
    {
        await _directory.BootstrapAsync(CancellationToken.None);
        var directory = new LanguageDirectory(_db, new NoLanguageReferences());
        var updated = await directory.UpdateAsync(
            "en-US",
            new UpdateLanguageSpec("en-GB", "gb", "English UK", "English", "ltr", "en-GB", "Gregorian", true, false, 1),
            CancellationToken.None);
        Assert.Equal("en-GB", updated.Code);
        Assert.Equal("gb", updated.UrlPrefix);
    }

    [Fact]
    public async Task Referenced_language_allows_safe_field_update()
    {
        await _directory.BootstrapAsync(CancellationToken.None);
        var referenced = new ReferencedLanguageGuard(["fa-IR"]);
        var directory = new LanguageDirectory(_db, referenced);
        var updated = await directory.UpdateAsync(
            "fa-IR",
            new UpdateLanguageSpec("fa-IR", "fa", "Persian", "فارسی", "rtl", "fa-IR", "Jalali", true, true, 0),
            CancellationToken.None);
        Assert.Equal("Persian", updated.DisplayName);
    }

    [Fact]
    public async Task ListAdmin_exposes_capability_flags()
    {
        await _directory.BootstrapAsync(CancellationToken.None);
        var referenced = new ReferencedLanguageGuard(["fa-IR"]);
        var directory = new LanguageDirectory(_db, referenced);
        var rows = await directory.ListAdminAsync(CancellationToken.None);
        var fa = rows.Single(x => x.Snapshot.Code == "fa-IR");
        var en = rows.Single(x => x.Snapshot.Code == "en-US");
        Assert.True(fa.IsReferenced);
        Assert.False(fa.CanEditCode);
        Assert.False(fa.CanEditUrlPrefix);
        Assert.False(en.IsReferenced);
        Assert.True(en.CanEditCode);
        Assert.True(en.CanEditUrlPrefix);
    }

    private sealed class NoLanguageReferences : ILanguageReferenceGuard
    {
        public Task<bool> IsReferencedAsync(string languageCode, CancellationToken cancellationToken) =>
            Task.FromResult(false);
    }

    private sealed class ReferencedLanguageGuard(IReadOnlyCollection<string> referenced) : ILanguageReferenceGuard
    {
        public Task<bool> IsReferencedAsync(string languageCode, CancellationToken cancellationToken) =>
            Task.FromResult(referenced.Contains(languageCode));
    }
}

