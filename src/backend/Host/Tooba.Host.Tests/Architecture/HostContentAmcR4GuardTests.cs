using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>TB-TMAR-HOST-CONTENT-AMC-001-R4 — semantic Contracts + capability-first foldering guards.</summary>
public sealed class HostContentAmcR4GuardTests
{
    private static readonly string[] CapabilityFolders =
    [
        "Articles", "Authors", "Categories", "Comments", "Media", "Tags",
    ];

    [Fact]
    public void Application_has_no_Models_Contracts_cs_bundles()
    {
        var app = Path.Combine(ContentRoot(), "Tooba.Content.Application");
        var contractsFiles = Directory.GetFiles(app, "*Contracts.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .ToArray();
        Assert.True(contractsFiles.Length == 0,
            "Application must not contain *Contracts.cs bundles: " + string.Join(", ", contractsFiles));

        Assert.False(Directory.Exists(Path.Combine(app, "Models")));
    }

    [Fact]
    public void No_unjustified_single_file_Command_or_Query_leaf_directories()
    {
        var app = Path.Combine(ContentRoot(), "Tooba.Content.Application");
        var violations = new List<string>();

        foreach (var capability in CapabilityFolders)
        {
            foreach (var kind in new[] { "Commands", "Queries" })
            {
                var kindRoot = Path.Combine(app, capability, kind);
                if (!Directory.Exists(kindRoot))
                    continue;

                // Leaf under Commands/Queries that is a directory containing only one .cs request file
                // (legacy one-folder-per-request) is forbidden.
                foreach (var sub in Directory.GetDirectories(kindRoot))
                {
                    var cs = Directory.GetFiles(sub, "*.cs", SearchOption.TopDirectoryOnly);
                    var nestedDirs = Directory.GetDirectories(sub);
                    if (cs.Length == 1 && nestedDirs.Length == 0)
                    {
                        violations.Add(sub[(app.Length + 1)..].Replace('\\', '/'));
                    }
                }

                // Also forbid resurrecting Application/Commands/<UseCase>/ pattern at project root
            }
        }

        foreach (var legacyKind in new[] { "Commands", "Queries" })
        {
            var legacyRoot = Path.Combine(app, legacyKind);
            if (!Directory.Exists(legacyRoot))
                continue;
            violations.Add($"legacy top-level {legacyKind}/");
            foreach (var sub in Directory.GetDirectories(legacyRoot))
            {
                var cs = Directory.GetFiles(sub, "*.cs", SearchOption.TopDirectoryOnly);
                if (cs.Length == 1)
                    violations.Add(sub[(app.Length + 1)..].Replace('\\', '/'));
            }
        }

        Assert.True(violations.Count == 0,
            "Unjustified single-file Command/Query leaf folders: " + string.Join(", ", violations));
    }

    [Fact]
    public void No_foreign_module_references_Content_Application()
    {
        var modulesRoot = Path.Combine(RepoRoot(), "src", "backend", "Modules");
        var violations = new List<string>();
        foreach (var csproj in Directory.GetFiles(modulesRoot, "*.csproj", SearchOption.AllDirectories))
        {
            if (csproj.Contains($"{Path.DirectorySeparatorChar}Content{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                continue;

            var text = File.ReadAllText(csproj);
            if (text.Contains("Tooba.Content.Application", StringComparison.Ordinal))
                violations.Add(csproj[(modulesRoot.Length + 1)..].Replace('\\', '/'));
        }

        Assert.True(violations.Count == 0,
            "Foreign modules must not reference Tooba.Content.Application: " + string.Join(", ", violations));
    }

    [Fact]
    public void Capability_folders_exist_with_owned_surface()
    {
        var app = Path.Combine(ContentRoot(), "Tooba.Content.Application");
        foreach (var capability in CapabilityFolders)
        {
            var capRoot = Path.Combine(app, capability);
            Assert.True(Directory.Exists(capRoot), $"missing capability folder {capability}");
            var hasCs = Directory.GetFiles(capRoot, "*.cs", SearchOption.AllDirectories)
                .Any(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal));
            Assert.True(hasCs, $"empty decorative capability folder {capability}");
        }

        Assert.True(File.Exists(Path.Combine(app, "Composition", "ContentOperation.cs")));
        Assert.True(File.Exists(Path.Combine(app, "Validators", "ContentValidationCodes.cs")));
        Assert.False(Directory.Exists(Path.Combine(app, "Commands")));
        Assert.False(Directory.Exists(Path.Combine(app, "Queries")));
        Assert.False(Directory.Exists(Path.Combine(app, "Ports")));
        Assert.False(Directory.Exists(Path.Combine(app, "Models")));
    }

    [Fact]
    public void Content_Application_path_equals_namespace_exactly()
    {
        var project = "Tooba.Content.Application";
        var projectPath = Path.Combine(ContentRoot(), project);
        var rootFull = Path.GetFullPath(projectPath);
        var violations = new List<string>();
        foreach (var file in Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories))
        {
            var relative = file[rootFull.Length..].TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (relative.StartsWith($"obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || relative.StartsWith($"bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                continue;

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
                violations.Add($"{relative}: expected {expected}, got {match.Groups[1].Value}");
        }

        Assert.True(violations.Count == 0, string.Join("\n", violations));
    }

    [Fact]
    public void No_duplicate_legacy_command_shaped_models_beside_mediatr()
    {
        var app = Path.Combine(ContentRoot(), "Tooba.Content.Application");
        var joined = string.Join("\n", Directory.GetFiles(app, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(File.ReadAllText));

        Assert.DoesNotContain("CreateContentAuthorCommand", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("UpdateContentAuthorCommand", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateContentCategoryCommand", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("UpdateContentCategoryCommand", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("UpdateContentCategorySeoCommand", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("UpdateContentCategoryMediaCommand", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("MoveContentCategoryCommand", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateContentTagCommand", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("ModerateArticleCommentCommand", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("namespace Tooba.Content.Application.Models", joined, StringComparison.Ordinal);
    }

    [Fact]
    public void Manifest_and_SoT_reflect_R4_certification()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(RepoRoot(), "docs", "architecture", "tmar-module-structure-manifests.json")));
        var entry = doc.RootElement.GetProperty("modules").EnumerateArray()
            .Single(m => string.Equals(m.GetProperty("module").GetString(), "Content", StringComparison.Ordinal));
        Assert.True(entry.GetProperty("structureCertified").GetBoolean());
        Assert.Equal("ARCH-COMPLETE-002", entry.GetProperty("lockVersion").GetString());

        var appProject = entry.GetProperty("projects").EnumerateArray()
            .Single(p => p.GetProperty("projectName").GetString() == "Tooba.Content.Application");
        var forbiddenFolders = appProject.GetProperty("forbiddenTopLevelFolders").EnumerateArray()
            .Select(x => x.GetString()!).ToHashSet(StringComparer.Ordinal);
        Assert.Contains("Commands", forbiddenFolders);
        Assert.Contains("Queries", forbiddenFolders);
        Assert.Contains("Models", forbiddenFolders);
        Assert.Contains("Ports", forbiddenFolders);

        using var state = JsonDocument.Parse(File.ReadAllText(Path.Combine(RepoRoot(), "docs", "architecture", "tmar-current-state.json")));
        var certified = state.RootElement.GetProperty("structureLock").GetProperty("certifiedModules")
            .EnumerateArray().Select(x => x.GetString()!).ToArray();
        Assert.Contains("Content", certified, StringComparer.Ordinal);

        var r4 = state.RootElement.GetProperty("hostContentAmcR4");
        Assert.Equal("TB-TMAR-HOST-CONTENT-AMC-001-R4", r4.GetProperty("task").GetString());
        Assert.True(r4.GetProperty("structureCertified").GetBoolean());
        Assert.Equal("ELIMINATED", r4.GetProperty("applicationLegacyContractsState").GetString());
        Assert.Equal("CAPABILITY_FIRST_SHALLOW", r4.GetProperty("capabilityFolderingState").GetString());
        Assert.Equal("ZERO", r4.GetProperty("singleFileRequestFolderState").GetString());
        Assert.False(r4.GetProperty("nextHostFolderStarted").GetBoolean());
        Assert.Equal("USER_REVIEW_HOST_CONTENT_R4_CHECKPOINT", r4.GetProperty("workflowStop").GetString());
    }

    [Fact]
    public void R1_R2_R3_preservation_boundaries_remain()
    {
        var hostContent = Path.Combine(RepoRoot(), "src", "backend", "Host", "Tooba.Host", "Content");
        Assert.True(!Directory.Exists(hostContent) || Directory.GetFiles(hostContent, "*.cs", SearchOption.AllDirectories).Length == 0);

        var endpointsCsproj = File.ReadAllText(Path.Combine(ContentRoot(), "Tooba.Content.Endpoints", "Tooba.Content.Endpoints.csproj"));
        Assert.DoesNotContain("Tooba.Content.Infrastructure", endpointsCsproj, StringComparison.Ordinal);

        var infraCsproj = File.ReadAllText(Path.Combine(ContentRoot(), "Tooba.Content.Infrastructure", "Tooba.Content.Infrastructure.csproj"));
        Assert.Contains("Tooba.Media.Contracts", infraCsproj, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Media.Application", infraCsproj, StringComparison.Ordinal);
        Assert.Contains("Tooba.Localization.Contracts", infraCsproj, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Localization.Application", infraCsproj, StringComparison.Ordinal);

        var contentOp = File.ReadAllText(Path.Combine(ContentRoot(), "Tooba.Content.Application", "Composition", "ContentOperation.cs"));
        Assert.Contains("catch (ContractOperationException ex)", contentOp, StringComparison.Ordinal);
        Assert.Contains("new SemanticError(ex.Code)", contentOp, StringComparison.Ordinal);

        var slnx = Path.Combine(RepoRoot(), "src", "backend", "Tooba.slnx");
        var xdoc = XDocument.Load(slnx);
        Assert.Contains(xdoc.Root!.Elements("Folder"),
            f => string.Equals((string?)f.Attribute("Name"), "/Modules/Content/", StringComparison.Ordinal));
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
