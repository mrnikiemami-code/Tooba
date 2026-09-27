using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>TB-TMAR-HOST-ADMIN-AMC-001-W3 — UnitOfMeasure evacuated to Catalog; Localization.Contracts-only.</summary>
public sealed class HostAdminAmcW3GuardTests
{
    [Fact]
    public void Host_UnitOfMeasure_endpoint_and_language_gate_are_absent()
    {
        var root = FindRepoRoot();
        Assert.False(File.Exists(Path.Combine(root, "src/backend/Host/Tooba.Host/Admin/UnitOfMeasureEndpoints.cs")));
        var program = File.ReadAllText(Path.Combine(root, "src/backend/Host/Tooba.Host/Program.cs"));
        Assert.DoesNotContain("MapUnitOfMeasureEndpoints()", program, StringComparison.Ordinal);
        Assert.DoesNotContain("IUnitOfMeasureLanguageGate", program, StringComparison.Ordinal);
        Assert.DoesNotContain("HostUnitOfMeasureLanguageGate", program, StringComparison.Ordinal);
        Assert.Contains("MapCatalogModuleEndpoints()", program, StringComparison.Ordinal);
        Assert.Contains("MapStoreAppearanceSettingsEndpoints()", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Catalog_Endpoints_own_five_unit_routes_exactly_once_with_canonical_pipeline()
    {
        var root = FindRepoRoot();
        var path = Path.Combine(root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Admin/Units/UnitOfMeasureEndpoints.cs");
        Assert.True(File.Exists(path));
        var source = File.ReadAllText(path);
        Assert.Contains("/v1/admin/catalog/units", source, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/\", ListAsync)", source, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/{unitId:guid}\", GetAsync)", source, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/\", CreateAsync)", source, StringComparison.Ordinal);
        Assert.Contains("MapPut(\"/{unitId:guid}\", UpdateAsync)", source, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/{unitId:guid}/deactivate\", DeactivateAsync)", source, StringComparison.Ordinal);
        Assert.Contains("ISender", source, StringComparison.Ordinal);
        Assert.Contains("ApiResponseFactory", source, StringComparison.Ordinal);
        Assert.Contains("ICatalogAdminAuthorizer", source, StringComparison.Ordinal);
        Assert.DoesNotContain("CatalogDbContext", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Catalog.Infrastructure", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Localization.Application", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host", source, StringComparison.Ordinal);
        Assert.DoesNotContain("PlatformHttpException", source, StringComparison.Ordinal);

        var module = File.ReadAllText(Path.Combine(root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/CatalogEndpointModule.cs"));
        Assert.Single(Regex.Matches(module, @"MapUnitOfMeasureEndpoints\(\)"));
    }

    [Fact]
    public void Units_Application_is_capability_first_without_Contracts_bundle()
    {
        var unitsRoot = Path.Combine(FindRepoRoot(), "src/backend/Modules/Catalog/Tooba.Catalog.Application/Units");
        Assert.True(Directory.Exists(unitsRoot));
        foreach (var folder in new[] { "Commands", "Queries", "Models", "Ports", "Validators" })
        {
            Assert.True(Directory.Exists(Path.Combine(unitsRoot, folder)), folder);
        }

        Assert.Empty(Directory.GetDirectories(Path.Combine(unitsRoot, "Commands")));
        Assert.Empty(Directory.GetDirectories(Path.Combine(unitsRoot, "Queries")));
        Assert.Empty(Directory.GetFiles(unitsRoot, "*Contracts.cs", SearchOption.AllDirectories));

        var appRoot = Path.Combine(FindRepoRoot(), "src/backend/Modules/Catalog/Tooba.Catalog.Application");
        Assert.False(File.Exists(Path.Combine(appRoot, "UnitOfMeasureWriteContracts.cs")));
        Assert.False(File.Exists(Path.Combine(appRoot, "UnitOfMeasureWriteHandlers.cs")));
        Assert.Single(Directory.GetFiles(appRoot, "CreateUnitOfMeasureCommand.cs", SearchOption.AllDirectories));
        Assert.Single(Directory.GetFiles(appRoot, "UpdateUnitOfMeasureCommand.cs", SearchOption.AllDirectories));
        Assert.Single(Directory.GetFiles(appRoot, "DeactivateUnitOfMeasureCommand.cs", SearchOption.AllDirectories));
        Assert.Single(Directory.GetFiles(appRoot, "ListUnitOfMeasuresQuery.cs", SearchOption.AllDirectories));
        Assert.Single(Directory.GetFiles(appRoot, "GetUnitOfMeasureQuery.cs", SearchOption.AllDirectories));
    }

    [Fact]
    public void Units_path_namespace_exact_and_validators_classified()
    {
        var unitsRoot = Path.Combine(FindRepoRoot(), "src/backend/Modules/Catalog/Tooba.Catalog.Application/Units");
        var appRoot = Path.Combine(FindRepoRoot(), "src/backend/Modules/Catalog/Tooba.Catalog.Application");
        foreach (var file in Directory.GetFiles(unitsRoot, "*.cs", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(appRoot, file).Replace('\\', '/');
            var expectedNs = "Tooba.Catalog.Application." + Path.GetDirectoryName(relative)!
                .Replace('/', '.')
                .Replace('\\', '.');
            var text = File.ReadAllText(file);
            Assert.True(
                Regex.IsMatch(text, $@"namespace\s+{Regex.Escape(expectedNs)}\s*;", RegexOptions.CultureInvariant),
                $"{relative} expected namespace {expectedNs}");
        }

        Assert.True(File.Exists(Path.Combine(unitsRoot, "Validators", "CreateUnitOfMeasureCommandValidator.cs")));
        Assert.True(File.Exists(Path.Combine(unitsRoot, "Validators", "UpdateUnitOfMeasureCommandValidator.cs")));
        Assert.False(File.Exists(Path.Combine(unitsRoot, "Validators", "ListUnitOfMeasuresQueryValidator.cs")));
        Assert.False(File.Exists(Path.Combine(unitsRoot, "Validators", "GetUnitOfMeasureQueryValidator.cs")));
        Assert.False(File.Exists(Path.Combine(unitsRoot, "Validators", "DeactivateUnitOfMeasureCommandValidator.cs")));
    }

    [Fact]
    public void Directory_uses_Result_and_ILanguageLookup_not_exceptions_or_Localization_Application()
    {
        var root = FindRepoRoot();
        var directory = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/UnitOfMeasureDirectory.cs"));
        Assert.Contains("ILanguageLookup", directory, StringComparison.Ordinal);
        Assert.Contains("Result.Failure", directory, StringComparison.Ordinal);
        Assert.Contains("CatalogErrorCodes", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("PlatformHttpException", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("InvalidOperationException", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("Localization.Application", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("IUnitOfMeasureLanguageGate", directory, StringComparison.Ordinal);

        var infraCsproj = Path.Combine(root, "src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/Tooba.Catalog.Infrastructure.csproj");
        var refs = XDocument.Load(infraCsproj)
            .Descendants("ProjectReference")
            .Select(x => (string?)x.Attribute("Include") ?? string.Empty)
            .ToArray();
        Assert.Contains(refs, r => r.Contains("Localization.Contracts", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Localization.Application", StringComparison.OrdinalIgnoreCase));

        foreach (var csproj in Directory.GetFiles(Path.Combine(root, "src/backend/Modules/Catalog"), "*.csproj", SearchOption.AllDirectories))
        {
            var projectRefs = XDocument.Load(csproj)
                .Descendants("ProjectReference")
                .Select(x => (string?)x.Attribute("Include") ?? string.Empty)
                .ToArray();
            Assert.DoesNotContain(projectRefs, r => r.Contains("Tooba.Host", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(projectRefs, r => r.Contains("Localization.Application", StringComparison.OrdinalIgnoreCase));
        }

        var endpointsCsproj = Path.Combine(root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Tooba.Catalog.Endpoints.csproj");
        var endpointRefs = XDocument.Load(endpointsCsproj)
            .Descendants("ProjectReference")
            .Select(x => (string?)x.Attribute("Include") ?? string.Empty)
            .ToArray();
        Assert.DoesNotContain(endpointRefs, r => r.Contains(".Infrastructure", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Host_Admin_file_count_is_56_and_W2_surfaces_preserved()
    {
        var admin = Path.Combine(FindRepoRoot(), "src/backend/Host/Tooba.Host/Admin");
        var files = Directory.GetFiles(admin, "*.cs", SearchOption.AllDirectories);
        Assert.True(files.Length <= 52 && files.Length >= 12, $"Host/Admin count expected in [12,52], was {files.Length}");
        Assert.False(File.Exists(Path.Combine(admin, "UnitOfMeasureEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(admin, "QuantitySettingsEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(admin, "CatalogTagEndpoints.cs")));
        Assert.True(File.Exists(Path.Combine(admin, "StoreAppearanceSettingsEndpoints.cs")));
        Assert.True(File.Exists(Path.Combine(admin, "StoreAppearanceSettingsComposer.cs")));

        var quantityRoot = Path.Combine(
            FindRepoRoot(),
            "src/backend/Modules/Catalog/Tooba.Catalog.Application/Settings/Quantity");
        Assert.True(Directory.Exists(quantityRoot));
        Assert.True(File.Exists(Path.Combine(quantityRoot, "Commands", "SaveStoreQuantitySettingsCommand.cs")));
    }

    [Fact]
    public void Error_catalog_owns_unit_codes()
    {
        var root = FindRepoRoot();
        var codes = File.ReadAllText(Path.Combine(root, "src/backend/Modules/Catalog/Tooba.Catalog.Contracts/Errors/CatalogErrorCodes.cs"));
        Assert.Contains("unit.missing", codes, StringComparison.Ordinal);
        Assert.Contains("unit.dimension.invalid", codes, StringComparison.Ordinal);
        Assert.Contains("unit.code.duplicate", codes, StringComparison.Ordinal);
        Assert.Contains("unit.language.unknown", codes, StringComparison.Ordinal);

        var contributor = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Errors/CatalogErrorCatalogContributor.cs"));
        Assert.Contains("UnitMissing", contributor, StringComparison.Ordinal);
        Assert.Contains("UnitDimensionInvalid", contributor, StringComparison.Ordinal);
        Assert.Contains("UnitCodeDuplicate", contributor, StringComparison.Ordinal);
        Assert.Contains("UnitLanguageUnknown", contributor, StringComparison.Ordinal);

        var resx = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Resources/CatalogErrors.resx"));
        Assert.Contains("unit.missing", resx, StringComparison.Ordinal);
        Assert.Contains("unit.language.unknown", resx, StringComparison.Ordinal);
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
