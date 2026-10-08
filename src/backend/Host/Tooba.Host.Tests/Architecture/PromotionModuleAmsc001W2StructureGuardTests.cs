using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-PROMOTION-AMSC-001-W2 (Structure) — durable structure lock for the Promotion module:
/// capability-first shallow Application tree, exact path↔namespace, enforced root allowlists, clean
/// physical copies, canonical <c>/Modules/Promotion/</c> solution grouping and manifest↔disk agreement.
/// </summary>
public sealed class PromotionModuleAmsc001W2StructureGuardTests
{
    private const string ModuleRoot = "src/backend/Modules/Promotion";
    private const string App = ModuleRoot + "/Tooba.Promotion.Application";
    private const string Contracts = ModuleRoot + "/Tooba.Promotion.Contracts";
    private const string Domain = ModuleRoot + "/Tooba.Promotion.Domain";
    private const string Infra = ModuleRoot + "/Tooba.Promotion.Infrastructure";
    private const string Endpoints = ModuleRoot + "/Tooba.Promotion.Endpoints";
    private const string Tests = ModuleRoot + "/Tooba.Promotion.Tests";

    private static readonly string[] Projects =
    [
        "Tooba.Promotion.Contracts",
        "Tooba.Promotion.Domain",
        "Tooba.Promotion.Application",
        "Tooba.Promotion.Infrastructure",
        "Tooba.Promotion.Endpoints",
        "Tooba.Promotion.Tests",
    ];

    [Fact]
    public void Promotion_path_namespace_alignment_is_exact()
    {
        var root = Repo();
        foreach (var project in Projects)
        {
            var projectPath = Path.Combine(root, ModuleRoot, project);
            var projectFull = Path.GetFullPath(projectPath);
            foreach (var file in Directory.EnumerateFiles(projectPath, "*.cs", SearchOption.AllDirectories)
                         .Where(f => !IsBuildOutput(f))
                         .Where(f => !IsEfMigrationOrSnapshot(f)))
            {
                var relative = file[projectFull.Length..]
                    .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var directory = Path.GetDirectoryName(relative);
                var expected = string.IsNullOrEmpty(directory)
                    ? project
                    : project + "." + directory
                        .Replace(Path.DirectorySeparatorChar, '.')
                        .Replace(Path.AltDirectorySeparatorChar, '.');

                Assert.Equal(expected, DeclaredNamespace(file));
            }
        }
    }

