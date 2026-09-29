using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-SELLER-AMC-001-R5 — final Host/Seller closure guard.
///
/// The last Host-owned seller surface (the Development route <c>GET /v1/seller/dev-contexts</c> and its
/// seller demo-actor/authorization bootstrap) was evacuated from <c>Host/Seller</c> into the
/// AccessControl Development capability:
/// <c>AccessControl.Endpoints/Seller/Development</c>,
/// <c>AccessControl.Application/Development/Seller</c> and
/// <c>AccessControl.Infrastructure/Development/Seller</c>.
///
/// The guard proves Host/Seller production file count = ZERO, Host seller route count = ZERO, the
/// directory is ABSENT, the route is AccessControl-owned exactly once, cross-module boundaries stay
/// Contracts-only (Party.Contracts / Identity.Contracts), no sink-folder regression into Host/Development
/// happened, and the R1A <c>Host/Security/Seller</c> boundary plus the R1A/R2/R3/R4 ownership remain intact.
/// </summary>
public sealed class HostSellerAmcR5GuardTests
{
    private static readonly Regex ForeignModuleLayerInAccessControlDevelopment = new(
        @"Tooba\.(Catalog|Party|Identity|Order|Offer|Promotion|Returns|Settlement|Notification|Support)\.(Application|Domain|Infrastructure|Persistence)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact]
    public void Host_seller_directory_is_absent_and_production_file_count_is_zero()
    {
        var hostSeller = Path.Combine(HostRoot(), "Seller");
        Assert.False(Directory.Exists(hostSeller), "Host/Seller directory must be absent after R5");

        // Host/Development keeps its accepted allowlist: no seller bootstrap migrated there as a sink.
        var development = Path.Combine(HostRoot(), "Development");
        var developmentFiles = Directory.EnumerateFiles(development, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(
            [
                "DevelopmentSchemaMigrator.cs",
                "DevelopmentTenantCommerceContext.cs",
                "MarketplaceAdminDevBootstrap.cs",
                "MarketplaceDevelopmentBootstrap.cs",
                "MarketplaceSellerDevBootstrap.cs",
            ],
            developmentFiles);

        // Only the pre-existing Filesystem-backed admin grid and the accepted development seed may
        // carry a "Seller" identifier on the Host side; Host/Security/Seller is the R1A boundary.
        var hostSellerResidue = Directory.EnumerateFiles(HostRoot(), "*Seller*", SearchOption.AllDirectories)
            .Select(p => p.Replace('\\', '/'))
            .Where(p => !p.Contains("/obj/", StringComparison.Ordinal) && !p.Contains("/bin/", StringComparison.Ordinal))
            .Where(p => !p.Contains("/Security/Seller/", StringComparison.Ordinal))
            .Where(p => !p.EndsWith("/Development/MarketplaceSellerDevBootstrap.cs", StringComparison.Ordinal))
            .Where(p => !p.EndsWith("/Grid/AdminSellersGridQueryEngine.cs", StringComparison.Ordinal))
            .ToArray();
        Assert.Empty(hostSellerResidue);

        foreach (var evacuated in new[]
                 {
                     "SellerPanelEndpoints.cs",
                     "SellerDevActorBootstrap.cs",
                     "SellerPanelComposer.cs",
                     "SellerPanelModels.cs",
                     "SellerSettingsEndpoints.cs",
                     "SellerDashboardEndpoints.cs",
                 })
        {
            Assert.False(File.Exists(Path.Combine(hostSeller, evacuated)), evacuated);
        }
    }

    [Fact]
    public void Host_seller_route_count_is_zero_and_dev_contexts_is_accesscontrol_owned_once()
    {
        // No Host production file may map a /v1/seller route or reference the removed Host seller seam.
        var hostRoot = HostRoot();
        var violations = new List<string>();
        foreach (var path in Directory.EnumerateFiles(hostRoot, "*.cs", SearchOption.AllDirectories))
        {
            var normalized = path.Replace('\\', '/');
            if (normalized.Contains("/obj/", StringComparison.Ordinal) || normalized.Contains("/bin/", StringComparison.Ordinal))
            {
                continue;
            }

            var text = File.ReadAllText(path);
            if (text.Contains("Tooba.Host.Seller", StringComparison.Ordinal)
                || text.Contains("MapSellerPanelEndpoints", StringComparison.Ordinal))
            {
                violations.Add(Path.GetFileName(path));
            }
        }

        Assert.True(violations.Count == 0, "Host seller route ownership residue: " + string.Join("; ", violations));

        // The evacuated Development route is AccessControl-owned exactly once.
        var endpoint = Read(
            "src/backend/Modules/AccessControl/Tooba.AccessControl.Endpoints/Seller/Development/SellerDevContextEndpoints.cs");
        Assert.Contains("MapGet(\"/dev-contexts\"", endpoint, StringComparison.Ordinal);
        Assert.Contains("GetSellerDevContextsQuery", endpoint, StringComparison.Ordinal);
        Assert.Contains("ISender", endpoint, StringComparison.Ordinal);
        Assert.Contains("\"seller.dev.unavailable\"", endpoint, StringComparison.Ordinal);
        Assert.Contains("\"seller.dev.not-ready\"", endpoint, StringComparison.Ordinal);
        Assert.Contains("IsDevelopment()", endpoint, StringComparison.Ordinal);
        Assert.DoesNotContain("DbContext", endpoint, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.AccessControl.Infrastructure", endpoint, StringComparison.Ordinal);

        var module = Read("src/backend/Modules/AccessControl/Tooba.AccessControl.Endpoints/AccessControlEndpointModule.cs");
        Assert.Equal(1, Regex.Matches(module, @"SellerDevContextEndpoints\.Map\(").Count);

        var program = Read("src/backend/Host/Tooba.Host/Program.cs");
        Assert.DoesNotContain("dev-contexts", program, StringComparison.Ordinal);
        Assert.Contains("MapAccessControlModuleEndpoints()", program, StringComparison.Ordinal);
    }

    [Fact]
    public void No_duplicate_route_ownership_remains_for_dev_contexts()
    {
        var repoRoot = FindRepoRoot();
        var owners = new List<string>();
        foreach (var root in new[]
                 {
                     Path.Combine(repoRoot, "src", "backend", "Modules"),
                     Path.Combine(repoRoot, "src", "backend", "Host", "Tooba.Host"),
                 })
        {
            foreach (var path in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                var normalized = path.Replace('\\', '/');
                if (normalized.Contains("/obj/", StringComparison.Ordinal) || normalized.Contains("/bin/", StringComparison.Ordinal))
                {
                    continue;
                }

                // Only an actual route registration counts as ownership; documentation/test
                // references to the path are not route owners.
                if (File.ReadAllText(path).Contains("MapGet(\"/dev-contexts\"", StringComparison.Ordinal))
                {
                    owners.Add(normalized);
                }
            }
        }

        Assert.Single(owners);
        Assert.Contains("Tooba.AccessControl.Endpoints/Seller/Development/SellerDevContextEndpoints.cs", owners[0].Replace('\\', '/'), StringComparison.Ordinal);
    }

    [Fact]
    public void AccessControl_development_consumes_party_and_identity_contracts_only()
    {
        var developmentRoot = Path.Combine(
            FindRepoRoot(), "src", "backend", "Modules", "AccessControl",
            "Tooba.AccessControl.Infrastructure", "Development", "Seller");
        Assert.True(Directory.Exists(developmentRoot), developmentRoot);

        var bootstrap = Path.Combine(developmentRoot, "SellerDevContextBootstrap.cs");
        Assert.True(File.Exists(bootstrap));
        var text = File.ReadAllText(bootstrap);
        Assert.Contains("Tooba.Party.Contracts", text, StringComparison.Ordinal);
        Assert.Contains("Tooba.Identity.Contracts", text, StringComparison.Ordinal);
        Assert.Contains("IPartyDevelopmentSeedGateway", text, StringComparison.Ordinal);
        Assert.Contains("IIdentityAuthenticationService", text, StringComparison.Ordinal);
        Assert.Contains("IAuthorizationTupleWriter", text, StringComparison.Ordinal);
        Assert.DoesNotContain("PartyDbContext", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IdentityDbContext", text, StringComparison.Ordinal);
        Assert.DoesNotContain("using Microsoft.EntityFrameworkCore", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Party.Infrastructure", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Party.Domain", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Identity.Infrastructure", text, StringComparison.Ordinal);

        // The whole AccessControl development surface stays Contracts-only.
        var violations = new List<string>();
        var acRoot = Path.Combine(FindRepoRoot(), "src", "backend", "Modules", "AccessControl");
        foreach (var path in Directory.EnumerateFiles(acRoot, "*.cs", SearchOption.AllDirectories))
        {
            var normalized = path.Replace('\\', '/');
            if (normalized.Contains("/obj/", StringComparison.Ordinal) || normalized.Contains("/bin/", StringComparison.Ordinal))
            {
                continue;
            }

            if (!normalized.Contains("/Development/", StringComparison.Ordinal))
            {
                continue;
            }

            foreach (var raw in File.ReadLines(path))
            {
                if (ForeignModuleLayerInAccessControlDevelopment.IsMatch(raw))
                {
                    violations.Add(Path.GetFileName(path) + ": " + raw.Trim());
                }
            }
        }

        Assert.True(violations.Count == 0, "foreign module layer in AccessControl Development: " + string.Join("; ", violations));
    }

    [Fact]
    public void Party_development_seam_exposes_no_party_persistence_or_domain_types()
    {
        var contract = Read("src/backend/Modules/Party/Tooba.Party.Contracts/IPartyDevelopmentSeedGateway.cs");
        Assert.Contains("FindDevelopmentOrganizationByDisplayNameAsync", contract, StringComparison.Ordinal);
        Assert.Contains("FindDevelopmentMembershipSellerPartyAsync", contract, StringComparison.Ordinal);
        Assert.Contains("EnsureDevelopmentMemberMembershipAsync", contract, StringComparison.Ordinal);
        Assert.DoesNotContain("PartyDbContext", contract, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Party.Domain", contract, StringComparison.Ordinal);

        // The implementation stays inside Party.Infrastructure (no leak into the contract assembly).
        var implementation = Read("src/backend/Modules/Party/Tooba.Party.Infrastructure/PartyDevelopmentSeedGateway.cs");
        Assert.Contains(": IPartyDevelopmentSeedGateway", implementation, StringComparison.Ordinal);
    }

    [Fact]
    public void Host_development_folder_is_unchanged_and_no_seller_bootstrap_moved_into_host()
    {
        var development = Path.Combine(HostRoot(), "Development");
        var files = Directory.EnumerateFiles(development, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        // The accepted Host/Development allowlist is unchanged: no seller bootstrap moved in.
        Assert.Equal(
            [
                "DevelopmentSchemaMigrator.cs",
                "DevelopmentTenantCommerceContext.cs",
                "MarketplaceAdminDevBootstrap.cs",
                "MarketplaceDevelopmentBootstrap.cs",
                "MarketplaceSellerDevBootstrap.cs",
            ],
            files);

        // The seller dev seam lives in AccessControl only — never as a Host class.
        var accessControlSeam = Path.Combine(
            FindRepoRoot(), "src", "backend", "Modules", "AccessControl",
            "Tooba.AccessControl.Infrastructure", "Development", "Seller");
        Assert.True(Directory.Exists(accessControlSeam), accessControlSeam);

        var hostRoot = HostRoot();
        Assert.False(File.Exists(Path.Combine(hostRoot, "Seller", "SellerDevActorBootstrap.cs")));
        Assert.DoesNotContain(
            "SellerDevActorBootstrap",
            Read("src/backend/Host/Tooba.Host/Program.cs"),
            StringComparison.Ordinal);
    }

    [Fact]
    public void Host_security_seller_boundary_remains_canonical_and_thin()
    {
        var boundaryRoot = Path.Combine(HostRoot(), "Security", "Seller");
        Assert.True(Directory.Exists(boundaryRoot));

        foreach (var file in new[]
                 {
                     "SellerPanelAccess.cs",
                     "SellerSecurityErrorCodes.cs",
                     "HostSellerPanelAccess.cs",
                     "HostOfferSellerAuthorizer.cs",
                     "HostOrderSellerAuthorizer.cs",
                     "HostReturnSellerAuthorizer.cs",
                     "HostSettlementSellerAuthorizer.cs",
                     "HostNotificationSellerAuthorizer.cs",
                     "HostPromotionSellerAuthorizer.cs",
                     "HostPartySellerAuthorizer.cs",
                     "HostSupportSellerAuthorizer.cs",
                     "HostCatalogSellerAuthorizer.cs",
                 })
        {
            Assert.True(File.Exists(Path.Combine(boundaryRoot, file)), file);
        }

        var program = Read("src/backend/Host/Tooba.Host/Program.cs");
        Assert.Contains("Tooba.Host.Security.Seller.HostSellerPanelAccess", program, StringComparison.Ordinal);
        Assert.Contains("Tooba.Host.Security.Seller.HostOfferSellerAuthorizer", program, StringComparison.Ordinal);
        Assert.Contains("Tooba.Host.Security.Seller.HostPartySellerAuthorizer", program, StringComparison.Ordinal);
    }

    [Fact]
    public void R1A_R2_R3_R4_route_and_ownership_remain_intact()
    {
        // R1A: the panel gate and stable seller security codes are Host-boundary owned and unchanged.
        var gate = Read("src/backend/Host/Tooba.Host/Security/Seller/SellerPanelAccess.cs");
        Assert.Contains("SellerSecurityErrorCodes.ActorMissing", gate, StringComparison.Ordinal);
        Assert.Contains("SellerSecurityErrorCodes.AuthorizationDenied", gate, StringComparison.Ordinal);
        Assert.Contains("SellerSecurityErrorCodes.AuthorizationUnavailable", gate, StringComparison.Ordinal);

        // R2: Catalog owns the three seller Catalog routes.
        var catalogSeller = Read("src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Seller/CatalogSellerEndpoints.cs");
        Assert.Contains("MapGet(\"/catalog-variants\"", catalogSeller, StringComparison.Ordinal);

        // R3: Party owns the seller settings pair.
        var partySeller = Read("src/backend/Modules/Party/Tooba.Party.Endpoints/Seller/PartySellerSettingsEndpoints.cs");
        Assert.Contains("RequireViewAsync", partySeller, StringComparison.Ordinal);
        Assert.Contains("RequireManageAsync", partySeller, StringComparison.Ordinal);

        // R4: Order owns the seller dashboard route.
        var orderDashboard = Read("src/backend/Modules/Order/Tooba.Order.Endpoints/Seller/SellerDashboardEndpoints.cs");
        Assert.Contains("MapGet(\"/dashboard\"", orderDashboard, StringComparison.Ordinal);
        Assert.Contains("GetSellerOrderDashboardSummaryQuery", orderDashboard, StringComparison.Ordinal);
    }

    [Fact]
    public void Dev_contexts_behavior_parity_is_preserved()
    {
        var handler = Read(
            "src/backend/Modules/AccessControl/Tooba.AccessControl.Application/Development/Seller/GetSellerDevContextsQueryHandler.cs");
        Assert.Contains("\"seller-owner\"", handler, StringComparison.Ordinal);
        Assert.Contains("\"seller-owner-alt\"", handler, StringComparison.Ordinal);
        Assert.Contains("\"scoped-employee\"", handler, StringComparison.Ordinal);

        var models = Read(
            "src/backend/Modules/AccessControl/Tooba.AccessControl.Application/Development/Seller/SellerDevContextModels.cs");
        Assert.Contains("Guid ActorUserId", models, StringComparison.Ordinal);
        Assert.Contains("string ActorLabel", models, StringComparison.Ordinal);
        Assert.Contains("Guid SellerPartyId", models, StringComparison.Ordinal);
        Assert.Contains("string SellerLabel", models, StringComparison.Ordinal);
        Assert.Contains("string ContextKind", models, StringComparison.Ordinal);

        var bootstrap = Read(
            "src/backend/Modules/AccessControl/Tooba.AccessControl.Infrastructure/Development/Seller/SellerDevContextBootstrap.cs");
        Assert.Contains("seller-actor-a@tooba.local", bootstrap, StringComparison.Ordinal);
        Assert.Contains("seller-actor-b@tooba.local", bootstrap, StringComparison.Ordinal);
        Assert.Contains("اپراتور آرمان", bootstrap, StringComparison.Ordinal);
        Assert.Contains("اپراتور دیجی‌استایل", bootstrap, StringComparison.Ordinal);
        Assert.Contains("AuthorizationRelations.Member", bootstrap, StringComparison.Ordinal);
        Assert.Contains("catch (InvalidOperationException)", bootstrap, StringComparison.Ordinal);
    }

    private static string HostRoot() =>
        Path.Combine(FindRepoRoot(), "src", "backend", "Host", "Tooba.Host");

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
