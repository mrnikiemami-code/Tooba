using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-STORY-AMSC-001-W2 — structure wave durable guard.
/// Locks the canonical physical layout of the touched Story surface: the Infrastructure composition
/// entry under <c>DependencyInjection/</c>, the outbox registration split into <c>Messaging/</c>, the
/// plural <c>Directories/</c> home, the shallow <c>Application/Composition/</c> fault seam, project
/// root allowlists that match disk, exact path to namespace alignment, no use-case-named single-file
/// request leaf, no technical-axis-first tree, the canonical <c>/Modules/Story/</c> solution group and
/// a manifest that matches disk.
/// </summary>
public sealed class StoryModuleAmsc001W2StructureGuardTests
{
    private const string StoryRootRelative = "src/backend/Modules/Story";

    private static readonly string[] ProductionProjects =
    [
        "Tooba.Story.Contracts",
        "Tooba.Story.Domain",
        "Tooba.Story.Application",
        "Tooba.Story.Infrastructure",
        "Tooba.Story.Endpoints",
    ];

    [Fact]
    public void Infrastructure_composition_entry_outbox_and_directories_are_canonically_foldered()
    {
        var infra = Path.Combine(Repo(), StoryRootRelative, "Tooba.Story.Infrastructure");

        Assert.True(File.Exists(Path.Combine(infra, "DependencyInjection", "StoryModule.cs")));
        Assert.True(File.Exists(Path.Combine(infra, "Messaging", "StoryOutboxRegistration.cs")));
        Assert.True(File.Exists(Path.Combine(infra, "Directories", "StoryDirectory.cs")));
        Assert.True(Directory.Exists(Path.Combine(infra, "Persistence", "Migrations")));

        // The pre-structure layout must not resurrect.
        Assert.False(File.Exists(Path.Combine(infra, "StoryModule.cs")));
        Assert.False(Directory.Exists(Path.Combine(infra, "Directory")));
        Assert.False(Directory.Exists(Path.Combine(infra, "Migrations")));

        // The outbox registration is its own cohesive file, not a second class in the composition entry.
        var module = File.ReadAllText(Path.Combine(infra, "DependencyInjection", "StoryModule.cs"));
        Assert.DoesNotContain("class StoryOutboxRegistration", module, StringComparison.Ordinal);
        Assert.Contains("StoryOutboxRegistration", module, StringComparison.Ordinal);

        var outbox = File.ReadAllText(Path.Combine(infra, "Messaging", "StoryOutboxRegistration.cs"));
        Assert.Contains("namespace Tooba.Story.Infrastructure.Messaging;", outbox, StringComparison.Ordinal);
    }

    [Fact]
    public void Application_fault_seam_is_shallow_and_no_technical_axis_first_tree_exists()
    {
        var app = Path.Combine(Repo(), StoryRootRelative, "Tooba.Story.Application");

        Assert.True(File.Exists(Path.Combine(app, "Composition", "StoryOperation.cs")));
        Assert.False(Directory.Exists(Path.Combine(app, "Stories", "Composition")));

        // Capability is the primary axis: no root-level technical request tree.
        foreach (var forbidden in new[] { "Commands", "Queries", "Validators", "Models", "Ports" })
        {
            Assert.False(Directory.Exists(Path.Combine(app, forbidden)), $"Application/{forbidden} is a technical-axis root");
        }

        // Exactly one shared composition home; the fault seam lives there.
        Assert.Single(Directory.GetFiles(Path.Combine(app, "Composition"), "*.cs"));
    }

    /// <summary>
    /// A folder named like a single use case that wraps exactly one production source file is
    /// over-foldering. Audience-grouped folders under a request axis are the canonical precedent and
    /// are explicitly allowed.
    /// </summary>
    [Fact]
    public void No_use_case_named_single_file_leaf_folder_exists_under_the_Stories_capability()
    {
        var stories = Path.Combine(Repo(), StoryRootRelative, "Tooba.Story.Application", "Stories");
        var offenders = new List<string>();

        foreach (var directory in Directory.EnumerateDirectories(stories, "*", SearchOption.AllDirectories))
        {
            var name = Path.GetFileName(directory);
            var isUseCaseFolder = name.EndsWith("Command", StringComparison.Ordinal)
                || name.EndsWith("CommandHandler", StringComparison.Ordinal)
                || name.EndsWith("Query", StringComparison.Ordinal)
                || name.EndsWith("QueryHandler", StringComparison.Ordinal)
                || name.EndsWith("Request", StringComparison.Ordinal);
            if (!isUseCaseFolder)
            {
                continue;
            }

            var sources = Directory.GetFiles(directory, "*.cs", SearchOption.TopDirectoryOnly);
            if (sources.Length <= 1)
            {
                offenders.Add(Relative(directory) + $" ({sources.Length} source file)");
            }
        }

        Assert.Empty(offenders);
    }

