using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-SECURITY-AMC-001 — Host/Security is the intentional thin platform security boundary
/// (KEEP, not HOST_ZERO). Seller R1A + Storefront R2 lineage preserved.
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

    private static readonly Regex SellerForeignModuleLayer = new(
        @"Tooba\.(Catalog|Party|AccessControl|Identity|Order|Offer|Promotion|Returns|Settlement|Notification|Support|Story|Persistence)\.(Application|Domain|Infrastructure|Persistence)",
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
        {
            Assert.True(File.Exists(Path.Combine(security, "Seller", file)), file);
        }

        var files = Directory.EnumerateFiles(security, "*.cs", SearchOption.AllDirectories).ToArray();
        Assert.Equal(18, files.Length);
    }

    [Fact]
    public void Seller_security_boundary_has_zero_foreign_module_layers()
    {
        var seller = Path.Combine(FindRepoRoot(), "src/backend/Host/Tooba.Host/Security/Seller");
        var violations = new List<string>();
        foreach (var path in Directory.EnumerateFiles(seller, "*.cs"))
        {
            foreach (var raw in File.ReadLines(path))
            {
                var line = raw.Trim();
                if (SellerForeignModuleLayer.IsMatch(line))
                    violations.Add(Path.GetFileName(path) + ": " + line);
            }
        }

        Assert.True(violations.Count == 0, string.Join("; ", violations));
    }

    [Fact]
    public void Checkout_gate_uses_catalog_contracts_only_and_payment_authorizer_has_no_service_locator()
    {
        var root = FindRepoRoot();
        var gate = File.ReadAllText(Path.Combine(root, "src/backend/Host/Tooba.Host/Security/Checkout/CheckoutIdentityGate.cs"));
        Assert.Contains("ICatalogCheckoutIdentityPolicyLookup", gate, StringComparison.Ordinal);
        Assert.DoesNotContain("CatalogDbContext", gate, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Catalog.Application", gate, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Catalog.Infrastructure", gate, StringComparison.Ordinal);

        var payment = File.ReadAllText(Path.Combine(
            root, "src/backend/Host/Tooba.Host/Security/Payment/HostPaymentStorefrontAuthorizer.cs"));
        Assert.DoesNotContain("GetRequiredService", payment, StringComparison.Ordinal);
        Assert.Contains("CurrentAuthenticatedSession", payment, StringComparison.Ordinal);

        var reviews = File.ReadAllText(Path.Combine(root, "src/backend/Host/Tooba.Host/Reviews/ReviewEndpoints.cs"));
        Assert.Contains("ISellerPanelAccess", reviews, StringComparison.Ordinal);
        Assert.DoesNotContain("SellerPanelAccess.RequireAuthorizedAsync", reviews, StringComparison.Ordinal);
    }

    [Fact]
    public void Program_registers_security_platform_and_sot_records_keep_disposition()
    {
        var root = FindRepoRoot();
        var program = File.ReadAllText(Path.Combine(root, "src/backend/Host/Tooba.Host/Program.cs"));
        Assert.Contains("AuthSecurityHostOptions", program, StringComparison.Ordinal);
        Assert.Contains("SecurityHeadersMiddleware", program, StringComparison.Ordinal);
        Assert.Contains("CheckoutIdentityGate", program, StringComparison.Ordinal);
        Assert.Contains("HostPaymentStorefrontAuthorizer", program, StringComparison.Ordinal);
        Assert.Contains("HostSellerPanelAccess", program, StringComparison.Ordinal);
        Assert.Contains("HostStorySellerAuthorizer", program, StringComparison.Ordinal);

        var sot = File.ReadAllText(Path.Combine(root, "docs/architecture/tmar-current-state.json"));
        Assert.Contains("\"hostSecurityAmc\"", sot, StringComparison.Ordinal);
        Assert.Contains("KEEP_THIN_PLATFORM_SECURITY_BOUNDARY", sot, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-HOST-SECURITY-AMC-001", sot, StringComparison.Ordinal);
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
