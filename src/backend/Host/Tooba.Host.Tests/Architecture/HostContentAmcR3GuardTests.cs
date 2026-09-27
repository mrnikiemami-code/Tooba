using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>TB-TMAR-HOST-CONTENT-AMC-001-R3 — ARCH-COMPLETE-002 structure certification guards.</summary>
public sealed class HostContentAmcR3GuardTests
{
    private static readonly string[] ContentProjects =
    [
        "Tooba.Content.Application",
        "Tooba.Content.Contracts",
        "Tooba.Content.Domain",
        "Tooba.Content.Endpoints",
        "Tooba.Content.Infrastructure",
    ];

    [Fact]
    public void Content_manifest_entry_is_exactly_once_and_structure_certified()
    {
        using var doc = ReadManifest();
        var entries = doc.RootElement.GetProperty("modules").EnumerateArray()
            .Where(m => string.Equals(m.GetProperty("module").GetString(), "Content", StringComparison.Ordinal))
            .ToArray();
        Assert.Single(entries);
        Assert.True(entries[0].GetProperty("structureCertified").GetBoolean());
        Assert.Equal("ARCH-COMPLETE-002", entries[0].GetProperty("lockVersion").GetString());

        var projects = entries[0].GetProperty("projects").EnumerateArray()
            .Select(p => p.GetProperty("projectName").GetString()!)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(ContentProjects.OrderBy(x => x, StringComparer.Ordinal).ToArray(), projects);
    }

