using System.Xml.Linq;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// Durable guard for two closed AddressBook gaps: canonical error presentation (no parallel
/// AddressBook problem pipeline) and Visual Studio solution-folder grouping in <c>Tooba.slnx</c>.
/// </summary>
public sealed class AddressBookCanonicalPresentationGuardTests
{
    private static readonly string[] AddressBookProjects =
    [
        "Tooba.AddressBook.Application",
        "Tooba.AddressBook.Contracts",
        "Tooba.AddressBook.Domain",
        "Tooba.AddressBook.Endpoints",
        "Tooba.AddressBook.Infrastructure",
    ];

    [Fact]
    public void Endpoints_use_the_canonical_presentation_stack_not_a_parallel_problem_pipeline()
    {
        var endpointsRoot = Path.Combine(AddressBookRoot(), "Tooba.AddressBook.Endpoints");
        var read = File.ReadAllText(Path.Combine(endpointsRoot, "Customer", "AddressBookCustomerReadEndpoints.cs"));
        var write = File.ReadAllText(Path.Combine(endpointsRoot, "Customer", "AddressBookCustomerWriteEndpoints.cs"));

        foreach (var source in new[] { read, write })
        {
            Assert.Contains("ApiResponseFactory", source, StringComparison.Ordinal);
            Assert.Contains("FromFailure", source, StringComparison.Ordinal);
            Assert.Contains("SemanticError", source, StringComparison.Ordinal);
            Assert.DoesNotContain("Results.Problem", source, StringComparison.Ordinal);
            Assert.DoesNotContain("Results.Json(new {", source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Module_registers_catalog_contributor_and_resource_set()
    {
        var module = File.ReadAllText(Path.Combine(AddressBookRoot(), "Tooba.AddressBook.Endpoints", "AddressBookEndpointModule.cs"));
        Assert.Contains("IErrorCatalogContributor, AddressBookErrorCatalogContributor", module, StringComparison.Ordinal);
        Assert.Contains("IErrorResourceSet, AddressBookErrorResourceSet", module, StringComparison.Ordinal);

        Assert.True(File.Exists(Path.Combine(
            AddressBookRoot(), "Tooba.AddressBook.Endpoints", "Errors", "AddressBookErrorCatalogContributor.cs")));
        Assert.True(File.Exists(Path.Combine(
            AddressBookRoot(), "Tooba.AddressBook.Endpoints", "Resources", "AddressBookErrorResources.cs")));
        Assert.True(File.Exists(Path.Combine(
            AddressBookRoot(), "Tooba.AddressBook.Endpoints", "Resources", "AddressBookErrors.resx")));
        Assert.True(File.Exists(Path.Combine(
            AddressBookRoot(), "Tooba.AddressBook.Endpoints", "Resources", "AddressBookErrors.fa.resx")));
    }

    [Fact]
    public void AddressBook_projects_are_grouped_under_one_solution_folder()
    {
        var slnx = Path.Combine(RepoRoot(), "src", "backend", "Tooba.slnx");
        Assert.True(File.Exists(slnx), slnx);
        var doc = XDocument.Load(slnx);

        var folders = doc.Root!.Elements("Folder").ToArray();
        var addressBookFolder = folders.SingleOrDefault(f =>
            string.Equals((string?)f.Attribute("Name"), "/Modules/AddressBook/", StringComparison.Ordinal));
        Assert.True(addressBookFolder is not null, "missing /Modules/AddressBook/ solution folder");

        var nested = addressBookFolder!.Elements("Project")
            .Select(p => Path.GetFileNameWithoutExtension((string)p.Attribute("Path")!))
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(AddressBookProjects.OrderBy(x => x, StringComparer.Ordinal).ToArray(), nested);

        // No AddressBook project may remain duplicated at another solution folder (e.g. bare /Modules/).
        var outside = folders
            .Where(f => !string.Equals((string?)f.Attribute("Name"), "/Modules/AddressBook/", StringComparison.Ordinal))
            .SelectMany(f => f.Elements("Project"))
            .Select(p => Path.GetFileNameWithoutExtension((string)p.Attribute("Path")!))
            .Where(n => AddressBookProjects.Contains(n, StringComparer.Ordinal))
            .ToArray();
        Assert.True(outside.Length == 0, "AddressBook projects duplicated outside the AddressBook folder: " + string.Join(", ", outside));
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
