using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-CONFIGURATION-AMC-001-W1 — structure/fail-fast hygiene guard (not certification).
/// </summary>
public sealed class HostConfigurationAmcW1GuardTests
{
    private static readonly string[] ExpectedFiles =
    [
        "ControlPlaneRegistry.cs",
        "MarketplaceOptions.cs",
        "PlatformOptionsValidator.cs",
        "PostgreSqlOptions.cs",
        "SingleStoreOptions.cs",
        "StoreCommerceOptions.cs",
        "TenantRecord.cs",
        "TenantRecordOptions.cs",
        "ToobaPlatformOptions.cs",
    ];

    private static readonly string[] ExpectedTypes =
    [
        "ControlPlaneRegistry",
        "MarketplaceOptions",
        "PlatformOptionsValidator",
        "PostgreSqlOptions",
        "SingleStoreOptions",
        "StoreCommerceOptions",
        "TenantRecord",
        "TenantRecordOptions",
        "ToobaPlatformOptions",
    ];

    private static readonly Regex TopLevelType = new(
        @"^\s*internal\s+sealed\s+class\s+(\w+)",
        RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.CultureInvariant);

    [Fact]
    public void Configuration_exact_nine_files_one_type_each_exact_namespace()
    {
        var dir = Dir("src/backend/Host/Tooba.Host/Configuration");
        Assert.True(Directory.Exists(dir));
        var files = Directory.GetFiles(dir, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(ExpectedFiles, files);

        var foundTypes = new List<string>();
        foreach (var file in ExpectedFiles)
        {
            var text = File.ReadAllText(Path.Combine(dir, file!));
            Assert.Contains("namespace Tooba.Host.Configuration;", text, StringComparison.Ordinal);
            Assert.DoesNotContain("namespace Tooba.Host;", text, StringComparison.Ordinal);
            var matches = TopLevelType.Matches(text);
            Assert.Equal(1, matches.Count);
            foundTypes.Add(matches[0].Groups[1].Value);
            Assert.Equal(Path.GetFileNameWithoutExtension(file), matches[0].Groups[1].Value);
        }

        Assert.Equal(ExpectedTypes, foundTypes.OrderBy(x => x, StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public void Configuration_program_binding_registry_and_trusted_proxy_fail_fast()
    {
        var program = Read("src/backend/Host/Tooba.Host/Program.cs");
        Assert.Contains("using Tooba.Host.Configuration;", program, StringComparison.Ordinal);
        Assert.Contains("AddOptions<ToobaPlatformOptions>()", program, StringComparison.Ordinal);
        Assert.Contains("ValidateOnStart()", program, StringComparison.Ordinal);
        Assert.Contains("IValidateOptions<ToobaPlatformOptions>, PlatformOptionsValidator", program, StringComparison.Ordinal);
        Assert.Contains("PlatformOptionsValidator.BuildRegistry", program, StringComparison.Ordinal);
        Assert.Contains("IPAddress.Parse(proxy)", program, StringComparison.Ordinal);
        Assert.DoesNotContain("IPAddress.TryParse(proxy", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Configuration_legacy_connection_string_removed_and_fail_fast_hooks_present()
    {
        var postgres = Read("src/backend/Host/Tooba.Host/Configuration/PostgreSqlOptions.cs");
        Assert.Contains("ConnectionReferences", postgres, StringComparison.Ordinal);
        Assert.DoesNotContain("ConnectionString", postgres, StringComparison.Ordinal);

        var validator = Read("src/backend/Host/Tooba.Host/Configuration/PlatformOptionsValidator.cs");
        Assert.Contains("ValidateTrustedProxies", validator, StringComparison.Ordinal);
        Assert.Contains("invalid PrimaryDomain", validator, StringComparison.Ordinal);
        Assert.Contains("IPAddress.TryParse", validator, StringComparison.Ordinal);
    }

    [Fact]
    public void Configuration_contracts_only_and_zero_foreign_layers()
    {
        var dir = Dir("src/backend/Host/Tooba.Host/Configuration");
        foreach (var path in Directory.EnumerateFiles(dir, "*.cs"))
        {
            var text = File.ReadAllText(path);
            Assert.DoesNotContain("Tooba.StoreContext.Application", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.StoreContext.Infrastructure", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.StoreContext.Domain", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Offer.Application", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Offer.Infrastructure", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Offer.Domain", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Microsoft.EntityFrameworkCore", text, StringComparison.Ordinal);
            Assert.DoesNotContain("DbContext", text, StringComparison.Ordinal);
            if (text.Contains("StoreCommerceContext", StringComparison.Ordinal))
                Assert.Contains("Tooba.StoreContext.Contracts.Current", text, StringComparison.Ordinal);
            if (text.Contains("Enum.TryParse<SalesChannel>", StringComparison.Ordinal)
                || text.Contains("Tooba.Offer.Contracts", StringComparison.Ordinal))
                Assert.Contains("Tooba.Offer.Contracts.Dtos", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Configuration_protected_certifications_preserved_in_sot()
    {
        var state = Read("docs/architecture/tmar-current-state.json");
        Assert.Contains("HOST_PERSISTENCE_AMC_CERTIFIED", state, StringComparison.Ordinal);
        Assert.Contains("HOST_OUTBOX_AMC_CERTIFIED", state, StringComparison.Ordinal);
        Assert.Contains("HOST_OBSERVABILITY_AMC_CERTIFIED", state, StringComparison.Ordinal);
        Assert.Contains("HOST_MESSAGING_AMC_CERTIFIED", state, StringComparison.Ordinal);
        Assert.Contains("HOST_HEALTH_AMC_CERTIFIED", state, StringComparison.Ordinal);
        Assert.Contains("HOST_MULTITENANCY_AMC_CERTIFIED", state, StringComparison.Ordinal);
        Assert.Contains("HOST_ERRORS_AMC_CERTIFIED", state, StringComparison.Ordinal);
        Assert.Contains("HOST_SECURITY_AMC_CERTIFIED", state, StringComparison.Ordinal);
        Assert.Contains("HOST_ADMIN_FULLY_CERTIFIED", state, StringComparison.Ordinal);
    }

    private static string Dir(string relative) => Path.Combine(Repo(), relative.Replace('/', Path.DirectorySeparatorChar));

    private static string Read(string relative) => File.ReadAllText(Repo(relative));

    private static string Repo(string? relative = null)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
            {
                return relative is null
                    ? directory.FullName
                    : Path.Combine(directory.FullName, relative.Replace('/', Path.DirectorySeparatorChar));
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
