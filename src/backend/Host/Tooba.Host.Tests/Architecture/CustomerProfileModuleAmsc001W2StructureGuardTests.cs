using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-CUSTOMERPROFILE-AMSC-001-W2 — durable capability-first structure lock for CustomerProfile:
/// shallow capability trees, no single-file request leaves, exact path↔namespace, root allowlists,
/// canonical Contracts semantics and solution grouping.
/// </summary>
public sealed class CustomerProfileModuleAmsc001W2StructureGuardTests
{
    private const string ModuleRoot = "src/backend/Modules/CustomerProfile";
    private static readonly string[] Projects =
    [
        "Tooba.CustomerProfile.Application",
        "Tooba.CustomerProfile.Contracts",
        "Tooba.CustomerProfile.Domain",
        "Tooba.CustomerProfile.Endpoints",
        "Tooba.CustomerProfile.Infrastructure",
    ];

    /// <summary>Capability axis first — technical request folders must not be a top-level Application axis.</summary>
    [Fact]
    public void CustomerProfile_Application_is_capability_first_not_technical_axis_first()
    {
        var app = Path.Combine(Repo(), ModuleRoot, "Tooba.CustomerProfile.Application");
        var topLevel = Directory.GetDirectories(app)
            .Select(Path.GetFileName!)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        foreach (var forbidden in new[] { "Commands", "Queries", "Models", "Validators" })
        {
            Assert.DoesNotContain(forbidden, topLevel);
        }

        foreach (var capability in new[] { "Account", "Profile" })
        {
            Assert.Contains(capability, topLevel);
        }

        // Shared, genuinely cross-capability concerns stay at the Application root.
        foreach (var shared in new[] { "Composition", "Ports" })
        {
            Assert.Contains(shared, topLevel);
        }
    }

