using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-CUSTOMER-FULL-CLOSURE-001-R1 — CustomerProfile Solution Folder grouping in Tooba.slnx.
/// </summary>
public sealed class CustomerProfileSolutionGroupingGuardTests
{
    private static readonly string[] ExpectedProjects =
    [
        "Modules/CustomerProfile/Tooba.CustomerProfile.Domain/Tooba.CustomerProfile.Domain.csproj",
        "Modules/CustomerProfile/Tooba.CustomerProfile.Contracts/Tooba.CustomerProfile.Contracts.csproj",
        "Modules/CustomerProfile/Tooba.CustomerProfile.Application/Tooba.CustomerProfile.Application.csproj",
        "Modules/CustomerProfile/Tooba.CustomerProfile.Infrastructure/Tooba.CustomerProfile.Infrastructure.csproj",
        "Modules/CustomerProfile/Tooba.CustomerProfile.Endpoints/Tooba.CustomerProfile.Endpoints.csproj",
    ];

    [Fact]
    public void CustomerProfile_projects_are_grouped_exactly_once_under_Modules_CustomerProfile()
    {
        var slnxPath = Path.Combine(FindRepoRoot(), "src", "backend", "Tooba.slnx");
        var doc = XDocument.Load(slnxPath);
        var folders = doc.Root!.Elements("Folder").ToArray();

        var customerFolder = folders.Single(f =>
            string.Equals((string?)f.Attribute("Name"), "/Modules/CustomerProfile/", StringComparison.Ordinal));
        var inFolder = customerFolder.Elements("Project")
            .Select(p => (string?)p.Attribute("Path") ?? string.Empty)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(ExpectedProjects.OrderBy(x => x, StringComparer.Ordinal).ToArray(), inFolder);

        var flatModules = folders.Single(f =>
            string.Equals((string?)f.Attribute("Name"), "/Modules/", StringComparison.Ordinal));
        var flatCustomer = flatModules.Elements("Project")
            .Select(p => (string?)p.Attribute("Path") ?? string.Empty)
            .Where(p => p.Contains("/CustomerProfile/", StringComparison.Ordinal))
            .ToArray();
        Assert.Empty(flatCustomer);

        var allCustomerPaths = doc.Descendants("Project")
            .Select(p => (string?)p.Attribute("Path") ?? string.Empty)
            .Where(p => p.Contains("/CustomerProfile/", StringComparison.Ordinal))
            .ToArray();
        Assert.Equal(5, allCustomerPaths.Length);
        Assert.Equal(5, allCustomerPaths.Distinct(StringComparer.Ordinal).Count());
        Assert.True(ExpectedProjects.All(p => allCustomerPaths.Contains(p, StringComparer.Ordinal)));
    }

    [Fact]
    public void Slnx_text_contains_Modules_CustomerProfile_folder_marker()
    {
        var text = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "backend", "Tooba.slnx"));
        Assert.Contains("""Name="/Modules/CustomerProfile/">""", text, StringComparison.Ordinal);
        Assert.Single(Regex.Matches(text, """Name="/Modules/CustomerProfile/">"""));
    }

    private static string FindRepoRoot()
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