    [Fact]
    public void Every_production_namespace_matches_its_physical_path()
    {
        var mismatches = new List<string>();
        var storyRoot = Path.Combine(Repo(), StoryRootRelative);

        foreach (var project in ProductionProjects)
        {
            var projectRoot = Path.Combine(storyRoot, project);
            var prefix = project;
            foreach (var file in Directory.EnumerateFiles(projectRoot, "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    || file.Contains($"{Path.DirectorySeparatorChar}artifacts{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    || file.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase)
                    || file.EndsWith("ModelSnapshot.cs", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var relative = Path.GetRelativePath(projectRoot, file);
                var segments = Path.GetDirectoryName(relative)?.Split(Path.DirectorySeparatorChar)
                    .Where(s => s.Length > 0) ?? [];
                var expected = segments.Aggregate(prefix, (acc, segment) => acc + "." + segment);

                var text = File.ReadAllText(file);
                var match = Regex.Match(text, @"^namespace\s+([A-Za-z0-9_.]+)\s*[;{]", RegexOptions.Multiline);
                if (!match.Success)
                {
                    continue;
                }

                if (!string.Equals(match.Groups[1].Value, expected, StringComparison.Ordinal))
                {
                    mismatches.Add($"{Relative(file)}: declared {match.Groups[1].Value}, expected {expected}");
                }
            }
        }

        Assert.Empty(mismatches);
    }

    [Fact]
    public void Project_root_allowlists_match_disk_and_match_the_manifest()
    {
        var storyRoot = Path.Combine(Repo(), StoryRootRelative);

        // No loose production source floats at a project root, except the single deliberate
        // Endpoints composition entry that the manifest keeps as an explicit allowlist.
        Assert.Empty(Directory.GetFiles(Path.Combine(storyRoot, "Tooba.Story.Contracts"), "*.cs", SearchOption.TopDirectoryOnly));
        Assert.Empty(Directory.GetFiles(Path.Combine(storyRoot, "Tooba.Story.Domain"), "*.cs", SearchOption.TopDirectoryOnly));
        Assert.Empty(Directory.GetFiles(Path.Combine(storyRoot, "Tooba.Story.Application"), "*.cs", SearchOption.TopDirectoryOnly));
        Assert.Empty(Directory.GetFiles(Path.Combine(storyRoot, "Tooba.Story.Infrastructure"), "*.cs", SearchOption.TopDirectoryOnly));
        Assert.Equal(
            ["StoryEndpointModule.cs"],
            Directory.GetFiles(Path.Combine(storyRoot, "Tooba.Story.Endpoints"), "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .OrderBy(n => n, StringComparer.Ordinal)
                .ToArray());

        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            Repo(), "docs", "architecture", "tmar-module-structure-manifests.json")));
        var story = doc.RootElement.GetProperty("modules").EnumerateArray()
            .Single(m => m.GetProperty("module").GetString() == "Story");
        Assert.True(story.GetProperty("structureCertified").GetBoolean());
        Assert.Equal("ARCH-COMPLETE-002", story.GetProperty("lockVersion").GetString());
        Assert.Contains("AMSC-001", story.GetProperty("certificationNote").GetString(), StringComparison.Ordinal);

        var expectedAllowlists = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["Tooba.Story.Contracts"] = [],
            ["Tooba.Story.Domain"] = [],
            ["Tooba.Story.Application"] = [],
            ["Tooba.Story.Infrastructure"] = [],
            ["Tooba.Story.Endpoints"] = ["StoryEndpointModule.cs"],
        };

        var seen = new List<string>();
        foreach (var project in story.GetProperty("projects").EnumerateArray())
        {
            var name = project.GetProperty("projectName").GetString()!;
            seen.Add(name);
            var allowlist = project.GetProperty("rootAllowlist").EnumerateArray()
                .Select(e => e.GetString()!)
                .OrderBy(n => n, StringComparer.Ordinal)
                .ToArray();
            Assert.Equal(expectedAllowlists[name].OrderBy(n => n, StringComparer.Ordinal).ToArray(), allowlist);
        }

        Assert.Equal(ProductionProjects.OrderBy(n => n, StringComparer.Ordinal), seen.OrderBy(n => n, StringComparer.Ordinal));
    }

    [Fact]
    public void Solution_group_contains_exactly_the_five_Story_projects_once_each()
    {
        var slnx = File.ReadAllText(Path.Combine(Repo(), "src", "backend", "Tooba.slnx"));
        Assert.Contains("<Folder Name=\"/Modules/Story/\">", slnx, StringComparison.Ordinal);

        var start = slnx.IndexOf("<Folder Name=\"/Modules/Story/\">", StringComparison.Ordinal);
        var end = slnx.IndexOf("</Folder>", start, StringComparison.Ordinal);
        Assert.True(end > start, "Story solution folder is not closed");
        var block = slnx[start..end];

        foreach (var project in ProductionProjects)
        {
            var needle = $"Modules/Story/{project}/{project}.csproj";
            var count = Regex.Matches(block, Regex.Escape(needle)).Count;
            Assert.Equal(1, count);
        }

        Assert.Equal(5, Regex.Matches(block, @"<Project ").Count);
    }

    [Fact]
    public void SoT_records_the_AMSC_001_W2_structure_wave()
    {
        var sot = File.ReadAllText(Path.Combine(Repo(), "docs", "architecture", "tmar-current-state.json"));
        Assert.Contains("\"storyAmsc001W2\"", sot, StringComparison.Ordinal);
        Assert.Contains("STRUCTURE_COMPLETE", sot, StringComparison.Ordinal);
        Assert.Contains("READY_FOR_CERTIFY", sot, StringComparison.Ordinal);
    }

    private static string Relative(string absolute) =>
        absolute.Replace(Repo() + Path.DirectorySeparatorChar, string.Empty).Replace('\\', '/');

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
