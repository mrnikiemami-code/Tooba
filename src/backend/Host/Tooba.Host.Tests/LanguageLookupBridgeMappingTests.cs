using Microsoft.EntityFrameworkCore;
using Tooba.Localization.Application.Models;
using Tooba.Localization.Contracts.Ports;
using Tooba.Localization.Infrastructure.Adapters;
using Tooba.Localization.Infrastructure.Languages;
using Tooba.Localization.Infrastructure.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// TB-SHIPPING-ADMIN-CONFIGURATION-CONTEXT-188 — the additive <see cref="ILanguageLookup"/> metadata.
/// </summary>
/// <remarks>
/// This is the existing Localization test home: a real <see cref="LanguageDirectory"/> over the
/// Localization EF model with the in-memory provider (a controlled test-owned database), driven
/// through the real <see cref="LanguageLookupBridge"/>. It proves the bridge projects the
/// authoritative row unchanged and that the previously published five-member contract, the
/// deconstruction and the known-language (not active-only) semantics are preserved.
/// </remarks>
public sealed class LanguageLookupBridgeMappingTests : IDisposable
{
    private readonly LocalizationDbContext _db;
    private readonly LanguageDirectory _directory;
    private readonly LanguageLookupBridge _bridge;

    public LanguageLookupBridgeMappingTests()
    {
        var options = new DbContextOptionsBuilder<LocalizationDbContext>()
            .UseInMemoryDatabase($"language-lookup-bridge-{Guid.NewGuid():N}")
            .Options;
        _db = new LocalizationDbContext(options);
        _directory = new LanguageDirectory(_db, new NoLanguageReferences());
        _bridge = new LanguageLookupBridge(_directory);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task The_bridge_still_returns_every_known_language_in_directory_order()
    {
        await _directory.BootstrapAsync(CancellationToken.None);

        var rows = await _bridge.ListAsync(CancellationToken.None);
        var direct = await _directory.ListAsync(CancellationToken.None);

        // Known-language set and order are unchanged: the additive metadata did not become a filter.
        Assert.Equal(direct.Select(row => row.LanguageId), rows.Select(row => row.LanguageId));
        Assert.Equal(2, rows.Count);
        Assert.Equal(new[] { "fa-IR", "en-US" }, rows.Select(row => row.Culture).ToArray());
    }

    [Fact]
    public async Task Every_added_member_is_projected_from_the_authoritative_row()
    {
        await _directory.BootstrapAsync(CancellationToken.None);
        var direct = await _directory.ListAsync(CancellationToken.None);

        var rows = await _bridge.ListAsync(CancellationToken.None);

        Assert.Equal(direct.Count, rows.Count);
        for (var index = 0; index < direct.Count; index++)
        {
            var source = direct[index];
            var projected = rows[index];

            Assert.Equal(source.LanguageId, projected.LanguageId);
            Assert.Equal(source.Code, projected.Code);
            Assert.Equal(source.Culture, projected.Culture);
            Assert.Equal(source.UrlPrefix, projected.UrlPrefix);
            Assert.Equal(source.IsDefault, projected.IsDefault);

            // The five new facts are real row values, not inferred defaults.
            Assert.Equal(source.IsActive, projected.IsActive);
            Assert.Equal(source.Direction, projected.Direction);
            Assert.Equal(source.DisplayName, projected.DisplayName);
            Assert.Equal(source.NativeName, projected.NativeName);
            Assert.Equal(source.SortOrder, projected.SortOrder);
            Assert.NotNull(projected.IsActive);
            Assert.False(string.IsNullOrWhiteSpace(projected.Direction));
            Assert.False(string.IsNullOrWhiteSpace(projected.DisplayName));
            Assert.False(string.IsNullOrWhiteSpace(projected.NativeName));
        }

        // Bootstrap data really carries the RTL/LTR distinction, not a hardcoded default.
        Assert.Contains(rows, row => row.Direction == "rtl");
        Assert.Contains(rows, row => row.Direction == "ltr");
    }

    [Fact]
    public async Task An_inactive_language_keeps_its_identity_and_reports_the_real_inactive_flag()
    {
        await _directory.BootstrapAsync(CancellationToken.None);
        var before = await _directory.GetByCodeAsync("en-US", CancellationToken.None);
        Assert.NotNull(before);
        Assert.True(before.IsActive);

        // Deactivating the non-default language keeps the single-active-default invariant intact.
        await _directory.PatchAsync("en-US", new PatchLanguageSpec(false, null, null), CancellationToken.None);

        var rows = await _bridge.ListAsync(CancellationToken.None);
        var english = Assert.Single(rows, row => row.Culture == "en-US");

        Assert.False(english.IsActive);
        Assert.Equal(before.LanguageId, english.LanguageId);
        Assert.Equal("ltr", english.Direction);

        // The other row is untouched, and the known-language set still contains the inactive row.
        Assert.True(Assert.Single(rows, row => row.Culture == "fa-IR").IsActive);
        Assert.Equal(2, rows.Count);
    }

    [Fact]
    public async Task Owner_provided_order_is_projected_and_not_recomputed()
    {
        await _directory.BootstrapAsync(CancellationToken.None);
        await _directory.PatchAsync("fa-IR", new PatchLanguageSpec(null, null, 7), CancellationToken.None);

        var rows = await _bridge.ListAsync(CancellationToken.None);

        Assert.Equal(7, Assert.Single(rows, row => row.Culture == "fa-IR").SortOrder);
    }

    [Fact]
    public void The_legacy_five_member_contract_and_deconstruction_are_unchanged()
    {
        var id = Guid.Parse("01900000-0000-7000-8000-00000000aa01");

        var snapshot = new LanguageLookupSnapshot(id, "fa", "fa-IR", "fa", true);
        var (languageId, code, culture, urlPrefix, isDefault) = snapshot;

        Assert.Equal(id, languageId);
        Assert.Equal("fa", code);
        Assert.Equal("fa-IR", culture);
        Assert.Equal("fa", urlPrefix);
        Assert.True(isDefault);

        // A legacy positional snapshot never claims configured metadata it was not given.
        Assert.Null(snapshot.IsActive);
        Assert.Null(snapshot.Direction);
        Assert.Null(snapshot.DisplayName);
        Assert.Null(snapshot.NativeName);
        Assert.Null(snapshot.SortOrder);

        // The added metadata is immutable: a `with` copy changes only the copy.
        var configured = snapshot with { Direction = "rtl", SortOrder = 3 };
        Assert.Equal("rtl", configured.Direction);
        Assert.Equal(3, configured.SortOrder);
        Assert.Null(snapshot.Direction);
        Assert.Null(snapshot.SortOrder);
    }

    private sealed class NoLanguageReferences : Tooba.Localization.Contracts.Ports.ILanguageReferenceGuard
    {
        public Task<bool> IsReferencedAsync(string languageCode, CancellationToken cancellationToken) =>
            Task.FromResult(false);
    }
}
