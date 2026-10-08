using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-RETURNS-AMSC-001-W2 — structure wave durable guard.
/// Locks the capability-first shallow Application layout (no technical-axis roots, no single-file
/// use-case leaf folders), exact path↔namespace alignment, root allowlists, clean physical copies,
/// the canonical <c>/Modules/Returns/</c> solution grouping, and the honest pre-certification
/// manifest record (structureCertified stays false until the W3 Certify wave).
/// </summary>
public sealed class ReturnsModuleAmsc001W2StructureGuardTests
{
    private const string ModuleRootRelative = "src/backend/Modules/Returns";

    private static readonly string[] ProductionProjects =
    [
        "Tooba.Returns.Contracts",
        "Tooba.Returns.Domain",
        "Tooba.Returns.Application",
        "Tooba.Returns.Infrastructure",
        "Tooba.Returns.Endpoints",
    ];

    [Fact]
    public void Application_is_capability_first_shallow_without_technical_axis_roots()
    {
        var applicationRoot = Path.Combine(Repo(), ModuleRootRelative, "Tooba.Returns.Application");
        var topFolders = Directory.EnumerateDirectories(applicationRoot)
            .Select(Path.GetFileName)
            .Where(n => n is not "bin" and not "obj" and not "artifacts")
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(["Composition", "ReturnRequests", "Validation"], topFolders);

        foreach (var retired in new[] { "Commands", "Queries", "Models", "Ports", "Validators", "Errors", "Handlers", "Requests" })
        {
            Assert.False(
                Directory.Exists(Path.Combine(applicationRoot, retired)),
                $"Application/{retired} must stay retired (technical-axis-first root)");
        }

        var returnRequests = Path.Combine(applicationRoot, "ReturnRequests");
        var subFolders = Directory.EnumerateDirectories(returnRequests)
            .Select(Path.GetFileName)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(["Commands", "Models", "Ports", "Queries"], subFolders);
    }

    [Fact]
    public void No_single_file_use_case_leaf_folders_remain_in_the_request_axes()
    {
        var returnRequests = Path.Combine(Repo(), ModuleRootRelative, "Tooba.Returns.Application/ReturnRequests");
        foreach (var axis in new[] { "Commands", "Queries" })
        {
            var axisRoot = Path.Combine(returnRequests, axis);
            Assert.True(Directory.Exists(axisRoot), $"{axis} axis must exist");

            var leafFolders = Directory.EnumerateDirectories(axisRoot).ToArray();
            Assert.True(
                leafFolders.Length == 0,
                $"per-use-case leaf folders are over-foldering: {string.Join(", ", leafFolders.Select(Path.GetFileName))}");

            var sources = Directory.EnumerateFiles(axisRoot, "*.cs", SearchOption.TopDirectoryOnly).ToArray();
            Assert.NotEmpty(sources);
        }
    }

