using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-SECURITY-AMC-001 / R1 — KEEP_THIN_PLATFORM_SECURITY_BOUNDARY certification.
/// </summary>
public sealed class HostSecurityAmcGuardTests
{
    private static readonly string[] ExpectedSellerFiles =
    [
        "SellerPanelAccess.cs",
        "SellerSecurityErrorCodes.cs",
        "HostSellerPanelAccess.cs",
        "HostSupportSellerAuthorizer.cs",
        "HostOfferSellerAuthorizer.cs",
        "HostOrderSellerAuthorizer.cs",
        "HostReturnSellerAuthorizer.cs",
        "HostSettlementSellerAuthorizer.cs",
        "HostNotificationSellerAuthorizer.cs",
        "HostPromotionSellerAuthorizer.cs",
        "HostPartySellerAuthorizer.cs",
        "HostCatalogSellerAuthorizer.cs",
        "HostStorySellerAuthorizer.cs",
    ];

    private static readonly Regex ForeignModuleLayer = new(
        @"Tooba\.(Catalog|Party|AccessControl|Identity|Order|Offer|Promotion|Returns|Settlement|Notification|Support|Story|Payment|Persistence)\.(Application|Domain|Infrastructure|Persistence)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact]
    public void Host_security_folder_is_present_as_thin_platform_boundary_not_host_zero()
    {
        var root = FindRepoRoot();
        var security = Path.Combine(root, "src/backend/Host/Tooba.Host/Security");
        Assert.True(Directory.Exists(security));

        Assert.True(File.Exists(Path.Combine(security, "AuthSecurityHostOptions.cs")));
        Assert.True(File.Exists(Path.Combine(security, "SecurityHeadersMiddleware.cs")));
        Assert.True(File.Exists(Path.Combine(security, "Checkout", "CheckoutIdentityGate.cs")));
        Assert.True(File.Exists(Path.Combine(security, "Checkout", "HostCheckoutActorPolicyAdapter.cs")));
        Assert.True(File.Exists(Path.Combine(security, "Payment", "HostPaymentStorefrontAuthorizer.cs")));

        foreach (var file in ExpectedSellerFiles)
            Assert.True(File.Exists(Path.Combine(security, "Seller", file)), file);

        Assert.Equal(18, Directory.EnumerateFiles(security, "*.cs", SearchOption.AllDirectories).Count());
    }

    [Fact]
    public void Security_seller_and_checkout_have_zero_foreign_module_layers()
    {
        var root = FindRepoRoot();
        var violations = new List<string>();
        foreach (var sub in new[] { "Seller", "Checkout", "Payment" })
        {
            var dir = Path.Combine(root, "src/backend/Host/Tooba.Host/Security", sub);
            foreach (var path in Directory.EnumerateFiles(dir, "*.cs"))
            {
                foreach (var raw in File.ReadLines(path))
                {
                    var line = raw.Trim();
                    if (ForeignModuleLayer.IsMatch(line))
                        violations.Add(Path.GetFileName(path) + ": " + line);
                }
            }
        }

        Assert.True(violations.Count == 0, string.Join("; ", violations));
    }

    [Fact]
    public void Checkout_payment_and_seller_hygiene_hold()
    {
        var root = FindRepoRoot();
        var gate = File.ReadAllText(Path.Combine(root, "src/backend/Host/Tooba.Host/Security/Checkout/CheckoutIdentityGate.cs"));
        Assert.Contains("ICatalogCheckoutIdentityPolicyLookup", gate, StringComparison.Ordinal);
        Assert.DoesNotContain("CatalogDbContext", gate, StringComparison.Ordinal);
        Assert.DoesNotContain("InvalidOperationException", gate, StringComparison.Ordinal);
        Assert.Contains("SemanticException", gate, StringComparison.Ordinal);
        Assert.Contains("FoundationErrorCodes.CheckoutAuthenticationRequired", gate, StringComparison.Ordinal);
        Assert.DoesNotContain("message.Contains", gate, StringComparison.Ordinal);

        var adapter = File.ReadAllText(Path.Combine(
            root, "src/backend/Host/Tooba.Host/Security/Checkout/HostCheckoutActorPolicyAdapter.cs"));
        Assert.Contains("Tooba.Payment.Contracts.Ports", adapter, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Payment.Application", adapter, StringComparison.Ordinal);

        var payment = File.ReadAllText(Path.Combine(
            root, "src/backend/Host/Tooba.Host/Security/Payment/HostPaymentStorefrontAuthorizer.cs"));
        Assert.DoesNotContain("GetRequiredService", payment, StringComparison.Ordinal);

        var seller = File.ReadAllText(Path.Combine(
            root, "src/backend/Host/Tooba.Host/Security/Seller/SellerPanelAccess.cs"));
        Assert.DoesNotContain("ToobaEdition.SingleStore", seller, StringComparison.Ordinal);
        Assert.Contains("ICurrentEdition", seller, StringComparison.Ordinal);

        var reviews = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Reviews/Tooba.Reviews.Endpoints/Seller/ReviewsSellerEndpoints.cs"));
        Assert.Contains("IReviewsSellerAuthorizer", reviews, StringComparison.Ordinal);
        Assert.DoesNotContain("SellerPanelAccess.RequireAuthorizedAsync", reviews, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host.", reviews, StringComparison.Ordinal);

        var hostAdapter = File.ReadAllText(Path.Combine(
            root, "src/backend/Host/Tooba.Host/Security/Seller/HostReviewsSellerAuthorizer.cs"));
        Assert.Contains("ISellerPanelAccess", hostAdapter, StringComparison.Ordinal);
    }

    [Fact]
    public void Path_namespace_exact_and_program_sot_keep_disposition()
    {
        var root = FindRepoRoot();
        var securityRoot = Path.Combine(root, "src/backend/Host/Tooba.Host/Security");
        foreach (var path in Directory.EnumerateFiles(securityRoot, "*.cs", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(securityRoot, path).Replace('\\', '/');
            var text = File.ReadAllText(path);
            var nsMatch = Regex.Match(text, @"^namespace\s+([^\s;{]+)", RegexOptions.Multiline);
            Assert.True(nsMatch.Success, path);
            var ns = nsMatch.Groups[1].Value;
            if (rel is "AuthSecurityHostOptions.cs" or "SecurityHeadersMiddleware.cs")
                Assert.Equal("Tooba.Host.Security", ns);
            else if (rel.StartsWith("Checkout/", StringComparison.Ordinal))
                Assert.Equal("Tooba.Host.Security.Checkout", ns);
            else if (rel.StartsWith("Payment/", StringComparison.Ordinal))
                Assert.Equal("Tooba.Host.Security.Payment", ns);
            else if (rel.StartsWith("Seller/", StringComparison.Ordinal))
                Assert.Equal("Tooba.Host.Security.Seller", ns);
        }

        var program = File.ReadAllText(Path.Combine(root, "src/backend/Host/Tooba.Host/Program.cs"));
        Assert.Contains("AuthSecurityHostOptions", program, StringComparison.Ordinal);
        Assert.Contains("SecurityHeadersMiddleware", program, StringComparison.Ordinal);
        Assert.Contains("Tooba.Payment.Contracts.Ports.ICheckoutActorPolicyPort", program, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Payment.Application.Ports.ICheckoutActorPolicyPort", program, StringComparison.Ordinal);

        var sot = File.ReadAllText(Path.Combine(root, "docs/architecture/tmar-current-state.json"));
        Assert.Contains("\"hostSecurityAmc\"", sot, StringComparison.Ordinal);
        Assert.Contains("KEEP_THIN_PLATFORM_SECURITY_BOUNDARY", sot, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-HOST-SECURITY-AMC-001-R1", sot, StringComparison.Ordinal);
        Assert.Contains("USER_REVIEW_HOST_SECURITY_AMC_001_R1_KEEP_THIN_PLATFORM_CERTIFIED", sot, StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
                return directory.FullName;
            directory = directory.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
