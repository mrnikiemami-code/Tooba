using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-PERSISTENCE-AMC-001-W2-CERT — durable certification of Host/Persistence platform boundary.
/// </summary>
public sealed class HostPersistenceAmcCertGuardTests
{
    private static readonly Regex TopLevelType = new(
        @"^\s*(?:internal\s+|public\s+|file\s+)?(?:sealed\s+|static\s+|abstract\s+)*(?:partial\s+)?(?:class|record|struct|enum|interface)\s+(\w+)",
        RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.CultureInvariant);

    private static readonly Regex ForeignModuleLayer = new(
        @"Tooba\.(Catalog|Party|AccessControl|Identity|Order|Offer|Promotion|Returns|Settlement|Notification|Support|Story|Payment|Inventory|Cart|Reviews)\.(Application|Domain|Infrastructure)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact]
    public void Persistence_certified_exact_tree_resolver_boundaries_and_secrets()
    {
        var dir = Dir("src/backend/Host/Tooba.Host/Persistence");
        Assert.True(Directory.Exists(dir));
        Assert.Empty(Directory.GetDirectories(dir));
        var files = Directory.GetFiles(dir, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(new[] { "DatabaseConnectionResolver.cs" }, files);

        var text = File.ReadAllText(Path.Combine(dir, "DatabaseConnectionResolver.cs"));
        Assert.Contains("namespace Tooba.Host.Persistence;", text, StringComparison.Ordinal);
        Assert.DoesNotContain("namespace Tooba.Host;", text, StringComparison.Ordinal);
        Assert.DoesNotContain("TypeForwardedTo", text, StringComparison.Ordinal);

        var types = TopLevelType.Matches(text).Select(m => m.Groups[1].Value).ToArray();
        Assert.Equal(new[] { "DatabaseConnectionResolver" }, types);

        Assert.Contains("IDatabaseConnectionResolver", text, StringComparison.Ordinal);
        Assert.Contains("ConnectionReferences", text, StringComparison.Ordinal);
        Assert.DoesNotContain("PostgreSQL.ConnectionString", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Environment.GetEnvironmentVariable", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IConfiguration", text, StringComparison.Ordinal);
        Assert.Contains("NpgsqlConnectionStringBuilder", text, StringComparison.Ordinal);
        Assert.DoesNotContain("OpenAsync", text, StringComparison.Ordinal);
        Assert.DoesNotContain(".Open(", text, StringComparison.Ordinal);
        Assert.DoesNotContain("DbContext", text, StringComparison.Ordinal);
        Assert.DoesNotContain("new NpgsqlConnection(", text, StringComparison.Ordinal);
        Assert.DoesNotContain("NpgsqlDataSource", text, StringComparison.Ordinal);
        Assert.DoesNotContain("ExecuteSql", text, StringComparison.Ordinal);
        Assert.Contains("platform.connection.unconfigured", text, StringComparison.Ordinal);
        Assert.Contains("PlatformHttpException", text, StringComparison.Ordinal);
        Assert.Contains("Status503ServiceUnavailable", text, StringComparison.Ordinal);
        Assert.Contains("Service Unavailable", text, StringComparison.Ordinal);
        Assert.Contains("catch (ArgumentException)", text, StringComparison.Ordinal);
        Assert.DoesNotContain("catch (Exception", text, StringComparison.Ordinal);
        Assert.DoesNotContain("ILogger", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Console.Write", text, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Message.Contains", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Message.StartsWith", text, StringComparison.Ordinal);
        Assert.DoesNotContain("$\"", text, StringComparison.Ordinal);
        Assert.DoesNotContain("string.Format", text, StringComparison.Ordinal);

        foreach (Match m in ForeignModuleLayer.Matches(text))
            Assert.Fail("foreign module layer: " + m.Value);

        var program = Read("src/backend/Host/Tooba.Host/Program.cs");
        Assert.Contains("using Tooba.Host.Persistence;", program, StringComparison.Ordinal);
        Assert.Contains("AddSingleton<IDatabaseConnectionResolver, DatabaseConnectionResolver>()", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Sot_certifies_persistence_and_preserves_prior_host_certs()
    {
        var sot = Read("docs/architecture/tmar-current-state.json");
        Assert.Contains("HOST_PERSISTENCE_AMC_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("HOST_PERSISTENCE_PLATFORM_BOUNDARY_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("\"hostPersistenceAmc001W2Cert\"", sot, StringComparison.Ordinal);
        Assert.Contains("\"hostPersistenceAmc001\"", sot, StringComparison.Ordinal);
        Assert.Contains("USER_REVIEW_HOST_PERSISTENCE_AMC_001_W2_CERT", sot, StringComparison.Ordinal);
        Assert.Contains("GLOBAL_HOST_PERSISTENCE_PLATFORM_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("CANONICAL_HOST_PLATFORM_FAIL_CLOSED_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("CONFIG_SYNTAX_VALIDATION_ONLY_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("HostPersistenceAmcCertGuardTests", sot, StringComparison.Ordinal);
        Assert.Contains("HostPersistenceAmcGuardTests", sot, StringComparison.Ordinal);
        Assert.Contains("382ef10af3a5eb49f519e49cb399809b19844bbc", sot, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-HOST-OUTBOX-AMC-001-W1", sot, StringComparison.Ordinal);
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
