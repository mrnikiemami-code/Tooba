using System.Text.Json;
using System.Xml.Linq;
using Xunit;

namespace Tooba.Offer.Tests.Architecture;

/// <summary>
/// TB-TMAR-OFFER-AMC-001-W2 — durable manifest↔disk reconciliation and solution-grouping guard.
///
/// The Offer entry in <c>tmar-module-structure-manifests.json</c> must describe the real certified
/// production surface: exactly the five Offer production projects, each with a <c>rootAllowlist</c>
/// equal to the actual top-level production <c>.cs</c> files on disk and with
/// <c>forbiddenTopLevelFolders</c> that are genuinely absent.
///
/// This guard reads the real manifest, the real project directories and the real solution file.
/// It never hard-codes a PASS that is independent of disk state.
/// </summary>
public sealed class OfferManifestDiskReconciliationGuardTests
{
    private const string ManifestRelativePath = "docs/architecture/tmar-module-structure-manifests.json";
    private const string ModuleName = "Offer";
    private const string SolutionRelativePath = "src/backend/Tooba.slnx";

    /// <summary>The certified Offer production surface (tests project excluded by design).</summary>
    private static readonly string[] ExpectedProductionProjects =
    [
        "Tooba.Offer.Contracts",
        "Tooba.Offer.Domain",
        "Tooba.Offer.Application",
        "Tooba.Offer.Endpoints",
        "Tooba.Offer.Infrastructure",
    ];

    [Fact]
    public void Offer_manifest_has_exactly_one_entry_certified_under_arch_complete_002()
    {
        using var doc = ReadManifest();
        var entries = doc.RootElement.GetProperty("modules").EnumerateArray()
            .Where(m => string.Equals(m.GetProperty("module").GetString(), ModuleName, StringComparison.Ordinal))
            .ToArray();

        Assert.Single(entries);
        Assert.True(entries[0].GetProperty("structureCertified").GetBoolean(), "Offer structureCertified must be true");
        Assert.Equal("ARCH-COMPLETE-002", entries[0].GetProperty("lockVersion").GetString());
    }

    [Fact]
    public void Offer_manifest_represents_exactly_the_five_production_projects()
    {
        var declared = ReadOfferProjectNames();
        var onDisk = ProductionProjectDirectories()
            .Select(Path.GetFileName)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        // Missing project in the manifest is a defect; an extra project directory is a defect too.
        Assert.Equal(onDisk, declared);
        Assert.Equal(
            ExpectedProductionProjects.OrderBy(x => x, StringComparer.Ordinal).ToArray(),
            declared);
    }

