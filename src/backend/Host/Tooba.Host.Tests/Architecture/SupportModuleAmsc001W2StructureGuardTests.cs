using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-SUPPORT-AMSC-001-W2 — structure wave durable guard.
/// Locks the capability-first shallow Application layout (no technical-axis roots, no single-file
/// use-case leaf folders), exact path↔namespace alignment, root allowlists, clean physical copies,
/// the canonical <c>/Modules/Support/</c> solution grouping, and the honest pre-certification
/// manifest record (structureCertified stays false until the W3 Certify wave).
/// </summary>
public sealed class SupportModuleAmsc001W2StructureGuardTests
{
    private const string ModuleRootRelative = "src/backend/Modules/Support";

    private static readonly string[] ProductionProjects =
    [
        "Tooba.Support.Contracts",
        "Tooba.Support.Domain",
        "Tooba.Support.Application",
        "Tooba.Support.Infrastructure",
        "Tooba.Support.Endpoints",
    ];

    [Fact]
    public void Application_is_capability_first_shallow_without_technical_axis_roots()
    {
        var applicationRoot = Path.Combine(Repo(), ModuleRootRelative, "Tooba.Support.Application");
        var topFolders = Directory.EnumerateDirectories(applicationRoot)
            .Select(Path.GetFileName)
            .Where(n => n is not "bin" and not "obj" and not "artifacts")
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(["Composition", "Tickets", "Validation"], topFolders);

        foreach (var retired in new[] { "Commands", "Queries", "Models", "Ports", "Validators", "Errors", "Handlers", "Requests" })
        {
            Assert.False(
                Directory.Exists(Path.Combine(applicationRoot, retired)),
                $"Application/{retired} must stay retired (technical-axis-first root)");
        }

        var tickets = Path.Combine(applicationRoot, "Tickets");
        var subFolders = Directory.EnumerateDirectories(tickets)
            .Select(Path.GetFileName)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(["Commands", "Models", "Ports", "Queries"], subFolders);
    }

    [Fact]
    public void No_single_file_use_case_leaf_folders_remain_in_the_request_axes()
    {
        var tickets = Path.Combine(Repo(), ModuleRootRelative, "Tooba.Support.Application/Tickets");
        foreach (var axis in new[] { "Commands", "Queries" })
        {
            var axisRoot = Path.Combine(tickets, axis);
            Assert.True(Directory.Exists(axisRoot), $"{axis} axis must exist");

            var leafFolders = Directory.EnumerateDirectories(axisRoot).ToArray();
            Assert.True(
                leafFolders.Length == 0,
                $"per-use-case leaf folders are over-foldering: {string.Join(", ", leafFolders.Select(Path.GetFileName))}");

            Assert.NotEmpty(Directory.EnumerateFiles(axisRoot, "*.cs", SearchOption.TopDirectoryOnly));
        }
    }