    /// <summary>A folder wrapping exactly one production request source file is over-foldering.</summary>
    [Fact]
    public void CustomerProfile_Application_has_no_single_file_request_leaf_folder()
    {
        var app = Path.Combine(Repo(), ModuleRoot, "Tooba.CustomerProfile.Application");
        var violations = new List<string>();

        foreach (var dir in Directory.GetDirectories(app, "*", SearchOption.AllDirectories))
        {
            var name = Path.GetFileName(dir);
            var sources = Directory.GetFiles(dir, "*.cs", SearchOption.TopDirectoryOnly)
                .Where(p => !Path.GetFileName(p).StartsWith("GlobalUsings", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            var isUseCaseFolder = name.EndsWith("Command", StringComparison.Ordinal)
                || name.EndsWith("Query", StringComparison.Ordinal);
            if (isUseCaseFolder && sources.Length == 1)
            {
                violations.Add(dir[(app.Length + 1)..]);
            }
        }

        Assert.True(violations.Count == 0, "single-file request leaf folders: " + string.Join(", ", violations));
    }

    /// <summary>The stale no-op Application placeholder must never return.</summary>
    [Fact]
    public void CustomerProfile_stale_application_placeholder_is_absent()
    {
        Assert.False(File.Exists(Path.Combine(
            Repo(), ModuleRoot, "Tooba.CustomerProfile.Application", "CustomerProfileContracts.cs")));
        Assert.False(File.Exists(Path.Combine(
            Repo(), ModuleRoot, "Tooba.CustomerProfile.Contracts", "CustomerProfileContracts.cs")));
    }

    /// <summary>Contracts owns boundary semantics only — no Application CQRS/model dump.</summary>
    [Fact]
    public void CustomerProfile_Contracts_holds_boundary_semantics_only()
    {
        var contracts = Path.Combine(Repo(), ModuleRoot, "Tooba.CustomerProfile.Contracts");
        var joined = string.Join("\n", Directory.GetFiles(contracts, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(File.ReadAllText));

        Assert.DoesNotContain("IRequest<", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("IRequestHandler", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("DbContext", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.CustomerProfile.Domain", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.CustomerProfile.Application", joined, StringComparison.Ordinal);

        // The public boundary port consumed by Identity must stay in Contracts, not Application.
        Assert.True(File.Exists(Path.Combine(contracts, "Ports", "ICustomerProfileDirectory.cs")));
        Assert.True(File.Exists(Path.Combine(contracts, "Dtos", "CustomerProfileSnapshot.cs")));
        Assert.True(File.Exists(Path.Combine(contracts, "Dtos", "CustomerProfileWrite.cs")));
    }

    /// <summary>Exactly one canonical stable-code owner — no Domain-local code class.</summary>
    [Fact]
    public void CustomerProfile_has_single_stable_error_code_owner()
    {
        var domain = Path.Combine(Repo(), ModuleRoot, "Tooba.CustomerProfile.Domain");
        var joined = string.Join("\n", Directory.GetFiles(domain, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(File.ReadAllText));

        // Domain consumes the canonical Contracts-owned codes (Content/AddressBook pattern) but must not
        // declare any code constant of its own — the single stable-code owner stays in Contracts/Errors.
        Assert.DoesNotContain("public const string", joined, StringComparison.Ordinal);

        Assert.True(File.Exists(Path.Combine(
            Repo(), ModuleRoot, "Tooba.CustomerProfile.Contracts", "Errors", "CustomerProfileErrorCodes.cs")));
    }

    /// <summary>Endpoints must not depend on Domain directly.</summary>
    [Fact]
    public void CustomerProfile_endpoints_do_not_reference_domain()
    {
        var endpoints = Path.Combine(Repo(), ModuleRoot, "Tooba.CustomerProfile.Endpoints");
        var csproj = File.ReadAllText(Path.Combine(endpoints, "Tooba.CustomerProfile.Endpoints.csproj"));
        Assert.DoesNotContain("Tooba.CustomerProfile.Domain", csproj, StringComparison.Ordinal);

        var joined = string.Join("\n", Directory.GetFiles(endpoints, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(File.ReadAllText));
        Assert.DoesNotContain("Tooba.CustomerProfile.Domain", joined, StringComparison.Ordinal);
    }

    /// <summary>Infrastructure uses the canonical capability folders; the root has no loose module files.</summary>
    [Fact]
    public void CustomerProfile_Infrastructure_uses_canonical_capability_folders()
    {
        var infra = Path.Combine(Repo(), ModuleRoot, "Tooba.CustomerProfile.Infrastructure");
        Assert.True(File.Exists(Path.Combine(infra, "DependencyInjection", "CustomerProfileModule.cs")));
        Assert.True(File.Exists(Path.Combine(infra, "Directories", "CustomerProfileDirectory.cs")));
        Assert.True(File.Exists(Path.Combine(infra, "Messaging", "CustomerProfileOutboxRegistration.cs")));
        Assert.True(File.Exists(Path.Combine(infra, "Persistence", "Migrations", "CustomerProfileDbContextModelSnapshot.cs")));
        Assert.False(Directory.Exists(Path.Combine(infra, "Migrations")));
        Assert.Empty(Directory.GetFiles(infra, "*.cs", SearchOption.TopDirectoryOnly));
    }

    [Fact]
    public void CustomerProfile_root_allowlists_match_disk_and_forbidden_roots_absent()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            Repo(), "docs/architecture/tmar-module-structure-manifests.json")));
        // CustomerProfile was promoted by TB-TMAR-CUSTOMERPROFILE-AMSC-001-W3 from preCertModules to the
        // certified modules array (structureCertified true). The structural allowlists below are unchanged.
        var entry = doc.RootElement.GetProperty("modules").EnumerateArray()
            .Single(m => string.Equals(m.GetProperty("module").GetString(), "CustomerProfile", StringComparison.Ordinal));
        Assert.True(entry.GetProperty("structureCertified").GetBoolean());
        Assert.DoesNotContain(
            doc.RootElement.GetProperty("preCertModules").EnumerateArray(),
            m => string.Equals(m.GetProperty("module").GetString(), "CustomerProfile", StringComparison.Ordinal));

        foreach (var project in entry.GetProperty("projects").EnumerateArray())
        {
            var projectName = project.GetProperty("projectName").GetString()!;
            var projectPath = Path.Combine(Repo(), ModuleRoot, projectName);
            Assert.True(Directory.Exists(projectPath), projectPath);

            var allowlist = project.GetProperty("rootAllowlist").EnumerateArray()
                .Select(x => x.GetString()!).OrderBy(x => x, StringComparer.Ordinal).ToArray();
            var actual = Directory.GetFiles(projectPath, "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName!).OrderBy(x => x, StringComparer.Ordinal).ToArray();
            Assert.Equal(allowlist, actual);

            foreach (var forbidden in project.GetProperty("forbiddenRootFiles").EnumerateArray())
            {
                Assert.False(File.Exists(Path.Combine(projectPath, forbidden.GetString()!)));
            }
        }
    }

    [Fact]
    public void CustomerProfile_production_path_equals_namespace_exactly()
    {
        var violations = new List<string>();
        foreach (var project in Projects)
        {
            var projectPath = Path.Combine(Repo(), ModuleRoot, project);
            var rootFull = Path.GetFullPath(projectPath);
            foreach (var file in Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories))
            {
                var relative = file[rootFull.Length..].TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (relative.StartsWith($"obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    || relative.StartsWith($"bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    || Path.GetFileName(relative).StartsWith("GlobalUsings", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var dir = Path.GetDirectoryName(relative);
                var expected = string.IsNullOrEmpty(dir)
                    ? project
                    : project + "." + dir.Replace(Path.DirectorySeparatorChar, '.').Replace(Path.AltDirectorySeparatorChar, '.');
                var match = Regex.Match(File.ReadAllText(file), @"^namespace\s+([A-Za-z0-9_.]+)", RegexOptions.Multiline);
                if (!match.Success || !string.Equals(expected, match.Groups[1].Value, StringComparison.Ordinal))
                {
                    violations.Add($"{relative}: expected {expected}, got {(match.Success ? match.Groups[1].Value : "<none>")}");
                }
            }
        }

        Assert.True(violations.Count == 0, string.Join("\n", violations));
    }

    [Fact]
    public void CustomerProfile_projects_grouped_under_Modules_CustomerProfile_solution_folder()
    {
        var slnx = Path.Combine(Repo(), "src", "backend", "Tooba.slnx");
        var doc = XDocument.Load(slnx);
        var folder = doc.Root!.Elements("Folder").SingleOrDefault(f =>
            string.Equals((string?)f.Attribute("Name"), "/Modules/CustomerProfile/", StringComparison.Ordinal));
        Assert.True(folder is not null, "missing /Modules/CustomerProfile/ solution folder");

        var nested = folder!.Elements("Project")
            .Select(p => Path.GetFileNameWithoutExtension((string)p.Attribute("Path")!))
            .OrderBy(x => x, StringComparer.Ordinal).ToArray();
        Assert.Equal(Projects.OrderBy(x => x, StringComparer.Ordinal).ToArray(), nested);
    }

    private static string Repo()
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
