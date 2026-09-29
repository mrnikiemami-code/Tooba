using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-SELLER-AMC-001-R3 — durable guard proving the two Host-owned Seller settings routes
/// (<c>GET /v1/seller/settings</c>, <c>PUT /v1/seller/settings</c>) and the seller settings capability
/// checks were evacuated from <c>Host/Seller</c> into Party (Endpoints -> Application -> Infrastructure),
/// with a narrow neutral <c>IPartySellerAuthorizer</c> port in <c>Party.Endpoints.Seller</c> and a thin
/// Host adapter inside the unchanged R1A <c>Host/Security/Seller</c> boundary.
/// Host/Seller route count 4 -> 2, file count 5 -> 4, and the evacuated Host settings layer leakage is ZERO.
/// R5 then evacuated the last Host/Seller file and route, so the folder is ABSENT and both invariants hold.
/// </summary>
public sealed class HostSellerAmcR3GuardTests
{
    private static readonly string[] EvacuatedSettingsRoutes = ["MapGet(\"/\"", "MapPut(\"/\""];

    [Fact]
    public void Party_endpoints_own_both_seller_settings_routes_exactly_once()
    {
        var endpoints = Read("src/backend/Modules/Party/Tooba.Party.Endpoints/Seller/PartySellerSettingsEndpoints.cs");
        foreach (var route in EvacuatedSettingsRoutes)
        {
            Assert.Contains(route, endpoints, StringComparison.Ordinal);
        }

        Assert.Contains("ISender", endpoints, StringComparison.Ordinal);
        Assert.Contains("ApiResponseFactory", endpoints, StringComparison.Ordinal);
        Assert.Contains("IPartySellerAuthorizer", endpoints, StringComparison.Ordinal);
        Assert.Contains("canManage", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("DbContext", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("IAccessControlDirectory", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("Results.Json", endpoints, StringComparison.Ordinal);

        // Exactly two route mappings in the Party-owned seller settings surface.
        Assert.Equal(2, Regex.Matches(endpoints, @"Map(?:Get|Post|Put|Patch|Delete)\(").Count);
    }

    [Fact]
    public void Party_module_registers_and_maps_the_seller_settings_surface_once()
    {
        var module = Read("src/backend/Modules/Party/Tooba.Party.Endpoints/PartyEndpointModule.cs");
        Assert.Equal(1, Regex.Matches(module, @"PartySellerSettingsEndpoints\.Map\(").Count);
        Assert.Contains("/v1/seller/settings", module, StringComparison.Ordinal);
        Assert.Contains("IErrorCatalogContributor, PartyErrorCatalogContributor", module, StringComparison.Ordinal);
        Assert.Contains("IErrorResourceSet, PartyErrorResourceSet", module, StringComparison.Ordinal);

        var program = Read("src/backend/Host/Tooba.Host/Program.cs");
        Assert.Contains("MapPartyEndpoints()", program, StringComparison.Ordinal);
        Assert.Contains("AddPartyEndpointPresentation()", program, StringComparison.Ordinal);
        // Host must not register the evacuated settings route mapping any more.
        Assert.DoesNotContain("MapSellerSettingsEndpoints", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Host_seller_settings_file_is_absent_and_folder_is_gone()
    {
        var hostSeller = Path.Combine(FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Seller");
        Assert.False(File.Exists(Path.Combine(hostSeller, "SellerSettingsEndpoints.cs")));

        // R3 shrank Host/Seller to four files; R4 then removed the now-zero-consumer SellerPanelComposer.cs
        // and SellerPanelModels.cs, and R5 evacuated the final two files: Host/Seller is ABSENT.
        Assert.False(Directory.Exists(hostSeller), "Host/Seller must be absent after R5");
    }

    [Fact]
    public void AccessControl_owns_the_dev_contexts_route_and_host_owns_zero_seller_route()
    {
        var endpoints = Read(
            "src/backend/Modules/AccessControl/Tooba.AccessControl.Endpoints/Seller/Development/SellerDevContextEndpoints.cs");
        Assert.Contains("MapGet(\"/dev-contexts\"", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("MapGet(\"/settings\"", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPut(\"/settings\"", endpoints, StringComparison.Ordinal);

        var hostSeller = Path.Combine(FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Seller");
        Assert.False(Directory.Exists(hostSeller), "Host/Seller must be absent after R5");
    }

    [Fact]
    public void Host_seller_has_zero_settings_layer_leakage()
    {
        // R5 removed the whole Host/Seller folder, so the settings-layer leakage invariant holds vacuously.
        var hostSeller = Path.Combine(FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Seller");
        Assert.False(Directory.Exists(hostSeller), "Host/Seller must be absent after R5");
    }

    [Fact]
    public void Party_owns_the_seller_settings_cqrs_and_validator_classification()
    {
        var app = FindRepoRoot() + "/src/backend/Modules/Party/Tooba.Party.Application/Seller";
        Assert.True(File.Exists(app + "/Queries/GetSellerSettingsQuery.cs"));
        Assert.True(File.Exists(app + "/Commands/UpdateSellerSettingsCommand.cs"));
        Assert.True(File.Exists(app + "/Models/PartySellerSettingsModels.cs"));

        // Write command carries the required transport validator; the auth-scoped read query needs none.
        Assert.True(File.Exists(app + "/Validators/UpdateSellerSettingsCommandValidator.cs"));
        Assert.False(File.Exists(app + "/Validators/GetSellerSettingsQueryValidator.cs"));

        var query = Read("src/backend/Modules/Party/Tooba.Party.Application/Seller/Queries/GetSellerSettingsQuery.cs");
        Assert.Contains("IRequest<Result<PartySellerSettingsView>>", query, StringComparison.Ordinal);
        Assert.Contains("IPartySellerSettings", query, StringComparison.Ordinal);

        var command = Read("src/backend/Modules/Party/Tooba.Party.Application/Seller/Commands/UpdateSellerSettingsCommand.cs");
        Assert.Contains("IRequest<Result<PartySellerSettingsView>>", command, StringComparison.Ordinal);
        Assert.Contains("PartySellerSettingsErrorCodes.Rejected", command, StringComparison.Ordinal);

        var infrastructure = Read("src/backend/Modules/Party/Tooba.Party.Infrastructure/Seller/PartySellerSettingsAdapter.cs");
        Assert.Contains(": IPartySellerSettings", infrastructure, StringComparison.Ordinal);
        Assert.Contains("IPartyDirectory", infrastructure, StringComparison.Ordinal);
        Assert.DoesNotContain("PartyDbContext", infrastructure, StringComparison.Ordinal);
        Assert.DoesNotContain("using Microsoft.EntityFrameworkCore", infrastructure, StringComparison.Ordinal);

        var module = Read("src/backend/Modules/Party/Tooba.Party.Infrastructure/PartyModule.cs");
        Assert.Contains("IPartySellerSettings, Tooba.Party.Infrastructure.Seller.PartySellerSettingsAdapter", module, StringComparison.Ordinal);
    }

    [Fact]
    public void Seller_settings_error_codes_keep_parity_and_no_duplicate_descriptor_is_registered()
    {
        var codes = Read("src/backend/Modules/Party/Tooba.Party.Application/Seller/PartySellerSettingsErrorCodes.cs");
        Assert.Contains("\"seller.settings.missing\"", codes, StringComparison.Ordinal);
        Assert.Contains("\"seller.settings.rejected\"", codes, StringComparison.Ordinal);

        var contributor = Read("src/backend/Modules/Party/Tooba.Party.Endpoints/Errors/PartyErrorCatalogContributor.cs");
        Assert.Contains("PartySellerSettingsErrorCodes.Missing", contributor, StringComparison.Ordinal);
        Assert.Contains("PartySellerSettingsErrorCodes.Rejected", contributor, StringComparison.Ordinal);
        // seller.authorization.denied stays owned by FoundationErrorCatalogContributor (no duplicate).
        Assert.DoesNotContain("\"seller.authorization.denied\"", contributor, StringComparison.Ordinal);

        var query = Read("src/backend/Modules/Party/Tooba.Party.Application/Seller/Queries/GetSellerSettingsQuery.cs");
        Assert.Contains("PartySellerSettingsErrorCodes.Missing", query, StringComparison.Ordinal);
    }

    [Fact]
    public void Host_party_seller_authorizer_is_a_thin_neutral_security_adapter_inside_the_r1a_boundary()
    {
        var adapterPath = Path.Combine(
            FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Security", "Seller", "HostPartySellerAuthorizer.cs");
        Assert.True(File.Exists(adapterPath), "HostPartySellerAuthorizer.cs must live under Host/Security/Seller");

        var adapter = File.ReadAllText(adapterPath);
        Assert.Contains("namespace Tooba.Host.Security.Seller;", adapter, StringComparison.Ordinal);
        Assert.Contains(": IPartySellerAuthorizer", adapter, StringComparison.Ordinal);
        Assert.Contains("ISellerPanelAccess sellerAccess", adapter, StringComparison.Ordinal);
        Assert.Contains("IPlatformEffectiveAccessReader", adapter, StringComparison.Ordinal);
        Assert.Contains("PlatformAccessOwnerKind.Seller", adapter, StringComparison.Ordinal);
        Assert.Contains("PlatformAccessScopeKind.GlobalWithinOwner", adapter, StringComparison.Ordinal);
        Assert.Contains("DeniedByCeiling", adapter, StringComparison.Ordinal);
        Assert.Contains("SellerSecurityErrorCodes.AuthorizationDenied", adapter, StringComparison.Ordinal);
        Assert.DoesNotContain("RequestServices", adapter, StringComparison.Ordinal);
        Assert.DoesNotContain("DbContext", adapter, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Party.Application", adapter, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Party.Domain", adapter, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Party.Infrastructure", adapter, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.AccessControl", adapter, StringComparison.Ordinal);

        var program = Read("src/backend/Host/Tooba.Host/Program.cs");
        Assert.Contains(
            "Tooba.Party.Endpoints.Seller.IPartySellerAuthorizer, Tooba.Host.Security.Seller.HostPartySellerAuthorizer",
            program,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Behavior_parity_and_recovery_invariants_are_preserved()
    {
        var endpoints = Read("src/backend/Modules/Party/Tooba.Party.Endpoints/Seller/PartySellerSettingsEndpoints.cs");
        // Exact path -> namespace mapping and HTTP verb set unchanged.
        Assert.Contains("RequireViewAsync", endpoints, StringComparison.Ordinal);
        Assert.Contains("RequireManageAsync", endpoints, StringComparison.Ordinal);
        Assert.Equal(2, Regex.Matches(endpoints, @"Map(?:Get|Put)\(").Count);

        var module = Read("src/backend/Modules/Party/Tooba.Party.Endpoints/PartyEndpointModule.cs");
        Assert.Contains("MapGroup(\"/v1/seller/settings\")", module, StringComparison.Ordinal);

        // R1A panel gate untouched.
        var gate = Read("src/backend/Host/Tooba.Host/Security/Seller/SellerPanelAccess.cs");
        Assert.Contains("SellerSecurityErrorCodes.ActorMissing", gate, StringComparison.Ordinal);
        Assert.Contains("SellerSecurityErrorCodes.AuthorizationDenied", gate, StringComparison.Ordinal);
        Assert.Contains("SellerSecurityErrorCodes.AuthorizationUnavailable", gate, StringComparison.Ordinal);

        // R2 Catalog evacuation invariants remain intact.
        Assert.True(File.Exists(Path.Combine(
            FindRepoRoot(), "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Seller/CatalogSellerEndpoints.cs")));
        Assert.True(File.Exists(Path.Combine(
            FindRepoRoot(), "src/backend/Host/Tooba.Host/Security/Seller/HostCatalogSellerAuthorizer.cs")));

        // No route sink-folder regression: the evacuated surface lives in the module, not Host.
        Assert.False(Directory.Exists(Path.Combine(
            FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Seller")));
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
