using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-SELLER-AMC-001-R2 — durable guard proving the three Seller Catalog routes and the
/// Catalog persistence composition were evacuated from Host/Seller into Catalog
/// (Endpoints → Application → Infrastructure), with no duplicate route ownership, no DbContext in
/// Catalog Endpoints, no Catalog layer leakage in Host/Seller and behavior/error-code parity.
/// </summary>
public sealed class HostSellerAmcR2GuardTests
{
    private static readonly string[] EvacuatedCatalogSellerRoutes =
    [
        "MapGet(\"/catalog-variants\"",
        "MapPut(\"/products/{productId:guid}/attributes/{definitionId:guid}\"",
        "MapPut(\"/products/{productId:guid}/variant-axes\"",
    ];

    private static readonly Regex CatalogLayerInHostSeller = new(
        @"CatalogDbContext|Tooba\.Catalog\.Domain|Tooba\.Catalog\.Infrastructure|Tooba\.Catalog\.Application",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact]
    public void Host_seller_owns_zero_route_and_no_catalog_settings_or_dashboard_route()
    {
        // R5 evacuated the final Host-owned seller route (/dev-contexts) into AccessControl.Endpoints.
        var hostSeller = Path.Combine(FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Seller");
        Assert.False(Directory.Exists(hostSeller), "Host/Seller must be absent after R5");

        var accessControlSellerDevelopment = Read(
            "src/backend/Modules/AccessControl/Tooba.AccessControl.Endpoints/Seller/Development/SellerDevContextEndpoints.cs");
        Assert.Contains("MapGet(\"/dev-contexts\"", accessControlSellerDevelopment, StringComparison.Ordinal);
        Assert.DoesNotContain("MapGet(\"/dashboard\"", accessControlSellerDevelopment, StringComparison.Ordinal);
        foreach (var route in EvacuatedCatalogSellerRoutes)
        {
            Assert.DoesNotContain(route, accessControlSellerDevelopment, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("catalog-variants", accessControlSellerDevelopment, StringComparison.Ordinal);
        Assert.DoesNotContain("ICatalogDirectory", accessControlSellerDevelopment, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Catalog.Application", accessControlSellerDevelopment, StringComparison.Ordinal);

        Assert.False(File.Exists(Path.Combine(
            FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Seller", "SellerSettingsEndpoints.cs")));
    }

    [Fact]
    public void Catalog_owns_the_three_seller_routes_exactly_once()
    {
        var catalogSeller = Read("src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Seller/CatalogSellerEndpoints.cs");
        foreach (var route in EvacuatedCatalogSellerRoutes)
        {
            Assert.Contains(route, catalogSeller, StringComparison.Ordinal);
        }

        Assert.Contains("ISender", catalogSeller, StringComparison.Ordinal);
        Assert.Contains("ApiResponseFactory", catalogSeller, StringComparison.Ordinal);
        Assert.Contains("ICatalogSellerAuthorizer", catalogSeller, StringComparison.Ordinal);
        Assert.DoesNotContain("DbContext", catalogSeller, StringComparison.Ordinal);
        Assert.DoesNotContain("ICatalogDirectory", catalogSeller, StringComparison.Ordinal);
        Assert.DoesNotContain("Results.Json", catalogSeller, StringComparison.Ordinal);

        // Module registration wires the seller group exactly once.
        var module = Read("src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/CatalogEndpointModule.cs");
        Assert.Equal(1, Regex.Matches(module, @"MapCatalogSellerEndpoints\(\)").Count);

        // No duplicate ownership anywhere in module or Host endpoint registration.
        var program = Read("src/backend/Host/Tooba.Host/Program.cs");
        Assert.DoesNotContain("MapCatalogSellerEndpoints", program, StringComparison.Ordinal);
        Assert.Contains("MapCatalogModuleEndpoints()", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Catalog_seller_endpoint_reachable_requests_follow_canonical_cqrs_and_validator_classification()
    {
        var catalogSeller = Read("src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Seller/CatalogSellerEndpoints.cs");
        Assert.Contains("ListSellerCatalogVariantsQuery", catalogSeller, StringComparison.Ordinal);
        Assert.Contains("SetProductAttributeCommand", catalogSeller, StringComparison.Ordinal);
        Assert.Contains("SetProductVariantAxesCommand", catalogSeller, StringComparison.Ordinal);

        var app = FindRepoRoot() + "/src/backend/Modules/Catalog/Tooba.Catalog.Application";
        Assert.True(File.Exists(app + "/Seller/Queries/ListSellerCatalogVariantsQuery.cs"));
        Assert.True(File.Exists(app + "/Seller/Queries/ListSellerCatalogVariantsHandler.cs"));

        // AUTH_SCOPED_QUERY => no ceremonial validator for the seller-scoped list query.
        Assert.False(File.Exists(app + "/Seller/Validators/ListSellerCatalogVariantsQueryValidator.cs"));

        // Reused module requests keep W10/W11 validator classification: no new or removed validators.
        Assert.True(File.Exists(app + "/Variants/Validators/SetProductVariantAxesCommandValidator.cs"));
        Assert.False(File.Exists(app + "/Attributes/ProductValues/Validators/SetProductAttributeCommandValidator.cs"));
    }

    [Fact]
    public void Host_seller_has_zero_catalog_persistence_and_zero_catalog_layer_leakage()
    {
        // R5 removed the whole Host/Seller folder; the Catalog-evacuation invariants are now asserted
        // on the retained module surface (AccessControl-owned seller/development endpoint).
        var hostSeller = Path.Combine(FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Seller");
        Assert.False(Directory.Exists(hostSeller), "Host/Seller must be absent after R5");

        var endpoints = Read(
            "src/backend/Modules/AccessControl/Tooba.AccessControl.Endpoints/Seller/Development/SellerDevContextEndpoints.cs");
        Assert.DoesNotContain("ListCatalogVariantsAsync", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("CatalogPublicationStatus", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("LocalizedTexts", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("SellerCatalogVariantOption", endpoints, StringComparison.Ordinal);
    }

    [Fact]
    public void Catalog_owns_seller_variant_model_parity_and_published_ordering()
    {
        var model = Read("src/backend/Modules/Catalog/Tooba.Catalog.Application/Seller/Models/SellerCatalogVariantOption.cs");
        Assert.Contains("Guid CatalogVariantId", model, StringComparison.Ordinal);
        Assert.Contains("Guid ProductId", model, StringComparison.Ordinal);
        Assert.Contains("string ProductTitle", model, StringComparison.Ordinal);
        Assert.Contains("string? CatalogCode", model, StringComparison.Ordinal);
        Assert.Contains("string ProductStatus", model, StringComparison.Ordinal);

        var directory = Read("src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/Seller/SellerCatalogVariantDirectory.cs");
        Assert.Contains("CatalogPublicationStatus.Published", directory, StringComparison.Ordinal);
        Assert.Contains("Take(100)", directory, StringComparison.Ordinal);
        Assert.Contains("OrderByDescending(x => x.UpdatedAt)", directory, StringComparison.Ordinal);
        Assert.Contains("StartsWith(\"fa\")", directory, StringComparison.Ordinal);
        Assert.Contains("OrderBy(x => x.CatalogCodeSeam)", directory, StringComparison.Ordinal);

        var module = Read("src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/CatalogModule.cs");
        Assert.Contains("ISellerCatalogVariantDirectory, Seller.SellerCatalogVariantDirectory", module, StringComparison.Ordinal);
    }

    [Fact]
    public void Seller_catalog_error_codes_keep_parity_and_no_duplicate_descriptor_is_registered()
    {
        var codes = Read("src/backend/Modules/Catalog/Tooba.Catalog.Application/Seller/SellerCatalogErrorCodes.cs");
        Assert.Contains("\"seller.missing\"", codes, StringComparison.Ordinal);

        // seller.missing descriptor/localization stays owned by Order; Catalog must not re-register it.
        var contributor = Read("src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Errors/CatalogErrorCatalogContributor.cs");
        Assert.DoesNotContain("\"seller.missing\"", contributor, StringComparison.Ordinal);

        // Stable Catalog business codes used by the evacuated write routes stay descriptor-backed.
        var catalogCodes = Read("src/backend/Modules/Catalog/Tooba.Catalog.Contracts/Errors/CatalogErrorCodes.cs");
        Assert.Contains("\"catalog.attribute.invalid\"", catalogCodes, StringComparison.Ordinal);
        Assert.Contains("\"catalog.variant.axes.duplicate\"", catalogCodes, StringComparison.Ordinal);
    }

    [Fact]
    public void Host_catalog_seller_authorizer_is_a_thin_security_adapter_inside_the_r1a_boundary()
    {
        var adapterPath = Path.Combine(
            FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Security", "Seller", "HostCatalogSellerAuthorizer.cs");
        Assert.True(File.Exists(adapterPath), "HostCatalogSellerAuthorizer.cs must live under Host/Security/Seller");

        var adapter = File.ReadAllText(adapterPath);
        Assert.Contains("namespace Tooba.Host.Security.Seller;", adapter, StringComparison.Ordinal);
        Assert.Contains(": ICatalogSellerAuthorizer", adapter, StringComparison.Ordinal);
        Assert.Contains("ISellerPanelAccess sellerAccess", adapter, StringComparison.Ordinal);
        Assert.DoesNotContain("RequestServices", adapter, StringComparison.Ordinal);
        Assert.DoesNotContain("DbContext", adapter, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Catalog.Application", adapter, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Catalog.Domain", adapter, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Catalog.Infrastructure", adapter, StringComparison.Ordinal);

        var program = Read("src/backend/Host/Tooba.Host/Program.cs");
        Assert.Contains(
            "Tooba.Catalog.Endpoints.Seller.ICatalogSellerAuthorizer, Tooba.Host.Security.Seller.HostCatalogSellerAuthorizer",
            program,
            StringComparison.Ordinal);

        // R1A boundary untouched: the panel gate still owns the stable seller security codes.
        var gate = Read("src/backend/Host/Tooba.Host/Security/Seller/SellerPanelAccess.cs");
        Assert.Contains("SellerSecurityErrorCodes.ActorMissing", gate, StringComparison.Ordinal);
        Assert.Contains("SellerSecurityErrorCodes.AuthorizationUnavailable", gate, StringComparison.Ordinal);
    }

    [Fact]
    public void No_route_sink_regression_and_no_host_seller_business_file_remains()
    {
        var hostSeller = Path.Combine(FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Seller");

        // R3 removed SellerSettingsEndpoints.cs, R4 removed the zero-consumer SellerPanelComposer.cs and
        // SellerPanelModels.cs, and R5 evacuated the final two files: Host/Seller is ABSENT.
        Assert.False(Directory.Exists(hostSeller), "Host/Seller must be absent after R5");
        Assert.False(File.Exists(Path.Combine(hostSeller, "HostCatalogSellerAuthorizer.cs")));
    }

    private static string Read(string relativePath) =>
        File.ReadAllText(Path.Combine(FindRepoRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar)));

    private static string FindRepoRoot()
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

        throw new InvalidOperationException("Repository root not found.");
    }
}
