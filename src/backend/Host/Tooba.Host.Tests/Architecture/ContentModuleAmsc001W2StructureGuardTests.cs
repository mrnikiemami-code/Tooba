using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-CONTENT-AMSC-001-W2 — durable capability-first structure lock for Content:
/// shallow capability trees, no single-file request leaves, exact path↔namespace,
/// root allowlists, canonical Contracts semantics and solution grouping.
/// </summary>
public sealed class ContentModuleAmsc001W2StructureGuardTests
{
    private const string ModuleRoot = "src/backend/Modules/Content";
    private static readonly string[] Projects =
    [
        "Tooba.Content.Application",
        "Tooba.Content.Contracts",
        "Tooba.Content.Domain",
        "Tooba.Content.Endpoints",
        "Tooba.Content.Infrastructure",
    ];

    /// <summary>Capability axis first — technical request folders must not be a top-level Application axis.</summary>
    [Fact]
    public void Content_Application_is_capability_first_not_technical_axis_first()
    {
        var app = Path.Combine(Repo(), ModuleRoot, "Tooba.Content.Application");
        var topLevel = Directory.GetDirectories(app)
            .Select(Path.GetFileName!)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        foreach (var forbidden in new[] { "Commands", "Queries", "Models", "Ports" })
        {
            Assert.DoesNotContain(forbidden, topLevel);
        }

        // Real Content capabilities discovered from the module's own responsibility map.
        foreach (var capability in new[] { "Articles", "Authors", "Categories", "Comments", "Media", "Tags" })
        {
            Assert.Contains(capability, topLevel);
        }
    }

    /// <summary>A folder wrapping exactly one production request source file is over-foldering.</summary>
    [Fact]
    public void Content_Application_has_no_single_file_request_leaf_folder()
    {
        var app = Path.Combine(Repo(), ModuleRoot, "Tooba.Content.Application");
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

    /// <summary>Contracts owns boundary semantics only — no Application CQRS/model dump.</summary>
    [Fact]
    public void Content_Contracts_holds_boundary_semantics_only()
    {
        var contracts = Path.Combine(Repo(), ModuleRoot, "Tooba.Content.Contracts");
        var joined = string.Join("\n", Directory.GetFiles(contracts, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(File.ReadAllText));

        Assert.DoesNotContain("IRequest<", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("IRequestHandler", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("DbContext", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Content.Domain", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Content.Application", joined, StringComparison.Ordinal);
    }

    /// <summary>Exactly one canonical stable-code owner — no Domain-local code classes.</summary>
    [Fact]
    public void Content_has_single_stable_error_code_owner()
    {
        var domain = Path.Combine(Repo(), ModuleRoot, "Tooba.Content.Domain");
        var joined = string.Join("\n", Directory.GetFiles(domain, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(File.ReadAllText));

        foreach (var legacy in new[]
                 {
                     "ContentArticleErrorCodes",
                     "ContentAuthorErrorCodes",
                     "ContentCategoryErrorCodes",
                     "ContentTagErrorCodes",
                     "ArticleCommentCodes",
                 })
        {
            Assert.DoesNotContain(legacy, joined, StringComparison.Ordinal);
        }

        Assert.True(File.Exists(Path.Combine(
            Repo(), ModuleRoot, "Tooba.Content.Contracts", "Errors", "ContentErrorCodes.cs")));
    }

    /// <summary>Endpoints must not depend on Domain directly.</summary>
    [Fact]
    public void Content_endpoints_do_not_reference_domain()
    {
        var endpoints = Path.Combine(Repo(), ModuleRoot, "Tooba.Content.Endpoints");
        var csproj = File.ReadAllText(Path.Combine(endpoints, "Tooba.Content.Endpoints.csproj"));
        Assert.DoesNotContain("Tooba.Content.Domain", csproj, StringComparison.Ordinal);

        var joined = string.Join("\n", Directory.GetFiles(endpoints, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(File.ReadAllText));
        Assert.DoesNotContain("Tooba.Content.Domain", joined, StringComparison.Ordinal);
    }

    [Fact]
    public void Content_root_allowlists_match_disk_and_forbidden_roots_absent()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            Repo(), "docs/architecture/tmar-module-structure-manifests.json")));
        var entry = doc.RootElement.GetProperty("modules").EnumerateArray()
            .Single(m => string.Equals(m.GetProperty("module").GetString(), "Content", StringComparison.Ordinal));

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
    public void Content_production_path_equals_namespace_exactly()
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
    public void Content_projects_grouped_under_Modules_Content_solution_folder()
    {
        var slnx = Path.Combine(Repo(), "src", "backend", "Tooba.slnx");
        var doc = XDocument.Load(slnx);
        var folder = doc.Root!.Elements("Folder").SingleOrDefault(f =>
            string.Equals((string?)f.Attribute("Name"), "/Modules/Content/", StringComparison.Ordinal));
        Assert.True(folder is not null, "missing /Modules/Content/ solution folder");

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
