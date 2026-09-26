using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// AddressBook physical layout is locked to the Offer COMPLETE_REFERENCE_PATTERN:
/// production files must live under approved responsibility folders with exact
/// path-derived namespaces, and no flat dumping ground may reappear at project roots.
/// </summary>
public sealed class AddressBookPhysicalStructureGuardTests
{
    private static readonly string[] AllowedContractsFolders = ["Dtos", "Ports", "Errors"];

    private static readonly string[] AllowedDomainFolders =
        ["Aggregates", "Entities", "ValueObjects", "Policies", "Events", "Errors"];

    private static readonly string[] AllowedApplicationFolders =
        ["Commands", "Queries", "Mappings", "Ports", "Validators", "Dtos", "ReadModels", "Models", "Policies"];

    private static readonly string[] AllowedInfrastructureFolders =
        ["Persistence", "Repositories", "Adapters", "Outbox", "Events", "DependencyInjection"];

    private static readonly string[] AllowedEndpointsFolders =
        ["Admin", "Storefront", "Seller", "Customer", "Errors", "Resources"];

    [Fact]
    public void AddressBook_production_files_live_under_approved_offer_style_folders()
    {
        var violations = new List<string>();
        CheckProjectFiles("Tooba.AddressBook.Contracts", AllowedContractsFolders, violations, allowRootFiles: []);
        CheckProjectFiles("Tooba.AddressBook.Domain", AllowedDomainFolders, violations, allowRootFiles: []);
        CheckProjectFiles("Tooba.AddressBook.Application", AllowedApplicationFolders, violations, allowRootFiles: []);
        CheckProjectFiles("Tooba.AddressBook.Infrastructure", AllowedInfrastructureFolders, violations, allowRootFiles: []);
        CheckProjectFiles(
            "Tooba.AddressBook.Endpoints",
            AllowedEndpointsFolders,
            violations,
            allowRootFiles: ["AddressBookEndpointModule.cs"]);

        Assert.True(violations.Count == 0, "physical layout violations:\n" + string.Join("\n", violations));
    }

    [Fact]
    public void AddressBook_namespaces_equal_path_derived_namespaces_exactly()
    {
        var violations = new List<string>();
        foreach (var project in new[]
                 {
                     "Tooba.AddressBook.Contracts",
                     "Tooba.AddressBook.Domain",
                     "Tooba.AddressBook.Application",
                     "Tooba.AddressBook.Infrastructure",
                     "Tooba.AddressBook.Endpoints",
                 })
        {
            CheckExactNamespaces(project, violations);
        }

        Assert.True(violations.Count == 0, "namespace/path mismatches:\n" + string.Join("\n", violations));
    }

    [Fact]
    public void AddressBook_domain_aggregate_and_contracts_ports_are_in_offer_style_locations()
    {
        var root = AddressBookRoot();
        Assert.True(File.Exists(Path.Combine(root, "Tooba.AddressBook.Domain", "Aggregates", "CustomerAddress.cs")));
        Assert.True(File.Exists(Path.Combine(root, "Tooba.AddressBook.Contracts", "Dtos", "CustomerAddressRecord.cs")));
        Assert.True(File.Exists(Path.Combine(root, "Tooba.AddressBook.Contracts", "Ports", "IAddressBookCheckoutLookup.cs")));
        Assert.True(File.Exists(Path.Combine(root, "Tooba.AddressBook.Infrastructure", "DependencyInjection", "AddressBookModule.cs")));
        Assert.True(File.Exists(Path.Combine(root, "Tooba.AddressBook.Infrastructure", "Adapters", "AddressBookDirectory.cs")));
        Assert.True(File.Exists(Path.Combine(root, "Tooba.AddressBook.Infrastructure", "Outbox", "AddressBookOutboxRegistration.cs")));

        // Legacy non-Offer-style locations must stay gone.
        Assert.False(File.Exists(Path.Combine(root, "Tooba.AddressBook.Domain", "CustomerAddress.cs")));
        Assert.False(Directory.Exists(Path.Combine(root, "Tooba.AddressBook.Contracts", "Customer")));
        Assert.False(Directory.Exists(Path.Combine(root, "Tooba.AddressBook.Application", "Customer")));
        Assert.False(Directory.Exists(Path.Combine(root, "Tooba.AddressBook.Infrastructure", "Directories")));
        Assert.False(Directory.Exists(Path.Combine(root, "Tooba.AddressBook.Infrastructure", "Development")));
    }

    private static void CheckProjectFiles(
        string project,
        string[] allowedFolders,
        List<string> violations,
        string[] allowRootFiles)
    {
        var root = Path.Combine(AddressBookRoot(), project);
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

    private static void CheckExactNamespaces(string project, List<string> violations)
    {
        var root = Path.Combine(AddressBookRoot(), project);
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

            // EF migrations and the model snapshot are generated with block-scoped namespaces.
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

    private static string AddressBookRoot() =>
        Path.Combine(RepoRoot(), "src", "backend", "Modules", "AddressBook");

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "AGENTS.md")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
