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

/// <summary>TB-TMAR-HOST-LOCALIZATION-AMC-001-R1 — typed failure semantics.</summary>
public sealed class LocalizationFailureSemanticsTests : IDisposable
{
    private readonly LocalizationDbContext _db;

    public LocalizationFailureSemanticsTests()
    {
        var options = new DbContextOptionsBuilder<LocalizationDbContext>()
            .UseInMemoryDatabase($"localization-failure-{Guid.NewGuid():N}")
            .Options;
        _db = new LocalizationDbContext(options);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task Expected_rejection_yields_exact_LanguageErrorCodes_not_message_text()
    {
        var directory = new LanguageDirectory(_db, new AlwaysFalseGuard());
        await directory.BootstrapAsync(CancellationToken.None);

        var duplicate = await Assert.ThrowsAsync<SemanticException>(() => directory.CreateAsync(
            new CreateLanguageSpec("fa-IR", "xx", "x", "x", "rtl", "fa-IR", "Jalali", true, false, 9),
            CancellationToken.None));
        Assert.Equal(LanguageErrorCodes.CodeDuplicate, duplicate.Error.Code);

        var inactive = await Assert.ThrowsAsync<SemanticException>(() =>
            directory.EnsureActiveLanguageCodeAsync("zz-ZZ", CancellationToken.None));
        Assert.Equal(LanguageErrorCodes.Inactive, inactive.Error.Code);
    }

    [Fact]
    public async Task Changing_SemanticException_message_cannot_change_machine_code()
    {
        var directory = new LanguageDirectory(_db, new AlwaysFalseGuard());
        await directory.BootstrapAsync(CancellationToken.None);
        var ex = await Assert.ThrowsAsync<SemanticException>(() => directory.CreateAsync(
            new CreateLanguageSpec("en-US", "yy", "x", "x", "ltr", "en-US", "Gregorian", true, false, 9),
            CancellationToken.None));

        Assert.Equal(LanguageErrorCodes.CodeDuplicate, ex.Error.Code);
        Assert.Equal(ex.Error.Code, ex.Message);
        Assert.NotEqual("some localized prose", ex.Error.Code);
    }

    [Fact]
    public void Unexpected_InvalidOperationException_is_not_remapped_by_endpoints_source()
    {
        var endpoints = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src/backend/Modules/Localization/Tooba.Localization.Endpoints/Admin/LocaleAdminEndpoints.cs"));
        Assert.DoesNotContain("InvalidOperationException", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("TryMapLanguageFault", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("StartsWith", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("Contains(", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("catch (SemanticException", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("ILanguageDirectory", endpoints, StringComparison.Ordinal);
        Assert.Contains("ISender", endpoints, StringComparison.Ordinal);
        Assert.Contains("api.From", endpoints, StringComparison.Ordinal);
        Assert.Contains("ApiResponseFactory", endpoints, StringComparison.Ordinal);
    }

    private sealed class AlwaysFalseGuard : ILanguageReferenceGuard
    {
        public Task<bool> IsReferencedAsync(string languageCode, CancellationToken cancellationToken) =>
            Task.FromResult(false);
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}

