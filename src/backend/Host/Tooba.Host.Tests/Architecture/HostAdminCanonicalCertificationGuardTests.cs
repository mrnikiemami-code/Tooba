using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-ADMIN-CANON-010-FINAL-CERT — durable certification guard for the whole recursive
/// Host/Admin tree. Host/Admin is CERTIFIED as a canonical Host platform boundary: it may hold only
/// platform panel authorization, thin module endpoint-authorizer adapters, cross-module admin panel
/// composition through lawful Contracts/seams, the generic admin grid HTTP boundary, and the
/// Development-only admin bootstrap.
/// </summary>
/// <remarks>
/// Service-locator posture: <c>HttpContext.RequestServices</c> is ZERO across all 15 files. The
/// explicit <c>IServiceProvider provider</c> parameter of the Development bootstrap is the canonical
/// repo DI composition pattern accepted by CANON-007 and is therefore the ONLY place where
/// <c>GetRequiredService</c> may appear.
/// </remarks>
public sealed class HostAdminCanonicalCertificationGuardTests
{
    private const string AdminProject = "src/backend/Host/Tooba.Host/Admin";
    private const string HostProject = "src/backend/Host/Tooba.Host";

    private const string DevelopmentBootstrap = "Development/AdminDevActorBootstrap.cs";

    private static readonly string[] ForeignBusinessModules =
    [
        "Tooba.AccessControl",
        "Tooba.AddressBook",
        "Tooba.Analytics",
        "Tooba.Cart",
        "Tooba.Catalog",
        "Tooba.Content",
        "Tooba.Fulfillment",
        "Tooba.Identity",
        "Tooba.Inventory",
        "Tooba.Media",
        "Tooba.Notification",
        "Tooba.Offer",
        "Tooba.Order",
        "Tooba.Party",
        "Tooba.Payment",
        "Tooba.Pricing",
        "Tooba.Promotion",
        "Tooba.Returns",
        "Tooba.Settlement",
        "Tooba.Storage",
        "Tooba.Support",
        "Tooba.Wallet",
    ];

    private static readonly Dictionary<string, string[]> ExpectedStructure = new(StringComparer.Ordinal)
    {
        ["Access"] =
        [
            "AdminPanelAccess.cs",
            "HostAdminPanelAccess.cs",
        ],
        ["Access/Authorizers"] =
        [
            "HostLocalizationAdminAuthorizer.cs",
            "HostOperatorProfileAdminAuthorizer.cs",
            "HostOrderAdminAuthorizer.cs",
            "HostOrderAdminEffectiveAccessReader.cs",
            "HostPaymentAdminAuthorizer.cs",
            "HostReturnAdminAuthorizer.cs",
            "HostSettlementAdminAuthorizer.cs",
            "HostSupportAdminAuthorizer.cs",
            "HostUserPreferenceAdminAuthorizer.cs",
            "HostWalletAdminAuthorizer.cs",
        ],
        ["Panel"] =
        [
            "AdminPanelComposer.cs",
            "AdminPanelEndpoints.cs",
            "AdminPanelModels.cs",
        ],
        ["Development"] =
        [
            "AdminDevActorBootstrap.cs",
            "AdminDevContextEndpoints.cs",
        ],
    };

    private static readonly string[] EvacuatedBusinessEndpointResidue =
    [
        "AdminOrderOperationsEndpoints.cs",
        "AdminOrderOperationsComposer.cs",
        "AdminOrderOperationsModels.cs",
        "ProductWorkspaceEndpoints.cs",
        "QuantitySettingsEndpoints.cs",
        "CatalogAttributeEndpoints.cs",
        "StoreAppearanceSettingsEndpoints.cs",
        "StoreAppearanceSettingsComposer.cs",
        "HoldPolicySettingsEndpoints.cs",
    ];

    // ---------------------------------------------------------------------
    // 1. Structure and namespace
    // ---------------------------------------------------------------------

