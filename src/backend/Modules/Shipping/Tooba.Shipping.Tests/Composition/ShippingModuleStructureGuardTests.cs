using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Shipping.Tests.Composition;

/// <summary>
/// TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W2 — durable, Shipping-scoped physical-structure guard.
/// </summary>
/// <remarks>
/// Scope is deliberately limited to the six existing Shipping projects. It machine-checks only the
/// structural facts the Structure skill owns: physical project roots against the certified manifest
/// entry (root allowlist, forbidden root files, forbidden top-level folders), capability-first folders
/// without unjustified single-file request leaves, exact path&lt;-&gt;namespace alignment, the single root
/// composition entry, canonical solution membership and stale/duplicate copies.
/// It does not certify the module, its business behavior, its validators or its localization semantics.
/// </remarks>
public sealed class ShippingModuleStructureGuardTests
{
    private const string ModuleRoot = "src/backend/Modules/Shipping";

    /// <summary>Production projects only; the test project is guarded by the solution-membership check.</summary>
    private static readonly string[] ProductionProjects =
    [
        "Tooba.Shipping.Application",
        "Tooba.Shipping.Domain",
        "Tooba.Shipping.Contracts",
        "Tooba.Shipping.Infrastructure",
        "Tooba.Shipping.Endpoints",
    ];

    /// <summary>Capability branches proven by the W0/W2 capability map, plus the TB-188 additive read.</summary>
    private static readonly string[] ApplicationCapabilities =
        ["Authoring", "Operations", "Packaging", "Rates", "Scheduling", "Validation"];

    /// <summary>Secondary (technical) axes are allowed only underneath a capability branch.</summary>
    private static readonly string[] ApplicationSecondaryAxes =
        ["Commands", "Queries", "Ports", "ReadModels", "Models", "Validation"];

    /// <summary>Audience/capability folders plus the shared error/resource homes.</summary>
    private static readonly string[] EndpointsAudienceFolders = ["Admin", "Errors", "Resources"];

    [Fact]
    public void Manifest_entry_is_certified_with_the_shipping_certification_lineage()
    {
        var entry = ShippingManifestEntry(out var raw);

        // W2's stage assertion (structureCertified == false) described the pre-certify lifecycle only.
        // TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W3 promoted it, then the Architect REJECTED that promotion
        // (no IModuleCallTracer child span on the two outbound Contracts reads; "non-blocking residual"
        // was refused). TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W3-R1 applied the bounded trace repair and is
        // now the corrective certification authority; W3 stays in history as rejected/superseded.
        Assert.True(
            entry.GetProperty("structureCertified").GetBoolean(),
            "Shipping must carry the certified structure state after the W3-R1 corrective trace repair.");
        Assert.Equal("ARCH-COMPLETE-002", entry.GetProperty("lockVersion").GetString());
        Assert.Equal("COMPLETE_REFERENCE_PATTERN", entry.GetProperty("certificationState").GetString());
        Assert.Equal(
            "TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W3-R1",
            entry.GetProperty("certificationAuthorityTask").GetString());
        Assert.Equal(
            "docs/architecture/evidence/TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W3-R1/RESULT.md",
            entry.GetProperty("evidence").GetString());

        // Corrective provenance must stay pinned: W3's rejection is not silently erased.
        var history = entry.GetProperty("certificationHistory").GetString()!;
        Assert.Contains("TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W3_", history, StringComparison.Ordinal);
        Assert.Contains("REJECTED", history, StringComparison.Ordinal);
        Assert.Contains("W3-R1", history, StringComparison.Ordinal);
        Assert.Contains(
            "IModuleCallTracer",
            entry.GetProperty("certificationCorrection").GetString()!,
            StringComparison.Ordinal);

        // Honest lineage: the certification must cite the structure handoff it certifies over.
        Assert.Contains(
            "TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W2",
            entry.GetProperty("structureAuthorityTask").GetString()!,
            StringComparison.Ordinal);
        Assert.Equal(
            "READY_FOR_CERTIFY",
            entry.GetProperty("structureHandoffState").GetString());

        var declared = entry.GetProperty("projects")
            .EnumerateArray()
            .Select(project => project.GetProperty("projectName").GetString()!)
            .ToArray();
        Assert.Equal(ProductionProjects, declared);

        // Every declared project must exist on disk as a real .csproj; no phantom manifest project.
        foreach (var project in declared)
        {
            Assert.True(
                File.Exists(Path.Combine(ProjectDirectory(project), $"{project}.csproj")),
                $"manifest project {project} has no csproj on disk");
        }

        // Exactly one Shipping entry; promotion must not leave a temporary pre-cert duplicate behind.
        Assert.Equal(1, Regex.Matches(raw, "\"module\": \"Shipping\"").Count);
    }

