using System.Text.Json;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-ACCESSCONTROL-AMSC-001-W3 (Certify) — durable certification locks.
///
/// Locks the ARCH-COMPLETE-002 certification verdict for the AccessControl module: canonical
/// mechanisms (result/error mapping, localization, stable codes, observability, correlation),
/// module-owned HTTP endpoints with zero Host HTTP ownership, an unchanged schema, and an honest
/// SoT record. It reads real repository files, so a regression in any of these invariants fails here.
/// </summary>
public sealed class AccessControlModuleAmsc001W3CertGuardTests
{
    private const string ModuleRelative = "src/backend/Modules/AccessControl";

    [Fact]
    public void AccessControl_certification_state_is_recorded_honestly_in_sot()
    {
        using var doc = ReadJson("docs/architecture/tmar-current-state.json");
        var root = doc.RootElement;

        var record = root.GetProperty("accessControlModuleAmsc001W3");
        Assert.Equal("ACCESSCONTROL_AMSC_001_CERTIFIED", record.GetProperty("state").GetString());
        Assert.Equal("COMPLETE_REFERENCE_PATTERN", record.GetProperty("verdict").GetString());
        Assert.Equal("ARCH-COMPLETE-002", record.GetProperty("lockVersion").GetString());
        Assert.True(record.GetProperty("structureCertified").GetBoolean());
        Assert.True(record.GetProperty("microserviceExtractable").GetBoolean());
        Assert.Equal("ZERO", record.GetProperty("blockingResidualDebt").GetString());
        Assert.Equal("NONE", record.GetProperty("guardsWeakened").GetString());
        Assert.Equal("NONE", record.GetProperty("baselinesWidened").GetString());
        Assert.Equal("NONE", record.GetProperty("behaviorChange").GetString());
        Assert.Equal("NONE", record.GetProperty("schemaChange").GetString());

        // Host final closure must remain intact.
        Assert.Equal("HOST_ROOT_FINAL_CERTIFIED", root.GetProperty("currentHostCheckpoint").GetString());

        // The whole AMSC lineage stays recorded.
        foreach (var key in new[]
                 {
                     "accessControlModuleAmsc001W0", "accessControlModuleAmsc001W1",
                     "accessControlModuleAmsc001W2", "accessControlModuleAmsc001W3",
                 })
        {
            Assert.True(root.TryGetProperty(key, out _), $"missing SoT record {key}");
        }
    }

