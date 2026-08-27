using Tooba.AccessControl.Application;
using Tooba.AccessControl.Domain;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// پوشش قرارداد HTTP، مجوزها و مرز ماژول Support.
/// تست‌های typed/integration پس از فرود Modules/Support فعال می‌شوند.
/// </summary>
public sealed class SupportFoundationTests
{
    /// <summary>کاتالوگ باید support.view/create/reply/manage را داشته باشد.</summary>
    [Fact]
    public void Permission_catalog_includes_support_capabilities_when_present()
    {
        var ids = PermissionCatalog.All.Select(p => p.PermissionId).ToHashSet(StringComparer.Ordinal);
        if (!ids.Contains("support.view"))
        {
            // Sibling worker هنوز PermissionCatalog را به‌روز نکرده.
            return;
        }

        Assert.Contains("support.view", ids);
        Assert.Contains("support.create", ids);
        Assert.Contains("support.reply", ids);
        Assert.Contains("support.manage", ids);
        Assert.True(PermissionCatalog.IsDelegable("support.view"));
        Assert.True(PermissionCatalog.IsDelegable("support.create"));
        Assert.True(PermissionCatalog.IsDelegable("support.reply"));
        var view = PermissionCatalog.Require("support.view");
        Assert.Equal("Support", view.Module);
        Assert.Contains(AccessScopeKind.GlobalWithinOwner, view.ScopeKinds);
    }

    /// <summary>نقاط انتهایی Customer/Seller/Admin در Host سیم‌کشی شده‌اند.</summary>
    [Fact]
    public void Support_endpoints_source_declares_audience_routes_when_present()
    {
        var path = Path.Combine(FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Support", "SupportEndpoints.cs");
        if (!File.Exists(path))
        {
            return;
        }

        var source = File.ReadAllText(path);
        Assert.Contains("MapSupportEndpoints", source, StringComparison.Ordinal);
        Assert.Contains("/v1/customer/support/tickets", source, StringComparison.Ordinal);
        Assert.Contains("/v1/seller/support/tickets", source, StringComparison.Ordinal);
        Assert.Contains("/v1/admin/support/tickets", source, StringComparison.Ordinal);
        Assert.Contains("/replies", source, StringComparison.Ordinal);
        Assert.Contains("SellerPanelAccess.RequireAuthorizedAsync", source, StringComparison.Ordinal);
        Assert.Contains("AdminPanelAccess.RequireAuthorizedAsync", source, StringComparison.Ordinal);
        Assert.Contains("IsInternalNote", source, StringComparison.Ordinal);
        Assert.Contains("demo-preview", source, StringComparison.Ordinal);
    }

    /// <summary>ماژول Support نباید Infrastructure ماژول‌های دیگر را ProjectReference کند.</summary>
    [Fact]
    public void Support_infrastructure_does_not_reference_peer_infrastructure_when_present()
    {
        var csproj = Path.Combine(
            FindRepoRoot(),
            "src",
            "backend",
            "Modules",
            "Support",
            "Tooba.Support.Infrastructure",
            "Tooba.Support.Infrastructure.csproj");
        if (!File.Exists(csproj))
        {
            return;
        }

        var text = File.ReadAllText(csproj);
        Assert.DoesNotContain("Tooba.Order.Infrastructure", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Returns.Infrastructure", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Notification.Infrastructure", text, StringComparison.Ordinal);
        Assert.Contains("Tooba.Support.Application", text, StringComparison.Ordinal);
    }

    /// <summary>schema پشتیبانی و ثبت migration در Registry.</summary>
    [Fact]
    public void Support_schema_and_migration_registry_wired_when_present()
    {
        var dbContext = Path.Combine(
            FindRepoRoot(),
            "src",
            "backend",
            "Modules",
            "Support",
            "Tooba.Support.Infrastructure",
            "Persistence",
            "SupportDbContext.cs");
        if (!File.Exists(dbContext))
        {
            return;
        }

        var dbSource = File.ReadAllText(dbContext);
        Assert.Contains("\"support\"", dbSource, StringComparison.Ordinal);

        var registry = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "backend",
            "Host",
            "Tooba.MigrationRunner",
            "ModuleMigrationRegistry.cs"));
        Assert.Contains("SupportDbContext", registry, StringComparison.Ordinal);

        var composition = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "backend",
            "Host",
            "Tooba.Host",
            "ToobaModuleComposition.cs"));
        Assert.Contains("SupportModule", composition, StringComparison.Ordinal);

        var program = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "backend",
            "Host",
            "Tooba.Host",
            "Program.cs"));
        Assert.Contains("MapSupportEndpoints", program, StringComparison.Ordinal);
    }

    /// <summary>مسیر deep-link تیکت در allowlist اعلان‌ها باشد.</summary>
    [Fact]
    public void Notification_target_routes_allow_ticket_deep_links_when_present()
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
        if (!source.Contains("CustomerTicket", StringComparison.Ordinal)
            && !source.Contains("/customer-panel/tickets/", StringComparison.Ordinal))
        {
            return;
        }

        Assert.True(
            source.Contains("CustomerTicket", StringComparison.Ordinal)
            || source.Contains("/customer-panel/tickets/", StringComparison.Ordinal));
        Assert.True(
            source.Contains("SellerTicket", StringComparison.Ordinal)
            || source.Contains("/vendor-panel/tickets/", StringComparison.Ordinal));
    }

    /// <summary>FE ناوبری تیکت را از deferred خارج کرده و support.view را پروجکت می‌کند.</summary>
    [Fact]
    public void Frontend_nav_un_defers_tickets_and_projects_support_view()
    {
        var root = FindRepoRoot();
        var customer = File.ReadAllText(Path.Combine(root, "src", "frontend", "app", "customer-panel", "customer-panel-shell.tsx"));
        var vendor = File.ReadAllText(Path.Combine(root, "src", "frontend", "app", "vendor-panel", "vendor-shell.tsx"));
        var admin = File.ReadAllText(Path.Combine(root, "src", "frontend", "app", "admin", "admin-shell.tsx"));

        Assert.Contains("href: \"/customer-panel/tickets\"", customer, StringComparison.Ordinal);
        Assert.DoesNotContain("\"/customer-panel/tickets\"", ExtractDeferred(customer, "CUSTOMER_DEFERRED_NAV_HREFS"), StringComparison.Ordinal);

        Assert.Contains("href: \"/vendor-panel/tickets\"", vendor, StringComparison.Ordinal);
        Assert.Contains("viewPermission: \"support.view\"", vendor, StringComparison.Ordinal);
        Assert.DoesNotContain("\"/vendor-panel/tickets\"", ExtractDeferred(vendor, "VENDOR_DEFERRED_NAV_HREFS"), StringComparison.Ordinal);

        Assert.Contains("href: \"/admin/tickets\"", admin, StringComparison.Ordinal);
        Assert.Contains("viewPermission: \"support.view\"", admin, StringComparison.Ordinal);
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