    [Fact]
    public void Certified_structure_is_exactly_18_recursive_files_with_zero_flat_root()
    {
        var root = AdminRoot();
        Assert.Empty(Directory.GetFiles(root, "*.cs", SearchOption.TopDirectoryOnly));

        var discovered = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
            .Select(p => Path.GetRelativePath(root, p).Replace('\\', '/'))
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        var expected = ExpectedStructure
            .SelectMany(kv => kv.Value.Select(f => $"{kv.Key}/{f}"))
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(17, discovered.Length);
        Assert.Equal(expected, discovered);

        var folders = Directory.GetDirectories(root)
            .Select(Path.GetFileName)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(["Access", "Development", "Panel"], folders);

        var nested = Directory.GetDirectories(Path.Combine(root, "Access"))
            .Select(Path.GetFileName)
            .ToArray();
        Assert.Equal(["Authorizers"], nested);
    }

    [Fact]
    public void Every_certified_file_namespace_matches_its_capability_path_exactly()
    {
        var root = AdminRoot();
        foreach (var (folder, files) in ExpectedStructure)
        {
            var expectedNamespace = "Tooba.Host.Admin." + folder.Replace('/', '.');
            foreach (var file in files)
            {
                var path = Path.Combine(root, folder.Replace('/', Path.DirectorySeparatorChar), file);
                Assert.True(File.Exists(path), $"missing {path}");
                var match = Regex.Match(File.ReadAllText(path), @"(?m)^namespace\s+([A-Za-z0-9_.]+);");
                Assert.True(match.Success, $"no file-scoped namespace in {file}");
                Assert.Equal(expectedNamespace, match.Groups[1].Value);
            }
        }

        var stale = EnumerateAdminFiles()
            .Where(p => Regex.IsMatch(File.ReadAllText(p), @"(?m)^namespace\s+Tooba\.Host\.Admin;"))
            .Select(Path.GetFileName)
            .ToArray();
        Assert.Empty(stale);
    }

    // ---------------------------------------------------------------------
    // 2. Foreign module layer boundaries
    // ---------------------------------------------------------------------

