using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-BULKINQUIRY-AMSC-001-W2 — durable structure guard: capability-first shallow tree,
/// exact path↔namespace, enforced root allowlists, no stale/duplicate copy, canonical solution grouping.
/// </summary>
public sealed class BulkInquiryModuleAmsc001W2StructureGuardTests
{
    private const string ModuleRoot = "src/backend/Modules/BulkInquiry";
    private const string App = ModuleRoot + "/Tooba.BulkInquiry.Application";
    private const string Domain = ModuleRoot + "/Tooba.BulkInquiry.Domain";
    private const string Infra = ModuleRoot + "/Tooba.BulkInquiry.Infrastructure";
    private const string Endpoints = ModuleRoot + "/Tooba.BulkInquiry.Endpoints";
    private const string Contracts = ModuleRoot + "/Tooba.BulkInquiry.Contracts";

    [Fact]
    public void BulkInquiry_path_namespace_alignment_is_exact()
    {
        var root = Repo();
        var moduleAbsolute = Path.Combine(root, ModuleRoot);
        foreach (var file in Production(root, ModuleRoot))
        {
            var relative = file[moduleAbsolute.Length..].TrimStart(Path.DirectorySeparatorChar);
            var parts = relative.Split(Path.DirectorySeparatorChar);
            var expected = parts.Length > 2
                ? parts[0] + "." + string.Join('.', parts[1..^1])
                : parts[0];

            var declared = DeclaredNamespace(file);
            if (declared is null)
            {
                continue; // files without a file-scoped namespace declaration (e.g. designer partials)
            }

            Assert.Equal(expected, declared);
        }
    }

