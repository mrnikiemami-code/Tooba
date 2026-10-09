using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-SETTLEMENT-AMSC-001-W2 — structure wave durable guard.
/// Locks the capability-first shallow Application layout (no technical-axis roots, no single-file
/// use-case leaf folders, no audience-first validator tree), exact path↔namespace alignment, root
/// allowlists matched to disk, clean physical copies, and the canonical <c>/Modules/Settlement/</c>
/// solution grouping with all six projects present.
/// </summary>
public sealed class SettlementModuleAmsc001W2StructureGuardTests
{
    private const string ModuleRootRelative = "src/backend/Modules/Settlement";

    private static readonly string[] ProductionProjects =
    [
        "Tooba.Settlement.Contracts",
        "Tooba.Settlement.Domain",
        "Tooba.Settlement.Application",
        "Tooba.Settlement.Infrastructure",
        "Tooba.Settlement.Endpoints",
    ];

    [Fact]
    public void Application_is_capability_first_shallow_without_technical_axis_roots()
    {
        var applicationRoot = Path.Combine(Repo(), ModuleRootRelative, "Tooba.Settlement.Application");
        var topFolders = Directory.EnumerateDirectories(applicationRoot)
            .Select(Path.GetFileName)
            .Where(n => n is not "bin" and not "obj" and not "artifacts")
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(["Composition", "Payouts", "Validation"], topFolders);

        foreach (var retired in new[] { "Commands", "Queries", "Models", "Ports", "Validators", "Errors", "Handlers", "Requests" })
        {
            Assert.False(
                Directory.Exists(Path.Combine(applicationRoot, retired)),
                $"Application/{retired} must stay retired (technical-axis-first root)");
        }

        var payouts = Path.Combine(applicationRoot, "Payouts");
        var subFolders = Directory.EnumerateDirectories(payouts)
            .Select(Path.GetFileName)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(["Commands", "Models", "Ports", "Queries"], subFolders);
    }

