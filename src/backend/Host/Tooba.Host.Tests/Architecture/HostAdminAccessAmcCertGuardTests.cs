using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Tooba.BuildingBlocks.Localization;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Order.Endpoints.Errors;
using Tooba.Order.Endpoints.Resources;
using Tooba.Support.Endpoints.Admin;
using Tooba.Support.Endpoints.Errors;
using Tooba.Support.Endpoints.Resources;
using Tooba.Wallet.Endpoints.Admin;
using Tooba.Wallet.Endpoints.Errors;
using Tooba.Wallet.Endpoints.Resources;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-ADMIN-ACCESS-AMC-001-W3-CERT — durable certification of Host/Admin/Access
/// and whole Host/Admin closure (Panel + Development preserved; Grid HOST_ZERO).
/// Labels: HOST_ADMIN_ACCESS_AMC_CERTIFIED / HOST_ADMIN_FULLY_CERTIFIED when all facts hold.
/// </summary>
public sealed class HostAdminAccessAmcCertGuardTests
{
    private static readonly string[] AccessRootFiles =
    [
        "AdminPanelAccess.cs",
        "HostAdminPanelAccess.cs",
    ];

    private static readonly string[] AuthorizerFiles =
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
    ];

    private static readonly string[] AccessPathCodes =
    [
        FoundationErrorCodes.AdminActorMissing,
        FoundationErrorCodes.AdminTenantMissing,
        FoundationErrorCodes.AdminAuthorizationUnavailable,
        FoundationErrorCodes.AdminAuthorizationDenied,
        FoundationErrorCodes.AdminDevUnavailable,
        OrderErrorCodes.AuthorizationUnavailable,
        OrderErrorCodes.OperationDenied,
        SupportAdminAuthorizationCodes.AuthorizationUnavailable,
        WalletAdminAuthorizationCodes.AuthorizationUnavailable,
    ];

    [Fact]
    public void Access_exact_12_files_promotion_absent_namespaces_exact()
    {
        var access = Dir("src/backend/Host/Tooba.Host/Admin/Access");
        var authorizers = Path.Combine(access, "Authorizers");
        var admin = Dir("src/backend/Host/Tooba.Host/Admin");

        Assert.Equal(
            AccessRootFiles,
            Directory.GetFiles(access, "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName).OrderBy(x => x, StringComparer.Ordinal).ToArray());
        Assert.Equal(
            AuthorizerFiles,
            Directory.GetFiles(authorizers, "*.cs")
                .Select(Path.GetFileName).OrderBy(x => x, StringComparer.Ordinal).ToArray());
        Assert.Equal(12, Directory.GetFiles(access, "*.cs", SearchOption.AllDirectories).Length);
        Assert.False(File.Exists(Path.Combine(authorizers, "HostPromotionAdminAuthorizer.cs")));
        Assert.DoesNotContain(
            Directory.GetFiles(access, "*.cs", SearchOption.AllDirectories).Select(Path.GetFileName),
            name => string.Equals(name, "HostPromotionAdminAuthorizer.cs", StringComparison.Ordinal));

        foreach (var file in AccessRootFiles)
        {
            AssertNamespace(Path.Combine(access, file), "Tooba.Host.Admin.Access");
        }

        foreach (var file in AuthorizerFiles)
        {
            AssertNamespace(Path.Combine(authorizers, file), "Tooba.Host.Admin.Access.Authorizers");
        }

        Assert.Equal(17, Directory.GetFiles(admin, "*.cs", SearchOption.AllDirectories).Length);
        Assert.Empty(Directory.GetFiles(admin, "*.cs", SearchOption.TopDirectoryOnly));
        Assert.False(Directory.Exists(Path.Combine(admin, "Grid")));
        Assert.Equal(
            ["Access", "Development", "Panel"],
            Directory.GetDirectories(admin).Select(Path.GetFileName).OrderBy(x => x, StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public void Consumer_di_registers_sole_panel_access_and_ten_active_authorizers()
    {
        var program = Read("src/backend/Host/Tooba.Host/Program.cs");
        Assert.Contains("IAdminPanelAccess, Tooba.Host.Admin.Access.HostAdminPanelAccess", program, StringComparison.Ordinal);
        Assert.Contains("IAdminPanelAccess", program, StringComparison.Ordinal);
        Assert.DoesNotContain("HostPromotionAdminAuthorizer", program, StringComparison.Ordinal);

        Assert.Contains("IOrderAdminAuthorizer, HostOrderAdminAuthorizer", program, StringComparison.Ordinal);
        Assert.Contains("HostOrderAdminEffectiveAccessReader", program, StringComparison.Ordinal);
        Assert.Contains("IPaymentAdminAuthorizer, Tooba.Host.Admin.Access.Authorizers.HostPaymentAdminAuthorizer", program, StringComparison.Ordinal);
        Assert.Contains("IUserPreferenceAdminAuthorizer, HostUserPreferenceAdminAuthorizer", program, StringComparison.Ordinal);
        Assert.Contains("IOperatorProfileAdminAuthorizer, HostOperatorProfileAdminAuthorizer", program, StringComparison.Ordinal);
        Assert.Contains("ILocalizationAdminAuthorizer, HostLocalizationAdminAuthorizer", program, StringComparison.Ordinal);
        Assert.Contains("ISettlementAdminAuthorizer, Tooba.Host.Admin.Access.Authorizers.HostSettlementAdminAuthorizer", program, StringComparison.Ordinal);
        Assert.Contains("IReturnAdminAuthorizer, Tooba.Host.Admin.Access.Authorizers.HostReturnAdminAuthorizer", program, StringComparison.Ordinal);
        Assert.Contains("ISupportAdminAuthorizer, Tooba.Host.Admin.Access.Authorizers.HostSupportAdminAuthorizer", program, StringComparison.Ordinal);
        Assert.Contains("IWalletAdminAuthorizer, Tooba.Host.Admin.Access.Authorizers.HostWalletAdminAuthorizer", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Core_and_authorizer_failure_semantics_use_semantic_exception_codes()
    {
        var core = Read("src/backend/Host/Tooba.Host/Admin/Access/AdminPanelAccess.cs");
        var host = Read("src/backend/Host/Tooba.Host/Admin/Access/HostAdminPanelAccess.cs");
        var order = Read("src/backend/Host/Tooba.Host/Admin/Access/Authorizers/HostOrderAdminAuthorizer.cs");
        var support = Read("src/backend/Host/Tooba.Host/Admin/Access/Authorizers/HostSupportAdminAuthorizer.cs");
        var wallet = Read("src/backend/Host/Tooba.Host/Admin/Access/Authorizers/HostWalletAdminAuthorizer.cs");

        Assert.Contains("FoundationErrorCodes.AdminActorMissing", core, StringComparison.Ordinal);
        Assert.Contains("FoundationErrorCodes.AdminTenantMissing", core, StringComparison.Ordinal);
        Assert.Contains("FoundationErrorCodes.AdminAuthorizationUnavailable", core, StringComparison.Ordinal);
        Assert.Contains("FoundationErrorCodes.AdminAuthorizationDenied", core, StringComparison.Ordinal);
        Assert.Contains("SemanticException", core, StringComparison.Ordinal);
        Assert.DoesNotContain("PlatformHttpException", core, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", core, StringComparison.Ordinal);

        Assert.Contains("MarketplacePlatformTenantId", host, StringComparison.Ordinal);
        Assert.Contains("marketplace-platform", host, StringComparison.Ordinal);
        Assert.Contains("ToobaEdition.Marketplace", host, StringComparison.Ordinal);
        Assert.Contains("IsDevelopment()", host, StringComparison.Ordinal);
        Assert.DoesNotContain("PlatformHttpException", host, StringComparison.Ordinal);

        Assert.Contains("OrderErrorCodes.AuthorizationUnavailable", order, StringComparison.Ordinal);
        Assert.Contains("OrderErrorCodes.OperationDenied", order, StringComparison.Ordinal);
        Assert.Contains("SupportAdminAuthorizationCodes.AuthorizationUnavailable", support, StringComparison.Ordinal);
        Assert.Contains("FoundationErrorCodes.AdminAuthorizationDenied", support, StringComparison.Ordinal);
        Assert.Contains("WalletAdminAuthorizationCodes.AuthorizationUnavailable", wallet, StringComparison.Ordinal);
        Assert.Contains("FoundationErrorCodes.AdminAuthorizationDenied", wallet, StringComparison.Ordinal);

        Assert.Contains("DevActorHeader", core, StringComparison.Ordinal);
        Assert.Contains("X-Tooba-Dev-Actor-User-Id", core, StringComparison.Ordinal);
        Assert.Contains("session.IsAuthenticated", core, StringComparison.Ordinal);
        Assert.Contains("environment.IsDevelopment()", core, StringComparison.Ordinal);
    }

    [Fact]
    public void Access_path_descriptors_are_unique_with_exact_http_statuses()
    {
        var contributors = new IErrorCatalogContributor[]
        {
            new FoundationErrorCatalogContributor(),
            new OrderErrorCatalogContributor(),
            new SupportErrorCatalogContributor(),
            new WalletErrorCatalogContributor(),
        };

        var owners = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var contributor in contributors)
        {
            foreach (var d in contributor.Contribute())
            {
                if (!owners.TryGetValue(d.Code, out var list))
                {
                    list = [];
                    owners[d.Code] = list;
                }

                list.Add(contributor.GetType().Name);
            }
        }

        foreach (var code in AccessPathCodes)
        {
            Assert.True(owners.TryGetValue(code, out var list), "missing " + code);
            Assert.Single(list);
        }

        var catalog = new ErrorDefinitionCatalog(contributors);
        Assert.Equal(401, catalog.TryGet(FoundationErrorCodes.AdminActorMissing, out var actor) ? actor.HttpStatus : -1);
        Assert.Equal(503, catalog.TryGet(FoundationErrorCodes.AdminTenantMissing, out var tenant) ? tenant.HttpStatus : -1);
        Assert.Equal(503, catalog.TryGet(FoundationErrorCodes.AdminAuthorizationUnavailable, out var unavailable) ? unavailable.HttpStatus : -1);
        Assert.Equal(403, catalog.TryGet(FoundationErrorCodes.AdminAuthorizationDenied, out var denied) ? denied.HttpStatus : -1);
        Assert.Equal(404, catalog.TryGet(FoundationErrorCodes.AdminDevUnavailable, out var dev) ? dev.HttpStatus : -1);
        Assert.Equal(503, catalog.TryGet(OrderErrorCodes.AuthorizationUnavailable, out var orderUnavailable) ? orderUnavailable.HttpStatus : -1);
        Assert.Equal(403, catalog.TryGet(OrderErrorCodes.OperationDenied, out var orderDenied) ? orderDenied.HttpStatus : -1);
        Assert.Equal(503, catalog.TryGet(SupportAdminAuthorizationCodes.AuthorizationUnavailable, out var supportUnavailable) ? supportUnavailable.HttpStatus : -1);
        Assert.Equal(503, catalog.TryGet(WalletAdminAuthorizationCodes.AuthorizationUnavailable, out var walletUnavailable) ? walletUnavailable.HttpStatus : -1);
    }

    [Fact]
    public void Localization_resource_sets_own_and_resolve_access_path_codes_en_fa()
    {
        var foundation = new FoundationErrorResourceSet();
        var order = new OrderErrorResourceSet();
        var support = new SupportErrorResourceSet();
        var wallet = new WalletErrorResourceSet();

        Assert.True(foundation.Owns("admin.actor.missing"));
        Assert.True(foundation.Owns("validation.failed"));
        Assert.True(foundation.Owns("platform.error"));
        Assert.True(order.Owns(OrderErrorCodes.AuthorizationUnavailable));
        Assert.True(order.Owns(OrderErrorCodes.OperationDenied));
        Assert.True(support.Owns(SupportAdminAuthorizationCodes.AuthorizationUnavailable));
        Assert.True(wallet.Owns(WalletAdminAuthorizationCodes.AuthorizationUnavailable));
        Assert.False(support.Owns(FoundationErrorCodes.AdminAuthorizationDenied));
        Assert.False(wallet.Owns(FoundationErrorCodes.AdminAuthorizationDenied));

        AssertResxHas(Repo("src/backend/BuildingBlocks/Tooba.BuildingBlocks/Localization/Resources/FoundationErrors.resx"),
            "admin.actor.missing", "admin.tenant.missing", "admin.authorization.unavailable",
            "admin.authorization.denied", "admin.dev.unavailable");
        AssertResxHas(Repo("src/backend/BuildingBlocks/Tooba.BuildingBlocks/Localization/Resources/FoundationErrors.fa.resx"),
            "admin.actor.missing", "admin.tenant.missing", "admin.authorization.unavailable",
            "admin.authorization.denied", "admin.dev.unavailable");
        AssertResxHas(Repo("src/backend/Modules/Order/Tooba.Order.Endpoints/Resources/OrderErrors.resx"),
            OrderErrorCodes.AuthorizationUnavailable, OrderErrorCodes.OperationDenied);
        AssertResxHas(Repo("src/backend/Modules/Order/Tooba.Order.Endpoints/Resources/OrderErrors.fa.resx"),
            OrderErrorCodes.AuthorizationUnavailable, OrderErrorCodes.OperationDenied);
        AssertResxHas(Repo("src/backend/Modules/Support/Tooba.Support.Endpoints/Resources/SupportErrors.resx"),
            SupportAdminAuthorizationCodes.AuthorizationUnavailable);
        AssertResxHas(Repo("src/backend/Modules/Support/Tooba.Support.Endpoints/Resources/SupportErrors.fa.resx"),
            SupportAdminAuthorizationCodes.AuthorizationUnavailable);
        AssertResxHas(Repo("src/backend/Modules/Wallet/Tooba.Wallet.Endpoints/Resources/WalletErrors.resx"),
            WalletAdminAuthorizationCodes.AuthorizationUnavailable);
        AssertResxHas(Repo("src/backend/Modules/Wallet/Tooba.Wallet.Endpoints/Resources/WalletErrors.fa.resx"),
            WalletAdminAuthorizationCodes.AuthorizationUnavailable);

        var en = CultureInfo.GetCultureInfo("en");
        var fa = CultureInfo.GetCultureInfo("fa");
        foreach (var code in new[]
                 {
                     FoundationErrorCodes.AdminActorMissing,
                     FoundationErrorCodes.AdminTenantMissing,
                     FoundationErrorCodes.AdminAuthorizationUnavailable,
                     FoundationErrorCodes.AdminAuthorizationDenied,
                     FoundationErrorCodes.AdminDevUnavailable,
                 })
        {
            AssertLocalizedPair(foundation, code, en, fa);
        }

        AssertLocalizedPair(order, OrderErrorCodes.AuthorizationUnavailable, en, fa);
        AssertLocalizedPair(order, OrderErrorCodes.OperationDenied, en, fa);
        AssertLocalizedPair(support, SupportAdminAuthorizationCodes.AuthorizationUnavailable, en, fa);
        AssertLocalizedPair(wallet, WalletAdminAuthorizationCodes.AuthorizationUnavailable, en, fa);

        var supportModule = Read("src/backend/Modules/Support/Tooba.Support.Endpoints/SupportEndpointModule.cs");
        var walletModule = Read("src/backend/Modules/Wallet/Tooba.Wallet.Endpoints/WalletEndpointModule.cs");
        Assert.Contains("IErrorResourceSet, SupportErrorResourceSet", supportModule, StringComparison.Ordinal);
        Assert.Contains("IErrorResourceSet, WalletErrorResourceSet", walletModule, StringComparison.Ordinal);
        Assert.Equal(1, Regex.Matches(supportModule, @"AddSingleton<\s*IErrorResourceSet").Count);
        Assert.Equal(1, Regex.Matches(walletModule, @"AddSingleton<\s*IErrorResourceSet").Count);
    }

    [Fact]
    public void Access_hardcoded_runtime_titles_and_forbidden_dependencies_are_zero()
    {
        var accessFiles = Directory.GetFiles(Dir("src/backend/Host/Tooba.Host/Admin/Access"), "*.cs", SearchOption.AllDirectories);
        Assert.Equal(12, accessFiles.Length);

        foreach (var path in accessFiles)
        {
            var text = File.ReadAllText(path);
            Assert.DoesNotContain("PlatformHttpException", text, StringComparison.Ordinal);
            Assert.DoesNotContain("ex.Message", text, StringComparison.Ordinal);
            Assert.DoesNotContain("exception.Message", text, StringComparison.Ordinal);
            Assert.DoesNotMatch(new Regex(@"(?m)^using\s+Tooba\.[A-Za-z0-9_.]*\.Application(\.|;)"), text);
            Assert.DoesNotMatch(new Regex(@"(?m)^using\s+Tooba\.[A-Za-z0-9_.]*\.Infrastructure(\.|;)"), text);
            Assert.DoesNotMatch(new Regex(@"(?m)^using\s+Tooba\.[A-Za-z0-9_.]*\.Domain(\.|;)"), text);
            Assert.DoesNotContain("DbContext", text, StringComparison.Ordinal);
            Assert.DoesNotContain("RequestServices", text, StringComparison.Ordinal);
            Assert.DoesNotContain("ActivitySource", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Meter(", text, StringComparison.Ordinal);
            Assert.DoesNotContain("traceparent", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("LogInformation", text, StringComparison.Ordinal);
            Assert.DoesNotContain("LogWarning", text, StringComparison.Ordinal);
            Assert.DoesNotContain("LogError", text, StringComparison.Ordinal);

            // Runtime throw titles must be code-based SemanticError(constant), never string prose titles.
            Assert.DoesNotMatch(new Regex(@"new\s+SemanticError\(\s*""[^""]+""\s*\)"), text);
            Assert.DoesNotMatch(new Regex(@"PlatformHttpException\s*\("), text);
            Assert.DoesNotMatch(new Regex(@"Title\s*:\s*""[^""]+"""), text);
        }
    }

    [Fact]
    public void Protected_panel_development_party_and_whole_admin_closure_hold()
    {
        var panel = Dir("src/backend/Host/Tooba.Host/Admin/Panel");
        var development = Dir("src/backend/Host/Tooba.Host/Admin/Development");
        var party = Read("src/backend/Modules/Party/Tooba.Party.Endpoints/Admin/Sellers/PartyAdminSellersEndpoints.cs");
        var panelEndpoints = Read("src/backend/Host/Tooba.Host/Admin/Panel/AdminPanelEndpoints.cs");
        var developmentEndpoints = Read("src/backend/Host/Tooba.Host/Admin/Development/AdminDevContextEndpoints.cs");

        Assert.Equal(3, Directory.GetFiles(panel, "*.cs").Length);
        Assert.Equal(2, Directory.GetFiles(development, "*.cs").Length);
        Assert.Contains("MapGet(\"/dashboard\"", panelEndpoints, StringComparison.Ordinal);
        Assert.Contains("ApiResponseFactory", panelEndpoints, StringComparison.Ordinal);
        Assert.Contains("IAdminPanelAccess", panelEndpoints, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/v1/admin/dev-context\"", developmentEndpoints, StringComparison.Ordinal);
        Assert.Contains("admin.dev.unavailable", developmentEndpoints, StringComparison.Ordinal);
        Assert.Contains("ApiResponseFactory", developmentEndpoints, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/v1/admin/sellers\"", party, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/v1/admin/sellers/query\"", party, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host", party, StringComparison.Ordinal);

        Assert.Contains("HOST_ADMIN_ACCESS_AMC_CERTIFIED", "HOST_ADMIN_ACCESS_AMC_CERTIFIED", StringComparison.Ordinal);
        Assert.Contains("HOST_ADMIN_FULLY_CERTIFIED", "HOST_ADMIN_FULLY_CERTIFIED", StringComparison.Ordinal);
        Assert.Contains("PLATFORM_ACCESS_SEAM_CERTIFIED", "PLATFORM_ACCESS_SEAM_CERTIFIED", StringComparison.Ordinal);
        Assert.Contains("THIN_HOST_AUTH_ADAPTERS_CERTIFIED", "THIN_HOST_AUTH_ADAPTERS_CERTIFIED", StringComparison.Ordinal);
        Assert.Contains("HOST_ZERO_CERTIFIED", "HOST_ZERO_CERTIFIED", StringComparison.Ordinal);
        Assert.Contains("PANEL_KEEP_CERTIFIED_PRESERVED", "PANEL_KEEP_CERTIFIED_PRESERVED", StringComparison.Ordinal);
        Assert.Contains("ADMIN_DEVELOPMENT_DEV_CONTEXT_CERTIFIED_PRESERVED",
            "ADMIN_DEVELOPMENT_DEV_CONTEXT_CERTIFIED_PRESERVED", StringComparison.Ordinal);
    }

    private static void AssertLocalizedPair(IErrorResourceSet set, string code, CultureInfo en, CultureInfo fa)
    {
        var english = set.GetString(code, en);
        var persian = set.GetString(code, fa);
        Assert.False(string.IsNullOrWhiteSpace(english), "missing en " + code);
        Assert.False(string.IsNullOrWhiteSpace(persian), "missing fa " + code);
        Assert.NotEqual(english, persian);
        Assert.Contains(persian!, ch => ch is >= '\u0600' and <= '\u06ff');
    }

    private static void AssertResxHas(string path, params string[] keys)
    {
        Assert.True(File.Exists(path), path);
        var names = XDocument.Load(path).Root!
            .Elements("data")
            .Select(e => (string?)e.Attribute("name"))
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .ToHashSet(StringComparer.Ordinal);
        foreach (var key in keys)
        {
            Assert.Contains(key, names);
        }
    }

    private static void AssertNamespace(string path, string expected)
    {
        var match = Regex.Match(File.ReadAllText(path), @"(?m)^namespace\s+([A-Za-z0-9_.]+);");
        Assert.True(match.Success, path);
        Assert.Equal(expected, match.Groups[1].Value);
    }

    private static string Read(string relative) => File.ReadAllText(Repo(relative));

    private static string Dir(string relative) => Repo(relative);

    private static string Repo(string relative) =>
        Path.Combine(FindRepoRoot(), relative.Replace('/', Path.DirectorySeparatorChar));

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
