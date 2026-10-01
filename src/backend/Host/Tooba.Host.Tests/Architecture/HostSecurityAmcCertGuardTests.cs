using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Tooba.BuildingBlocks.Localization;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Order.Endpoints.Errors;
using Tooba.Order.Endpoints.Resources;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-SECURITY-AMC-001-W3-CERT — durable certification of Host/Security
/// as KEEP_THIN_PLATFORM_SECURITY_BOUNDARY_CERTIFIED_CURRENT_19 (not HOST_ZERO).
/// </summary>
public sealed class HostSecurityAmcCertGuardTests
{
    private static readonly string[] RootFiles =
    [
        "AuthSecurityHostOptions.cs",
        "SecurityHeadersMiddleware.cs",
    ];

    private static readonly string[] CheckoutFiles =
    [
        "CheckoutIdentityGate.cs",
        "HostCheckoutActorPolicyAdapter.cs",
    ];

    private static readonly string[] PaymentFiles =
    [
        "HostPaymentStorefrontAuthorizer.cs",
    ];

    private static readonly string[] SellerFiles =
    [
        "HostCatalogSellerAuthorizer.cs",
        "HostNotificationSellerAuthorizer.cs",
        "HostOfferSellerAuthorizer.cs",
        "HostOrderSellerAuthorizer.cs",
        "HostPartySellerAuthorizer.cs",
        "HostPromotionSellerAuthorizer.cs",
        "HostReturnSellerAuthorizer.cs",
        "HostReviewsSellerAuthorizer.cs",
        "HostSellerPanelAccess.cs",
        "HostSettlementSellerAuthorizer.cs",
        "HostStorySellerAuthorizer.cs",
        "HostSupportSellerAuthorizer.cs",
        "SellerPanelAccess.cs",
        "SellerSecurityErrorCodes.cs",
    ];

    private static readonly string[] SellerPathCodes =
    [
        "seller.actor.missing",
        "seller.identity.missing",
        "seller.authorization.unavailable",
        "seller.authorization.denied",
    ];