    [Fact]
    public void Domain_enums_and_infrastructure_migrations_live_in_the_canonical_folders()
    {
        var domain = Path.Combine(Repo(), ModuleRootRelative, "Tooba.Support.Domain");
        Assert.True(Directory.Exists(Path.Combine(domain, "Enums")));
        Assert.False(Directory.Exists(Path.Combine(domain, "ValueObjects")));

        var infrastructure = Path.Combine(Repo(), ModuleRootRelative, "Tooba.Support.Infrastructure");
        Assert.True(Directory.Exists(Path.Combine(infrastructure, "Persistence", "Migrations")));
        Assert.False(Directory.Exists(Path.Combine(infrastructure, "Migrations")));
        Assert.False(Directory.Exists(Path.Combine(infrastructure, "Seeds")));
        Assert.True(File.Exists(Path.Combine(infrastructure, "Development", "SupportDevelopmentSeed.cs")));
        Assert.True(File.Exists(Path.Combine(infrastructure, "Development", "SupportDevelopmentSeedBootstrap.cs")));
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
        var entry = manifest.GetProperty("preCertModules").EnumerateArray()
            .Single(x => x.GetProperty("module").GetString() == "Support");

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
    public void Manifest_records_support_as_pre_cert_not_certified()
    {
        var manifest = LoadManifest();

        Assert.False(
            manifest.GetProperty("uncertifiedHttpOwningModules").EnumerateArray()
                .Any(x => x.GetString() == "Support"),
            "Support must be removed from uncertifiedHttpOwningModules after the W2 structure wave");

        // Promotion into the certified modules[] array is the exclusive authority of the W3 wave.
        Assert.DoesNotContain(
            manifest.GetProperty("modules").EnumerateArray(),
            x => x.GetProperty("module").GetString() == "Support");

        var entry = manifest.GetProperty("preCertModules").EnumerateArray()
            .Single(x => x.GetProperty("module").GetString() == "Support");
        Assert.False(entry.GetProperty("structureCertified").GetBoolean());
        Assert.Equal("ARCH-COMPLETE-002", entry.GetProperty("lockVersion").GetString());
        Assert.Equal(6, entry.GetProperty("projects").GetArrayLength());
    }

    [Fact]
    public void Solution_explorer_grouping_is_canonical()
    {
        var slnx = File.ReadAllText(Path.Combine(Repo(), "src/backend/Tooba.slnx"));
        Assert.Contains("<Folder Name=\"/Modules/Support/\">", slnx, StringComparison.Ordinal);

        var block = Regex.Match(slnx, @"<Folder Name=""/Modules/Support/"">(?<body>.*?)</Folder>", RegexOptions.Singleline).Groups["body"].Value;
        foreach (var project in new[]
                 {
                     "Tooba.Support.Contracts", "Tooba.Support.Domain", "Tooba.Support.Application",
                     "Tooba.Support.Infrastructure", "Tooba.Support.Endpoints", "Tooba.Support.Tests",
                 })
        {
            Assert.Contains($"Modules/Support/{project}/{project}.csproj", block, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void No_stale_or_duplicate_physical_copy_of_moved_files_remains()
    {
        var applicationRoot = Path.Combine(Repo(), ModuleRootRelative, "Tooba.Support.Application");
        foreach (var stale in new[]
                 {
                     "Errors/SupportErrorCodes.cs",
                     "Errors/SupportExceptionMapper.cs",
                     "Commands/CreateCustomerTicket/CreateCustomerTicketCommand.cs",
                     "Commands/ReplyCustomerTicket/ReplyCustomerTicketCommand.cs",
                     "Commands/CloseCustomerTicket/CloseCustomerTicketCommand.cs",
                     "Queries/ListAdminTickets/ListAdminTicketsQuery.cs",
                     "Queries/GetSupportDemoPreview/GetSupportDemoPreviewQuery.cs",
                     "Models/SupportDtos.cs",
                     "Ports/ISupportDirectory.cs",
                 })
        {
            Assert.False(File.Exists(Path.Combine(applicationRoot, stale)), $"stale physical copy: {stale}");
        }

        var domainRoot = Path.Combine(Repo(), ModuleRootRelative, "Tooba.Support.Domain");
        Assert.False(File.Exists(Path.Combine(domainRoot, "ValueObjects/SupportEnums.cs")));

        var infrastructureRoot = Path.Combine(Repo(), ModuleRootRelative, "Tooba.Support.Infrastructure");
        Assert.False(File.Exists(Path.Combine(infrastructureRoot, "Migrations/20260827120000_InitialSupport.cs")));
        Assert.False(File.Exists(Path.Combine(infrastructureRoot, "Seeds/SupportDevelopmentSeed.cs")));

        // The moved files exist exactly once at their canonical destination.
        Assert.True(File.Exists(Path.Combine(applicationRoot, "Tickets/Commands/CreateCustomerTicketCommand.cs")));
        Assert.True(File.Exists(Path.Combine(applicationRoot, "Tickets/Queries/ListAdminTicketsQuery.cs")));
        Assert.True(File.Exists(Path.Combine(applicationRoot, "Tickets/Models/SupportDtos.cs")));
        Assert.True(File.Exists(Path.Combine(applicationRoot, "Tickets/Ports/ISupportDirectory.cs")));
        Assert.True(File.Exists(Path.Combine(domainRoot, "Enums/SupportEnums.cs")));
        Assert.True(File.Exists(Path.Combine(infrastructureRoot, "Persistence/Migrations/20260827120000_InitialSupport.cs")));
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
