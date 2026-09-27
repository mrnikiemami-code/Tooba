using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-ADMIN-AMC-001-W18 — ProductWorkspace module skeleton + boundary locks;
/// zero route moves; Host retains 19 ProductWorkspace routes; Host/Admin stays 52.
/// </summary>
public sealed class HostAdminAmcW18GuardTests
{
    private static readonly string[] ModuleProjects =
    [
        "Tooba.ProductWorkspace.Contracts",
        "Tooba.ProductWorkspace.Domain",
        "Tooba.ProductWorkspace.Application",
        "Tooba.ProductWorkspace.Infrastructure",
        "Tooba.ProductWorkspace.Endpoints",
    ];

    private static readonly Regex MapRouteRegex = new(
        @"\bMap(Get|Post|Put|Patch|Delete)\s*\(",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact]
    public void ProductWorkspace_module_family_exists_exactly_once()
    {
        var root = FindRepoRoot();
        var moduleRoot = Path.Combine(root, "src/backend/Modules/ProductWorkspace");
        Assert.True(Directory.Exists(moduleRoot));

        foreach (var project in ModuleProjects)
        {
            Assert.True(Directory.Exists(Path.Combine(moduleRoot, project)), project);
            Assert.True(File.Exists(Path.Combine(moduleRoot, project, $"{project}.csproj")), project);
        }

        var slnx = File.ReadAllText(Path.Combine(root, "src/backend/Tooba.slnx"));
        Assert.Single(Regex.Matches(slnx, @"Name=""/Modules/ProductWorkspace/"""));
        foreach (var project in ModuleProjects)
        {
            Assert.Single(Regex.Matches(slnx, Regex.Escape($"Modules/ProductWorkspace/{project}/{project}.csproj")));
        }
    }

