using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-MEDIA-AMSC-001-W2 — durable capability-first structure lock for Media: shallow capability
/// trees, no single-file request leaves, no root dump, exact path↔namespace, manifest root allowlists
/// and canonical /Modules/Media/ solution grouping.
/// </summary>
public sealed class MediaModuleAmsc001W2StructureGuardTests
{
    private const string ModuleRoot = "src/backend/Modules/Media";

    private static readonly string[] Projects =
    [
        "Tooba.Media.Application",
        "Tooba.Media.Contracts",
        "Tooba.Media.Domain",
        "Tooba.Media.Endpoints",
        "Tooba.Media.Infrastructure",
    ];

    /// <summary>Capability axis first — technical request folders must not be a top-level Application axis.</summary>
    [Fact]
    public void Media_Application_is_capability_first_not_technical_axis_first()
    {
        var app = Path.Combine(Repo(), ModuleRoot, "Tooba.Media.Application");
        var topLevel = Directory.GetDirectories(app)
            .Select(Path.GetFileName!)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        // Request trees must never be a top-level Application axis; capability is the first axis.
        // Shared cross-capability buckets (Composition/Models/Ports) are allowed, matching the
        // certified Localization/Inventory/CustomerProfile/AddressBook/BulkInquiry precedent.
        foreach (var forbidden in new[] { "Commands", "Queries", "Validators" })
        {
            Assert.DoesNotContain(forbidden, topLevel);
        }

        Assert.Contains("Assets", topLevel);
        Assert.True(Directory.Exists(Path.Combine(app, "Assets", "Commands")));
        Assert.True(Directory.Exists(Path.Combine(app, "Assets", "Queries")));
        Assert.True(Directory.Exists(Path.Combine(app, "Assets", "Validators")));
    }