    [Fact]
    public void Promotion_application_is_capability_first_shallow_without_technical_axis_roots()
    {
        var root = Repo();

        // The retired W0/W1 technical-axis Application roots must never come back.
        foreach (var banned in new[] { "Commands", "Queries", "Models", "Ports", "Validators", "Errors", "Handlers", "Requests" })
        {
            Assert.False(
                Directory.Exists(Path.Combine(root, App, banned)),
                $"Application/{banned} must not be a top-level technical axis for a capability-first module.");
        }

        // Application root carries the two capabilities plus only genuinely shared folders.
        Assert.Equal(
            ["Checkout", "Composition", "Merchandising", "Promotions", "Validation"],
            TopLevelFolders(Path.Combine(root, App)));

        // Promotion capability axes.
        Assert.Equal(
            ["Commands", "Models", "Ports", "Queries"],
            TopLevelFolders(Path.Combine(root, App, "Promotions")));

        // Merchandising capability axes plus its Admin write/read surface.
        Assert.Equal(
            ["Admin", "Models", "Ports"],
            TopLevelFolders(Path.Combine(root, App, "Merchandising")));
        Assert.Equal(
            ["Commands", "Queries"],
            TopLevelFolders(Path.Combine(root, App, "Merchandising", "Admin")));

        // The shared Composition folder keeps only the single typed-fault seam.
        Assert.Equal(
            ["PromotionOperation.cs"],
            Directory.EnumerateFiles(Path.Combine(root, App, "Composition"), "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName).OrderBy(x => x, StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public void Promotion_has_no_single_file_use_case_request_leaf_folders()
    {
        var root = Repo();

        // A folder wrapping exactly one production source file and named after a request/use case is
        // OVER_FOLDERED by default; the W2 capability axes carry the requests as files instead.
        var offenders = Directory.EnumerateDirectories(Path.Combine(root, App), "*", SearchOption.AllDirectories)
            .Where(dir => !IsBuildOutput(dir))
            .Where(dir => Directory.EnumerateFiles(dir, "*.cs", SearchOption.TopDirectoryOnly).Count() == 1)
            .Select(Path.GetFileName)
            .Where(name => name.StartsWith("Create", StringComparison.Ordinal)
                           || name.StartsWith("Update", StringComparison.Ordinal)
                           || name.StartsWith("Get", StringComparison.Ordinal)
                           || name.StartsWith("List", StringComparison.Ordinal)
                           || name.StartsWith("Query", StringComparison.Ordinal)
                           || name.StartsWith("Activate", StringComparison.Ordinal)
                           || name.StartsWith("Deactivate", StringComparison.Ordinal)
                           || name.StartsWith("Publish", StringComparison.Ordinal)
                           || name.StartsWith("Unpublish", StringComparison.Ordinal)
                           || name.StartsWith("Archive", StringComparison.Ordinal)
                           || name.StartsWith("Restore", StringComparison.Ordinal)
                           || name.StartsWith("Set", StringComparison.Ordinal)
                           || name.StartsWith("Reorder", StringComparison.Ordinal)
                           || name.StartsWith("Add", StringComparison.Ordinal)
                           || name.StartsWith("Remove", StringComparison.Ordinal))
            .ToArray();
        Assert.Empty(offenders);

        // The nine retired use-case leaf folders and their single files are gone from disk.
        foreach (var retired in new[]
                 {
                     "Commands/ActivateSellerPromotion", "Commands/CreateSellerPromotion",
                     "Commands/DeactivateAdminPromotion", "Commands/DeactivateSellerPromotion",
                     "Commands/UpdateSellerPromotion", "Queries/GetAdminPromotion",
                     "Queries/GetSellerPromotion", "Queries/ListAdminPromotions", "Queries/ListSellerPromotions",
                 })
        {
            Assert.False(
                Directory.Exists(Path.Combine(root, App, retired.Replace('/', Path.DirectorySeparatorChar))),
                $"retired use-case leaf folder {retired} must not return");
        }

        // The real W2 homes exist exactly once.
        Assert.Equal(5, Directory.EnumerateFiles(Path.Combine(root, App, "Promotions", "Commands"), "*.cs").Count());
        Assert.Equal(4, Directory.EnumerateFiles(Path.Combine(root, App, "Promotions", "Queries"), "*.cs").Count());
        Assert.Equal(8, Directory.EnumerateFiles(Path.Combine(root, App, "Merchandising", "Admin", "Commands"), "*.cs").Count());
        Assert.Equal(4, Directory.EnumerateFiles(Path.Combine(root, App, "Merchandising", "Admin", "Queries"), "*.cs").Count());
    }

    [Fact]
    public void Promotion_root_allowlists_match_disk_and_no_ceremony_remains()
    {
        var root = Repo();

        Assert.Empty(Directory.EnumerateFiles(Path.Combine(root, ModuleRoot), ".gitkeep", SearchOption.AllDirectories)
            .Where(f => !IsBuildOutput(f)).ToArray());

        Assert.Equal([], RootCs(root, Contracts));
        Assert.Equal([], RootCs(root, Domain));
        Assert.Equal([], RootCs(root, App));
        Assert.Equal([], RootCs(root, Infra));
        Assert.Equal(["PromotionEndpointModule.cs"], RootCs(root, Endpoints));
        Assert.Equal([], RootCs(root, Tests));

        // No stale/duplicate copy of the retired generic bundles.
        foreach (var retired in new[]
                 {
                     "PromotionDirectoryPorts.cs", "MerchandisingCampaignPorts.cs",
                     "MerchandisingCampaignAdminCqrs.cs", "PromotionErrors.cs",
                 })
        {
            Assert.Empty(Directory.EnumerateFiles(Path.Combine(root, ModuleRoot), retired, SearchOption.AllDirectories)
                .Where(f => !IsBuildOutput(f)).ToArray());
        }
    }

    [Fact]
    public void Promotion_solution_explorer_grouping_is_canonical()
    {
        var root = Repo();
        var slnx = File.ReadAllText(Path.Combine(root, "src/backend/Tooba.slnx"));
        Assert.Contains("<Folder Name=\"/Modules/Promotion/\">", slnx, StringComparison.Ordinal);
        foreach (var project in Projects)
        {
            Assert.Contains(
                $"Modules/Promotion/{project}/{project}.csproj",
                slnx,
                StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Promotion_manifest_structure_allowlists_match_disk()
    {
        var root = Repo();
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-module-structure-manifests.json")));

        // Promotion moved out of the uncertified list and into the pre-cert structure record in W2.
        Assert.DoesNotContain(
            manifest.RootElement.GetProperty("uncertifiedHttpOwningModules").EnumerateArray(),
            m => m.GetString() == "Promotion");

        var entry = manifest.RootElement.GetProperty("preCertModules").EnumerateArray()
            .Single(m => m.GetProperty("module").GetString() == "Promotion");
        Assert.False(entry.GetProperty("structureCertified").GetBoolean());
        Assert.Equal("ARCH-COMPLETE-002", entry.GetProperty("lockVersion").GetString());
        Assert.Equal(
            Projects.OrderBy(x => x, StringComparer.Ordinal).ToArray(),
            entry.GetProperty("projects").EnumerateArray()
                .Select(p => p.GetProperty("projectName").GetString()!)
                .OrderBy(x => x, StringComparer.Ordinal).ToArray());

        foreach (var project in entry.GetProperty("projects").EnumerateArray())
        {
            var projectName = project.GetProperty("projectName").GetString()!;
            var projectPath = Path.Combine(root, ModuleRoot, projectName);

            Assert.Equal(
                project.GetProperty("rootAllowlist").EnumerateArray().Select(x => x.GetString()!)
                    .OrderBy(x => x, StringComparer.Ordinal).ToArray(),
                RootCs(root, projectPath));

            foreach (var forbidden in project.GetProperty("forbiddenRootFiles").EnumerateArray())
            {
                Assert.False(
                    File.Exists(Path.Combine(projectPath, forbidden.GetString()!)),
                    $"{projectName} still has forbidden root file {forbidden.GetString()}");
            }

            foreach (var forbidden in project.GetProperty("forbiddenTopLevelFolders").EnumerateArray())
            {
                Assert.False(
                    Directory.Exists(Path.Combine(projectPath, forbidden.GetString()!)),
                    $"{projectName} still has forbidden top-level folder {forbidden.GetString()}");
            }
        }

        // The Application allowlist must forbid the retired technical-axis roots, not just omit them.
        var app = entry.GetProperty("projects").EnumerateArray()
            .Single(p => p.GetProperty("projectName").GetString() == "Tooba.Promotion.Application");
        Assert.Equal(
            ["Commands", "Errors", "Handlers", "Models", "Ports", "Queries", "Requests", "Validators"],
            app.GetProperty("forbiddenTopLevelFolders").EnumerateArray().Select(x => x.GetString()!)
                .OrderBy(x => x, StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public void Promotion_merchandising_models_namespace_is_repaired_and_consumed_explicitly()
    {
        var root = Repo();

        // W1 left these two files under the Ports namespace after moving them into Models/.
        Assert.Equal(
            "Tooba.Promotion.Application.Merchandising.Models",
            DeclaredNamespace(Path.Combine(root, App, "Merchandising", "Models", "MerchandisingCampaignAdminModels.cs")));
        Assert.Equal(
            "Tooba.Promotion.Application.Merchandising.Models",
            DeclaredNamespace(Path.Combine(root, App, "Merchandising", "Models", "MerchandisingCampaignReferences.cs")));

        // Every consumer of the merchandising DTOs imports the Models namespace explicitly.
        foreach (var consumer in new[]
                 {
                     App + "/Merchandising/Ports/IMerchandisingCampaignAdminComposer.cs",
                     App + "/Merchandising/Ports/IMerchandisingCampaignDirectory.cs",
                     Endpoints + "/Admin/MerchandisingCampaignAdminEndpoints.cs",
                     Infra + "/Directories/MerchandisingCampaignDirectory.cs",
                     Infra + "/Merchandising/MerchandisingCampaignAdminComposer.cs",
                 })
        {
            Assert.Contains(
                "using Tooba.Promotion.Application.Merchandising.Models;",
                File.ReadAllText(Path.Combine(root, consumer)),
                StringComparison.Ordinal);
        }

        // No production file may declare a namespace that contradicts its physical folder.
        foreach (var project in new[] { "Tooba.Promotion.Contracts", "Tooba.Promotion.Domain", "Tooba.Promotion.Application", "Tooba.Promotion.Infrastructure", "Tooba.Promotion.Endpoints" })
        {
            var projectPath = Path.Combine(root, ModuleRoot, project);
            foreach (var file in Directory.EnumerateFiles(projectPath, "*.cs", SearchOption.AllDirectories)
                         .Where(f => !IsBuildOutput(f) && !IsEfMigrationOrSnapshot(f)))
            {
                var relative = file[Path.GetFullPath(projectPath).Length..]
                    .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var directory = Path.GetDirectoryName(relative);
                var expected = string.IsNullOrEmpty(directory)
                    ? project
                    : project + "." + directory.Replace(Path.DirectorySeparatorChar, '.');
                Assert.Equal(expected, DeclaredNamespace(file));
            }
        }
    }

    private static string[] RootCs(string root, string projectRelativePath) =>
        Directory.EnumerateFiles(Path.Combine(root, projectRelativePath), "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName!)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

    private static string[] TopLevelFolders(string absolutePath) =>
        Directory.EnumerateDirectories(absolutePath)
            .Where(dir => !IsBuildOutput(dir))
            .Select(Path.GetFileName!)
            .Where(name => !string.Equals(name, "bin", StringComparison.OrdinalIgnoreCase)
                           && !string.Equals(name, "obj", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

    private static string DeclaredNamespace(string file)
    {
        var match = Regex.Match(
            File.ReadAllText(file).TrimStart('\uFEFF'),
            @"^namespace\s+([A-Za-z0-9_.]+)",
            RegexOptions.Multiline);
        Assert.True(match.Success, $"no namespace declaration in {file}");
        return match.Groups[1].Value;
    }

    private static bool IsEfMigrationOrSnapshot(string path) =>
        path.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
        || path.EndsWith("ModelSnapshot.cs", StringComparison.OrdinalIgnoreCase);

    private static bool IsBuildOutput(string path) =>
        path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
        || path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal);

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
