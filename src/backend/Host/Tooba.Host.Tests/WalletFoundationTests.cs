using Tooba.AccessControl.Application;
using Tooba.AccessControl.Domain;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>پوشش قرارداد HTTP، مجوزها و مرز ماژول Wallet.</summary>
public sealed class WalletFoundationTests
{
    /// <summary>کاتالوگ باید wallet/giftcard را داشته باشد.</summary>
    [Fact]
    public void Permission_catalog_includes_wallet_capabilities()
    {
        var ids = PermissionCatalog.All.Select(p => p.PermissionId).ToHashSet(StringComparer.Ordinal);
        Assert.Contains("wallet.view", ids);
        Assert.Contains("wallet.adjust", ids);
        Assert.Contains("giftcard.view", ids);
        Assert.Contains("giftcard.manage", ids);
        Assert.False(PermissionCatalog.IsDelegable("wallet.view"));
        Assert.False(PermissionCatalog.IsDelegable("giftcard.manage"));
        var view = PermissionCatalog.Require("wallet.view");
        Assert.Equal("Wallet", view.Module);
        Assert.Contains(AccessScopeKind.GlobalWithinOwner, view.ScopeKinds);
    }

    /// <summary>نقاط انتهایی Customer/Admin در Host سیم‌کشی شده‌اند.</summary>
    [Fact]
    public void Wallet_endpoints_source_declares_audience_routes()
    {
        var path = Path.Combine(FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Wallet", "WalletEndpoints.cs");
        Assert.True(File.Exists(path));
        var source = File.ReadAllText(path);
        Assert.Contains("MapWalletEndpoints", source, StringComparison.Ordinal);
        Assert.Contains("/v1/customer/wallet", source, StringComparison.Ordinal);
        Assert.Contains("/v1/customer/wallet/ledger", source, StringComparison.Ordinal);
        Assert.Contains("/v1/customer/wallet/gift-cards/redeem", source, StringComparison.Ordinal);
        Assert.Contains("/v1/admin/gift-cards", source, StringComparison.Ordinal);
        Assert.Contains("/v1/admin/wallets/", source, StringComparison.Ordinal);
        Assert.Contains("demo-preview", source, StringComparison.Ordinal);
        Assert.Contains("AdminPanelAccess.RequireAuthorizedAsync", source, StringComparison.Ordinal);
    }

    /// <summary>ماژول Wallet نباید Infrastructure همتا را ProjectReference کند.</summary>
    [Fact]
    public void Wallet_infrastructure_does_not_reference_peer_infrastructure()
    {
        var csproj = Path.Combine(
            FindRepoRoot(),
            "src",
            "backend",
            "Modules",
            "Wallet",
            "Tooba.Wallet.Infrastructure",
            "Tooba.Wallet.Infrastructure.csproj");
        Assert.True(File.Exists(csproj));
        var text = File.ReadAllText(csproj);
        Assert.DoesNotContain("Tooba.Order.Infrastructure", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Payment.Infrastructure", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Notification.Infrastructure", text, StringComparison.Ordinal);
        Assert.Contains("Tooba.Wallet.Application", text, StringComparison.Ordinal);
        Assert.Contains("Tooba.Notification.Application", text, StringComparison.Ordinal);
    }

    /// <summary>schema کیف پول و ثبت migration در Registry.</summary>
    [Fact]
    public void Wallet_schema_and_migration_registry_wired()
    {
        var dbContext = Path.Combine(
            FindRepoRoot(),
            "src",
            "backend",
            "Modules",
            "Wallet",
            "Tooba.Wallet.Infrastructure",
            "Persistence",
            "WalletDbContext.cs");
        Assert.True(File.Exists(dbContext));
        Assert.Contains("\"wallet\"", File.ReadAllText(dbContext), StringComparison.Ordinal);

        var registry = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "backend",
            "Host",
            "Tooba.MigrationRunner",
            "ModuleMigrationRegistry.cs"));
        Assert.Contains("WalletDbContext", registry, StringComparison.Ordinal);

        var composition = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "backend",
            "Host",
            "Tooba.Host",
            "ToobaModuleComposition.cs"));
        Assert.Contains("WalletModule", composition, StringComparison.Ordinal);

        var program = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "backend",
            "Host",
            "Tooba.Host",
            "Program.cs"));
        Assert.Contains("MapWalletEndpoints", program, StringComparison.Ordinal);
        Assert.Contains("WalletDevelopmentSeedHost", program, StringComparison.Ordinal);
    }

    /// <summary>مسیر deep-link کیف پول در allowlist اعلان‌ها باشد.</summary>
    [Fact]
    public void Notification_target_routes_allow_customer_wallet()
    {
        var path = Path.Combine(
            FindRepoRoot(),
            "src",
            "backend",
            "Modules",
            "Notification",
            "Tooba.Notification.Application",
            "NotificationContracts.cs");
        var source = File.ReadAllText(path);
        Assert.Contains("CustomerWallet", source, StringComparison.Ordinal);
        Assert.Contains("/customer-panel/wallet", source, StringComparison.Ordinal);
        Assert.Contains("WalletGiftCardRedeemed", source, StringComparison.Ordinal);
        Assert.Contains("WalletAdminAdjustment", source, StringComparison.Ordinal);
    }

    /// <summary>FE ناوبری کیف پول/کارت هدیه را از deferred خارج کرده است.</summary>
    [Fact]
    public void Frontend_nav_un_defers_wallet_and_gift_cards()
    {
        var root = FindRepoRoot();
        var customer = File.ReadAllText(Path.Combine(root, "src", "frontend", "app", "customer-panel", "customer-panel-shell.tsx"));
        var admin = File.ReadAllText(Path.Combine(root, "src", "frontend", "app", "admin", "admin-shell.tsx"));

        Assert.Contains("href: \"/customer-panel/wallet\"", customer, StringComparison.Ordinal);
        Assert.Contains("href: \"/customer-panel/gift-cards\"", customer, StringComparison.Ordinal);
        Assert.DoesNotContain("\"/customer-panel/wallet\"", ExtractDeferred(customer, "CUSTOMER_DEFERRED_NAV_HREFS"), StringComparison.Ordinal);
        Assert.DoesNotContain("\"/customer-panel/gift-cards\"", ExtractDeferred(customer, "CUSTOMER_DEFERRED_NAV_HREFS"), StringComparison.Ordinal);

        Assert.Contains("href: \"/admin/gift-cards\"", admin, StringComparison.Ordinal);
        Assert.Contains("viewPermission: \"giftcard.view\"", admin, StringComparison.Ordinal);
        Assert.Contains("viewPermission: \"wallet.view\"", admin, StringComparison.Ordinal);
    }

    private static string ExtractDeferred(string source, string exportName)
    {
        var start = source.IndexOf($"export const {exportName}", StringComparison.Ordinal);
        Assert.True(start >= 0, exportName);
        var end = source.IndexOf("] as const;", start, StringComparison.Ordinal);
        Assert.True(end > start);
        return source[start..(end + 11)];
    }

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "AGENTS.md")))
        {
            current = current.Parent;
        }

        Assert.NotNull(current);
        return current!.FullName;
    }
}
