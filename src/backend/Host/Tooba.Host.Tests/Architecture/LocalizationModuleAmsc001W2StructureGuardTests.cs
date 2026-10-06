using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-LOCALIZATION-AMSC-001-W2 — durable capability-first structure lock for Localization:
/// shallow capability trees, no single-file request leaves, no root dump, exact path↔namespace,
/// manifest root allowlists and canonical /Modules/Localization/ solution grouping.
/// </summary>
public sealed class LocalizationModuleAmsc001W2StructureGuardTests
{
    private const string ModuleRoot = "src/backend/Modules/Localization";

    private static readonly string[] Projects =
    [
        "Tooba.Localization.Application",
        "Tooba.Localization.Contracts",
        "Tooba.Localization.Domain",
        "Tooba.Localization.Endpoints",
        "Tooba.Localization.Infrastructure",
    ];

    /// <summary>Capability axis first — technical request folders must not be a top-level Application axis.</summary>
    [Fact]
    public void Localization_Application_is_capability_first_not_technical_axis_first()
    {
        var app = Path.Combine(Repo(), ModuleRoot, "Tooba.Localization.Application");
        var topLevel = Directory.GetDirectories(app)
            .Select(Path.GetFileName!)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        // Request trees must never be a top-level Application axis; capability is the first axis.
        // Shared cross-capability buckets (Composition/Models/Ports) are allowed, matching the
        // certified Inventory/CustomerProfile/AddressBook/BulkInquiry/AccessControl precedent.
        foreach (var forbidden in new[] { "Commands", "Queries", "Validators" })
        {
            Assert.DoesNotContain(forbidden, topLevel);
        }

        Assert.Contains("Languages", topLevel);
        Assert.True(Directory.Exists(Path.Combine(app, "Languages", "Commands")));
        Assert.True(Directory.Exists(Path.Combine(app, "Languages", "Queries")));
        Assert.True(Directory.Exists(Path.Combine(app, "Languages", "Validators")));
    }

    /// <summary>A folder wrapping exactly one production request source file is over-foldering.</summary>
    [Fact]
    public void Localization_Application_has_no_single_file_request_leaf_folder()
    {
        var app = Path.Combine(Repo(), ModuleRoot, "Tooba.Localization.Application");
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

    /// <summary>No capability/implementation source may sit at a project root outside the allowlist.</summary>
    [Fact]
    public void Localization_root_allowlists_match_disk_and_forbidden_roots_absent()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            Repo(), "docs/architecture/tmar-module-structure-manifests.json")));
        var entry = doc.RootElement.GetProperty("modules").EnumerateArray()
            .Single(m => string.Equals(m.GetProperty("module").GetString(), "Localization", StringComparison.Ordinal));

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
                Assert.False(File.Exists(Path.Combine(projectPath, forbidden.GetString()!)),
                    $"forbidden root file present: {projectName}/{forbidden.GetString()}");
            }

            foreach (var forbidden in project.GetProperty("forbiddenTopLevelFolders").EnumerateArray())
            {
                Assert.False(Directory.Exists(Path.Combine(projectPath, forbidden.GetString()!)),
                    $"forbidden top-level folder present: {projectName}/{forbidden.GetString()}");
            }
        }
    }

    /// <summary>Contracts owns boundary semantics only — no Application CQRS/model dump.</summary>
    [Fact]
    public void Localization_Contracts_holds_boundary_semantics_only()
    {
        var contracts = Path.Combine(Repo(), ModuleRoot, "Tooba.Localization.Contracts");
        var joined = string.Join("\n", Directory.GetFiles(contracts, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(File.ReadAllText));

        Assert.DoesNotContain("IRequest<", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("IRequestHandler", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("DbContext", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Localization.Domain", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Localization.Application", joined, StringComparison.Ordinal);
    }

    /// <summary>Endpoints must not depend on Domain or Infrastructure directly.</summary>
    [Fact]
    public void Localization_endpoints_do_not_reference_domain_or_infrastructure()
    {
        var endpoints = Path.Combine(Repo(), ModuleRoot, "Tooba.Localization.Endpoints");
        var csproj = File.ReadAllText(Path.Combine(endpoints, "Tooba.Localization.Endpoints.csproj"));
        Assert.DoesNotContain("Tooba.Localization.Domain", csproj, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Localization.Infrastructure", csproj, StringComparison.Ordinal);

        var joined = string.Join("\n", Directory.GetFiles(endpoints, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(File.ReadAllText));
        Assert.DoesNotContain("Tooba.Localization.Domain", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Localization.Infrastructure", joined, StringComparison.Ordinal);
    }

    [Fact]
    public void Localization_production_path_equals_namespace_exactly()
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
                // Domain/Enums files are UTF-8 BOM prefixed; strip before matching the namespace declaration.
                var text = File.ReadAllText(file).TrimStart('\uFEFF');
                var match = Regex.Match(text, @"^namespace\s+([A-Za-z0-9_.]+)", RegexOptions.Multiline);
                if (!match.Success || !string.Equals(expected, match.Groups[1].Value, StringComparison.Ordinal))
                {
                    violations.Add($"{relative}: expected {expected}, got {(match.Success ? match.Groups[1].Value : "<none>")}");
                }
            }
        }

        Assert.True(violations.Count == 0, string.Join("\n", violations));
    }

    [Fact]
    public void Localization_projects_grouped_under_Modules_Localization_solution_folder()
    {
        var slnx = Path.Combine(Repo(), "src", "backend", "Tooba.slnx");
        var doc = XDocument.Load(slnx);
        var folder = doc.Root!.Elements("Folder").SingleOrDefault(f =>
            string.Equals((string?)f.Attribute("Name"), "/Modules/Localization/", StringComparison.Ordinal));
        Assert.True(folder is not null, "missing /Modules/Localization/ solution folder");

        var nested = folder!.Elements("Project")
            .Select(p => Path.GetFileNameWithoutExtension((string)p.Attribute("Path")!))
            .OrderBy(x => x, StringComparer.Ordinal).ToArray();
        Assert.Equal(Projects.OrderBy(x => x, StringComparer.Ordinal).ToArray(), nested);
    }

    /// <summary>Migrations must live under Persistence/Migrations — never at project root.</summary>
    [Fact]
    public void Localization_migrations_live_under_persistence()
    {
        var infra = Path.Combine(Repo(), ModuleRoot, "Tooba.Localization.Infrastructure");
        Assert.True(Directory.Exists(Path.Combine(infra, "Persistence", "Migrations")));
        Assert.False(Directory.Exists(Path.Combine(infra, "Migrations")));
        Assert.NotEmpty(Directory.GetFiles(Path.Combine(infra, "Persistence", "Migrations"), "*.cs"));

        var rootFiles = Directory.GetFiles(infra, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName).OrderBy(x => x, StringComparer.Ordinal).ToArray();
        Assert.Equal(new[] { "LocalizationModule.cs" }, rootFiles);
    }

    /// <summary>No alias workaround or TypeForwardedTo shim hides folder debt.</summary>
    [Fact]
    public void Localization_has_no_alias_or_typeforwarded_workaround()
    {
        foreach (var project in Projects)
        {
            var projectPath = Path.Combine(Repo(), ModuleRoot, project);
            foreach (var file in Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                {
                    continue;
                }

                var text = File.ReadAllText(file);
                Assert.DoesNotContain("TypeForwardedTo", text, StringComparison.Ordinal);
                Assert.DoesNotContain("global using Tooba.Localization", text, StringComparison.Ordinal);
            }
        }
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
