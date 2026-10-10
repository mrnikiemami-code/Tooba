using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-WISHLIST-AMSC-001-W2 — structure wave durable guard.
/// Locks the capability-first shallow Wishlist tree, the exact path↔namespace boundary, the enforced
/// per-project root allowlists, the canonical <c>/Modules/Wishlist/</c> solution grouping, the clean
/// physical copies, the INTERNAL move of the directory into <c>Persistence/</c> and of the development
/// seed into <c>Adapters/</c> (no invented ceremony folder), the split of the merged validator bundle into
/// one cohesive file per request under the shared <c>Validation/</c> seam, and the manifest structure
/// authority recorded for the module. Behavior and schema are untouched.
/// </summary>
public sealed class WishlistModuleAmsc001W2StructureGuardTests
{
    private const string ModuleRootRelative = "src/backend/Modules/Wishlist";

    private static readonly string[] ProductionProjects =
    [
        "Tooba.Wishlist.Contracts",
        "Tooba.Wishlist.Domain",
        "Tooba.Wishlist.Application",
        "Tooba.Wishlist.Infrastructure",
        "Tooba.Wishlist.Endpoints",
    ];

    [Fact]
    public void Application_is_capability_first_and_has_no_single_file_request_leaf_folders()
    {
        var application = Path.Combine(Repo(), ModuleRootRelative, "Tooba.Wishlist.Application");

        // Capability-first primary axis with the technical axes secondary and shared seams at root.
        Assert.True(Directory.Exists(Path.Combine(application, "Customer", "Commands")));
        Assert.True(Directory.Exists(Path.Combine(application, "Customer", "Queries")));
        Assert.True(Directory.Exists(Path.Combine(application, "Composition")));
        Assert.True(Directory.Exists(Path.Combine(application, "Models")));
        Assert.True(Directory.Exists(Path.Combine(application, "Ports")));
        Assert.True(Directory.Exists(Path.Combine(application, "Validation")));

        // The retired technical-axis-first request tree must stay retired.
        Assert.False(Directory.Exists(Path.Combine(application, "Commands")));
        Assert.False(Directory.Exists(Path.Combine(application, "Queries")));

        // A single-file folder is only acceptable for a technical axis or a shared seam (Commands,
        // Queries, Models, Ports, Validators, Validation, Composition). A folder named after one
        // request/use case is a single-file request leaf and must not exist.
        string[] axisOrSeam = ["Commands", "Queries", "Models", "Ports", "Validators", "Validation", "Composition"];
        foreach (var dir in Directory.EnumerateDirectories(application, "*", SearchOption.AllDirectories))
        {
            var name = Path.GetFileName(dir);
            if (name is "bin" or "obj" or "artifacts") continue;
            var files = Directory.GetFiles(dir, "*.cs", SearchOption.TopDirectoryOnly);
            if (files.Length == 1 && !axisOrSeam.Contains(name, StringComparer.Ordinal))
            {
                Assert.Fail($"single-file request/use-case leaf folder: {dir}");
            }
        }

        // No use-case-named leaf folder survives anywhere under the capability tree.
        var requestLeaves = Directory.EnumerateDirectories(application, "*", SearchOption.AllDirectories)
            .Where(dir => dir.Contains($"{Path.DirectorySeparatorChar}Commands{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                          || dir.Contains($"{Path.DirectorySeparatorChar}Queries{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        Assert.Empty(requestLeaves);
    }

    [Fact]
    public void Transport_validators_are_one_cohesive_file_per_request_under_the_shared_seam()
    {
        var validation = Path.Combine(Repo(), ModuleRootRelative, "Tooba.Wishlist.Application", "Validation");

        // The former merged Customer/Validators/WishlistValidators.cs bundle is retired: each transport
        // validator now has its own cohesive file and the capability folder no longer owns validators.
        Assert.False(Directory.Exists(Path.Combine(
            Repo(), ModuleRootRelative, "Tooba.Wishlist.Application", "Customer", "Validators")));

        Assert.Equal(
            new[]
            {
                "AddWishlistItemCommandValidator.cs",
                "GetWishlistMembershipQueryValidator.cs",
                "RemoveWishlistItemCommandValidator.cs",
            },
            Directory.GetFiles(validation, "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName!)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray());

        foreach (var file in Directory.GetFiles(validation, "*.cs", SearchOption.TopDirectoryOnly))
        {
            var text = File.ReadAllText(file);
            var name = Path.GetFileNameWithoutExtension(file);
            Assert.Contains($"namespace Tooba.Wishlist.Application.Validation;", text, StringComparison.Ordinal);
            Assert.Contains($"public sealed class {name} : AbstractValidator<", text, StringComparison.Ordinal);
            Assert.Contains("WishlistErrorCodes.", text, StringComparison.Ordinal);
            Assert.DoesNotContain("WithMessage(", text, StringComparison.Ordinal);
        }

        // The exempt request must stay validator-free (W0/W1 matrix preserved).
        Assert.DoesNotContain(
            "ListWishlistPageQuery",
            string.Join("\n", Directory.GetFiles(validation, "*.cs", SearchOption.TopDirectoryOnly).Select(File.ReadAllText)),
            StringComparison.Ordinal);
    }

    [Fact]
    public void Infrastructure_uses_Persistence_for_the_directory_and_Adapters_for_the_seed()
    {
        var infrastructure = Path.Combine(Repo(), ModuleRootRelative, "Tooba.Wishlist.Infrastructure");

        Assert.True(File.Exists(Path.Combine(infrastructure, "Persistence", "WishlistDirectory.cs")));
        Assert.True(File.Exists(Path.Combine(infrastructure, "Persistence", "WishlistDbContext.cs")));
        Assert.True(File.Exists(Path.Combine(infrastructure, "Persistence", "WishlistOutboxRegistration.cs")));
        Assert.True(File.Exists(Path.Combine(infrastructure, "Persistence", "Migrations", "20260825162434_InitialWishlist.cs")));
        Assert.True(File.Exists(Path.Combine(infrastructure, "Adapters", "WishlistDevelopmentSeed.cs")));
        Assert.True(File.Exists(Path.Combine(infrastructure, "WishlistModule.cs")));

        // The retired technical-axis roots must stay retired (no invented ceremony folder).
        Assert.False(Directory.Exists(Path.Combine(infrastructure, "Directories")));
        Assert.False(Directory.Exists(Path.Combine(infrastructure, "Development")));
        Assert.False(Directory.Exists(Path.Combine(infrastructure, "Migrations")));

        // The directory keeps the Persistence namespace identity (exact path↔namespace) next to its own
        // DbContext, and the seed keeps the Adapters namespace identity (the certified Support/Wallet shape).
        var directory = File.ReadAllText(Path.Combine(infrastructure, "Persistence", "WishlistDirectory.cs"));
        Assert.Contains("namespace Tooba.Wishlist.Infrastructure.Persistence;", directory, StringComparison.Ordinal);
        Assert.Contains("public sealed class WishlistDirectory", directory, StringComparison.Ordinal);
        Assert.Contains("IWishlistDirectory", directory, StringComparison.Ordinal);
        Assert.Contains("IWishlistCountPort", directory, StringComparison.Ordinal);

        var seed = File.ReadAllText(Path.Combine(infrastructure, "Adapters", "WishlistDevelopmentSeed.cs"));
        Assert.Contains("namespace Tooba.Wishlist.Infrastructure.Adapters;", seed, StringComparison.Ordinal);
        Assert.Contains("ICatalogDevelopmentPublishedProductSampler", seed, StringComparison.Ordinal);
        Assert.DoesNotContain("CatalogDbContext", seed, StringComparison.Ordinal);

        // The composition root consumes the new namespaces; no Directories/Development using survives.
        var module = File.ReadAllText(Path.Combine(infrastructure, "WishlistModule.cs"));
        Assert.Contains("using Tooba.Wishlist.Infrastructure.Persistence;", module, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Wishlist.Infrastructure.Directories", module, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Wishlist.Infrastructure.Development", module, StringComparison.Ordinal);
    }

    [Fact]
    public void Path_namespace_alignment_is_exact_for_every_wishlist_production_file()
    {
        var violations = new List<string>();
        foreach (var project in ProductionProjects)
        {
            var root = Path.GetFullPath(Path.Combine(Repo(), ModuleRootRelative, project));
            foreach (var file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                var relative = file[root.Length..].TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (relative.StartsWith("obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                    || relative.StartsWith("bin" + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                    || relative.StartsWith("artifacts" + Path.DirectorySeparatorChar, StringComparison.Ordinal))
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
                if (!match.Success)
                {
                    violations.Add($"{project}/{relative}: no namespace");
                    continue;
                }

                if (!string.Equals(expected, match.Groups[1].Value, StringComparison.Ordinal))
                {
                    violations.Add($"{project}/{relative}: ns={match.Groups[1].Value} expected={expected}");
                }
            }
        }

        Assert.True(violations.Count == 0, string.Join("\n", violations));
    }

    [Fact]
    public void Solution_grouping_is_the_canonical_modules_wishlist_folder_with_five_projects()
    {
        var slnx = File.ReadAllText(Path.Combine(Repo(), "src", "backend", "Tooba.slnx"));
        var match = Regex.Match(slnx, "<Folder Name=\"/Modules/Wishlist/\">(?<body>.*?)</Folder>", RegexOptions.Singleline);
        Assert.True(match.Success, "/Modules/Wishlist/ solution folder missing");
        var body = match.Groups["body"].Value;

        foreach (var project in ProductionProjects)
        {
            var occurrences = Regex.Matches(body, Regex.Escape($"Modules/Wishlist/{project}/{project}.csproj")).Count;
            Assert.Equal(1, occurrences);
        }

        // Wishlist is a production module with no module-local Tests project on disk, so no
        // Tooba.Wishlist.Tests entry may be invented in the solution.
        Assert.Equal(5, Regex.Matches(slnx, @"Modules/Wishlist/Tooba\.Wishlist\.[A-Za-z]+/").Count);
        Assert.DoesNotContain("Tooba.Wishlist.Tests", slnx, StringComparison.Ordinal);
    }

    [Fact]
    public void Root_allowlists_are_enforced_and_match_disk()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            Repo(), "docs", "architecture", "tmar-module-structure-manifests.json")));
        var wishlist = doc.RootElement.GetProperty("modules").EnumerateArray()
            .Single(m => string.Equals(m.GetProperty("module").GetString(), "Wishlist", StringComparison.Ordinal));

        var root = Path.Combine(Repo(), ModuleRootRelative);
        var projects = wishlist.GetProperty("projects").EnumerateArray()
            .ToDictionary(p => p.GetProperty("projectName").GetString()!, p => p);
        Assert.Equal(5, projects.Count);

        foreach (var (project, entry) in projects)
        {
            var projectPath = Path.Combine(root, project);
            Assert.True(Directory.Exists(projectPath), project);

            var allowlist = entry.GetProperty("rootAllowlist").EnumerateArray()
                .Select(x => x.GetString()!)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray();
            var actual = Directory.GetFiles(projectPath, "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName!)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray();
            Assert.Equal(allowlist, actual);

            foreach (var forbiddenRoot in entry.GetProperty("forbiddenRootFiles").EnumerateArray())
            {
                Assert.False(
                    File.Exists(Path.Combine(projectPath, forbiddenRoot.GetString()!)),
                    $"{project} still has forbidden root file {forbiddenRoot.GetString()}");
            }

            foreach (var forbiddenFolder in entry.GetProperty("forbiddenTopLevelFolders").EnumerateArray())
            {
                Assert.False(
                    Directory.Exists(Path.Combine(projectPath, forbiddenFolder.GetString()!)),
                    $"{project} still has forbidden top-level folder {forbiddenFolder.GetString()}");
            }
        }
    }

    [Fact]
    public void Manifest_records_wishlist_structure_authority_with_disk_accurate_allowlists()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            Repo(), "docs", "architecture", "tmar-module-structure-manifests.json")));

        var wishlist = doc.RootElement.GetProperty("modules").EnumerateArray()
            .Single(m => string.Equals(m.GetProperty("module").GetString(), "Wishlist", StringComparison.Ordinal));

        Assert.True(wishlist.GetProperty("structureCertified").GetBoolean());
        Assert.Equal("ARCH-COMPLETE-002", wishlist.GetProperty("lockVersion").GetString());
        Assert.Equal("TB-TMAR-WISHLIST-AMSC-001-W2", wishlist.GetProperty("structureAuthorityTask").GetString());
        Assert.Equal("READY_FOR_CERTIFY_CONSUMED_BY_W3", wishlist.GetProperty("structureHandoffState").GetString());

        var infra = wishlist.GetProperty("projects").EnumerateArray()
            .Single(p => p.GetProperty("projectName").GetString() == "Tooba.Wishlist.Infrastructure");
        Assert.Equal(
            new[] { "Development", "Directories", "Migrations" },
            infra.GetProperty("forbiddenTopLevelFolders").EnumerateArray()
                .Select(x => x.GetString()!)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray());

        // The global lock is untouched: Wishlist stays a single member of the 32-module certified set.
        using var state = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            Repo(), "docs", "architecture", "tmar-current-state.json")));
        var certified = state.RootElement.GetProperty("structureLock").GetProperty("certifiedModules")
            .EnumerateArray().Select(x => x.GetString()!).ToArray();
        Assert.Equal(32, certified.Length);
        Assert.Equal(1, certified.Count(x => x == "Wishlist"));
    }

    [Fact]
    public void Wishlist_w2_sot_record_records_the_structure_handoff()
    {
        using var state = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            Repo(), "docs", "architecture", "tmar-current-state.json")));
        var w2 = state.RootElement.GetProperty("wishlistAmsc001W2");
        Assert.Equal("TB-TMAR-WISHLIST-AMSC-001-W2", w2.GetProperty("task").GetString());
        Assert.Equal("tooba-architecture-structure", w2.GetProperty("skill").GetString());
        Assert.Equal("READY_FOR_CERTIFY", w2.GetProperty("structureState").GetString());
        Assert.Equal("EXACT", w2.GetProperty("pathNamespaceState").GetString());
        Assert.Equal("PROFESSIONAL_SHALLOW", w2.GetProperty("folderGranularityState").GetString());
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