    [Fact]
    public void Content_root_allowlists_match_disk_and_forbidden_roots_absent()
    {
        using var doc = ReadManifest();
        var entry = doc.RootElement.GetProperty("modules").EnumerateArray()
            .Single(m => string.Equals(m.GetProperty("module").GetString(), "Content", StringComparison.Ordinal));

        foreach (var project in entry.GetProperty("projects").EnumerateArray())
        {
            var projectName = project.GetProperty("projectName").GetString()!;
            var projectPath = Path.Combine(ContentRoot(), projectName);
            Assert.True(Directory.Exists(projectPath), projectPath);

            var allowlist = project.GetProperty("rootAllowlist").EnumerateArray()
                .Select(x => x.GetString()!)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray();
            var actualRoot = Directory.GetFiles(projectPath, "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName!)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray();
            Assert.Equal(allowlist, actualRoot);

            foreach (var forbidden in project.GetProperty("forbiddenRootFiles").EnumerateArray())
            {
                Assert.False(
                    File.Exists(Path.Combine(projectPath, forbidden.GetString()!)),
                    $"{projectName} still has forbidden root file {forbidden.GetString()}");
            }

            foreach (var forbiddenFolder in project.GetProperty("forbiddenTopLevelFolders").EnumerateArray())
            {
                Assert.False(
                    Directory.Exists(Path.Combine(projectPath, forbiddenFolder.GetString()!)),
                    $"{projectName} still has forbidden top-level folder {forbiddenFolder.GetString()}");
            }
        }
    }

    [Fact]
    public void Content_production_cs_path_equals_namespace_exactly()
    {
        var violations = new List<string>();
        foreach (var project in ContentProjects)
        {
            var projectPath = Path.Combine(ContentRoot(), project);
            var rootFull = Path.GetFullPath(projectPath);
            foreach (var file in Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories))
            {
                var relative = file[rootFull.Length..].TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (relative.StartsWith($"obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    || relative.StartsWith($"bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                {
                    continue;
                }

                if (Path.GetFileName(relative).StartsWith("GlobalUsings", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var dir = Path.GetDirectoryName(relative);
                var expected = string.IsNullOrEmpty(dir)
                    ? project
                    : project + "." + dir.Replace(Path.DirectorySeparatorChar, '.').Replace(Path.AltDirectorySeparatorChar, '.');
                var text = File.ReadAllText(file);
                var match = Regex.Match(text, @"^namespace\s+([A-Za-z0-9_.]+)", RegexOptions.Multiline);
                if (!match.Success)
                {
                    violations.Add($"{relative}: missing namespace");
                    continue;
                }

                if (!string.Equals(expected, match.Groups[1].Value, StringComparison.Ordinal))
                {
                    violations.Add($"{relative}: expected {expected}, got {match.Groups[1].Value}");
                }
            }
        }

        Assert.True(violations.Count == 0, string.Join("\n", violations));
    }

    [Fact]
    public void Content_projects_grouped_under_Modules_Content_solution_folder()
    {
        var slnx = Path.Combine(RepoRoot(), "src", "backend", "Tooba.slnx");
        Assert.True(File.Exists(slnx), slnx);
        var doc = XDocument.Load(slnx);
        var folders = doc.Root!.Elements("Folder").ToArray();
        var contentFolder = folders.SingleOrDefault(f =>
            string.Equals((string?)f.Attribute("Name"), "/Modules/Content/", StringComparison.Ordinal));
        Assert.True(contentFolder is not null, "missing /Modules/Content/ solution folder");

        var nested = contentFolder!.Elements("Project")
            .Select(p => Path.GetFileNameWithoutExtension((string)p.Attribute("Path")!))
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(ContentProjects.OrderBy(x => x, StringComparer.Ordinal).ToArray(), nested);

        var outside = folders
            .Where(f => !string.Equals((string?)f.Attribute("Name"), "/Modules/Content/", StringComparison.Ordinal))
            .SelectMany(f => f.Elements("Project"))
            .Select(p => Path.GetFileNameWithoutExtension((string)p.Attribute("Path")!))
            .Where(n => ContentProjects.Contains(n, StringComparer.Ordinal))
            .ToArray();
        Assert.True(outside.Length == 0, "Content projects duplicated outside /Modules/Content/: " + string.Join(", ", outside));
    }

    [Fact]
    public void Structure_lock_SoT_includes_Content_and_hostContentAmcR3()
    {
        using var state = JsonDocument.Parse(File.ReadAllText(Path.Combine(RepoRoot(), "docs", "architecture", "tmar-current-state.json")));
        var certified = state.RootElement.GetProperty("structureLock").GetProperty("certifiedModules")
            .EnumerateArray().Select(x => x.GetString()!).ToArray();
        Assert.Contains("Content", certified, StringComparer.Ordinal);

        var r3 = state.RootElement.GetProperty("hostContentAmcR3");
        Assert.Equal("TB-TMAR-HOST-CONTENT-AMC-001-R3", r3.GetProperty("task").GetString());
        Assert.True(r3.GetProperty("structureCertified").GetBoolean());
        Assert.Equal("ARCH-COMPLETE-002", r3.GetProperty("lockVersion").GetString());
        Assert.Equal("/Modules/Content/", r3.GetProperty("solutionGrouping").GetString());
        Assert.False(r3.GetProperty("nextHostFolderStarted").GetBoolean());
        Assert.Equal("USER_REVIEW_HOST_CONTENT_R3_FINAL_CHECKPOINT", r3.GetProperty("workflowStop").GetString());
    }

    [Fact]
    public void Content_R1_R2_boundary_guarantees_remain()
    {
        var hostContent = Path.Combine(RepoRoot(), "src", "backend", "Host", "Tooba.Host", "Content");
        Assert.True(!Directory.Exists(hostContent) || Directory.GetFiles(hostContent, "*.cs", SearchOption.AllDirectories).Length == 0);

        var endpointsCsproj = File.ReadAllText(Path.Combine(
            ContentRoot(), "Tooba.Content.Endpoints", "Tooba.Content.Endpoints.csproj"));
        Assert.DoesNotContain("Tooba.Content.Infrastructure", endpointsCsproj, StringComparison.Ordinal);

        var infraCsproj = File.ReadAllText(Path.Combine(
            ContentRoot(), "Tooba.Content.Infrastructure", "Tooba.Content.Infrastructure.csproj"));
        Assert.DoesNotContain("Tooba.Media.Application", infraCsproj, StringComparison.Ordinal);
        Assert.Contains("Tooba.Media.Contracts", infraCsproj, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Localization.Application", infraCsproj, StringComparison.Ordinal);
        Assert.Contains("Tooba.Localization.Contracts", infraCsproj, StringComparison.Ordinal);

        foreach (var layer in new[] { "Tooba.Content.Application", "Tooba.Content.Infrastructure" })
        {
            var joined = string.Join("\n", Directory.GetFiles(Path.Combine(ContentRoot(), layer), "*.cs", SearchOption.AllDirectories)
                .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    && !p.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                .Select(File.ReadAllText));
            Assert.DoesNotContain("IsKnownCode(", joined, StringComparison.Ordinal);
            Assert.DoesNotContain("SemanticError(ex.Message", joined, StringComparison.Ordinal);
            Assert.DoesNotContain(".Message.Contains(", joined, StringComparison.Ordinal);
            Assert.DoesNotContain("PlatformHttpException", joined, StringComparison.Ordinal);
        }

        var contentOp = File.ReadAllText(Path.Combine(
            ContentRoot(), "Tooba.Content.Application", "Composition", "ContentOperation.cs"));
        Assert.Contains("catch (ContractOperationException ex)", contentOp, StringComparison.Ordinal);
        Assert.Contains("new SemanticError(ex.Code)", contentOp, StringComparison.Ordinal);
    }

    [Fact]
    public void Content_required_physical_folders_exist_after_repair()
    {
        var root = ContentRoot();
        Assert.True(File.Exists(Path.Combine(root, "Tooba.Content.Domain", "Aggregates", "ContentArticle.cs")));
        Assert.True(File.Exists(Path.Combine(root, "Tooba.Content.Domain", "Rules", "ContentArticleLifecycleRules.cs")));
        Assert.True(File.Exists(Path.Combine(root, "Tooba.Content.Application", "Composition", "ContentOperation.cs")));
        Assert.True(File.Exists(Path.Combine(root, "Tooba.Content.Infrastructure", "Directories", "ContentDirectory.cs")));
        Assert.True(File.Exists(Path.Combine(root, "Tooba.Content.Infrastructure", "Development", "ContentDevelopmentSeed.cs")));
        Assert.True(File.Exists(Path.Combine(root, "Tooba.Content.Infrastructure", "ContentModule.cs")));
        Assert.False(File.Exists(Path.Combine(root, "Tooba.Content.Domain", "ContentArticle.cs")));
        Assert.False(File.Exists(Path.Combine(root, "Tooba.Content.Application", "ContentOperation.cs")));
        Assert.False(File.Exists(Path.Combine(root, "Tooba.Content.Infrastructure", "ContentDirectory.cs")));
        Assert.False(File.Exists(Path.Combine(root, "Tooba.Content.Infrastructure", "ContentDevelopmentSeed.cs")));
    }

    private static JsonDocument ReadManifest()
    {
        var path = Path.Combine(RepoRoot(), "docs", "architecture", "tmar-module-structure-manifests.json");
        return JsonDocument.Parse(File.ReadAllText(path));
    }

    private static string ContentRoot() =>
        Path.Combine(RepoRoot(), "src", "backend", "Modules", "Content");

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
                return directory.FullName;
            directory = directory.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
