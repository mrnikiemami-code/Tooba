using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-SECURITY-AMC-001-W2 — exact Host/Security structure guard (19 files).
/// Historical KEEP_THIN_PLATFORM CERT claims remain SoT history only; current certification is NOT_YET_REASSERTED.
/// </summary>
public sealed class HostSecurityAmcGuardTests
{
    private static readonly string[] ExpectedRootFiles =
    [
        "AuthSecurityHostOptions.cs",
        "SecurityHeadersMiddleware.cs",
    ];

    private static readonly string[] ExpectedCheckoutFiles =
    [
        "CheckoutIdentityGate.cs",
        "HostCheckoutActorPolicyAdapter.cs",
    ];

    private static readonly string[] ExpectedPaymentFiles =
    [
        "HostPaymentStorefrontAuthorizer.cs",
    ];

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
        "HostReviewsSellerAuthorizer.cs",
    ];

    private static readonly Regex ForeignModuleLayer = new(
        @"Tooba\.(Catalog|Party|AccessControl|Identity|Order|Offer|Promotion|Returns|Settlement|Notification|Support|Story|Payment|Persistence)\.(Application|Domain|Infrastructure|Persistence)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact]
    public void Host_security_folder_exact_tree_is_19_files_with_reviews_seller()
    {
        var root = FindRepoRoot();
        var security = Path.Combine(root, "src/backend/Host/Tooba.Host/Security");
        Assert.True(Directory.Exists(security));

        AssertExactFolderFiles(security, ExpectedRootFiles);
        AssertExactFolderFiles(Path.Combine(security, "Checkout"), ExpectedCheckoutFiles);
        AssertExactFolderFiles(Path.Combine(security, "Payment"), ExpectedPaymentFiles);
        AssertExactFolderFiles(Path.Combine(security, "Seller"), ExpectedSellerFiles);

        Assert.Equal(2, ExpectedRootFiles.Length);
        Assert.Equal(2, ExpectedCheckoutFiles.Length);
        Assert.Equal(1, ExpectedPaymentFiles.Length);
        Assert.Equal(14, ExpectedSellerFiles.Length);
        Assert.Equal(19, Directory.EnumerateFiles(security, "*.cs", SearchOption.AllDirectories).Count());

        var topDirs = Directory.GetDirectories(security)
            .Select(Path.GetFileName)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(new[] { "Checkout", "Payment", "Seller" }, topDirs);
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
        Assert.Contains("SemanticException", seller, StringComparison.Ordinal);
        Assert.DoesNotContain("PlatformHttpException", seller, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", seller, StringComparison.Ordinal);
        Assert.DoesNotContain("exception.Message", seller, StringComparison.Ordinal);

        var party = File.ReadAllText(Path.Combine(
            root, "src/backend/Host/Tooba.Host/Security/Seller/HostPartySellerAuthorizer.cs"));
        Assert.Contains("SemanticException", party, StringComparison.Ordinal);
        Assert.DoesNotContain("PlatformHttpException", party, StringComparison.Ordinal);

        var support = File.ReadAllText(Path.Combine(
            root, "src/backend/Host/Tooba.Host/Security/Seller/HostSupportSellerAuthorizer.cs"));
        Assert.Contains("SemanticException", support, StringComparison.Ordinal);
        Assert.DoesNotContain("PlatformHttpException", support, StringComparison.Ordinal);

        var order = File.ReadAllText(Path.Combine(
            root, "src/backend/Host/Tooba.Host/Security/Seller/HostOrderSellerAuthorizer.cs"));
        Assert.Contains("catch (SemanticException", order, StringComparison.Ordinal);
        Assert.DoesNotContain("catch (PlatformHttpException", order, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", order, StringComparison.Ordinal);
        Assert.DoesNotContain("exception.Message", order, StringComparison.Ordinal);

        AssertSecurityHasZeroHardCodedPlatformHttpTitles(root);

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
    public void Path_namespace_exact_and_program_sot_current_structure_authority()
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
        Assert.Contains("HostReviewsSellerAuthorizer", program, StringComparison.Ordinal);

        var sot = File.ReadAllText(Path.Combine(root, "docs/architecture/tmar-current-state.json"));
        // Historical lineage retained (not current certification authority).
        Assert.Contains("\"hostSecurityAmc\"", sot, StringComparison.Ordinal);
        Assert.Contains("KEEP_THIN_PLATFORM_SECURITY_BOUNDARY", sot, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-HOST-SECURITY-AMC-001-R1", sot, StringComparison.Ordinal);
        Assert.Contains("USER_REVIEW_HOST_SECURITY_AMC_001_R1_KEEP_THIN_PLATFORM_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("HISTORICAL_SUPERSEDED_STALE_METADATA", sot, StringComparison.Ordinal);
        Assert.Contains("HISTORICAL_SNAPSHOT_18", sot, StringComparison.Ordinal);
        // Current structure authority after W2.
        Assert.Contains("\"hostSecurityAmc001W2\"", sot, StringComparison.Ordinal);
        Assert.Contains("STRUCTURE_GUARD_SOT_RECONCILED", sot, StringComparison.Ordinal);
        Assert.Contains("NOT_YET_REASSERTED", sot, StringComparison.Ordinal);
        Assert.Contains("\"securityProductionFileCount\": 19", sot, StringComparison.Ordinal);
        Assert.Contains("USER_REVIEW_HOST_SECURITY_AMC_001_W2", sot, StringComparison.Ordinal);
    }

    private static void AssertExactFolderFiles(string folder, string[] expected)
    {
        Assert.True(Directory.Exists(folder), folder);
        var actual = Directory.EnumerateFiles(folder, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        var expectedSorted = expected.OrderBy(x => x, StringComparer.Ordinal).ToArray();
        Assert.Equal(expectedSorted, actual);
    }

    private static void AssertSecurityHasZeroHardCodedPlatformHttpTitles(string root)
    {
        var security = Path.Combine(root, "src/backend/Host/Tooba.Host/Security");
        foreach (var path in Directory.EnumerateFiles(security, "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(path);
            Assert.DoesNotContain("new PlatformHttpException(", text, StringComparison.Ordinal);
        }
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
