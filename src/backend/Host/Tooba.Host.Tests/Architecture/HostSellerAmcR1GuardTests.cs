using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-SELLER-AMC-001-R1 — durable guard for the Host seller platform security boundary.
///
/// The Host seller panel gate and its nine thin module-endpoint auth adapters live under
/// <c>Host/Security/Seller</c> (Host security platform boundary, not a business owner), the panel
/// gate resolves its runtime dependencies from DI instead of the service locator, the effective
/// permission projection consumes only the neutral platform seam, and no foreign
/// Application/Infrastructure/Domain module type is referenced from the boundary.
/// </summary>
public sealed class HostSellerAmcR1GuardTests
{
    private static readonly string[] BoundaryFiles =
    [
        "SellerPanelAccess.cs",
        "HostSellerPanelAccess.cs",
        "HostSellerOrderViewAccessReader.cs",
        "HostSupportSellerAuthorizer.cs",
        "HostOfferSellerAuthorizer.cs",
        "HostOrderSellerAuthorizer.cs",
        "HostReturnSellerAuthorizer.cs",
        "HostSettlementSellerAuthorizer.cs",
        "HostNotificationSellerAuthorizer.cs",
        "HostPromotionSellerAuthorizer.cs",
    ];

    private static readonly string[] ThinAdapterFiles =
    [
        "HostSupportSellerAuthorizer.cs",
        "HostOfferSellerAuthorizer.cs",
        "HostOrderSellerAuthorizer.cs",
        "HostReturnSellerAuthorizer.cs",
        "HostSettlementSellerAuthorizer.cs",
        "HostNotificationSellerAuthorizer.cs",
        "HostPromotionSellerAuthorizer.cs",
        "HostSellerOrderViewAccessReader.cs",
        "HostSellerPanelAccess.cs",
    ];