    [Fact]
    public void AccessControl_endpoints_use_only_the_canonical_result_factory()
    {
        var endpoints = Path.Combine(ModuleRoot(), "Tooba.AccessControl.Endpoints");

        foreach (var file in Directory.GetFiles(endpoints, "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            var relative = file[endpoints.Length..];

            Assert.DoesNotContain("Results.Json", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Results.BadRequest", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Results.Problem", text, StringComparison.Ordinal);
            Assert.DoesNotContain("new ProblemDetails", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Accept-Language", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.AccessControl.Domain", text, StringComparison.Ordinal);

            // Expected failures must never be classified by parsing exception prose.
            Assert.DoesNotContain(".Message.StartsWith", text, StringComparison.Ordinal);
            Assert.DoesNotContain(".Message.Contains", text, StringComparison.Ordinal);

            if (relative.EndsWith("Endpoints.cs", StringComparison.Ordinal))
            {
                Assert.Contains("api.From(", text, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void AccessControl_owns_exactly_one_error_descriptor_per_owned_code()
    {
        var contracts = File.ReadAllText(Path.Combine(
            ModuleRoot(), "Tooba.AccessControl.Contracts", "Errors", "AccessControlErrorCodes.cs"));
        var constants = System.Text.RegularExpressions.Regex
            .Matches(contracts, @"public const string (?<name>\w+) = ""(?<code>[^""]+)"";")
            .ToDictionary(m => m.Groups["name"].Value, m => m.Groups["code"].Value, StringComparer.Ordinal);

        Assert.Equal(20, constants.Count);
        Assert.Equal(constants.Count, constants.Values.Distinct(StringComparer.Ordinal).Count());

        // Every owned code is registered by the module's single contributor through its constant
        // (never a duplicated raw literal), so descriptor ownership stays unique.
        var contributor = File.ReadAllText(Path.Combine(
            ModuleRoot(), "Tooba.AccessControl.Endpoints", "Errors", "AccessControlErrorCatalogContributor.cs"));
        foreach (var name in constants.Keys)
        {
            Assert.Contains($"AccessControlErrorCodes.{name}", contributor, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("Contains(", contributor, StringComparison.Ordinal);
        Assert.DoesNotContain("StartsWith(", contributor, StringComparison.Ordinal);

        // No other contributor may register an owned AccessControl code.
        var hostTestsRoot = Path.Combine(RepoRoot(), "src", "backend");
        foreach (var file in Directory.GetFiles(hostTestsRoot, "*ErrorCatalogContributor.cs", SearchOption.AllDirectories))
        {
            if (file.EndsWith("AccessControlErrorCatalogContributor.cs", StringComparison.Ordinal)
                || file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            var text = File.ReadAllText(file);
            foreach (var code in constants.Values)
            {
                Assert.DoesNotContain($"\"{code}\"", text, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void AccessControl_has_no_raw_error_code_literals_or_ad_hoc_logging()
    {
        var root = ModuleRoot();
        var offenders = new List<string>();

        foreach (var file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            var relative = file[root.Length..];
            if (relative.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || relative.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || relative.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || relative.Contains("ErrorCodes.cs", StringComparison.Ordinal)
                || relative.Contains("ErrorCatalogContributor.cs", StringComparison.Ordinal)
                || relative.Contains("Errors.resx", StringComparison.Ordinal))
            {
                continue;
            }

            var text = File.ReadAllText(file);
            if (System.Text.RegularExpressions.Regex.IsMatch(text, @"""access\.[a-z_]+""")
                || System.Text.RegularExpressions.Regex.IsMatch(text, @"""seller\.dev\.[a-z_-]+""")
                || text.Contains("Console.Write", StringComparison.Ordinal)
                || text.Contains("Debug.Write", StringComparison.Ordinal)
                || text.Contains("ActivitySource.StartActivity", StringComparison.Ordinal)
                || text.Contains("traceparent", StringComparison.Ordinal))
            {
                offenders.Add(relative);
            }
        }

        Assert.Empty(offenders);
    }

    [Fact]
    public void AccessControl_has_no_foreign_module_project_or_namespace_dependency()
    {
        var root = ModuleRoot();
        var forbidden = new[]
        {
            "Tooba.Identity.Application", "Tooba.Identity.Domain", "Tooba.Identity.Infrastructure",
            "Tooba.OperatorProfile.Application", "Tooba.OperatorProfile.Domain", "Tooba.OperatorProfile.Infrastructure",
            "Tooba.Catalog.Application", "Tooba.Catalog.Domain", "Tooba.Catalog.Infrastructure",
            "Tooba.Party.Application", "Tooba.Party.Domain", "Tooba.Party.Infrastructure",
            "Tooba.Order.Application", "Tooba.Order.Domain", "Tooba.Order.Infrastructure",
        };

        foreach (var csproj in Directory.GetFiles(root, "*.csproj", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(csproj);
            foreach (var needle in forbidden)
            {
                Assert.DoesNotContain($"{needle}.csproj", text, StringComparison.Ordinal);
            }
        }

        foreach (var file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            var relative = file[root.Length..];
            if (relative.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || relative.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            var text = File.ReadAllText(file);
            foreach (var needle in forbidden)
            {
                Assert.DoesNotContain($"using {needle}", text, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void AccessControl_owns_its_http_surface_with_zero_host_http_ownership()
    {
        var endpointsRoot = Path.Combine(ModuleRoot(), "Tooba.AccessControl.Endpoints");

        Assert.True(File.Exists(Path.Combine(endpointsRoot, "AccessControlEndpointModule.cs")));
        Assert.False(File.Exists(Path.Combine(endpointsRoot, "AccessControlAdminEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(endpointsRoot, "AccessControlSellerEndpoints.cs")));
        Assert.True(Directory.Exists(Path.Combine(endpointsRoot, "Admin")));
        Assert.True(Directory.Exists(Path.Combine(endpointsRoot, "Seller")));

        // The Host HTTP surface for AccessControl must never be resurrected.
        Assert.False(Directory.Exists(Path.Combine(RepoRoot(), "src", "backend", "Host", "Tooba.Host", "AccessControl")));
    }

    [Fact]
    public void AccessControl_schema_and_migrations_are_unchanged()
    {
        var migrations = Path.Combine(ModuleRoot(), "Tooba.AccessControl.Infrastructure", "Persistence", "Migrations");
        var files = Directory.GetFiles(migrations, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            [
                "20260827140753_InitialAccessControl.Designer.cs",
                "20260827140753_InitialAccessControl.cs",
                "20260827181000_AddSellerCeilingScope.cs",
                "AccessControlDbContextModelSnapshot.cs",
            ],
            files);
    }

    private static string ModuleRoot() =>
        Path.Combine(RepoRoot(), ModuleRelative.Replace('/', Path.DirectorySeparatorChar));

    private static JsonDocument ReadJson(string relativePath) =>
        JsonDocument.Parse(File.ReadAllText(Path.Combine(
            RepoRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar))));

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "src", "backend", "Tooba.slnx")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
