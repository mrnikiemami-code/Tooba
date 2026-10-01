using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-SELLER-AMC-001-R1 / R1A — durable guard for the Host seller platform security boundary.
///
/// The Host seller panel gate and its thin module-endpoint auth adapters live under
/// <c>Host/Security/Seller</c> (Host security platform boundary, not a business owner), the panel
/// gate resolves its runtime dependencies from DI instead of the service locator, the effective
/// permission projection consumes only the neutral platform seam, and no foreign
/// Application/Infrastructure/Domain module type is referenced from the boundary.
///
/// R1A tightens the R1 boundary from "no AccessControl Application/Domain" to ZERO foreign
/// Application/Domain/Infrastructure/Persistence reference across the entire boundary: the Order
/// view-access port implementation moved into Order-owned Infrastructure and the stable seller
/// security codes are Host-boundary owned.
/// </summary>
public sealed class HostSellerAmcR1GuardTests
{
    private static readonly string[] BoundaryFiles =
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

    private static readonly string[] ThinAdapterFiles =
    [
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
        "HostSellerPanelAccess.cs",
    ];

    // R1A: the whole Host seller security boundary must be ZERO foreign
    // Application/Domain/Infrastructure/Persistence. No line allowance exists any more.
    private static readonly Regex ForeignModuleLayer = new(
        @"Tooba\.(Catalog|Party|AccessControl|Identity|Order|Offer|Promotion|Returns|Settlement|Notification|Support|Story|Persistence)\.(Application|Domain|Infrastructure|Persistence)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

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
    public void Seller_security_boundary_references_zero_foreign_module_layers()
    {
        var boundaryRoot = Path.Combine(HostRoot(), "Security", "Seller");
        var violations = new List<string>();

        foreach (var path in Directory.EnumerateFiles(boundaryRoot, "*.cs", SearchOption.TopDirectoryOnly))
        {
            foreach (var raw in File.ReadLines(path))
            {
                var line = raw.Trim();
                if (ForeignModuleLayer.IsMatch(line))
                {
                    violations.Add(Path.GetFileName(path) + ": " + line);
                }
            }
        }

        Assert.True(violations.Count == 0, "foreign module coupling: " + string.Join("; ", violations));
    }

    [Fact]
    public void Host_no_longer_implements_the_order_view_access_port_and_order_owns_it()
    {
        var hostRoot = HostRoot();
        Assert.False(
            File.Exists(Path.Combine(hostRoot, "Security", "Seller", "HostSellerOrderViewAccessReader.cs")),
            "Host must no longer implement the Order Application view-access port");

        var orderOwned = Path.Combine(
            FindRepoRoot(), "src", "backend", "Modules", "Order", "Tooba.Order.Infrastructure",
            "Seller", "SellerOrderViewAccessReader.cs");
        Assert.True(File.Exists(orderOwned), "Order-owned view-access implementation is required");
        var text = File.ReadAllText(orderOwned);
        Assert.Contains(": ISellerOrderViewAccessReader", text, StringComparison.Ordinal);
        Assert.Contains("IPlatformEffectiveAccessReader", text, StringComparison.Ordinal);
        Assert.Contains("PlatformAccessOwnerKind.Seller", text, StringComparison.Ordinal);
        Assert.Contains("PlatformAccessScopeKind.Category", text, StringComparison.Ordinal);
        Assert.Contains("DeniedByCeiling", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.AccessControl", text, StringComparison.Ordinal);

        var module = File.ReadAllText(Path.Combine(
            FindRepoRoot(), "src", "backend", "Modules", "Order", "Tooba.Order.Infrastructure", "OrderModule.cs"));
        Assert.Contains("Application.Seller.Ports.ISellerOrderViewAccessReader, SellerOrderViewAccessReader", module, StringComparison.Ordinal);
    }

    [Fact]
    public void Host_order_and_support_authorizers_no_longer_reference_foreign_application_error_codes()
    {
        var boundaryRoot = Path.Combine(HostRoot(), "Security", "Seller");

        var order = File.ReadAllText(Path.Combine(boundaryRoot, "HostOrderSellerAuthorizer.cs"));
        Assert.DoesNotContain("SellerOrderErrors", order, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Order.Application", order, StringComparison.Ordinal);
        Assert.Contains("catch (SemanticException", order, StringComparison.Ordinal);
        Assert.DoesNotContain("catch (PlatformHttpException", order, StringComparison.Ordinal);
        Assert.Contains("ex.Error", order, StringComparison.Ordinal);

        var support = File.ReadAllText(Path.Combine(boundaryRoot, "HostSupportSellerAuthorizer.cs"));
        Assert.DoesNotContain("SupportErrorCodes", support, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Support.Application", support, StringComparison.Ordinal);
        Assert.Contains("SellerSecurityErrorCodes.AuthorizationDenied", support, StringComparison.Ordinal);
        Assert.Contains("IPlatformEffectiveAccessReader", support, StringComparison.Ordinal);
        Assert.Contains("PlatformAccessOwnerKind.Seller", support, StringComparison.Ordinal);
    }

    [Fact]
    public void Host_owned_seller_security_error_codes_are_stable_and_unchanged()
    {
        var codes = File.ReadAllText(Path.Combine(
            HostRoot(), "Security", "Seller", "SellerSecurityErrorCodes.cs"));
        Assert.Contains("namespace Tooba.Host.Security.Seller;", codes, StringComparison.Ordinal);
        Assert.Contains("\"seller.actor.missing\"", codes, StringComparison.Ordinal);
        Assert.Contains("\"seller.identity.missing\"", codes, StringComparison.Ordinal);
        Assert.Contains("\"seller.authorization.denied\"", codes, StringComparison.Ordinal);
        Assert.Contains("\"seller.authorization.unavailable\"", codes, StringComparison.Ordinal);

        var gate = File.ReadAllText(Path.Combine(HostRoot(), "Security", "Seller", "SellerPanelAccess.cs"));
        Assert.Contains("SellerSecurityErrorCodes.ActorMissing", gate, StringComparison.Ordinal);
        Assert.Contains("SellerSecurityErrorCodes.IdentityMissing", gate, StringComparison.Ordinal);
        Assert.Contains("SellerSecurityErrorCodes.AuthorizationDenied", gate, StringComparison.Ordinal);
        Assert.Contains("SellerSecurityErrorCodes.AuthorizationUnavailable", gate, StringComparison.Ordinal);
    }

    [Fact]
    public void Panel_gate_resolves_runtime_dependencies_from_di_not_the_service_locator()
    {
        var gate = File.ReadAllText(Path.Combine(HostRoot(), "Security", "Seller", "SellerPanelAccess.cs"));
        var adapter = File.ReadAllText(Path.Combine(HostRoot(), "Security", "Seller", "HostSellerPanelAccess.cs"));

        Assert.Contains("IAuthorizationGuard guard", gate, StringComparison.Ordinal);
        Assert.Contains("CurrentAuthenticatedSession session", gate, StringComparison.Ordinal);
        Assert.Contains("SellerPartyHeader", gate, StringComparison.Ordinal);
        Assert.Contains("DevActorHeader", gate, StringComparison.Ordinal);

        Assert.Contains(": ISellerPanelAccess", adapter, StringComparison.Ordinal);
        Assert.DoesNotContain("RequestServices", adapter, StringComparison.Ordinal);
    }

    [Fact]
    public void Thin_adapters_delegate_to_the_neutral_panel_seam_and_reference_no_access_control_or_persistence()
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
        Assert.Contains("Tooba.Host.Security.Seller.HostPartySellerAuthorizer", program, StringComparison.Ordinal);

        Assert.DoesNotContain("Tooba.Host.Seller.HostOfferSellerAuthorizer", program, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host.Seller.HostOrderSellerAuthorizer", program, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host.Seller.HostSellerPanelAccess", program, StringComparison.Ordinal);
        Assert.DoesNotContain("HostSellerOrderViewAccessReader", program, StringComparison.Ordinal);
        // R5 removed the final Host/Seller surface: no Host seller panel mapping remains.
        Assert.DoesNotContain("MapSellerPanelEndpoints", program, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host.Seller", program, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "Tooba.Order.Application.Seller.Ports.ISellerOrderViewAccessReader",
            program,
            StringComparison.Ordinal);
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

        // R4 deleted the zero-consumer SellerPanelModels alias file; no foreign seller DTO global alias remains.
        Assert.Empty(aliasFiles);
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