    [Fact]
    public void BulkInquiry_is_capability_first_shallow_without_technical_axis_roots_or_use_case_leaf_folders()
    {
        var root = Repo();

        foreach (var banned in new[] { "Commands", "Queries", "Validators" })
        {
            Assert.False(
                Directory.Exists(Path.Combine(root, App, banned)),
                $"Application/{banned} must not be a top-level technical axis for a capability-first module.");
        }

        Assert.True(Directory.Exists(Path.Combine(root, App, "Storefront", "Commands")));
        Assert.True(Directory.Exists(Path.Combine(root, App, "Validation")));
        Assert.True(Directory.Exists(Path.Combine(root, App, "Composition")));
        Assert.True(Directory.Exists(Path.Combine(root, App, "Models")));
        Assert.True(Directory.Exists(Path.Combine(root, App, "Ports")));

        // No use-case-named leaf folder wrapping a single production source file.
        var offenders = Directory.EnumerateDirectories(Path.Combine(root, App), "*", SearchOption.AllDirectories)
            .Where(dir => !dir.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                          && !dir.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .Where(dir => Directory.EnumerateFiles(dir, "*.cs", SearchOption.TopDirectoryOnly).Count() == 1)
            .Select(dir => Path.GetFileName(dir))
            .Where(name => name.StartsWith("Submit", StringComparison.Ordinal)
                           || name.StartsWith("Get", StringComparison.Ordinal)
                           || name.StartsWith("Create", StringComparison.Ordinal)
                           || name.StartsWith("Update", StringComparison.Ordinal)
                           || name.StartsWith("Delete", StringComparison.Ordinal)
                           || name.StartsWith("List", StringComparison.Ordinal))
            .ToArray();
        Assert.Empty(offenders);
    }

    [Fact]
    public void BulkInquiry_root_allowlists_are_enforced_and_no_stale_copy_remains()
    {
        var root = Repo();

        Assert.Empty(Directory.EnumerateFiles(Path.Combine(root, App), "*.cs", SearchOption.TopDirectoryOnly));
        Assert.Empty(Directory.EnumerateFiles(Path.Combine(root, Domain), "*.cs", SearchOption.TopDirectoryOnly));
        Assert.Empty(Directory.EnumerateFiles(Path.Combine(root, Contracts), "*.cs", SearchOption.TopDirectoryOnly));

        Assert.Equal(
            ["BulkInquiryModule.cs"],
            Directory.EnumerateFiles(Path.Combine(root, Infra), "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName).OrderBy(x => x, StringComparer.Ordinal).ToArray());
        Assert.Equal(
            ["BulkInquiryEndpointModule.cs"],
            Directory.EnumerateFiles(Path.Combine(root, Endpoints), "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName).OrderBy(x => x, StringComparer.Ordinal).ToArray());

        // Stale/duplicate copy detection: retired technical-axis homes must not return.
        Assert.False(Directory.Exists(Path.Combine(root, App, "Storefront", "Validators")));
        Assert.False(Directory.Exists(Path.Combine(root, Infra, "Migrations")));
        Assert.False(File.Exists(Path.Combine(root, Infra, "BulkInquiryDirectory.cs")));
        Assert.False(File.Exists(Path.Combine(root, Domain, "BulkPurchaseInquiry.cs")));
        Assert.False(File.Exists(Path.Combine(root, App, "BulkInquiryContracts.cs")));

        // Single authoritative home for each responsibility.
        Assert.True(File.Exists(Path.Combine(root, App, "Validation", "SubmitBulkInquiryCommandValidator.cs")));
        Assert.True(File.Exists(Path.Combine(root, App, "Validation", "BulkInquiryValidationCodes.cs")));
        Assert.True(File.Exists(Path.Combine(root, Domain, "Aggregates", "BulkPurchaseInquiry.cs")));
        Assert.True(File.Exists(Path.Combine(root, Domain, "Enums", "BulkInquiryStatus.cs")));
        Assert.True(File.Exists(Path.Combine(root, Infra, "Persistence", "Migrations", "20260826120000_InitialBulkInquiry.cs")));
    }

    [Fact]
    public void BulkInquiry_solution_explorer_grouping_is_canonical()
    {
        var root = Repo();
        var slnx = File.ReadAllText(Path.Combine(root, "src/backend/Tooba.slnx"));
        Assert.Contains("<Folder Name=\"/Modules/BulkInquiry/\">", slnx, StringComparison.Ordinal);
        foreach (var project in new[]
                 {
                     "Tooba.BulkInquiry.Contracts",
                     "Tooba.BulkInquiry.Domain",
                     "Tooba.BulkInquiry.Application",
                     "Tooba.BulkInquiry.Infrastructure",
                     "Tooba.BulkInquiry.Endpoints",
                 })
        {
            Assert.Contains($"Modules/BulkInquiry/{project}/{project}.csproj", slnx, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void BulkInquiry_domain_references_only_buildingblocks_and_own_contracts()
    {
        var root = Repo();
        var csproj = File.ReadAllText(Path.Combine(root, Domain, "Tooba.BulkInquiry.Domain.csproj"));
        var references = csproj
            .Split('<', '>')
            .Where(token => token.StartsWith("ProjectReference Include=", StringComparison.Ordinal))
            .ToArray();

        Assert.NotEmpty(references);
        foreach (var reference in references)
        {
            Assert.True(
                reference.Contains("Tooba.BuildingBlocks.csproj", StringComparison.Ordinal)
                || reference.Contains("Tooba.BulkInquiry.Contracts.csproj", StringComparison.Ordinal),
                $"Domain must not depend on a non-foundation project: {reference}");
        }
    }

    private static IEnumerable<string> Production(string root, string relative) =>
        Directory.EnumerateFiles(Path.Combine(root, relative), "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                           && !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal));

    private static string? DeclaredNamespace(string file) =>
        File.ReadLines(file)
            .Select(line => line.Trim())
            .Where(line => line.StartsWith("namespace ", StringComparison.Ordinal) && line.EndsWith(';'))
            .Select(line => line["namespace ".Length..^1])
            .FirstOrDefault();

    private static string Repo()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
                return directory.FullName;
            directory = directory.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