    [Fact]
    public void Path_namespace_alignment_is_exact_for_every_production_source()
    {
        var violations = new List<string>();
        foreach (var project in ProductionProjects)
        {
            var projectRoot = Path.Combine(Repo(), ModuleRootRelative, project);
            foreach (var file in Directory.EnumerateFiles(projectRoot, "*.cs", SearchOption.AllDirectories))
            {
                var normalized = file.Replace('\\', '/');
                if (normalized.Contains("/obj/", StringComparison.Ordinal)
                    || normalized.Contains("/bin/", StringComparison.Ordinal)
                    || normalized.Contains("/artifacts/", StringComparison.Ordinal)
                    || normalized.Contains("/Migrations/", StringComparison.OrdinalIgnoreCase)
                    || normalized.EndsWith("ModelSnapshot.cs", StringComparison.OrdinalIgnoreCase)
                    || normalized.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var relative = Path.GetRelativePath(projectRoot, file).Replace('\\', '/');
                var parts = relative.Split('/');
                var expected = project + (parts.Length > 1 ? "." + string.Join('.', parts[..^1]) : string.Empty);
                var declared = Regex.Match(File.ReadAllText(file), @"^namespace\s+([\w.]+)", RegexOptions.Multiline).Groups[1].Value;
                if (!string.Equals(declared, expected, StringComparison.Ordinal))
                {
                    violations.Add($"{project}/{relative}: ns={declared} expected={expected}");
                }
            }
        }

        Assert.True(violations.Count == 0, string.Join("\n", violations));
    }

    [Fact]
    public void Root_allowlists_match_disk_and_forbidden_entries_are_absent()
    {
        var manifest = LoadManifest();
        // Returns was promoted from preCertModules into the certified modules[] array by the W3
        // certify wave (structureCertified true); the structural allowlists below are unchanged.
        var entry = manifest.GetProperty("modules").EnumerateArray()
            .Single(x => x.GetProperty("module").GetString() == "Returns");

        foreach (var project in entry.GetProperty("projects").EnumerateArray())
        {
            var name = project.GetProperty("projectName").GetString()!;
            var projectRoot = Path.Combine(Repo(), ModuleRootRelative, name);

            var actualRoot = Directory.EnumerateFiles(projectRoot, "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .OrderBy(n => n, StringComparer.Ordinal)
                .ToArray();
            var allowlist = project.GetProperty("rootAllowlist").EnumerateArray()
                .Select(x => x.GetString()!)
                .OrderBy(n => n, StringComparer.Ordinal)
                .ToArray();
            Assert.Equal(allowlist, actualRoot);

            foreach (var forbidden in project.GetProperty("forbiddenRootFiles").EnumerateArray().Select(x => x.GetString()!))
            {
                Assert.False(File.Exists(Path.Combine(projectRoot, forbidden)), $"{name} forbidden root file present: {forbidden}");
            }

            foreach (var forbidden in project.GetProperty("forbiddenTopLevelFolders").EnumerateArray().Select(x => x.GetString()!))
            {
                Assert.False(Directory.Exists(Path.Combine(projectRoot, forbidden)), $"{name} forbidden top-level folder present: {forbidden}");
            }
        }
    }

    [Fact]
    public void Manifest_records_returns_as_pre_cert_not_certified()
    {
        var manifest = LoadManifest();

        Assert.False(
            manifest.GetProperty("uncertifiedHttpOwningModules").EnumerateArray()
                .Any(x => x.GetString() == "Returns"),
            "Returns must be removed from uncertifiedHttpOwningModules after the W2 structure wave");

        var entry = manifest.GetProperty("modules").EnumerateArray()
            .Single(x => x.GetProperty("module").GetString() == "Returns");
        Assert.True(entry.GetProperty("structureCertified").GetBoolean());
        Assert.Equal("ARCH-COMPLETE-002", entry.GetProperty("lockVersion").GetString());
        Assert.Equal(6, entry.GetProperty("projects").GetArrayLength());

        Assert.DoesNotContain(
            manifest.GetProperty("preCertModules").EnumerateArray(),
            x => x.GetProperty("module").GetString() == "Returns");
    }

    [Fact]
    public void Solution_explorer_grouping_is_canonical()
    {
        var slnx = File.ReadAllText(Path.Combine(Repo(), "src/backend/Tooba.slnx"));
        Assert.Contains("<Folder Name=\"/Modules/Returns/\">", slnx, StringComparison.Ordinal);

        var block = Regex.Match(slnx, @"<Folder Name=""/Modules/Returns/"">(?<body>.*?)</Folder>", RegexOptions.Singleline).Groups["body"].Value;
        foreach (var project in new[]
                 {
                     "Tooba.Returns.Domain", "Tooba.Returns.Contracts", "Tooba.Returns.Application",
                     "Tooba.Returns.Infrastructure", "Tooba.Returns.Endpoints", "Tooba.Returns.Tests",
                 })
        {
            Assert.Contains($"Modules/Returns/{project}/{project}.csproj", block, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void No_stale_or_duplicate_physical_copy_of_moved_files_remains()
    {
        var applicationRoot = Path.Combine(Repo(), ModuleRootRelative, "Tooba.Returns.Application");
        foreach (var stale in new[]
                 {
                     "Errors/ReturnsExceptionMapper.cs",
                     "Ports/ReturnSemanticMapper.cs",
                     "Models/CreateReturnCommand.cs",
                     "Models/ApproveReturnCommand.cs",
                     "Models/RejectReturnCommand.cs",
                     "Models/RetryRefundCommand.cs",
                     "Models/AdminReturnWorkQueueModels.cs",
                     "Commands/CreateReturn/CreateReturnCommand.cs",
                     "Commands/ApproveReturn/ApproveReturnCommand.cs",
                     "Commands/RejectReturn/RejectReturnCommand.cs",
                     "Commands/RetryReturnRefund/RetryReturnRefundCommand.cs",
                     "Queries/GetAdminReturn/GetAdminReturnQuery.cs",
                     "Queries/QueryAdminReturnsGrid/QueryAdminReturnsGridQuery.cs",
                 })
        {
            Assert.False(File.Exists(Path.Combine(applicationRoot, stale)), $"stale physical copy: {stale}");
        }
    }

    private static JsonElement LoadManifest() =>
        JsonDocument.Parse(File.ReadAllText(Path.Combine(Repo(), "docs/architecture/tmar-module-structure-manifests.json")))
            .RootElement.Clone();

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