    /// <summary>A folder wrapping exactly one production request source file is over-foldering.</summary>
    [Fact]
    public void Media_Application_has_no_single_file_request_leaf_folder()
    {
        var app = Path.Combine(Repo(), ModuleRoot, "Tooba.Media.Application");
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
    public void Media_root_allowlists_match_disk_and_forbidden_roots_absent()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            Repo(), "docs/architecture/tmar-module-structure-manifests.json")));
        var entry = doc.RootElement.GetProperty("modules").EnumerateArray()
            .Single(m => string.Equals(m.GetProperty("module").GetString(), "Media", StringComparison.Ordinal));

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
    public void Media_Contracts_holds_boundary_semantics_only()
    {
        var contracts = Path.Combine(Repo(), ModuleRoot, "Tooba.Media.Contracts");
        var joined = string.Join("\n", Directory.GetFiles(contracts, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(File.ReadAllText));

        Assert.DoesNotContain("IRequest<", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("IRequestHandler", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("DbContext", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Media.Domain", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Media.Application", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Media.Infrastructure", joined, StringComparison.Ordinal);
    }

    /// <summary>Endpoints must not depend on Domain or Infrastructure directly.</summary>
    [Fact]
    public void Media_endpoints_do_not_reference_domain_or_infrastructure()
    {
        var endpoints = Path.Combine(Repo(), ModuleRoot, "Tooba.Media.Endpoints");
        var csproj = File.ReadAllText(Path.Combine(endpoints, "Tooba.Media.Endpoints.csproj"));
        Assert.DoesNotContain("Tooba.Media.Domain", csproj, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Media.Infrastructure", csproj, StringComparison.Ordinal);

        var joined = string.Join("\n", Directory.GetFiles(endpoints, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(File.ReadAllText));
        Assert.DoesNotContain("Tooba.Media.Domain", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Media.Infrastructure", joined, StringComparison.Ordinal);
    }

    [Fact]
    public void Media_production_path_equals_namespace_exactly()
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
                // Some Media sources are UTF-8 BOM prefixed; strip before matching the namespace declaration.
                var text = File.ReadAllText(file).TrimStart('\uFEFF');
                var match = Regex.Match(text, @"^\s*namespace\s+([A-Za-z0-9_.]+)\s*[;{]", RegexOptions.Multiline);
                if (!match.Success || !string.Equals(expected, match.Groups[1].Value, StringComparison.Ordinal))
                {
                    violations.Add($"{relative}: expected {expected}, got {(match.Success ? match.Groups[1].Value : "<none>")}");
                }
            }
        }

        Assert.True(violations.Count == 0, string.Join("\n", violations));
    }

    [Fact]
    public void Media_projects_grouped_under_Modules_Media_solution_folder()
    {
        var slnx = Path.Combine(Repo(), "src", "backend", "Tooba.slnx");
        var doc = XDocument.Load(slnx);
        var folder = doc.Root!.Elements("Folder").SingleOrDefault(f =>
            string.Equals((string?)f.Attribute("Name"), "/Modules/Media/", StringComparison.Ordinal));
        Assert.True(folder is not null, "missing /Modules/Media/ solution folder");

        var nested = folder!.Elements("Project")
            .Select(p => Path.GetFileNameWithoutExtension((string)p.Attribute("Path")!))
            .OrderBy(x => x, StringComparer.Ordinal).ToArray();
        Assert.Equal(Projects.OrderBy(x => x, StringComparer.Ordinal).ToArray(), nested);

        // Every entry must resolve to a real on-disk project — no stale or decorative solution entry.
        var slnxDir = Path.GetDirectoryName(slnx)!;
        foreach (var project in folder.Elements("Project"))
        {
            var path = (string)project.Attribute("Path")!;
            Assert.True(File.Exists(Path.Combine(slnxDir, path)), $"stale solution entry: {path}");
        }
    }

    /// <summary>Migrations must live under Persistence/Migrations — never at project root.</summary>
    [Fact]
    public void Media_migrations_live_under_persistence()
    {
        var infra = Path.Combine(Repo(), ModuleRoot, "Tooba.Media.Infrastructure");
        Assert.True(Directory.Exists(Path.Combine(infra, "Persistence", "Migrations")));
        Assert.False(Directory.Exists(Path.Combine(infra, "Migrations")));
        Assert.NotEmpty(Directory.GetFiles(Path.Combine(infra, "Persistence", "Migrations"), "*.cs"));

        var rootFiles = Directory.GetFiles(infra, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName).OrderBy(x => x, StringComparer.Ordinal).ToArray();
        Assert.Equal(new[] { "MediaModule.cs" }, rootFiles);
    }

    /// <summary>Infrastructure keeps one authoritative home per responsibility — no stale duplicate copy.</summary>
    [Fact]
    public void Media_infrastructure_has_no_stale_or_duplicate_physical_copy()
    {
        var infra = Path.Combine(Repo(), ModuleRoot, "Tooba.Media.Infrastructure");

        // W1 cohesion split: one MediaDirectory seam plus its two partials, nothing else claiming the type.
        var assets = Directory.GetFiles(Path.Combine(infra, "Assets"), "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName).OrderBy(x => x, StringComparer.Ordinal).ToArray();
        Assert.Equal(
            new[]
            {
                "MediaAssetDemoBridge.cs",
                "MediaAssetUploadBridge.cs",
                "MediaDirectory.Queries.cs",
                "MediaDirectory.Upload.cs",
                "MediaDirectory.cs",
            },
            assets);

        // The bridges must not also own the directory implementation (dual live home).
        var bridges = string.Join("\n", assets
            .Where(x => x.Contains("Bridge", StringComparison.Ordinal))
            .Select(x => File.ReadAllText(Path.Combine(infra, "Assets", x))));
        Assert.DoesNotContain("class MediaDirectory", bridges, StringComparison.Ordinal);

        // MediaDirectory is declared once as the seam plus exactly its two cohesive partials.
        var directoryDeclarations = assets
            .Where(x => x.StartsWith("MediaDirectory", StringComparison.Ordinal))
            .SelectMany(x => File.ReadAllLines(Path.Combine(infra, "Assets", x)))
            .Count(line => line.Contains("class MediaDirectory", StringComparison.Ordinal));
        Assert.Equal(3, directoryDeclarations);

        // No second physical copy of the EF context or the object store lives outside its own home.
        var otherHomes = Directory.GetFiles(infra, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}Persistence{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !p.Contains($"{Path.DirectorySeparatorChar}Storage{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(p => File.ReadAllText(p).Contains("class MediaDbContext", StringComparison.Ordinal)
                        || File.ReadAllText(p).Contains("class LocalFileMediaStore", StringComparison.Ordinal))
            .ToArray();
        Assert.Empty(otherHomes);
    }

    /// <summary>No alias workaround or TypeForwardedTo shim hides folder debt.</summary>
    [Fact]
    public void Media_has_no_alias_or_typeforwarded_workaround()
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
                Assert.DoesNotContain("global using Tooba.Media", text, StringComparison.Ordinal);
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
