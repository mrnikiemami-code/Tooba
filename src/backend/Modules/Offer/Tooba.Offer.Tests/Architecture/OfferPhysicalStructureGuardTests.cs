using System.Xml.Linq;
using Xunit;

namespace Tooba.Offer.Tests.Architecture;

/// <summary>
/// ARCH-MODULE-PHYSICAL-001 — reference-complete modules must place production files
/// under approved physical responsibility folders; namespace/docs alone are insufficient.
/// </summary>
public sealed class OfferPhysicalStructureGuardTests
{
    private static readonly string[] AllowedDomainFolders =
    [
        "Aggregates", "Entities", "ValueObjects", "Policies", "Events", "Errors"
    ];

    private static readonly string[] AllowedApplicationFolders =
    [
        "UseCases", "Commands", "Queries", "Mappings", "Ports", "Validators", "Dtos", "ReadModels", "Policies"
    ];

    private static readonly string[] AllowedContractsFolders =
    [
        "Ports", "Commands", "Events", "Dtos", "Errors"
    ];

    private static readonly string[] AllowedInfrastructureFolders =
    [
        "Persistence", "Repositories", "Adapters", "Outbox", "Events", "DependencyInjection"
    ];

    private static readonly string[] AllowedEndpointsFolders =
    [
        "Admin", "Storefront", "Seller", "Errors", "Resources"
    ];

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "docs", "ai", "TOOBA-PIPELINE-PROTOCOL.md"))
                || File.Exists(Path.Combine(dir.FullName, ".git", "HEAD")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }

    private static string OfferRoot() =>
        Path.Combine(RepoRoot(), "src", "backend", "Modules", "Offer");

    [Fact]
    public void ARCH_MODULE_PHYSICAL_001_projects_exist_on_disk()
    {
        string[] projects =
        [
            "Tooba.Offer.Domain",
            "Tooba.Offer.Application",
            "Tooba.Offer.Contracts",
            "Tooba.Offer.Infrastructure",
            "Tooba.Offer.Endpoints",
            "Tooba.Offer.Tests"
        ];

        foreach (var project in projects)
        {
            var csproj = Path.Combine(OfferRoot(), project, project + ".csproj");
            Assert.True(File.Exists(csproj), "missing physical project: " + csproj);
        }
    }

    [Fact]
    public void ARCH_MODULE_PHYSICAL_001_no_flat_dumping_ground_at_project_roots()
    {
        AssertNoRootDump("Tooba.Offer.Domain", AllowedDomainFolders, allowRootFiles: Array.Empty<string>());
        AssertNoRootDump("Tooba.Offer.Application", AllowedApplicationFolders, allowRootFiles: Array.Empty<string>());
        AssertNoRootDump("Tooba.Offer.Contracts", AllowedContractsFolders, allowRootFiles: Array.Empty<string>());
        AssertNoRootDump(
            "Tooba.Offer.Infrastructure",
            AllowedInfrastructureFolders,
            allowRootFiles: Array.Empty<string>());
        AssertNoRootDump(
            "Tooba.Offer.Endpoints",
            AllowedEndpointsFolders,
            allowRootFiles: ["OfferEndpointModule.cs"]);
    }

    [Fact]
    public void ARCH_MODULE_PHYSICAL_001_production_files_live_under_approved_folders()
    {
        var violations = new List<string>();
        CheckProjectFiles("Tooba.Offer.Domain", AllowedDomainFolders, violations, allowRootFiles: Array.Empty<string>());
        CheckProjectFiles("Tooba.Offer.Application", AllowedApplicationFolders, violations, allowRootFiles: Array.Empty<string>());
        CheckProjectFiles("Tooba.Offer.Contracts", AllowedContractsFolders, violations, allowRootFiles: Array.Empty<string>());
        CheckProjectFiles(
            "Tooba.Offer.Infrastructure",
            AllowedInfrastructureFolders,
            violations,
            allowRootFiles: Array.Empty<string>());
        CheckProjectFiles(
            "Tooba.Offer.Endpoints",
            AllowedEndpointsFolders,
            violations,
            allowRootFiles: ["OfferEndpointModule.cs"]);

        Assert.True(violations.Count == 0, "physical layout violations:\n" + string.Join("\n", violations));
    }

    [Fact]
    public void ARCH_MODULE_PHYSICAL_001_namespaces_equal_path_derived_namespaces_exactly()
    {
        var violations = new List<string>();
        foreach (var project in new[]
                 {
                     "Tooba.Offer.Domain",
                     "Tooba.Offer.Application",
                     "Tooba.Offer.Contracts",
                     "Tooba.Offer.Infrastructure",
                     "Tooba.Offer.Endpoints",
                 })
        {
            CheckExactNamespaces(project, violations);
        }

        Assert.True(violations.Count == 0, "namespace/path mismatches:\n" + string.Join("\n", violations));
    }

    [Fact]
    public void ARCH_MODULE_PHYSICAL_001_contracts_and_validator_folders_are_namespace_exact()
    {
        var violations = new List<string>();
        foreach (var project in new[]
                 {
                     "Tooba.Offer.Contracts",
                     "Tooba.Offer.Application",
                 })
        {
            foreach (var folder in new[] { "Dtos", "Ports", "Errors", "Validators" })
            {
                ExpectExactFolderNamespace(project, folder, violations);
            }
        }

        Assert.True(violations.Count == 0, "namespace/path mismatches:\n" + string.Join("\n", violations));
    }

    [Fact]
    public void ARCH_MODULE_PHYSICAL_001_no_empty_ceremonial_folders()
    {
        string[] ceremonialCandidates =
        [
            Path.Combine(OfferRoot(), "Tooba.Offer.Application", "UseCases"),
            Path.Combine(OfferRoot(), "Tooba.Offer.Application", "Validators"),
            Path.Combine(OfferRoot(), "Tooba.Offer.Application", "Dtos"),
            Path.Combine(OfferRoot(), "Tooba.Offer.Domain", "Entities"),
            Path.Combine(OfferRoot(), "Tooba.Offer.Domain", "ValueObjects"),
            Path.Combine(OfferRoot(), "Tooba.Offer.Domain", "Policies"),
            Path.Combine(OfferRoot(), "Tooba.Offer.Domain", "Errors"),
            Path.Combine(OfferRoot(), "Tooba.Offer.Contracts", "Commands"),
            Path.Combine(OfferRoot(), "Tooba.Offer.Contracts", "Events"),
            Path.Combine(OfferRoot(), "Tooba.Offer.Infrastructure", "Repositories"),
            Path.Combine(OfferRoot(), "Tooba.Offer.Endpoints", "Admin"),
            Path.Combine(OfferRoot(), "Tooba.Offer.Endpoints", "Storefront")
        ];

        var empties = ceremonialCandidates
            .Where(Directory.Exists)
            .Where(d => !Directory.EnumerateFileSystemEntries(d).Any())
            .ToArray();

        Assert.True(empties.Length == 0, "empty ceremonial folders: " + string.Join("; ", empties));
    }

    [Fact]
    public void ARCH_MODULE_PHYSICAL_001_offer_routes_absent_from_host_seller_endpoints()
    {
        var hostEndpoints = Path.Combine(
            RepoRoot(),
            "src",
            "backend",
            "Host",
            "Tooba.Host",
            "Seller",
            "SellerPanelEndpoints.cs");
        var text = File.ReadAllText(hostEndpoints);
        Assert.DoesNotContain("MapGet(\"/offers\"", text, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPost(\"/offers\"", text, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPatch(\"/offers/", text, StringComparison.Ordinal);

        var moduleEndpoints = Path.Combine(
            OfferRoot(),
            "Tooba.Offer.Endpoints",
            "Seller",
            "OfferSellerEndpoints.cs");
        Assert.True(File.Exists(moduleEndpoints), "Offer seller endpoints must live in module Endpoints project");
    }

    [Fact]
    public void ARCH_MODULE_PHYSICAL_001_no_alias_workaround_in_production_sources()
    {
        var violations = AllProductionFileContents()
            .Where(x => x.Text.Contains("global using", StringComparison.Ordinal))
            .Select(x => x.File)
            .ToList();

        Assert.True(violations.Count == 0, "global namespace alias workaround: " + string.Join("; ", violations));

        var hostRoot = Path.Combine(RepoRoot(), "src", "backend", "Host", "Tooba.Host");
        var hostAlias = Directory.EnumerateFiles(hostRoot, "*.cs", SearchOption.AllDirectories)
            .Where(p =>
            {
                var n = p.Replace('\\', '/');
                return !n.Contains("/bin/", StringComparison.Ordinal) && !n.Contains("/obj/", StringComparison.Ordinal);
            })
            .Where(p =>
            {
                var text = File.ReadAllText(p);
                return text.Contains("global using Tooba.Offer", StringComparison.Ordinal)
                    || text.Contains("global using Offer", StringComparison.Ordinal);
            })
            .Select(p => Path.GetRelativePath(RepoRoot(), p))
            .ToList();
        Assert.True(hostAlias.Count == 0, "Host Offer alias workaround: " + string.Join("; ", hostAlias));
    }

    private static void AssertNoRootDump(string project, string[] allowedFolders, string[] allowRootFiles)
    {
        var root = Path.Combine(OfferRoot(), project);
        var rootCs = Directory.EnumerateFiles(root, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .Where(n => n is not null && !allowRootFiles.Contains(n, StringComparer.OrdinalIgnoreCase))
            .ToArray();
        Assert.True(rootCs.Length == 0, $"{project} root dumping-ground: " + string.Join(", ", rootCs));

        foreach (var dir in Directory.EnumerateDirectories(root))
        {
            var name = Path.GetFileName(dir);
            if (name is "bin" or "obj")
            {
                continue;
            }

            if (!allowedFolders.Contains(name, StringComparer.OrdinalIgnoreCase)
                && !name.StartsWith("Tooba.", StringComparison.Ordinal))
            {
                // Tests project has more folders; this helper is for production projects only.
            }
        }
    }

    private static void CheckProjectFiles(
        string project,
        string[] allowedFolders,
        List<string> violations,
        string[] allowRootFiles)
    {
        var root = Path.Combine(OfferRoot(), project);
        foreach (var file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            var n = file.Replace('\\', '/');
            if (n.Contains("/bin/", StringComparison.Ordinal) || n.Contains("/obj/", StringComparison.Ordinal))
            {
                continue;
            }

            var rel = Path.GetRelativePath(root, file).Replace('\\', '/');
            if (!rel.Contains('/', StringComparison.Ordinal))
            {
                if (!allowRootFiles.Contains(Path.GetFileName(file), StringComparer.OrdinalIgnoreCase))
                {
                    violations.Add($"{project}/{rel}: flat root file");
                }

                continue;
            }

            var top = rel.Split('/')[0];
            if (!allowedFolders.Contains(top, StringComparer.OrdinalIgnoreCase))
            {
                violations.Add($"{project}/{rel}: top folder '{top}' not in approved set");
            }
        }
    }

    private static void ExpectExactFolderNamespace(
        string project,
        string folder,
        List<string> violations)
    {
        var dir = Path.Combine(OfferRoot(), project, folder);
        if (!Directory.Exists(dir))
        {
            return;
        }

        var expected = project + "." + folder;
        foreach (var file in Directory.EnumerateFiles(dir, "*.cs", SearchOption.AllDirectories))
        {
            var n = file.Replace('\\', '/');
            if (n.Contains("/bin/", StringComparison.Ordinal) || n.Contains("/obj/", StringComparison.Ordinal))
            {
                continue;
            }

            var match = System.Text.RegularExpressions.Regex.Match(
                File.ReadAllText(file),
                @"^namespace\s+([A-Za-z0-9_.]+)\s*;",
                System.Text.RegularExpressions.RegexOptions.Multiline);
            if (!match.Success)
            {
                violations.Add($"{Path.GetRelativePath(RepoRoot(), file)}: <missing> expected={expected}");
                continue;
            }

            if (!string.Equals(match.Groups[1].Value, expected, StringComparison.Ordinal))
            {
                violations.Add($"{Path.GetRelativePath(RepoRoot(), file)}: ns={match.Groups[1].Value} expected={expected}");
            }
        }
    }

    /// <summary>
    /// Exact path-derived namespace equality for every production .cs file.
    /// EF migrations and the model snapshot are excluded because they are generated with
    /// block-scoped namespaces that are not the regular single-line form.
    /// </summary>
    private static void CheckExactNamespaces(string project, List<string> violations)
    {
        var root = Path.Combine(OfferRoot(), project);
        if (!Directory.Exists(root))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            var n = file.Replace('\\', '/');
            if (n.Contains("/bin/", StringComparison.Ordinal) || n.Contains("/obj/", StringComparison.Ordinal))
            {
                continue;
            }

            if (n.Contains("/Migrations/", StringComparison.OrdinalIgnoreCase)
                || n.EndsWith("ModelSnapshot.cs", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var rel = Path.GetRelativePath(root, file).Replace('\\', '/');
            var dir = Path.GetDirectoryName(rel)?.Replace('\\', '/') ?? string.Empty;
            var expected = string.IsNullOrEmpty(dir)
                ? project
                : project + "." + dir.Replace('/', '.');

            var text = File.ReadAllText(file);
            var match = System.Text.RegularExpressions.Regex.Match(
                text,
                @"^namespace\s+([A-Za-z0-9_.]+)\s*;",
                System.Text.RegularExpressions.RegexOptions.Multiline);
            if (!match.Success)
            {
                violations.Add($"{Path.GetRelativePath(RepoRoot(), file)}: <missing> expected={expected}");
                continue;
            }

            if (!string.Equals(match.Groups[1].Value, expected, StringComparison.Ordinal))
            {
                violations.Add($"{Path.GetRelativePath(RepoRoot(), file)}: ns={match.Groups[1].Value} expected={expected}");
            }
        }
    }

    private static IReadOnlyList<(string File, string Text)> AllProductionFileContents()
    {
        var results = new List<(string, string)>();
        foreach (var project in new[]
                 {
                     "Tooba.Offer.Domain",
                     "Tooba.Offer.Application",
                     "Tooba.Offer.Contracts",
                     "Tooba.Offer.Infrastructure",
                     "Tooba.Offer.Endpoints",
                 })
        {
            var root = Path.Combine(OfferRoot(), project);
            foreach (var file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                var n = file.Replace('\\', '/');
                if (n.Contains("/bin/", StringComparison.Ordinal) || n.Contains("/obj/", StringComparison.Ordinal))
                {
                    continue;
                }

                results.Add((Path.GetRelativePath(RepoRoot(), file), File.ReadAllText(file)));
            }
        }

        return results;
    }
}
