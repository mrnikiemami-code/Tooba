using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-ADMIN-AMC-001 W1 — Catalog.Endpoints foundation + Host wiring (no Admin evacuation).
/// </summary>
public sealed class HostAdminAmcW1GuardTests
{
    [Fact]
    public void Catalog_Endpoints_project_exists_with_module_and_admin_authorizer()
    {
        var endpointsRoot = Path.Combine(
            FindRepoRoot(),
            "src",
            "backend",
            "Modules",
            "Catalog",
            "Tooba.Catalog.Endpoints");
        Assert.True(File.Exists(Path.Combine(endpointsRoot, "Tooba.Catalog.Endpoints.csproj")));
        Assert.True(File.Exists(Path.Combine(endpointsRoot, "CatalogEndpointModule.cs")));
        Assert.True(File.Exists(Path.Combine(endpointsRoot, "Admin", "ICatalogAdminAuthorizer.cs")));

        var module = File.ReadAllText(Path.Combine(endpointsRoot, "CatalogEndpointModule.cs"));
        Assert.Contains("MapCatalogModuleEndpoints", module, StringComparison.Ordinal);
        Assert.Contains("AddCatalogEndpointPresentation", module, StringComparison.Ordinal);
        Assert.Contains("ICatalogAdminAuthorizer", module, StringComparison.Ordinal);

        var authorizer = File.ReadAllText(Path.Combine(endpointsRoot, "Admin", "ICatalogAdminAuthorizer.cs"));
        Assert.Contains("IAdminPanelAccess", authorizer, StringComparison.Ordinal);
        Assert.Contains("CatalogAdminAuthorizer", authorizer, StringComparison.Ordinal);
    }

    [Fact]
    public void Catalog_Endpoints_references_Application_and_Contracts_not_Infrastructure_or_Host()
    {
        var csproj = Path.Combine(
            FindRepoRoot(),
            "src",
            "backend",
            "Modules",
            "Catalog",
            "Tooba.Catalog.Endpoints",
            "Tooba.Catalog.Endpoints.csproj");
        var refs = XDocument.Load(csproj)
            .Descendants("ProjectReference")
            .Select(x => (string?)x.Attribute("Include") ?? string.Empty)
            .ToArray();
        Assert.Contains(refs, r => r.Contains("Tooba.Catalog.Application", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(refs, r => r.Contains("Tooba.Catalog.Contracts", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(refs, r => r.Contains("Tooba.BuildingBlocks", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Tooba.Host", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains(".Infrastructure", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Program_wires_Catalog_endpoint_module_once()
    {
        var program = File.ReadAllText(Path.Combine(
            FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Program.cs"));
        Assert.Contains("AddCatalogEndpointPresentation()", program, StringComparison.Ordinal);
        Assert.Contains("MapCatalogModuleEndpoints()", program, StringComparison.Ordinal);
        Assert.True(
            Regex.IsMatch(program, @"using\s+Tooba\.Catalog\.Endpoints\s*;", RegexOptions.CultureInvariant),
            "Program must import Tooba.Catalog.Endpoints");
        Assert.Single(Regex.Matches(program, @"MapCatalogModuleEndpoints\(\)"));
        Assert.Single(Regex.Matches(program, @"AddCatalogEndpointPresentation\(\)"));
    }

    [Fact]
    public void Slnx_groups_Catalog_under_Modules_Catalog_folder()
    {
        var slnx = Path.Combine(FindRepoRoot(), "src", "backend", "Tooba.slnx");
        Assert.True(File.Exists(slnx), slnx);
        var doc = XDocument.Load(slnx);
        var catalogFolder = doc.Root?
            .Elements("Folder")
            .FirstOrDefault(f => (string?)f.Attribute("Name") == "/Modules/Catalog/");
        Assert.NotNull(catalogFolder);
        var paths = catalogFolder!.Elements("Project")
            .Select(p => (string?)p.Attribute("Path") ?? string.Empty)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(
            [
                "Modules/Catalog/Tooba.Catalog.Application/Tooba.Catalog.Application.csproj",
                "Modules/Catalog/Tooba.Catalog.Contracts/Tooba.Catalog.Contracts.csproj",
                "Modules/Catalog/Tooba.Catalog.Domain/Tooba.Catalog.Domain.csproj",
                "Modules/Catalog/Tooba.Catalog.Endpoints/Tooba.Catalog.Endpoints.csproj",
                "Modules/Catalog/Tooba.Catalog.Infrastructure/Tooba.Catalog.Infrastructure.csproj",
            ],
            paths);

        var mixedModules = doc.Root?
            .Elements("Folder")
            .FirstOrDefault(f => (string?)f.Attribute("Name") == "/Modules/");
        Assert.NotNull(mixedModules);
        Assert.DoesNotContain(
            mixedModules!.Elements("Project").Select(p => (string?)p.Attribute("Path") ?? string.Empty),
            p => p.Contains("/Catalog/", StringComparison.Ordinal));
    }

    [Fact]
    public void Host_Admin_still_has_production_files_w1_did_not_evacuate()
    {
        // Historical W1 assertion: W1 left Admin at 59. W2 evacuated QuantitySettingsEndpoints (−1 → 58).
        // Keep foundation checks; file-count lock moved to HostAdminAmcW2GuardTests.
        var admin = Path.Combine(FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Admin");
        Assert.True(Directory.Exists(admin));
        Assert.False(File.Exists(Path.Combine(admin, "QuantitySettingsEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(admin, "StoreAppearanceSettingsEndpoints.cs")));
        Assert.True(File.Exists(Path.Combine(admin, "AdminPanelAccess.cs")));
        Assert.True(File.Exists(Path.Combine(admin, "HostAdminPanelAccess.cs")));
        var endpointsRoot = Path.Combine(
            FindRepoRoot(), "src", "backend", "Modules", "Catalog", "Tooba.Catalog.Endpoints");
        Assert.True(File.Exists(Path.Combine(endpointsRoot, "CatalogEndpointModule.cs")));
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