    // Accepted pre-R1 Host→module security-port references only: a module-owned stable error-code
    // set and one module-owned seller access port. Anything else is forbidden coupling.
    private static readonly Regex ForeignModuleApplicationOrDomain = new(
        @"Tooba\.(Catalog|Party|AccessControl|Identity|Order|Offer|Promotion|Returns|Settlement|Notification|Support)\.(Application|Domain)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly string[] AcceptedForeignNamespaceLines =
    [
        "using Tooba.Order.Application.Seller;",
        "using Tooba.Order.Application.Seller.Ports;",
        "using Tooba.Support.Application.Errors;",
    ];

    [Fact]
    public void Seller_platform_security_boundary_files_exist_under_host_security_and_not_under_host_seller()
    {
        var hostRoot = HostRoot();
        var boundaryRoot = Path.Combine(hostRoot, "Security", "Seller");

        foreach (var file in BoundaryFiles)
        {
            Assert.True(
                File.Exists(Path.Combine(boundaryRoot, file)),
                "missing Host seller security boundary file: Security/Seller/" + file);
            Assert.False(
                File.Exists(Path.Combine(hostRoot, "Seller", file)),
                "stale Host/Seller security file remains: Seller/" + file);
        }
    }

    [Fact]
    public void Seller_security_boundary_references_no_foreign_module_application_or_domain()
    {
        var boundaryRoot = Path.Combine(HostRoot(), "Security", "Seller");
        var violations = new List<string>();

        foreach (var path in Directory.EnumerateFiles(boundaryRoot, "*.cs", SearchOption.TopDirectoryOnly))
        {
            foreach (var raw in File.ReadLines(path))
            {
                var line = raw.Trim();
                if (!ForeignModuleApplicationOrDomain.IsMatch(line))
                {
                    continue;
                }

                if (AcceptedForeignNamespaceLines.Contains(line, StringComparer.Ordinal))
                {
                    continue;
                }

                violations.Add(Path.GetFileName(path) + ": " + line);
            }
        }

        Assert.True(violations.Count == 0, "foreign module coupling: " + string.Join("; ", violations));
    }

    [Fact]
    public void Panel_gate_resolves_runtime_dependencies_from_di_not_the_service_locator()
    {
        var gate = File.ReadAllText(Path.Combine(HostRoot(), "Security", "Seller", "SellerPanelAccess.cs"));
        var adapter = File.ReadAllText(Path.Combine(HostRoot(), "Security", "Seller", "HostSellerPanelAccess.cs"));

        Assert.Contains("IAuthorizationGuard guard", gate, StringComparison.Ordinal);
        Assert.Contains("CurrentAuthenticatedSession session", gate, StringComparison.Ordinal);
        Assert.Contains("seller.actor.missing", gate, StringComparison.Ordinal);
        Assert.Contains("seller.identity.missing", gate, StringComparison.Ordinal);
        Assert.Contains("seller.authorization.denied", gate, StringComparison.Ordinal);
        Assert.Contains("seller.authorization.unavailable", gate, StringComparison.Ordinal);
        Assert.Contains("SellerPartyHeader", gate, StringComparison.Ordinal);
        Assert.Contains("DevActorHeader", gate, StringComparison.Ordinal);

        Assert.Contains(": ISellerPanelAccess", adapter, StringComparison.Ordinal);
        Assert.DoesNotContain("RequestServices", adapter, StringComparison.Ordinal);
    }

    [Fact]
    public void Thin_adapters_delegate_to_the_neutral_panel_seam_and_reference_no_access_control_application_or_domain()
    {
        var boundaryRoot = Path.Combine(HostRoot(), "Security", "Seller");

        foreach (var file in ThinAdapterFiles)
        {
            var text = File.ReadAllText(Path.Combine(boundaryRoot, file));
            Assert.DoesNotContain("RequestServices", text);
            Assert.DoesNotContain("Tooba.AccessControl", text);
            Assert.DoesNotContain("DbContext", text);
        }

        var offer = File.ReadAllText(Path.Combine(boundaryRoot, "HostOfferSellerAuthorizer.cs"));
        Assert.Contains("ISellerPanelAccess sellerAccess", offer, StringComparison.Ordinal);
        Assert.Contains("IOfferSellerAuthorizer", offer, StringComparison.Ordinal);

        var orderReader = File.ReadAllText(Path.Combine(boundaryRoot, "HostSellerOrderViewAccessReader.cs"));
        Assert.Contains("IPlatformEffectiveAccessReader", orderReader, StringComparison.Ordinal);
        Assert.Contains("PlatformAccessOwnerKind.Seller", orderReader, StringComparison.Ordinal);
        Assert.Contains("GetEffectivePermissionsAsync", orderReader, StringComparison.Ordinal);
        Assert.Contains("DeniedByCeiling", orderReader, StringComparison.Ordinal);

        var support = File.ReadAllText(Path.Combine(boundaryRoot, "HostSupportSellerAuthorizer.cs"));
        Assert.Contains("IPlatformEffectiveAccessReader", support, StringComparison.Ordinal);
        Assert.Contains("PlatformAccessOwnerKind.Seller", support, StringComparison.Ordinal);
        Assert.Contains("SellerAuthorizationDenied", support, StringComparison.Ordinal);
    }

    [Fact]
    public void Program_registers_the_security_seller_boundary_and_no_stale_host_seller_implementation()
    {
        var program = File.ReadAllText(Path.Combine(HostRoot(), "Program.cs"));

        Assert.Contains("Tooba.Host.Security.Seller.HostSellerPanelAccess", program, StringComparison.Ordinal);
        Assert.Contains("Tooba.Host.Security.Seller.HostOfferSellerAuthorizer", program, StringComparison.Ordinal);
        Assert.Contains("Tooba.Host.Security.Seller.HostOrderSellerAuthorizer", program, StringComparison.Ordinal);
        Assert.Contains("Tooba.Host.Security.Seller.HostReturnSellerAuthorizer", program, StringComparison.Ordinal);
        Assert.Contains("Tooba.Host.Security.Seller.HostSettlementSellerAuthorizer", program, StringComparison.Ordinal);
        Assert.Contains("Tooba.Host.Security.Seller.HostNotificationSellerAuthorizer", program, StringComparison.Ordinal);
        Assert.Contains("Tooba.Host.Security.Seller.HostSupportSellerAuthorizer", program, StringComparison.Ordinal);
        Assert.Contains("Tooba.Host.Security.Seller.HostPromotionSellerAuthorizer", program, StringComparison.Ordinal);
        Assert.Contains(
            "Tooba.Host.Security.Seller.HostSellerOrderViewAccessReader", program, StringComparison.Ordinal);

        Assert.DoesNotContain("Tooba.Host.Seller.HostOfferSellerAuthorizer", program, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host.Seller.HostOrderSellerAuthorizer", program, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host.Seller.HostSellerPanelAccess", program, StringComparison.Ordinal);
    }

    [Fact]
    public void No_foreign_seller_dto_global_alias_remains_in_host_production()
    {
        var hostRoot = HostRoot();
        var aliasFiles = Directory.EnumerateFiles(hostRoot, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(p => File.ReadAllText(p).Contains("global using SellerOffer", StringComparison.Ordinal))
            .Select(p => Path.GetFileName(p))
            .ToArray();

        Assert.Equal(["SellerPanelModels.cs"], aliasFiles);
    }

    private static string HostRoot() =>
        Path.Combine(FindRepoRoot(), "src", "backend", "Host", "Tooba.Host");

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