    private static readonly Regex ForeignModuleLayer = new(
        @"Tooba\.(Catalog|Party|AccessControl|Identity|Order|Offer|Promotion|Returns|Settlement|Notification|Support|Story|Payment|Persistence)\.(Application|Domain|Infrastructure|Persistence)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact]
    public void Security_exact_19_tree_namespaces_and_reviews_present()
    {
        var security = Dir("src/backend/Host/Tooba.Host/Security");
        AssertExact(security, RootFiles);
        AssertExact(Path.Combine(security, "Checkout"), CheckoutFiles);
        AssertExact(Path.Combine(security, "Payment"), PaymentFiles);
        AssertExact(Path.Combine(security, "Seller"), SellerFiles);
        Assert.Equal(19, Directory.GetFiles(security, "*.cs", SearchOption.AllDirectories).Length);
        Assert.Equal(
            ["Checkout", "Payment", "Seller"],
            Directory.GetDirectories(security).Select(Path.GetFileName).OrderBy(x => x, StringComparer.Ordinal).ToArray());

        foreach (var f in RootFiles)
            AssertNamespace(Path.Combine(security, f), "Tooba.Host.Security");
        foreach (var f in CheckoutFiles)
            AssertNamespace(Path.Combine(security, "Checkout", f), "Tooba.Host.Security.Checkout");
        foreach (var f in PaymentFiles)
            AssertNamespace(Path.Combine(security, "Payment", f), "Tooba.Host.Security.Payment");
        foreach (var f in SellerFiles)
            AssertNamespace(Path.Combine(security, "Seller", f), "Tooba.Host.Security.Seller");
    }

    [Fact]
    public void Di_registers_panel_and_all_seller_payment_checkout_adapters()
    {
        var program = Read("src/backend/Host/Tooba.Host/Program.cs");
        Assert.Contains("ISellerPanelAccess, Tooba.Host.Security.Seller.HostSellerPanelAccess", program, StringComparison.Ordinal);
        Assert.Contains("IReviewsSellerAuthorizer, Tooba.Host.Security.Seller.HostReviewsSellerAuthorizer", program, StringComparison.Ordinal);
        Assert.Contains("ICatalogSellerAuthorizer, Tooba.Host.Security.Seller.HostCatalogSellerAuthorizer", program, StringComparison.Ordinal);
        Assert.Contains("ICheckoutActorPolicyPort, Tooba.Host.Security.Checkout.HostCheckoutActorPolicyAdapter", program, StringComparison.Ordinal);
        Assert.Contains("IPaymentStorefrontAuthorizer, Tooba.Host.Security.Payment.HostPaymentStorefrontAuthorizer", program, StringComparison.Ordinal);
        Assert.Contains("SecurityHeadersMiddleware", program, StringComparison.Ordinal);
        Assert.Contains("AuthSecurityHostOptions", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Seller_w1_hygiene_and_boundaries_hold()
    {
        var sellerDir = Dir("src/backend/Host/Tooba.Host/Security/Seller");
        var panel = Read(Path.Combine(sellerDir, "SellerPanelAccess.cs"));
        Assert.Contains("SemanticException", panel, StringComparison.Ordinal);
        Assert.DoesNotContain("PlatformHttpException", panel, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", panel, StringComparison.Ordinal);
        Assert.Contains("SellerSecurityErrorCodes.ActorMissing", panel, StringComparison.Ordinal);
        Assert.Contains("SellerSecurityErrorCodes.IdentityMissing", panel, StringComparison.Ordinal);
        Assert.Contains("SellerSecurityErrorCodes.AuthorizationUnavailable", panel, StringComparison.Ordinal);
        Assert.Contains("SellerSecurityErrorCodes.AuthorizationDenied", panel, StringComparison.Ordinal);

        var order = Read(Path.Combine(sellerDir, "HostOrderSellerAuthorizer.cs"));
        Assert.Contains("catch (SemanticException", order, StringComparison.Ordinal);
        Assert.DoesNotContain("catch (PlatformHttpException", order, StringComparison.Ordinal);
        Assert.DoesNotContain("catch (Exception", order, StringComparison.Ordinal);
        Assert.Contains("ex.Error", order, StringComparison.Ordinal);

        foreach (var name in new[] { "HostPartySellerAuthorizer.cs", "HostSupportSellerAuthorizer.cs" })
        {
            var text = Read(Path.Combine(sellerDir, name));
            Assert.Contains("SemanticException", text, StringComparison.Ordinal);
            Assert.DoesNotContain("PlatformHttpException", text, StringComparison.Ordinal);
            Assert.Contains("IPlatformEffectiveAccessReader", text, StringComparison.Ordinal);
        }

        var codes = Read(Path.Combine(sellerDir, "SellerSecurityErrorCodes.cs"));
        foreach (var code in SellerPathCodes)
            Assert.Contains($"\"{code}\"", codes, StringComparison.Ordinal);

        var security = Dir("src/backend/Host/Tooba.Host/Security");
        foreach (var path in Directory.EnumerateFiles(security, "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(path);
            Assert.DoesNotContain("new PlatformHttpException(", text, StringComparison.Ordinal);
            Assert.DoesNotContain("GetRequiredService", text, StringComparison.Ordinal);
            Assert.DoesNotContain("DbContext", text, StringComparison.Ordinal);
            Assert.DoesNotContain("TypeForwardedTo", text, StringComparison.Ordinal);
            Assert.DoesNotContain("ILogger", text, StringComparison.Ordinal);
            Assert.DoesNotContain("ActivitySource", text, StringComparison.Ordinal);
            Assert.DoesNotContain("ex.Message", text, StringComparison.Ordinal);
            Assert.DoesNotContain("exception.Message", text, StringComparison.Ordinal);
            foreach (var line in File.ReadLines(path))
            {
                if (ForeignModuleLayer.IsMatch(line.Trim()))
                    Assert.Fail("foreign module layer: " + path + ": " + line.Trim());
            }
        }

        var checkout = Read("src/backend/Host/Tooba.Host/Security/Checkout/CheckoutIdentityGate.cs");
        Assert.Contains("SemanticException", checkout, StringComparison.Ordinal);
        Assert.Contains("FoundationErrorCodes.CheckoutAuthenticationRequired", checkout, StringComparison.Ordinal);
        Assert.Contains("ICatalogCheckoutIdentityPolicyLookup", checkout, StringComparison.Ordinal);

        var checkoutAdapter = Read("src/backend/Host/Tooba.Host/Security/Checkout/HostCheckoutActorPolicyAdapter.cs");
        Assert.Contains("Tooba.Payment.Contracts.Ports", checkoutAdapter, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Payment.Application", checkoutAdapter, StringComparison.Ordinal);

        var payment = Read("src/backend/Host/Tooba.Host/Security/Payment/HostPaymentStorefrontAuthorizer.cs");
        Assert.DoesNotContain("GetRequiredService", payment, StringComparison.Ordinal);
    }

    [Fact]
    public void Seller_and_checkout_codes_resolve_unique_statuses_en_fa()
    {
        var contributors = new IErrorCatalogContributor[]
        {
            new FoundationErrorCatalogContributor(),
            new OrderErrorCatalogContributor(),
        };
        var owners = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var c in contributors)
        {
            foreach (var d in c.Contribute())
            {
                if (!owners.TryGetValue(d.Code, out var list))
                {
                    list = [];
                    owners[d.Code] = list;
                }

                list.Add(c.GetType().Name);
            }
        }

        foreach (var code in SellerPathCodes.Append(FoundationErrorCodes.CheckoutAuthenticationRequired))
        {
            Assert.True(owners.TryGetValue(code, out var list), "missing " + code);
            Assert.Single(list);
        }

        var catalog = new ErrorDefinitionCatalog(contributors);
        Assert.Equal(401, catalog.TryGet("seller.actor.missing", out var actor) ? actor!.HttpStatus : -1);
        Assert.Equal(400, catalog.TryGet("seller.identity.missing", out var identity) ? identity!.HttpStatus : -1);
        Assert.Equal(503, catalog.TryGet("seller.authorization.unavailable", out var unavailable) ? unavailable!.HttpStatus : -1);
        Assert.Equal(403, catalog.TryGet("seller.authorization.denied", out var denied) ? denied!.HttpStatus : -1);
        Assert.Equal(401, catalog.TryGet(FoundationErrorCodes.CheckoutAuthenticationRequired, out var checkout) ? checkout!.HttpStatus : -1);

        Assert.Equal("FoundationErrorCatalogContributor", owners["seller.authorization.denied"].Single());
        Assert.Equal("OrderErrorCatalogContributor", owners["seller.actor.missing"].Single());

        var order = new OrderErrorResourceSet();
        var en = CultureInfo.GetCultureInfo("en");
        var fa = CultureInfo.GetCultureInfo("fa");
        foreach (var code in new[]
                 {
                     "seller.actor.missing",
                     "seller.identity.missing",
                     "seller.authorization.unavailable",
                     "seller.authorization.denied",
                 })
        {
            Assert.True(order.Owns(code));
            var enMsg = order.GetString(code, en);
            var faMsg = order.GetString(code, fa);
            Assert.False(string.IsNullOrWhiteSpace(enMsg));
            Assert.False(string.IsNullOrWhiteSpace(faMsg));
            Assert.NotEqual(enMsg, faMsg);
        }

        AssertResxHas(
            Repo("src/backend/Modules/Order/Tooba.Order.Endpoints/Resources/OrderErrors.resx"),
            SellerPathCodes);
        AssertResxHas(
            Repo("src/backend/Modules/Order/Tooba.Order.Endpoints/Resources/OrderErrors.fa.resx"),
            SellerPathCodes);
    }

    [Fact]
    public void Sot_current_cert_labels_and_admin_preserved_historical_18_not_authority()
    {
        var sot = Read("docs/architecture/tmar-current-state.json");
        Assert.Contains("HOST_SECURITY_AMC_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("KEEP_THIN_PLATFORM_SECURITY_BOUNDARY_CERTIFIED_CURRENT_19", sot, StringComparison.Ordinal);
        Assert.Contains("HOST_SECURITY_GLOBAL_PLATFORM_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("HOST_SECURITY_THIN_ADAPTERS_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("\"hostSecurityAmc001W3Cert\"", sot, StringComparison.Ordinal);
        Assert.Contains("\"securityProductionFileCount\": 19", sot, StringComparison.Ordinal);
        Assert.Contains("HOST_ADMIN_FULLY_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("HISTORICAL_SUPERSEDED_STALE_METADATA", sot, StringComparison.Ordinal);
        Assert.Contains("HISTORICAL_SNAPSHOT_18", sot, StringComparison.Ordinal);
        Assert.Contains("USER_REVIEW_HOST_SECURITY_AMC_001_W3_CERT", sot, StringComparison.Ordinal);
        Assert.DoesNotContain("\"certificationState\": \"NOT_YET_REASSERTED\"", sot, StringComparison.Ordinal);
    }

    private static void AssertExact(string folder, string[] expected)
    {
        Assert.True(Directory.Exists(folder), folder);
        var actual = Directory.GetFiles(folder, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName!)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(expected.OrderBy(x => x, StringComparer.Ordinal).ToArray(), actual);
    }

    private static void AssertNamespace(string path, string expected)
    {
        var text = File.ReadAllText(path);
        var m = Regex.Match(text, @"^namespace\s+([^\s;{]+)", RegexOptions.Multiline);
        Assert.True(m.Success, path);
        Assert.Equal(expected, m.Groups[1].Value);
    }

    private static void AssertResxHas(string path, IEnumerable<string> codes)
    {
        var doc = XDocument.Load(path);
        var names = doc.Root!
            .Elements("data")
            .Select(x => (string?)x.Attribute("name"))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.Ordinal);
        foreach (var code in codes)
            Assert.Contains(code, names);
    }

    private static string Dir(string relative) => Path.Combine(Repo(), relative.Replace('/', Path.DirectorySeparatorChar));

    private static string Read(string relative) => File.ReadAllText(Repo(relative));

    private static string Repo(string? relative = null)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
            {
                return relative is null
                    ? directory.FullName
                    : Path.Combine(directory.FullName, relative.Replace('/', Path.DirectorySeparatorChar));
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