    [Fact]
    public void Host_admin_has_zero_foreign_application_infrastructure_or_domain_reference()
    {
        var lines = EnumerateAdminFiles()
            .SelectMany(p => File.ReadAllLines(p)
                .Select(l => (File: Path.GetFileName(p), Text: l.Trim())))
            .Where(x => x.Text.StartsWith("using ", StringComparison.Ordinal))
            .ToArray();

        var violations = lines
            .Where(x => ForeignBusinessModules.Any(m =>
                x.Text.StartsWith($"using {m}.Application", StringComparison.Ordinal)
                || x.Text.StartsWith($"using {m}.Infrastructure", StringComparison.Ordinal)
                || x.Text.StartsWith($"using {m}.Domain", StringComparison.Ordinal)))
            .Select(x => $"{x.File}: {x.Text}")
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void Every_module_boundary_consumed_is_contracts_or_endpoints_seam_or_neutral()
    {
        var allowed = new[]
        {
            "Tooba.BuildingBlocks",
            "Tooba.Host.",
            "Tooba.Catalog.Contracts",
            "Tooba.Offer.Contracts",
            "Tooba.Order.Contracts",
            "Tooba.Party.Contracts",
            "Tooba.Identity.Contracts",
            "Tooba.Order.Endpoints",
            "Tooba.Payment.Endpoints",
            "Tooba.Promotion.Endpoints",
            "Tooba.Returns.Endpoints",
            "Tooba.Settlement.Endpoints",
            "Tooba.Support.Endpoints",
            "Tooba.Wallet.Endpoints",
            "Tooba.Localization.Endpoints",
            "Tooba.OperatorProfile.Endpoints",
            "Tooba.UserPreference.Endpoints",
            "Microsoft.AspNetCore.Http",
        };

        var moduleUsings = EnumerateAdminFiles()
            .SelectMany(p => File.ReadAllLines(p)
                .Select(l => l.Trim())
                .Where(l => l.StartsWith("using Tooba.", StringComparison.Ordinal)))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        Assert.NotEmpty(moduleUsings);
        foreach (var line in moduleUsings)
        {
            Assert.Contains(allowed, a => line.StartsWith($"using {a}", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void Host_admin_has_zero_persistence_grid_projection_or_transaction_ownership()
    {
        var forbidden = new[]
        {
            "DbContext",
            "IQueryable",
            "Microsoft.EntityFrameworkCore",
            "SaveChanges",
            "SaveChangesAsync",
            "BeginTransaction",
            "TransactionScope",
            "Database.Migrate",
        };

        var violations = new List<string>();
        foreach (var path in EnumerateAdminFiles())
        {
            var text = File.ReadAllText(path);
            foreach (var token in forbidden)
            {
                if (text.Contains(token, StringComparison.Ordinal))
                {
                    violations.Add($"{Path.GetFileName(path)}: {token}");
                }
            }
        }

        Assert.Empty(violations);
    }

    // ---------------------------------------------------------------------
    // 3. Service locator
    // ---------------------------------------------------------------------

    [Fact]
    public void Host_admin_has_zero_service_locator_and_only_dev_bootstrap_resolves_the_provider()
    {
        foreach (var path in EnumerateAdminFiles())
        {
            var relative = Path.GetRelativePath(AdminRoot(), path).Replace('\\', '/');
            var text = File.ReadAllText(path);

            Assert.DoesNotContain("RequestServices", text);
            Assert.DoesNotContain("HttpContext.RequestServices", text);

            if (relative == DevelopmentBootstrap)
            {
                Assert.Contains("IServiceProvider provider", text, StringComparison.Ordinal);
                Assert.Contains("provider.GetRequiredService", text, StringComparison.Ordinal);
                continue;
            }

            Assert.DoesNotContain("GetRequiredService", text);
            Assert.DoesNotContain("GetService(", text);
        }
    }

    // ---------------------------------------------------------------------
    // 4. Business ownership and write logic
    // ---------------------------------------------------------------------

    [Fact]
    public void Host_admin_owns_no_business_write_or_message_parsing_classification()
    {
        foreach (var path in EnumerateAdminFiles())
        {
            var text = File.ReadAllText(path);
            var file = Path.GetFileName(path);

            Assert.DoesNotContain("ex.Message", text);
            Assert.DoesNotContain("exception.Message", text);
            Assert.DoesNotContain("ISender", text);
            Assert.DoesNotContain("MediatR", text);
            Assert.DoesNotContain("ICommandHandler", text);
            Assert.DoesNotContain("IRequestHandler", text);

            if (file != "AdminPanelModels.cs")
            {
                Assert.DoesNotContain("aggregate", text, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    // ---------------------------------------------------------------------
    // 5. Authorization fail-closed
    // ---------------------------------------------------------------------

    [Fact]
    public void Capability_adapters_fail_closed_and_hold_no_fail_open_branch()
    {
        var failureReturn = new Regex(
            @"Unavailable[^;]*?return\s+(?!PlatformHttpException)",
            RegexOptions.Singleline);

        foreach (var path in EnumerateAdminFiles())
        {
            var text = File.ReadAllText(path);
            var file = Path.GetFileName(path);

            Assert.DoesNotContain("fail-open", text, StringComparison.Ordinal);
            Assert.DoesNotContain("fail open", text, StringComparison.Ordinal);
            Assert.False(
                failureReturn.IsMatch(text),
                $"{file}: fail-open branch after Unavailable");

            if (file == "AdminPanelAccess.cs" || file == "HostAdminPanelAccess.cs")
            {
                continue;
            }

            if (text.Contains("AuthorizationDecisionKind.Unavailable", StringComparison.Ordinal))
            {
                Assert.Contains("SemanticException", text, StringComparison.Ordinal);
                Assert.DoesNotContain("PlatformHttpException", text, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void Panel_authorization_policy_is_centralized_on_the_platform_seam()
    {
        var root = AdminRoot();

        foreach (var file in ExpectedStructure["Access/Authorizers"])
        {
            // HostOrderAdminEffectiveAccessReader is the neutral effective-access adapter, not a
            // panel-gate authorizer; its boundary is audited by its own certification fact.
            if (file == "HostOrderAdminEffectiveAccessReader.cs")
            {
                continue;
            }

            var text = File.ReadAllText(Path.Combine(root, "Access", "Authorizers", file));
            Assert.Contains("IAdminPanelAccess", text, StringComparison.Ordinal);
            Assert.Contains("adminAccess.RequireAuthorizedAsync(", text, StringComparison.Ordinal);
        }

        var panelAccess = File.ReadAllText(Path.Combine(root, "Access", "HostAdminPanelAccess.cs"));
        Assert.Contains(": IAdminPanelAccess", panelAccess, StringComparison.Ordinal);
        Assert.Contains("MarketplacePlatformTenantId", panelAccess, StringComparison.Ordinal);

        var settlement = File.ReadAllText(Path.Combine(root, "Access", "Authorizers", "HostSettlementAdminAuthorizer.cs"));
        Assert.DoesNotContain("AuthorizationCheck", settlement, StringComparison.Ordinal);
        Assert.DoesNotContain("MarketplacePlatformTenantId", settlement, StringComparison.Ordinal);

        foreach (var file in new[]
                 {
                     "HostPaymentAdminAuthorizer.cs",
                     "HostReturnAdminAuthorizer.cs",
                     "HostSettlementAdminAuthorizer.cs",
                 })
        {
            var text = File.ReadAllText(Path.Combine(root, "Access", "Authorizers", file));
            Assert.DoesNotContain("ICurrentTenant", text, StringComparison.Ordinal);
            Assert.DoesNotContain("AuthorizationCheck", text, StringComparison.Ordinal);
        }
    }

    // ---------------------------------------------------------------------
    // 6. Module-specific boundary audits
    // ---------------------------------------------------------------------

    [Fact]
    public void AdminPanelComposer_boundary_is_contracts_only()
    {
        var text = File.ReadAllText(Path.Combine(AdminRoot(), "Panel", "AdminPanelComposer.cs"));

        foreach (var module in ForeignBusinessModules)
        {
            Assert.DoesNotContain($"using {module}.Application", text, StringComparison.Ordinal);
            Assert.DoesNotContain($"using {module}.Infrastructure", text, StringComparison.Ordinal);
            Assert.DoesNotContain($"using {module}.Domain", text, StringComparison.Ordinal);
        }

        Assert.Contains("using Tooba.Catalog.Contracts;", text, StringComparison.Ordinal);
        Assert.DoesNotContain("using Tooba.Party.Contracts;", text, StringComparison.Ordinal);
        Assert.Contains("using Tooba.Order.Contracts.Admin;", text, StringComparison.Ordinal);
        Assert.Contains("using Tooba.Offer.Contracts.Ports;", text, StringComparison.Ordinal);
        Assert.Contains("ICatalogAdminProductCountGateway", text, StringComparison.Ordinal);
        Assert.Contains("IAdminOrderDashboardMetricsPort", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IAdminSellersGridPort", text, StringComparison.Ordinal);
        Assert.DoesNotContain("ListSellersAsync", text, StringComparison.Ordinal);
        Assert.DoesNotContain("QuerySellersGridAsync", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IPartyAdminSellerReadGateway", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IAdminSellerOrderCountPort", text, StringComparison.Ordinal);

        var sellersQuery = File.ReadAllText(RepoFile(
            "src/backend/Modules/Party/Tooba.Party.Application/Admin/Sellers/Queries/ListAdminSellersQuery.cs"));
        Assert.Contains("IPartyAdminSellerReadGateway", sellersQuery, StringComparison.Ordinal);
        Assert.Contains("IAdminSellerOrderCountPort", sellersQuery, StringComparison.Ordinal);

        var sellersGridQuery = File.ReadAllText(RepoFile(
            "src/backend/Modules/Party/Tooba.Party.Application/Admin/Sellers/Queries/QueryAdminSellersGridQuery.cs"));
        Assert.Contains("IAdminSellersGridPort", sellersGridQuery, StringComparison.Ordinal);
        Assert.Contains("ex.ErrorCode", sellersGridQuery, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", sellersGridQuery, StringComparison.Ordinal);
    }

    [Fact]
    public void Seller_grid_boundary_is_contracts_only()
    {
        var text = File.ReadAllText(RepoFile(
            "src/backend/Modules/Party/Tooba.Party.Infrastructure/Grid/AdminSellersGridQueryEngine.cs"));

        Assert.DoesNotContain("DbContext", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IQueryable", text, StringComparison.Ordinal);
        Assert.Contains("IPartyAdminSellerReadGateway", text, StringComparison.Ordinal);
        Assert.Contains("IAdminSellerOrderCountPort", text, StringComparison.Ordinal);

        var composer = File.ReadAllText(Path.Combine(AdminRoot(), "Panel", "AdminPanelComposer.cs"));
        Assert.DoesNotContain("IAdminSellersGridPort", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("AdminListGridPolicies", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("AdminSellersGridQueryEngine", composer, StringComparison.Ordinal);
    }

    [Fact]
    public void Order_authorizer_and_effective_access_boundaries_are_neutral()
    {
        var root = AdminRoot();

        var authorizer = File.ReadAllText(Path.Combine(root, "Access", "Authorizers", "HostOrderAdminAuthorizer.cs"));
        Assert.Contains("IAdminPanelAccess adminAccess", authorizer, StringComparison.Ordinal);
        Assert.Contains("IAuthorizationService authz", authorizer, StringComparison.Ordinal);
        Assert.Contains("AuthorizationDecisionKind.Unavailable", authorizer, StringComparison.Ordinal);
        Assert.Contains("OrderErrorCodes.AuthorizationUnavailable", authorizer, StringComparison.Ordinal);
        Assert.Contains("OrderErrorCodes.OperationDenied", authorizer, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.AccessControl", authorizer, StringComparison.Ordinal);

        var reader = File.ReadAllText(Path.Combine(root, "Access", "Authorizers", "HostOrderAdminEffectiveAccessReader.cs"));
        Assert.Contains("IPlatformEffectiveAccessReader", reader, StringComparison.Ordinal);
        Assert.Contains("using Tooba.Order.Contracts.Admin.Operations;", reader, StringComparison.Ordinal);
        Assert.Contains("IOrderAdminEffectiveAccessReader", reader, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.AccessControl", reader, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Order.Application", reader, StringComparison.Ordinal);
    }

    [Fact]
    public void Development_bootstrap_boundary_is_identity_contracts_only()
    {
        var text = File.ReadAllText(Path.Combine(AdminRoot(), "Development", "AdminDevActorBootstrap.cs"));

        Assert.Contains("using Tooba.Identity.Contracts;", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Identity.Infrastructure", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Identity.Domain", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Identity.Application", text, StringComparison.Ordinal);
        Assert.DoesNotContain("catch (InvalidOperationException)", text, StringComparison.Ordinal);
        Assert.Contains("catch (IdentityDuplicateIdentifierFault)", text, StringComparison.Ordinal);
        Assert.Contains("IAuthorizationTupleWriter", text, StringComparison.Ordinal);
        Assert.Contains("AuthorizationRelations.Member", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Support_and_wallet_admin_adapters_use_endpoints_owned_seams_only()
    {
        var root = Path.Combine(AdminRoot(), "Access", "Authorizers");

        var support = File.ReadAllText(Path.Combine(root, "HostSupportAdminAuthorizer.cs"));
        Assert.Contains("using Tooba.Support.Endpoints.Admin;", support, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Support.Application", support, StringComparison.Ordinal);
        Assert.Contains("SupportAdminAuthorizationCodes.AuthorizationUnavailable", support, StringComparison.Ordinal);
        Assert.Contains("FoundationErrorCodes.AdminAuthorizationDenied", support, StringComparison.Ordinal);

        var wallet = File.ReadAllText(Path.Combine(root, "HostWalletAdminAuthorizer.cs"));
        Assert.Contains("using Tooba.Wallet.Endpoints.Admin;", wallet, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Wallet.Application", wallet, StringComparison.Ordinal);
        Assert.Contains("WalletAdminAuthorizationCodes.AuthorizationUnavailable", wallet, StringComparison.Ordinal);
        Assert.Contains("FoundationErrorCodes.AdminAuthorizationDenied", wallet, StringComparison.Ordinal);
    }

    [Fact]
    public void Evacuated_business_endpoint_residue_is_absent()
    {
        var discovered = EnumerateAdminFiles()
            .Select(Path.GetFileName)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var name in EvacuatedBusinessEndpointResidue)
        {
            Assert.DoesNotContain(name, discovered);
        }

        var endpoints = File.ReadAllText(Path.Combine(AdminRoot(), "Panel", "AdminPanelEndpoints.cs"));
        Assert.DoesNotContain("MapGet(\"/orders\"", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("MapGet(\"/customers\"", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPost(\"/orders/query\"", endpoints, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/dashboard\"", endpoints, StringComparison.Ordinal);
        Assert.Contains("IAdminPanelAccess", endpoints, StringComparison.Ordinal);
        Assert.Contains("ApiResponseFactory", endpoints, StringComparison.Ordinal);
        Assert.Contains("Result.Success", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("AdminPanelAccess.RequireAuthorizedAsync", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("catch (PlatformHttpException", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("ToError", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("MapGet(\"/sellers\"", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPost(\"/sellers/query\"", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("AdminGridQueryEndpoint", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("MapGet(\"/dev-context\"", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("admin.dev.unavailable", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("using Tooba.Host.Admin.Development", endpoints, StringComparison.Ordinal);

        var devContext = File.ReadAllText(Path.Combine(AdminRoot(), "Development", "AdminDevContextEndpoints.cs"));
        Assert.Contains("MapGet(\"/v1/admin/dev-context\"", devContext, StringComparison.Ordinal);
        Assert.Contains("admin.dev.unavailable", devContext, StringComparison.Ordinal);
        Assert.Contains("ApiResponseFactory", devContext, StringComparison.Ordinal);
        Assert.Contains("HOST_DEVELOPMENT_PRESENTATION_CQRS_EXCEPTION", devContext, StringComparison.Ordinal);
        Assert.DoesNotContain("Not Found", devContext, StringComparison.Ordinal);
        Assert.DoesNotContain("Results.Json", devContext, StringComparison.Ordinal);

        var partySellers = File.ReadAllText(RepoFile(
            "src/backend/Modules/Party/Tooba.Party.Endpoints/Admin/Sellers/PartyAdminSellersEndpoints.cs"));
        Assert.Contains("MapGet(\"/v1/admin/sellers\"", partySellers, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/v1/admin/sellers/query\"", partySellers, StringComparison.Ordinal);
    }

    // ---------------------------------------------------------------------
    // 7. CANON-001..009 seam preservation
    // ---------------------------------------------------------------------

    [Fact]
    public void Canon001_through_009_guards_and_seams_are_preserved()
    {
        for (var wave = 1; wave <= 9; wave++)
        {
            Assert.True(File.Exists(RepoFile(
                $"src/backend/Host/Tooba.Host.Tests/Architecture/HostAdminCanon{wave:000}GuardTests.cs")),
                $"CANON-{wave:000} guard missing");
        }

        var program = File.ReadAllText(RepoFile($"{HostProject}/Program.cs"));
        Assert.Contains("Tooba.BuildingBlocks.Security.IAdminPanelAccess, Tooba.Host.Admin.Access.HostAdminPanelAccess", program, StringComparison.Ordinal);
        Assert.Contains("Tooba.Support.Endpoints.Admin.ISupportAdminAuthorizer, Tooba.Host.Admin.Access.Authorizers.HostSupportAdminAuthorizer", program, StringComparison.Ordinal);
        Assert.Contains("Tooba.Wallet.Endpoints.Admin.IWalletAdminAuthorizer, Tooba.Host.Admin.Access.Authorizers.HostWalletAdminAuthorizer", program, StringComparison.Ordinal);
        Assert.Contains("Tooba.Host.Admin.Panel.AdminPanelComposer", program, StringComparison.Ordinal);
        Assert.Contains("Tooba.Order.Contracts.Admin.Operations.IOrderAdminEffectiveAccessReader,", program, StringComparison.Ordinal);
        Assert.Contains("MapAdminPanelEndpoints()", program, StringComparison.Ordinal);
        Assert.Contains("MapAdminDevContextEndpoints()", program, StringComparison.Ordinal);
    }

    // ---------------------------------------------------------------------
    // helpers
    // ---------------------------------------------------------------------

    private static IEnumerable<string> EnumerateAdminFiles() =>
        Directory.GetFiles(AdminRoot(), "*.cs", SearchOption.AllDirectories)
            .OrderBy(x => x, StringComparer.Ordinal);

    private static string AdminRoot() => RepoFile(AdminProject);

    private static string RepoFile(string relative) =>
        Path.Combine(FindRepoRoot(), relative.Replace('/', Path.DirectorySeparatorChar));

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
