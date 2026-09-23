using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-CART-HOST-RESIDUAL-REVERIFY-001 — durable Host → Cart reverse-audit guard.
/// Prevents Cart business authority from re-entering production Host.
/// Explicit allowlists only; no broad wildcards.
/// </summary>
public sealed class HostCartResidualGuardTests
{
    /// <summary>
    /// Production Host files allowed to name Cart at all, with the reason each is thin.
    /// Every entry is a composition seam, execution shell, or dev/migration surface.
    /// </summary>
    private static readonly Dictionary<string, string> CartNamingAllowlist = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Program.cs"] = "endpoint/MediatR/module composition + Cart reconciler seam registration",
        ["Composition/ToobaModuleComposition.cs"] = "explicit module list composition",
        ["Composition/HostCartPersistenceHoursResolver.cs"] = "Host seam forwarding Catalog store override to Cart",
        ["CartExpiryHostedService.cs"] = "background execution shell calling one Cart-owned reconciler",
        ["CartExpiryHostOptions.cs"] = "Host scheduling knobs only (no Cart lifetime policy)",
        ["CommerceHoldPolicy.cs"] = "Payment/Order hold adapter; forwards Cart-owned persistence value",
        ["CheckoutReservationHoldPolicy.cs"] = "Order hold port adapter (Payment options only)",
        ["Admin/HoldPolicySettingsEndpoints.cs"] = "settings admin UX reading Cart-owned persistence seam",
        ["Admin/ProductWorkspaceDevelopmentBootstrap.cs"] = "Development-only schema migration list",
        ["AccessControl/AccessControlDevelopmentSeed.cs"] = "Development-only seed via Cart directory",
        ["Storefront/StorefrontModels.cs"] = "storefront wire DTOs that carry CartId",
        ["Storefront/StorefrontComposer.cs"] = "storefront read composition flag",
        ["Storefront/StorefrontEndpoints.cs"] = "checkout identity policy flag name (no Cart authority)",
        ["Order/HostOrderStorefrontActor.cs"] = "thin session → CartAccess adapter",
        ["Customer/HostFulfillmentCustomerAuthorizer.cs"] = "thin ownership probe via Cart contracts",
        ["GlobalUsings.SettlementApp.cs"] = "Settlement-only global usings (no Cart import)",
        ["GlobalUsings.SettlementDomain.cs"] = "Settlement-only global usings (no Cart import)",
        ["OfferGlobalUsings.cs"] = "Offer alias only",
        ["UnpaidOrderExpiryHostedService.cs"] = "Order worker shell (no Cart authority)",
        ["UnpaidOrderExpiryHostOptions.cs"] = "Order worker scheduling knobs",
        ["PaymentReconciliationHostedService.cs"] = "Payment worker shell",
        ["PaymentReconciliationHostOptions.cs"] = "Payment worker scheduling knobs",
        ["FulfillmentReturnsGridAliases.cs"] = "grid aliases",
    };

    [Fact]
    public void Host_Cart_naming_files_are_explicitly_allowlisted()
    {
        var hostRoot = Path.Combine(FindRepoRoot(), "src", "backend", "Host", "Tooba.Host");
        var offenders = new List<string>();
        foreach (var path in EnumerateProductionSources(hostRoot))
        {
            var relative = Path.GetRelativePath(hostRoot, path).Replace('\\', '/');
            var text = File.ReadAllText(path);
            if (!Regex.IsMatch(text, @"\bCart\w*", RegexOptions.Multiline))
            {
                continue;
            }

            if (!CartNamingAllowlist.ContainsKey(relative))
            {
                offenders.Add(relative);
            }
        }

        Assert.True(offenders.Count == 0, "unlisted Host file referencing Cart: " + string.Join("; ", offenders));
    }

    [Fact]
    public void Host_has_no_Cart_business_authority()
    {
        var hostRoot = Path.Combine(FindRepoRoot(), "src", "backend", "Host", "Tooba.Host");
        var forbidden = new List<string>();
        foreach (var path in EnumerateProductionSources(hostRoot))
        {
            var relative = Path.GetRelativePath(hostRoot, path).Replace('\\', '/');
            var text = File.ReadAllText(path);

            if (text.Contains("CartDbContext", StringComparison.Ordinal)
                && relative is not "Admin/ProductWorkspaceDevelopmentBootstrap.cs")
            {
                forbidden.Add($"{relative}: CartDbContext");
            }

            if (text.Contains("using Tooba.Cart.Domain", StringComparison.Ordinal)
                || text.Contains("global using Tooba.Cart.Domain", StringComparison.Ordinal))
            {
                forbidden.Add($"{relative}: Cart.Domain import");
            }

            if (text.Contains("Tooba.Cart.Infrastructure.Persistence", StringComparison.Ordinal)
                && relative is not "Admin/ProductWorkspaceDevelopmentBootstrap.cs")
            {
                forbidden.Add($"{relative}: Cart.Infrastructure persistence import");
            }

            if (Regex.IsMatch(text, @"\bShoppingCart\b|\bCartLine\b|\bCartStatus\b|\bCartConversionIntent\b"))
            {
                forbidden.Add($"{relative}: Cart domain type");
            }
        }

        Assert.True(forbidden.Count == 0, "Cart business authority in Host: " + string.Join("; ", forbidden));
    }

    [Fact]
    public void Host_Cart_expiry_worker_is_shell_only()
    {
        var hostRoot = Path.Combine(FindRepoRoot(), "src", "backend", "Host", "Tooba.Host");
        var worker = File.ReadAllText(Path.Combine(hostRoot, "CartExpiryHostedService.cs"));

        Assert.Contains("ICartExpiryReconciler", worker, StringComparison.Ordinal);
        Assert.Contains("ReconcileAsync(_options.BatchSize, cancellationToken)", worker, StringComparison.Ordinal);
        Assert.DoesNotContain("ICartDirectory", worker, StringComparison.Ordinal);
        Assert.DoesNotContain("ExpireDueCartsAsync", worker, StringComparison.Ordinal);
        Assert.DoesNotContain("new PositiveInteger", worker, StringComparison.Ordinal);
        Assert.DoesNotContain("SystemClock", worker, StringComparison.Ordinal);
    }

    [Fact]
    public void Cart_owns_expiry_reconciliation_and_persistence_policy()
    {
        var cartRoot = Path.Combine(FindRepoRoot(), "src", "backend", "Modules", "Cart");

        var reconciler = File.ReadAllText(Path.Combine(
            cartRoot, "Tooba.Cart.Infrastructure", "Lifetime", "CartExpiryReconciler.cs"));
        Assert.Contains("IClock", reconciler, StringComparison.Ordinal);
        Assert.Contains("_clock.UtcNow", reconciler, StringComparison.Ordinal);
        Assert.Contains("ExpireDueCartsAsync", reconciler, StringComparison.Ordinal);
        Assert.DoesNotContain("DateTimeOffset.UtcNow", reconciler, StringComparison.Ordinal);

        var policy = File.ReadAllText(Path.Combine(
            cartRoot, "Tooba.Cart.Application", "Ports", "CartPersistenceHours.cs"));
        Assert.Contains("ICartPersistenceHoursResolver", policy, StringComparison.Ordinal);
        Assert.Contains("DefaultHours", policy, StringComparison.Ordinal);

        var source = File.ReadAllText(Path.Combine(
            cartRoot, "Tooba.Cart.Infrastructure", "Lifetime", "CartPersistenceHoursSource.cs"));
        Assert.Contains("CartLifetimeOptions", source, StringComparison.Ordinal);

        var module = File.ReadAllText(Path.Combine(
            cartRoot, "Tooba.Cart.Infrastructure", "DependencyInjection", "CartModule.cs"));
        Assert.Contains("ICartExpiryReconciler, CartExpiryReconciler", module, StringComparison.Ordinal);
        Assert.Contains("ICartPersistenceHoursSource, CartPersistenceHoursSource", module, StringComparison.Ordinal);
        Assert.Contains("Configure<CartLifetimeOptions>", module, StringComparison.Ordinal);
    }

    [Fact]
    public void Host_has_no_broad_Cart_global_usings_or_duplicate_routes()
    {
        var hostRoot = Path.Combine(FindRepoRoot(), "src", "backend", "Host", "Tooba.Host");
        var globalUsings = Directory.EnumerateFiles(hostRoot, "GlobalUsings*.cs", SearchOption.TopDirectoryOnly)
            .Select(File.ReadAllText);
        Assert.DoesNotContain(globalUsings, text => text.Contains("Tooba.Cart.", StringComparison.Ordinal));

        Assert.False(File.Exists(Path.Combine(hostRoot, "GlobalUsings.CartSettlementApp.cs")));
        Assert.False(File.Exists(Path.Combine(hostRoot, "GlobalUsings.CartSettlementDomain.cs")));

        var program = File.ReadAllText(Path.Combine(hostRoot, "Program.cs"));
        Assert.Contains("MapCartEndpoints()", program, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPost(\"/cart\"", program, StringComparison.Ordinal);
        Assert.DoesNotContain("MapGet(\"/cart", program, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPatch(\"/cart", program, StringComparison.Ordinal);
        Assert.DoesNotContain("MapDelete(\"/cart", program, StringComparison.Ordinal);
        Assert.DoesNotContain("CartLifetimeOptions", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Cart_owns_its_domain_and_requires_no_Host_orchestrator()
    {
        var cartRoot = Path.Combine(FindRepoRoot(), "src", "backend", "Modules", "Cart");
        var offenders = new List<string>();
        foreach (var path in EnumerateProductionSources(cartRoot))
        {
            var relative = Path.GetRelativePath(cartRoot, path).Replace('\\', '/');
            var text = File.ReadAllText(path);
            if (text.Contains("ElapsedTime", StringComparison.Ordinal)
                || text.Contains("ICartExpiryOrchestrator", StringComparison.Ordinal)
                || text.Contains("HostDatabase", StringComparison.Ordinal)
                || text.Contains("INpgsqlLastActivityTracker", StringComparison.Ordinal))
            {
                offenders.Add(relative);
            }
        }

        Assert.True(offenders.Count == 0, "Cart internals depending on Host orchestration: " + string.Join("; ", offenders));
    }

    private static IEnumerable<string> EnumerateProductionSources(string root)
    {
        foreach (var path in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            var normalized = path.Replace('\\', '/');
            if (normalized.Contains("/bin/", StringComparison.Ordinal)
                || normalized.Contains("/obj/", StringComparison.Ordinal)
                || normalized.Contains("/Migrations/", StringComparison.OrdinalIgnoreCase)
                || normalized.EndsWith("ModelSnapshot.cs", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            yield return path;
        }
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "docs", "PROJECT-STATE.md")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo.root.not_found");
    }
}