    [Fact]
    public void Offer_manifest_root_allowlists_equal_real_disk_root_cs_files()
    {
        using var doc = ReadManifest();
        var offer = SingleOfferEntry(doc);

        foreach (var project in offer.GetProperty("projects").EnumerateArray())
        {
            var projectName = project.GetProperty("projectName").GetString()!;
            var projectDir = Path.Combine(ModulesOfferRoot(), projectName);
            Assert.True(Directory.Exists(projectDir), $"missing production project directory {projectName}");

            var declared = project.GetProperty("rootAllowlist").EnumerateArray()
                .Select(x => x.GetString()!)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray();
            var actual = Directory.GetFiles(projectDir, "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName!)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray();

            Assert.Equal(actual, declared);
        }
    }

    [Fact]
    public void Offer_manifest_forbidden_top_level_folders_are_absent_on_disk()
    {
        using var doc = ReadManifest();
        var offer = SingleOfferEntry(doc);
        var violations = new List<string>();

        foreach (var project in offer.GetProperty("projects").EnumerateArray())
        {
            var projectName = project.GetProperty("projectName").GetString()!;
            var projectDir = Path.Combine(ModulesOfferRoot(), projectName);

            foreach (var forbidden in project.GetProperty("forbiddenTopLevelFolders").EnumerateArray())
            {
                var name = forbidden.GetString()!;
                if (Directory.Exists(Path.Combine(projectDir, name)))
                {
                    violations.Add($"{projectName}/{name}");
                }
            }

            foreach (var forbidden in project.GetProperty("forbiddenRootFiles").EnumerateArray())
            {
                var name = forbidden.GetString()!;
                if (File.Exists(Path.Combine(projectDir, name)))
                {
                    violations.Add($"{projectName}/{name}");
                }
            }
        }

        Assert.True(violations.Count == 0, "forbidden Offer structure present: " + string.Join("; ", violations));
    }

    [Fact]
    public void Offer_projects_are_grouped_exactly_once_under_Modules_Offer_in_slnx()
    {
        var solutionPath = Path.Combine(RepoRoot(), SolutionRelativePath.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(solutionPath), solutionPath);

        var doc = XDocument.Load(solutionPath);
        var offerFolder = doc.Descendants()
            .Where(e => e.Name.LocalName == "Folder")
            .Single(e => string.Equals(e.Attribute("Name")?.Value, "/Modules/Offer/", StringComparison.Ordinal));

        var paths = offerFolder.Descendants()
            .Where(e => e.Name.LocalName == "Project")
            .Select(e => e.Attribute("Path")!.Value.Replace('\\', '/'))
            .ToArray();

        Assert.Equal(
            new[]
            {
                "Modules/Offer/Tooba.Offer.Application/Tooba.Offer.Application.csproj",
                "Modules/Offer/Tooba.Offer.Contracts/Tooba.Offer.Contracts.csproj",
                "Modules/Offer/Tooba.Offer.Domain/Tooba.Offer.Domain.csproj",
                "Modules/Offer/Tooba.Offer.Endpoints/Tooba.Offer.Endpoints.csproj",
                "Modules/Offer/Tooba.Offer.Infrastructure/Tooba.Offer.Infrastructure.csproj",
                "Modules/Offer/Tooba.Offer.Tests/Tooba.Offer.Tests.csproj",
            }.OrderBy(x => x, StringComparer.Ordinal).ToArray(),
            paths.OrderBy(x => x, StringComparer.Ordinal).ToArray());

        // No Offer project may appear twice anywhere in the solution.
        var allOfferProjects = doc.Descendants()
            .Where(e => e.Name.LocalName == "Project")
            .Select(e => e.Attribute("Path")!.Value.Replace('\\', '/'))
            .Where(p => p.Contains("/Offer/", StringComparison.Ordinal))
            .ToArray();
        Assert.Equal(allOfferProjects.Length, allOfferProjects.Distinct(StringComparer.Ordinal).Count());
    }

    private static string[] ReadOfferProjectNames()
    {
        using var doc = ReadManifest();
        return SingleOfferEntry(doc).GetProperty("projects").EnumerateArray()
            .Select(p => p.GetProperty("projectName").GetString()!)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
    }

    private static JsonElement SingleOfferEntry(JsonDocument doc)
    {
        var entries = doc.RootElement.GetProperty("modules").EnumerateArray()
            .Where(m => string.Equals(m.GetProperty("module").GetString(), ModuleName, StringComparison.Ordinal))
            .ToArray();
        Assert.Single(entries);
        return entries[0];
    }

    private static IEnumerable<string> ProductionProjectDirectories() =>
        Directory.GetDirectories(ModulesOfferRoot())
            .Where(dir =>
            {
                var name = Path.GetFileName(dir);
                return name.StartsWith("Tooba.Offer.", StringComparison.Ordinal)
                    && !name.EndsWith(".Tests", StringComparison.Ordinal);
            });

    private static string ModulesOfferRoot() =>
        Path.Combine(RepoRoot(), "src", "backend", "Modules", ModuleName);

    private static JsonDocument ReadManifest() =>
        JsonDocument.Parse(File.ReadAllText(Path.Combine(
            RepoRoot(), ManifestRelativePath.Replace('/', Path.DirectorySeparatorChar))));

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