    [Fact]
    public void Root_allowlists_and_forbidden_paths_match_the_real_physical_roots()
    {
        var entry = ShippingManifestEntry(out _);

        foreach (var project in entry.GetProperty("projects").EnumerateArray())
        {
            var name = project.GetProperty("projectName").GetString()!;
            var directory = ProjectDirectory(name);
            var actualRootFiles = Directory
                .EnumerateFiles(directory, "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .OrderBy(file => file, StringComparer.Ordinal)
                .ToArray();

            var rootAllowlist = project.GetProperty("rootAllowlist")
                .EnumerateArray()
                .Select(item => item.GetString()!)
                .OrderBy(file => file, StringComparer.Ordinal)
                .ToArray();
            Assert.Equal(rootAllowlist, actualRootFiles);

            foreach (var forbidden in project.GetProperty("forbiddenRootFiles").EnumerateArray())
            {
                Assert.False(
                    File.Exists(Path.Combine(directory, forbidden.GetString()!)),
                    $"{name} has a forbidden root file: {forbidden.GetString()}");
            }

            foreach (var forbiddenFolder in project.GetProperty("forbiddenTopLevelFolders").EnumerateArray())
            {
                Assert.False(
                    Directory.Exists(Path.Combine(directory, forbiddenFolder.GetString()!)),
                    $"{name} has a forbidden top-level folder: {forbiddenFolder.GetString()}");
            }
        }
    }

    [Fact]
    public void Physical_tree_is_capability_first_without_single_file_request_leaves()
    {
        var application = ProjectDirectory("Tooba.Shipping.Application");
        foreach (var directory in PhysicalDirectories(application))
        {
            var capability = Path.GetFileName(directory);
            Assert.Contains(capability, ApplicationCapabilities);

            foreach (var axis in PhysicalDirectories(directory))
            {
                Assert.Contains(Path.GetFileName(axis), ApplicationSecondaryAxes);
            }
        }

        var endpoints = ProjectDirectory("Tooba.Shipping.Endpoints");
        foreach (var directory in PhysicalDirectories(endpoints))
        {
            Assert.Contains(Path.GetFileName(directory), EndpointsAudienceFolders);
        }

        // A directory that exists only to wrap one source file named after itself is a use-case leaf.
        var requestLeaves = new List<string>();
        foreach (var project in new[] { "Tooba.Shipping.Application", "Tooba.Shipping.Endpoints" })
        {
            var projectDirectory = ProjectDirectory(project);
            foreach (var relative in ProductionFiles(project))
            {
                var directory = Path.GetDirectoryName(relative);
                if (string.IsNullOrEmpty(directory))
                {
                    continue;
                }

                var folder = Path.GetFileName(directory);
                var stem = Path.GetFileNameWithoutExtension(relative);
                var siblings = Directory.EnumerateFiles(Path.Combine(projectDirectory, directory), "*.cs").Count();
                if (siblings == 1 && stem.StartsWith(folder, StringComparison.Ordinal))
                {
                    requestLeaves.Add($"{project}/{relative}");
                }
            }
        }

        Assert.Empty(requestLeaves);
    }

    [Fact]
    public void Every_production_namespace_equals_its_path_derived_namespace()
    {
        var mismatches = new List<string>();
        foreach (var project in ProductionProjects)
        {
            foreach (var relative in ProductionFiles(project))
            {
                // Locked EF exemption: generated designer/snapshot files under Persistence/Migrations.
                if (IsMigrationExempt(relative))
                {
                    continue;
                }

                var expected = ExpectedNamespace(project, relative);
                var match = NamespacePattern().Match(File.ReadAllText(Path.Combine(ProjectDirectory(project), relative)));
                var actual = match.Success ? match.Groups["ns"].Value : "<missing>";
                if (!string.Equals(expected, actual, StringComparison.Ordinal))
                {
                    mismatches.Add($"{project}/{relative}: expected {expected} but found {actual}");
                }
            }
        }

        Assert.Empty(mismatches);
    }

    [Fact]
    public void Endpoints_composition_entry_lives_at_the_root_and_nowhere_else()
    {
        var endpoints = ProjectDirectory("Tooba.Shipping.Endpoints");
        var compositionPath = Path.Combine(endpoints, "ShippingEndpointModule.cs");
        Assert.True(File.Exists(compositionPath), "canonical root composition entry is missing");

        var composition = File.ReadAllText(compositionPath);
        Assert.Contains("namespace Tooba.Shipping.Endpoints;", composition, StringComparison.Ordinal);
        Assert.Contains("public static class ShippingEndpointModule", composition, StringComparison.Ordinal);

        // Behavior preserved verbatim by the extraction: one admin group, six capability mappers.
        // The sixth (Authoring) is the additive TB-188 admin read; the count is pinned so a mapper
        // cannot be added or dropped silently, and each mapper name is asserted explicitly.
        Assert.Equal(1, CountOccurrences(composition, "app.MapGroup(\"/v1/admin\")"));
        Assert.Equal(6, CountOccurrences(composition, "Endpoints.Map(admin);"));
        Assert.Contains("ShippingRateCoverageEndpoints.Map(admin);", composition, StringComparison.Ordinal);
        Assert.Contains("ShippingMethodFinancialStatusEndpoints.Map(admin);", composition, StringComparison.Ordinal);
        Assert.Contains("ShippingOperatingScheduleEndpoints.Map(admin);", composition, StringComparison.Ordinal);
        Assert.Contains("ShippingPackagingPolicyEndpoints.Map(admin);", composition, StringComparison.Ordinal);
        Assert.Contains("ShippingOperationalQuoteEndpoints.Map(admin);", composition, StringComparison.Ordinal);
        Assert.Contains("ShippingAuthoringContextEndpoints.Map(admin);", composition, StringComparison.Ordinal);
        Assert.Contains("IErrorCatalogContributor, ShippingErrorCatalogContributor", composition, StringComparison.Ordinal);
        Assert.Contains("IErrorResourceSet, ShippingErrorResourceSet", composition, StringComparison.Ordinal);

        // No second live home for the composition class and no admin-group mapping outside it.
        var duplicateHomes = ProductionFiles("Tooba.Shipping.Endpoints")
            .Where(relative => !string.Equals(relative, "ShippingEndpointModule.cs", StringComparison.Ordinal))
            .Where(relative => File.ReadAllText(Path.Combine(endpoints, relative))
                .Contains("class ShippingEndpointModule", StringComparison.Ordinal))
            .ToArray();
        Assert.Empty(duplicateHomes);

        var foreignGroups = ProductionFiles("Tooba.Shipping.Endpoints")
            .Where(relative => !string.Equals(relative, "ShippingEndpointModule.cs", StringComparison.Ordinal))
            .Where(relative => File.ReadAllText(Path.Combine(endpoints, relative))
                .Contains("MapGroup(\"/v1/admin\")", StringComparison.Ordinal))
            .ToArray();
        Assert.Empty(foreignGroups);

        // The former Admin-resident composition defect must stay repaired.
        var admin = File.ReadAllText(Path.Combine(endpoints, "Admin", "ShippingRateCoverageEndpoints.cs"));
        Assert.DoesNotContain("class ShippingEndpointModule", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("AddShippingEndpointPresentation", admin, StringComparison.Ordinal);
    }

    [Fact]
    public void Solution_membership_is_canonical_and_unique()
    {
        var slnx = File.ReadAllText(Path.Combine(RepoRoot(), "src", "backend", "Tooba.slnx"));
        var group = Regex.Match(
            slnx,
            "<Folder Name=\"/Modules/Shipping/\">(?<body>.*?)</Folder>",
            RegexOptions.Singleline | RegexOptions.CultureInvariant);
        Assert.True(group.Success, "/Modules/Shipping/ solution group is missing");

        var members = Regex.Matches(group.Groups["body"].Value, "Project Path=\"(?<path>[^\"]+)\"")
            .Select(match => match.Groups["path"].Value)
            .ToArray();

        var expected = ProductionProjects
            .Append("Tooba.Shipping.Tests")
            .Select(project => $"Modules/Shipping/{project}/{project}.csproj")
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(expected, members.OrderBy(path => path, StringComparer.Ordinal).ToArray());

        foreach (var project in expected)
        {
            var count = Regex.Matches(slnx, Regex.Escape(project)).Count;
            Assert.Equal(1, count);
            Assert.True(
                File.Exists(Path.Combine(RepoRoot(), "src", "backend", project.Replace('/', Path.DirectorySeparatorChar))),
                $"{project} missing on disk");
        }
    }

    [Fact]
    public void No_stale_or_duplicate_production_copy_exists()
    {
        foreach (var project in ProductionProjects)
        {
            var duplicates = ProductionFiles(project)
                .Where(relative => !IsMigrationExempt(relative))
                .GroupBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => $"{project}: {string.Join(", ", group)}")
                .ToArray();
            Assert.Empty(duplicates);
        }
    }

    private static JsonElement ShippingManifestEntry(out string raw)
    {
        var path = Path.Combine(RepoRoot(), "docs", "architecture", "tmar-module-structure-manifests.json");
        raw = File.ReadAllText(path);
        using var document = JsonDocument.Parse(raw);
        foreach (var module in document.RootElement.GetProperty("modules").EnumerateArray())
        {
            if (string.Equals(module.GetProperty("module").GetString(), "Shipping", StringComparison.Ordinal))
            {
                return module.Clone();
            }
        }

        throw new InvalidOperationException("Shipping has no structure manifest entry");
    }

    private static string[] ProductionFiles(string project) =>
        Directory
            .EnumerateFiles(ProjectDirectory(project), "*.cs", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(ProjectDirectory(project), path))
            .Where(relative => !IsGenerated(relative))
            .OrderBy(relative => relative, StringComparer.Ordinal)
            .ToArray();

    private static bool IsGenerated(string relative) =>
        relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Any(segment => segment is "obj" or "bin");

    /// <summary>Source-tree directories only; build output is not part of the physical structure.</summary>
    private static IEnumerable<string> PhysicalDirectories(string directory) =>
        Directory
            .EnumerateDirectories(directory)
            .Where(child => !IsGenerated(Path.GetRelativePath(directory, child)));

    private static bool IsMigrationExempt(string relative) =>
        relative.Replace('\\', '/').Contains("Persistence/Migrations/", StringComparison.Ordinal);

    private static string ExpectedNamespace(string project, string relative)
    {
        var directory = Path.GetDirectoryName(relative);
        return string.IsNullOrEmpty(directory)
            ? project
            : $"{project}.{directory.Replace(Path.DirectorySeparatorChar, '.')}";
    }

    private static string ProjectDirectory(string project) =>
        Path.Combine(RepoRoot(), ModuleRoot.Replace('/', Path.DirectorySeparatorChar), project);

    private static Regex NamespacePattern() =>
        new(@"^namespace\s+(?<ns>[A-Za-z0-9_.]+)", RegexOptions.Multiline | RegexOptions.CultureInvariant);

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        var index = haystack.IndexOf(needle, StringComparison.Ordinal);
        while (index >= 0)
        {
            count++;
            index = haystack.IndexOf(needle, index + needle.Length, StringComparison.Ordinal);
        }

        return count;
    }

    private static string RepoRoot()
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