    [Fact]
    public void No_single_file_use_case_leaf_folders_remain_in_the_request_axes()
    {
        var payouts = Path.Combine(Repo(), ModuleRootRelative, "Tooba.Settlement.Application/Payouts");
        foreach (var axis in new[] { "Commands", "Queries" })
        {
            var axisRoot = Path.Combine(payouts, axis);
            Assert.True(Directory.Exists(axisRoot), $"{axis} axis must exist");

            var leafFolders = Directory.EnumerateDirectories(axisRoot).ToArray();
            Assert.True(
                leafFolders.Length == 0,
                $"per-use-case leaf folders are over-foldering: {string.Join(", ", leafFolders.Select(Path.GetFileName))}");

            Assert.NotEmpty(Directory.EnumerateFiles(axisRoot, "*.cs", SearchOption.TopDirectoryOnly));
        }

        // Validation is a single flat capability leaf; the audience-first tree must stay retired.
        var validation = Path.Combine(Repo(), ModuleRootRelative, "Tooba.Settlement.Application/Validation");
        Assert.Empty(Directory.EnumerateDirectories(validation));
        Assert.Equal(
            ["SettlementRequestValidators.cs", "SettlementValidationCodes.cs"],
            Directory.EnumerateFiles(validation, "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .OrderBy(n => n, StringComparer.Ordinal)
                .ToArray());
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
    public void No_namespace_alias_workaround_or_global_using_file_remains()
    {
        var globalUsings = Directory.EnumerateFiles(Path.Combine(Repo(), ModuleRootRelative), "GlobalUsings*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(p => Path.GetRelativePath(Repo(), p).Replace('\\', '/'))
            .ToArray();
        Assert.Empty(globalUsings);

        foreach (var project in ProductionProjects)
        {
            var projectRoot = Path.Combine(Repo(), ModuleRootRelative, project);
            foreach (var file in Directory.EnumerateFiles(projectRoot, "*.cs", SearchOption.AllDirectories))
            {
                var normalized = file.Replace('\\', '/');
                if (normalized.Contains("/obj/", StringComparison.Ordinal) || normalized.Contains("/bin/", StringComparison.Ordinal))
                {
                    continue;
                }

                Assert.DoesNotContain("TypeForwardedTo", File.ReadAllText(file), StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void Root_allowlists_match_disk_and_forbidden_entries_are_absent()
    {
        var manifest = LoadManifest();
        var entry = manifest.GetProperty("modules").EnumerateArray()
            .Single(x => x.GetProperty("module").GetString() == "Settlement");

        Assert.Equal(3, entry.GetProperty("projects").GetArrayLength());

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
    public void Solution_explorer_grouping_is_canonical()
    {
        var slnx = File.ReadAllText(Path.Combine(Repo(), "src/backend/Tooba.slnx"));
        Assert.Contains("<Folder Name=\"/Modules/Settlement/\">", slnx, StringComparison.Ordinal);

        var block = Regex.Match(slnx, @"<Folder Name=""/Modules/Settlement/"">(?<body>.*?)</Folder>", RegexOptions.Singleline).Groups["body"].Value;
        foreach (var project in new[]
                 {
                     "Tooba.Settlement.Domain", "Tooba.Settlement.Contracts", "Tooba.Settlement.Application",
                     "Tooba.Settlement.Infrastructure", "Tooba.Settlement.Endpoints", "Tooba.Settlement.Tests",
                 })
        {
            Assert.Contains($"Modules/Settlement/{project}/{project}.csproj", block, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void No_stale_or_duplicate_physical_copy_of_moved_files_remains()
    {
        var applicationRoot = Path.Combine(Repo(), ModuleRootRelative, "Tooba.Settlement.Application");
        foreach (var stale in new[]
                 {
                     "Errors/SettlementExceptionMapper.cs",
                     "Errors/SettlementErrorCodes.cs",
                     "GlobalUsings.Domain.cs",
                     "GlobalUsings.Layout.cs",
                     "Ports/SettlementContracts.cs",
                     "Models/SettlementAdminModels.cs",
                     "Validators/SettlementValidationCodes.cs",
                     "Validators/Seller/RequestSellerPayoutCommandValidator.cs",
                     "Validators/Admin/ProcessAdminPayoutCommandValidator.cs",
                     "Validators/Admin/RetryAdminPayoutCommandValidator.cs",
                     "Validators/Admin/QueryAdminPayoutGridQueryValidator.cs",
                     "Commands/RequestSellerPayout/RequestSellerPayoutCommand.cs",
                     "Commands/ProcessAdminPayout/ProcessAdminPayoutCommand.cs",
                     "Commands/RetryAdminPayout/RetryAdminPayoutCommand.cs",
                     "Queries/GetSellerSettlementBalance/GetSellerSettlementBalanceQuery.cs",
                     "Queries/ListSellerSettlementEntries/ListSellerSettlementEntriesQuery.cs",
                     "Queries/ListSellerSettlementStatements/ListSellerSettlementStatementsQuery.cs",
                     "Queries/ListSellerPayoutRequests/ListSellerPayoutRequestsQuery.cs",
                     "Queries/ListAdminSettlementBalances/ListAdminSettlementBalancesQuery.cs",
                     "Queries/ListAdminPayoutQueue/ListAdminPayoutQueueQuery.cs",
                     "Queries/QueryAdminPayoutGrid/QueryAdminPayoutGridQuery.cs",
                     "Queries/QueryAdminPayoutGrid/AdminPayoutGridQueryPolicy.cs",
                 })
        {
            Assert.False(File.Exists(Path.Combine(applicationRoot, stale)), $"stale physical copy: {stale}");
        }

        var infrastructureRoot = Path.Combine(Repo(), ModuleRootRelative, "Tooba.Settlement.Infrastructure");
        foreach (var stale in new[] { "GlobalUsings.Domain.cs", "GlobalUsings.Layout.cs" })
        {
            Assert.False(File.Exists(Path.Combine(infrastructureRoot, stale)), $"stale physical copy: {stale}");
        }
    }

    [Fact]
    public void Duplicate_ownership_of_the_same_responsibility_does_not_exist()
    {
        var moduleRoot = Path.Combine(Repo(), ModuleRootRelative);
        foreach (var name in new[]
                 {
                     "SettlementErrorCodes.cs", "SettlementErrorResourceSet.cs", "SettlementOperation.cs",
                     "SettlementDirectory.cs", "SettlementDbContext.cs", "AdminPayoutGridQueryEngine.cs",
                     "OpenSettlementUseCaseGuard.cs", "SettlementRequestValidators.cs", "SettlementValidationCodes.cs",
                 })
        {
            var hits = Directory.EnumerateFiles(moduleRoot, name, SearchOption.AllDirectories)
                .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                            && !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                .Select(p => Path.GetRelativePath(Repo(), p).Replace('\\', '/'))
                .ToArray();
            Assert.True(hits.Length == 1, $"{name} must have exactly one physical home, found: {string.Join(", ", hits)}");
        }

        // Each CQRS request type has exactly one physical declaration.
        foreach (var request in new[]
                 {
                     "RequestSellerPayoutCommand", "ProcessAdminPayoutCommand", "RetryAdminPayoutCommand",
                     "GetSellerSettlementBalanceQuery", "ListSellerSettlementEntriesQuery",
                     "ListSellerSettlementStatementsQuery", "ListSellerPayoutRequestsQuery",
                     "ListAdminSettlementBalancesQuery", "ListAdminPayoutQueueQuery", "QueryAdminPayoutGridQuery",
                 })
        {
            var hits = Directory.EnumerateFiles(moduleRoot, $"{request}.cs", SearchOption.AllDirectories)
                .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                            && !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                .ToArray();
            Assert.True(hits.Length == 1, $"{request}.cs must have exactly one physical home, found {hits.Length}");
        }
    }

    [Fact]
    public void Module_is_certified_and_absent_from_uncertified_lists()
    {
        var manifest = LoadManifest();

        Assert.DoesNotContain(
            manifest.GetProperty("uncertifiedHttpOwningModules").EnumerateArray(),
            x => x.GetString() == "Settlement");

        Assert.DoesNotContain(
            manifest.GetProperty("preCertModules").EnumerateArray(),
            x => x.GetProperty("module").GetString() == "Settlement");

        var entry = manifest.GetProperty("modules").EnumerateArray()
            .Single(x => x.GetProperty("module").GetString() == "Settlement");
        Assert.True(entry.GetProperty("structureCertified").GetBoolean());
        Assert.Equal("ARCH-COMPLETE-002", entry.GetProperty("lockVersion").GetString());
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
