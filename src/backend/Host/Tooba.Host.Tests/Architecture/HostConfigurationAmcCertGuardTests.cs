using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-CONFIGURATION-AMC-001-W2-CERT — durable certification of Host/Configuration platform boundary.
/// </summary>
public sealed class HostConfigurationAmcCertGuardTests
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

    private static readonly Regex TopLevelType = new(
        @"^\s*internal\s+sealed\s+class\s+(\w+)",
        RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.CultureInvariant);

    private static readonly Regex ForeignModuleLayer = new(
        @"Tooba\.(Catalog|Party|AccessControl|Identity|Order|Offer|Promotion|Returns|Settlement|Notification|Support|Story|Payment|Inventory|Cart|Reviews|StoreContext)\.(Application|Domain|Infrastructure)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private const string W1ImplementationSha = "32719977bc6408490fe5945d75dedaa5c2f7af4c";

    [Fact]
    public void Configuration_certified_exact_tree_binding_failfast_and_boundaries()
    {
        var dir = Dir("src/backend/Host/Tooba.Host/Configuration");
        Assert.True(Directory.Exists(dir));
        Assert.Empty(Directory.GetDirectories(dir));
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
            Assert.DoesNotContain("TypeForwardedTo", text, StringComparison.Ordinal);
            var matches = TopLevelType.Matches(text);
            Assert.Equal(1, matches.Count);
            foundTypes.Add(matches[0].Groups[1].Value);
            Assert.Equal(Path.GetFileNameWithoutExtension(file), matches[0].Groups[1].Value);

            Assert.DoesNotContain("IServiceProvider", text, StringComparison.Ordinal);
            Assert.DoesNotContain("IOptionsMonitor", text, StringComparison.Ordinal);
            Assert.DoesNotContain("DbContext", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Microsoft.EntityFrameworkCore", text, StringComparison.Ordinal);
            Assert.DoesNotContain("File.Read", text, StringComparison.Ordinal);
            Assert.DoesNotContain("HttpClient", text, StringComparison.Ordinal);
            Assert.DoesNotContain("NpgsqlConnection", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.StoreContext.Application", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.StoreContext.Infrastructure", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.StoreContext.Domain", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Offer.Application", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Offer.Infrastructure", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Offer.Domain", text, StringComparison.Ordinal);
            foreach (Match m in ForeignModuleLayer.Matches(text))
                Assert.Fail("foreign module layer: " + file + ": " + m.Value);
        }

        Assert.Equal(ExpectedFiles.Select(Path.GetFileNameWithoutExtension!).OrderBy(x => x, StringComparer.Ordinal),
            foundTypes.OrderBy(x => x, StringComparer.Ordinal));

        var postgres = Read("src/backend/Host/Tooba.Host/Configuration/PostgreSqlOptions.cs");
        Assert.Contains("ConnectionReferences", postgres, StringComparison.Ordinal);
        Assert.DoesNotContain("ConnectionString", postgres, StringComparison.Ordinal);

        var validator = Read("src/backend/Host/Tooba.Host/Configuration/PlatformOptionsValidator.cs");
        Assert.Contains("ValidateTrustedProxies", validator, StringComparison.Ordinal);
        Assert.Contains("invalid PrimaryDomain", validator, StringComparison.Ordinal);
        Assert.Contains("ValidateProductionRequirements", validator, StringComparison.Ordinal);
        Assert.Contains("ValidateStoreCommerce", validator, StringComparison.Ordinal);
        Assert.Contains("Enum.TryParse<SalesChannel>", validator, StringComparison.Ordinal);
        Assert.Contains("Tooba.Offer.Contracts.Dtos", validator, StringComparison.Ordinal);
        Assert.Contains("Tooba.StoreContext.Contracts.Current", validator, StringComparison.Ordinal);
        Assert.Contains("BuildRegistry", validator, StringComparison.Ordinal);
        Assert.DoesNotContain("GetRequiredService", validator, StringComparison.Ordinal);
        Assert.DoesNotContain("ILogger", validator, StringComparison.Ordinal);

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
    public void Sot_certifies_configuration_and_preserves_prior_host_certs()
    {
        var sot = Read("docs/architecture/tmar-current-state.json");
        Assert.Contains("HOST_CONFIGURATION_AMC_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("HOST_CONFIGURATION_PLATFORM_BOUNDARY_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("\"hostConfigurationAmc001W2Cert\"", sot, StringComparison.Ordinal);
        Assert.Contains("\"hostConfigurationAmc001W1\"", sot, StringComparison.Ordinal);
        Assert.Contains("\"hostConfigurationAmc001\"", sot, StringComparison.Ordinal);
        Assert.Contains("USER_REVIEW_HOST_CONFIGURATION_AMC_001_W2_CERT", sot, StringComparison.Ordinal);
        Assert.Contains(W1ImplementationSha, sot, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-HOST-CONFIGURATION-AMC-001-W1", sot, StringComparison.Ordinal);
        Assert.Contains("HostConfigurationAmcCertGuardTests", sot, StringComparison.Ordinal);
        Assert.Contains("HostConfigurationAmcW1GuardTests", sot, StringComparison.Ordinal);
        Assert.Contains("HOST_PERSISTENCE_AMC_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("HOST_OUTBOX_AMC_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("HOST_OBSERVABILITY_AMC_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("HOST_MESSAGING_AMC_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("HOST_HEALTH_AMC_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("HOST_MULTITENANCY_AMC_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("HOST_ERRORS_AMC_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("HOST_SECURITY_AMC_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("HOST_ADMIN_FULLY_CERTIFIED", sot, StringComparison.Ordinal);
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
