using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-PAGECOMPOSITION-AMSC-001-W2 — scoped structure gate.
/// Pins the capability-first shallow tree (zero per-use-case request leaves), the manifest root
/// allowlists/forbidden lists, exact path↔namespace equality for the module projects and the
/// canonical /Modules/PageComposition/ solution grouping.
/// </summary>
public sealed class PageCompositionModuleAmsc001W2StructureGuardTests
{
    private const string ModuleRoot = "src/backend/Modules/PageComposition";

    [Fact]
    public void Capability_axes_are_flat_with_zero_per_use_case_request_leaves()
    {
        var root = Repo();
        foreach (var axis in new[]
        {
            "Tooba.PageComposition.Application/Admin/Commands",
            "Tooba.PageComposition.Application/Admin/Queries",
            "Tooba.PageComposition.Application/Admin/Validators",
            "Tooba.PageComposition.Application/Storefront/Queries",
            "Tooba.PageComposition.Application/Storefront/Validators"
        })
        {
            var dir = Path.Combine(root, ModuleRoot, axis.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(Directory.Exists(dir), $"missing capability axis {axis}");
            Assert.Empty(Directory.EnumerateDirectories(dir));
        }

        // No use-case-named single-file leaf anywhere in the touched Application/Endpoints trees.
        foreach (var project in new[] { "Tooba.PageComposition.Application", "Tooba.PageComposition.Endpoints" })
        {
            var projectDir = Path.Combine(root, ModuleRoot, project);
            foreach (var dir in Directory.EnumerateDirectories(projectDir, "*", SearchOption.AllDirectories))
            {
                if (dir.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    || dir.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                {
                    continue;
                }

                var files = Directory.EnumerateFiles(dir, "*.cs", SearchOption.TopDirectoryOnly).ToList();
                if (files.Count != 1 || Directory.EnumerateDirectories(dir).Any())
                {
                    continue;
                }

                // A per-use-case leaf is a FOLDER named after one Command/Query/UseCase wrapping a
                // single source file. Shared technical axes (Commands/, Queries/, Validators/, Models/,
                // Ports/) legitimately contain request-named files and are the canonical shape.
                var folderName = Path.GetFileName(dir.TrimEnd(Path.DirectorySeparatorChar));
                Assert.False(
                    folderName.EndsWith("Command", StringComparison.Ordinal)
                        || folderName.EndsWith("Query", StringComparison.Ordinal)
                        || folderName.EndsWith("UseCase", StringComparison.Ordinal),
                    $"per-use-case request leaf folder {dir}");
            }
        }
    }

    [Fact]
    public void Root_allowlists_and_forbidden_lists_match_the_manifest()
    {
        var root = Repo();
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, ManifestPath)).Replace("\uFEFF", string.Empty));
        var module = manifest.RootElement.GetProperty("modules").EnumerateArray()
            .Single(m => m.GetProperty("module").GetString() == "PageComposition");

        foreach (var project in module.GetProperty("projects").EnumerateArray())
        {
            var projectName = project.GetProperty("projectName").GetString()!;
            var projectPath = Path.Combine(root, ModuleRoot, projectName);

            var allowlist = project.GetProperty("rootAllowlist").EnumerateArray().Select(x => x.GetString()!)
                .OrderBy(x => x, StringComparer.Ordinal).ToArray();
            var actualRoot = Directory.GetFiles(projectPath, "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName!).OrderBy(x => x, StringComparer.Ordinal).ToArray();
            Assert.Equal(allowlist, actualRoot);

            foreach (var forbidden in project.GetProperty("forbiddenRootFiles").EnumerateArray())
            {
                Assert.False(File.Exists(Path.Combine(projectPath, forbidden.GetString()!)),
                    $"{projectName} resurrected forbidden root file {forbidden.GetString()}");
            }

            foreach (var forbiddenFolder in project.GetProperty("forbiddenTopLevelFolders").EnumerateArray())
            {
                Assert.False(Directory.Exists(Path.Combine(projectPath, forbiddenFolder.GetString()!)),
                    $"{projectName} resurrected forbidden top-level folder {forbiddenFolder.GetString()}");
            }
        }
    }

    [Fact]
    public void Path_derived_namespaces_are_exact()
    {
        var root = Repo();
        foreach (var project in new[]
        {
            "Tooba.PageComposition.Contracts", "Tooba.PageComposition.Domain",
            "Tooba.PageComposition.Application", "Tooba.PageComposition.Infrastructure",
            "Tooba.PageComposition.Endpoints"
        })
        {
            var projectPath = Path.Combine(root, ModuleRoot, project);
            foreach (var file in Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories))
            {
                var relative = file[Path.GetFullPath(projectPath).Length..]
                    .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (relative.StartsWith($"obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    || relative.StartsWith($"bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    || Path.GetFileName(relative).StartsWith("GlobalUsings", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // EF Persistence/Migrations designer/snapshot exemption follows the repository lock.
                if (relative.Contains($"Persistence{Path.DirectorySeparatorChar}Migrations", StringComparison.Ordinal))
                {
                    continue;
                }

                var dir = Path.GetDirectoryName(relative);
                var expected = string.IsNullOrEmpty(dir)
                    ? project
                    : project + "." + dir.Replace(Path.DirectorySeparatorChar, '.').Replace(Path.AltDirectorySeparatorChar, '.');
                var match = Regex.Match(
                    File.ReadAllText(file).TrimStart('\uFEFF'),
                    @"^namespace\s+([A-Za-z0-9_.]+)",
                    RegexOptions.Multiline);
                Assert.True(match.Success, $"no namespace in {relative}");
                Assert.Equal(expected, match.Groups[1].Value);
            }
        }
    }

    [Fact]
    public void Solution_grouping_is_canonical_modules_pagecomposition()
    {
        var slnx = File.ReadAllText(Path.Combine(Repo(), "src/backend/Tooba.slnx"));
        var folderStart = slnx.IndexOf("<Folder Name=\"/Modules/PageComposition/\">", StringComparison.Ordinal);
        Assert.True(folderStart >= 0, "missing /Modules/PageComposition/ solution folder");
        var folderEnd = slnx.IndexOf("</Folder>", folderStart, StringComparison.Ordinal);
        var group = slnx[folderStart..folderEnd];
        foreach (var project in new[]
        {
            "Tooba.PageComposition.Domain", "Tooba.PageComposition.Contracts",
            "Tooba.PageComposition.Application", "Tooba.PageComposition.Infrastructure",
            "Tooba.PageComposition.Endpoints"
        })
        {
            Assert.Contains($"Modules/PageComposition/{project}/{project}.csproj", group, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Endpoints_import_hygiene_stays_application_contracts_buildingblocks_only()
    {
        var csproj = File.ReadAllText(Path.Combine(
            Repo(), ModuleRoot.Replace('/', Path.DirectorySeparatorChar),
            "Tooba.PageComposition.Endpoints", "Tooba.PageComposition.Endpoints.csproj"));
        Assert.Contains("Tooba.PageComposition.Application.csproj", csproj, StringComparison.Ordinal);
        Assert.Contains("Tooba.PageComposition.Contracts.csproj", csproj, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.PageComposition.Infrastructure.csproj", csproj, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.PageComposition.Domain.csproj", csproj, StringComparison.Ordinal);
    }

    private const string ManifestPath = "docs/architecture/tmar-module-structure-manifests.json";

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