    [Fact]
    public void ProductWorkspace_project_reference_boundaries_are_lawful()
    {
        var root = FindRepoRoot();
        var moduleRoot = Path.Combine(root, "src/backend/Modules/ProductWorkspace");

        AssertProjectRefsContainNone(
            Path.Combine(moduleRoot, "Tooba.ProductWorkspace.Contracts", "Tooba.ProductWorkspace.Contracts.csproj"),
            "Application", "Infrastructure", "Domain", "Tooba.Host");

        AssertProjectRefsContainNone(
            Path.Combine(moduleRoot, "Tooba.ProductWorkspace.Domain", "Tooba.ProductWorkspace.Domain.csproj"),
            "Catalog", "Offer", "Pricing", "Inventory", "Tax", "Party", "Tooba.Host");

        var applicationRefs = GetProjectRefs(
            Path.Combine(moduleRoot, "Tooba.ProductWorkspace.Application", "Tooba.ProductWorkspace.Application.csproj"));
        Assert.DoesNotContain(applicationRefs, r => ContainsSegment(r, "Catalog.Application"));
        Assert.DoesNotContain(applicationRefs, r => ContainsSegment(r, "Offer.Application"));
        Assert.DoesNotContain(applicationRefs, r => ContainsSegment(r, "Pricing.Application"));
        Assert.DoesNotContain(applicationRefs, r => ContainsSegment(r, "Inventory.Application"));
        Assert.DoesNotContain(applicationRefs, r => ContainsSegment(r, "Tax.Application"));
        Assert.DoesNotContain(applicationRefs, r => ContainsSegment(r, "Party.Application"));
        Assert.DoesNotContain(applicationRefs, r => ContainsSegment(r, ".Infrastructure"));
        Assert.DoesNotContain(applicationRefs, r => ContainsSegment(r, "Tooba.Host"));
        Assert.Contains(applicationRefs, r => ContainsSegment(r, "Catalog.Contracts"));
        Assert.Contains(applicationRefs, r => ContainsSegment(r, "Offer.Contracts"));
        Assert.Contains(applicationRefs, r => ContainsSegment(r, "Pricing.Contracts"));
        Assert.Contains(applicationRefs, r => ContainsSegment(r, "Inventory.Contracts"));
        Assert.Contains(applicationRefs, r => ContainsSegment(r, "Tax.Contracts"));
        Assert.Contains(applicationRefs, r => ContainsSegment(r, "Party.Contracts"));

        var infrastructureRefs = GetProjectRefs(
            Path.Combine(moduleRoot, "Tooba.ProductWorkspace.Infrastructure", "Tooba.ProductWorkspace.Infrastructure.csproj"));
        Assert.DoesNotContain(infrastructureRefs, r => ContainsSegment(r, "Catalog."));
        Assert.DoesNotContain(infrastructureRefs, r => ContainsSegment(r, "Offer."));
        Assert.DoesNotContain(infrastructureRefs, r => ContainsSegment(r, "Pricing."));
        Assert.DoesNotContain(infrastructureRefs, r => ContainsSegment(r, "Inventory."));
        Assert.DoesNotContain(infrastructureRefs, r => ContainsSegment(r, "Tax."));
        Assert.DoesNotContain(infrastructureRefs, r => ContainsSegment(r, "Party."));
        Assert.DoesNotContain(infrastructureRefs, r => ContainsSegment(r, "Tooba.Host"));
        Assert.DoesNotContain(infrastructureRefs, r => r.Contains("DbContext", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(infrastructureRefs, r => ContainsSegment(r, "Tooba.Persistence"));

        var endpointRefs = GetProjectRefs(
            Path.Combine(moduleRoot, "Tooba.ProductWorkspace.Endpoints", "Tooba.ProductWorkspace.Endpoints.csproj"));
        Assert.DoesNotContain(endpointRefs, r => ContainsSegment(r, "ProductWorkspace.Infrastructure"));
        Assert.DoesNotContain(endpointRefs, r => ContainsSegment(r, ".Infrastructure"));
        Assert.DoesNotContain(endpointRefs, r => ContainsSegment(r, "Tooba.Host"));

        foreach (var project in ModuleProjects)
        {
            var refs = GetProjectRefs(Path.Combine(moduleRoot, project, $"{project}.csproj"));
            Assert.DoesNotContain(refs, r => ContainsSegment(r, "Tooba.Host"));
        }
    }

    [Fact]
    public void ProductWorkspace_endpoints_preserve_module_map_entry_and_Host_retains_remaining_routes()
    {
        var root = FindRepoRoot();
        var endpointModule = File.ReadAllText(Path.Combine(
            root,
            "src/backend/Modules/ProductWorkspace/Tooba.ProductWorkspace.Endpoints/ProductWorkspaceEndpointModule.cs"));
        Assert.Contains("MapProductWorkspaceModuleEndpoints", endpointModule, StringComparison.Ordinal);
        Assert.DoesNotContain("static void MapProductWorkspaceEndpoints", endpointModule, StringComparison.Ordinal);
        Assert.DoesNotContain("MapProductWorkspaceEndpoints(this", endpointModule, StringComparison.Ordinal);


        var program = File.ReadAllText(Path.Combine(root, "src/backend/Host/Tooba.Host/Program.cs"));
        Assert.DoesNotContain("MapProductWorkspaceEndpoints()", program, StringComparison.Ordinal);
        Assert.Contains("MapProductWorkspaceModuleEndpoints", program, StringComparison.Ordinal);
        Assert.Contains("AddProductWorkspaceEndpointPresentation", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Host_Admin_remains_52_StoreAppearance_deferred_and_Host_files_retained()
    {
        var root = FindRepoRoot();
        var admin = Path.Combine(root, "src/backend/Host/Tooba.Host/Admin");
        var files = Directory.GetFiles(admin, "*.cs", SearchOption.AllDirectories);
        Assert.True(files.Length <= 52 && files.Length >= 12, $"Host/Admin count expected in [12,52], was {files.Length}");
        Assert.False(File.Exists(Path.Combine(admin, "ProductWorkspaceEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(admin, "ProductWorkspaceComposer.cs")));
        Assert.False(File.Exists(Path.Combine(admin, "ProductWorkspaceModels.cs")));
        Assert.True(File.Exists(Path.Combine(admin, "StoreAppearanceSettingsEndpoints.cs")));
    }

    [Fact]
    public void ProductWorkspace_is_not_structure_certified_and_SoT_records_W18_checkpoint()
    {
        var root = FindRepoRoot();
        var sot = File.ReadAllText(Path.Combine(root, "docs/architecture/tmar-current-state.json"));
        Assert.Contains("\"hostAdminAmcW18\"", sot, StringComparison.Ordinal);
        Assert.Contains("USER_REVIEW_HOST_ADMIN_W18_CHECKPOINT", sot, StringComparison.Ordinal);
        Assert.Contains("SKELETON_ESTABLISHED", sot, StringComparison.Ordinal);
        Assert.Contains("\"nextHostFolderStarted\": false", sot, StringComparison.Ordinal);

        var structureLock = sot[
            sot.IndexOf("\"structureLock\"", StringComparison.Ordinal)
            ..sot.IndexOf("\"recoveryPhrase\"", StringComparison.Ordinal)];
        Assert.DoesNotContain("ProductWorkspace", structureLock, StringComparison.Ordinal);

        var manifest = File.ReadAllText(Path.Combine(root, "docs/architecture/tmar-module-structure-manifests.json"));
        Assert.Contains("\"preCertModules\"", manifest, StringComparison.Ordinal);
        Assert.Contains("\"module\": \"ProductWorkspace\"", manifest, StringComparison.Ordinal);
        Assert.Contains("\"structureCertified\": false", manifest, StringComparison.Ordinal);

        var modulesSectionStart = manifest.IndexOf("\"modules\":", StringComparison.Ordinal);
        var uncertifiedStart = manifest.IndexOf("\"uncertifiedHttpOwningModules\"", StringComparison.Ordinal);
        var certifiedModulesSection = manifest[modulesSectionStart..uncertifiedStart];
        Assert.DoesNotContain("\"module\": \"ProductWorkspace\"", certifiedModulesSection, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductWorkspace_has_no_foreign_DbContext_and_path_namespace_exact()
    {
        var root = FindRepoRoot();
        var moduleRoot = Path.Combine(root, "src/backend/Modules/ProductWorkspace");
        foreach (var file in Directory.GetFiles(moduleRoot, "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("CatalogDbContext", text, StringComparison.Ordinal);
            Assert.DoesNotContain(": DbContext", text, StringComparison.Ordinal);
            Assert.DoesNotContain("AddDbContext<", text, StringComparison.Ordinal);
            Assert.DoesNotContain("using Microsoft.EntityFrameworkCore", text, StringComparison.Ordinal);
        }

        AssertExactNamespace(
            Path.Combine(moduleRoot, "Tooba.ProductWorkspace.Contracts", "ProductWorkspaceContractsMarker.cs"),
            "Tooba.ProductWorkspace.Contracts");
        AssertExactNamespace(
            Path.Combine(moduleRoot, "Tooba.ProductWorkspace.Infrastructure", "ProductWorkspaceModule.cs"),
            "Tooba.ProductWorkspace.Infrastructure");
        AssertExactNamespace(
            Path.Combine(moduleRoot, "Tooba.ProductWorkspace.Endpoints", "ProductWorkspaceEndpointModule.cs"),
            "Tooba.ProductWorkspace.Endpoints");
        AssertExactNamespace(
            Path.Combine(moduleRoot, "Tooba.ProductWorkspace.Endpoints", "Admin", "IProductWorkspaceAdminAuthorizer.cs"),
            "Tooba.ProductWorkspace.Endpoints.Admin");
    }

    private static void AssertExactNamespace(string path, string expected)
    {
        var text = File.ReadAllText(path);
        Assert.Contains($"namespace {expected};", text, StringComparison.Ordinal);
    }

    private static bool ContainsSegment(string projectRef, string segment) =>
        projectRef.Contains(segment, StringComparison.OrdinalIgnoreCase);

    private static string[] GetProjectRefs(string csprojPath) =>
        XDocument.Load(csprojPath)
            .Descendants("ProjectReference")
            .Select(x => (string?)x.Attribute("Include") ?? string.Empty)
            .ToArray();

    private static void AssertProjectRefsContainNone(string csprojPath, params string[] forbidden)
    {
        var refs = GetProjectRefs(csprojPath);
        foreach (var item in forbidden)
        {
            Assert.DoesNotContain(refs, r => r.Contains(item, StringComparison.OrdinalIgnoreCase));
        }
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Tooba.sln"))
                || File.Exists(Path.Combine(dir.FullName, "AGENTS.md")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
    }
}
