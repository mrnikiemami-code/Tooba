using Microsoft.Extensions.Hosting;
using Tooba.Geography.Infrastructure.Development;
using Tooba.Host.Configuration;
using Xunit;

namespace Tooba.Host.Tests.Development;

/// <summary>
/// TB240-R1 focused guard facts for the reference-data commissioning seam (languages then geography) and for
/// the Geography-owned seed's documented constants. The live readback (real language/geography ids through
/// the real lookups) stays the runtime evidence.
/// </summary>
public sealed class MarketplaceReferenceDataBootstrapTests
{
    [Fact]
    public async Task Enabled_development_commissions_languages_first_then_geography()
    {
        var order = new List<string>();
        await MarketplaceDevelopmentBootstrap.EnsureReferenceDataAsync(
            new EnvironmentStub("Development"),
            new MarketplaceReferenceDataOptions { Enabled = true },
            _ =>
            {
                order.Add("languages");
                return Task.CompletedTask;
            },
            _ =>
            {
                order.Add("geography");
                return Task.CompletedTask;
            },
            CancellationToken.None);

        Assert.Equal(new[] { "languages", "geography" }, order);
    }

    [Fact]
    public async Task Disabled_or_non_development_configuration_writes_nothing()
    {
        foreach (var (environmentName, enabled) in new[]
                 {
                     ("Development", false),
                     ("Production", true),
                     ("Staging", true),
                 })
        {
            var calls = 0;
            await MarketplaceDevelopmentBootstrap.EnsureReferenceDataAsync(
                new EnvironmentStub(environmentName),
                new MarketplaceReferenceDataOptions { Enabled = enabled },
                _ =>
                {
                    calls++;
                    return Task.CompletedTask;
                },
                _ =>
                {
                    calls++;
                    return Task.CompletedTask;
                },
                CancellationToken.None);

            Assert.Equal(0, calls);
        }
    }

    [Fact]
    public async Task A_failing_language_step_never_reaches_the_geography_seed()
    {
        var geographyCalls = 0;
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await MarketplaceDevelopmentBootstrap.EnsureReferenceDataAsync(
                new EnvironmentStub("Development"),
                new MarketplaceReferenceDataOptions { Enabled = true },
                _ => throw new InvalidOperationException("languages unavailable"),
                _ =>
                {
                    geographyCalls++;
                    return Task.CompletedTask;
                },
                CancellationToken.None));

        Assert.Equal(0, geographyCalls);
    }

    [Fact]
    public void Checked_in_configuration_never_enables_reference_data_commissioning()
    {
        foreach (var file in Directory.EnumerateFiles(
                     Path.Combine(FindRepoRoot(), "src", "backend", "Host", "Tooba.Host"),
                     "appsettings*.json",
                     SearchOption.AllDirectories))
        {
            Assert.DoesNotContain("MarketplaceReferenceData", File.ReadAllText(file), StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Geography_seed_codes_are_documented_local_seed_codes_bound_to_real_iran()
    {
        Assert.Equal("IR", GeographyDevelopmentSeed.IranCountryCode);
        Assert.StartsWith("IR-DEV-", GeographyDevelopmentSeed.TehranAreaCode, StringComparison.Ordinal);
        Assert.StartsWith("IR-DEV-", GeographyDevelopmentSeed.TehranCityCode, StringComparison.Ordinal);
        Assert.True(GeographyDevelopmentSeed.TehranAreaCode.Length <= 32);
        Assert.True(GeographyDevelopmentSeed.TehranCityCode.Length <= 32);
        Assert.Equal(new[] { "fa-IR", "en-US" }, GeographyDevelopmentSeed.RequiredLanguageCodes);
        Assert.Equal("ایران", GeographyDevelopmentSeed.IranNameFa);
        Assert.Equal("تهران", GeographyDevelopmentSeed.TehranNameFa);
    }

    [Fact]
    public void Geography_infrastructure_writes_only_through_its_own_context_and_domain()
    {
        var text = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src", "backend", "Modules", "Geography", "Tooba.Geography.Infrastructure", "Development",
            "GeographyDevelopmentSeed.cs"));

        Assert.Contains("GeographyDbContext", text, StringComparison.Ordinal);
        Assert.Contains("Country.Create", text, StringComparison.Ordinal);
        Assert.Contains("AdministrativeArea.Create", text, StringComparison.Ordinal);
        Assert.Contains("City.Create", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Localization.Infrastructure", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Localization.Domain", text, StringComparison.Ordinal);
        Assert.DoesNotContain("RawSql", text, StringComparison.OrdinalIgnoreCase);
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }

    private sealed class EnvironmentStub(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "Tooba.Host";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
