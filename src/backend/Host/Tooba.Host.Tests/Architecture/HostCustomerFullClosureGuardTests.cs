using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// Durable guards for TB-TMAR-HOST-CUSTOMER-FULL-CLOSURE-001.
/// Fail if Host/Customer residue, Host-mapped customer profile/dashboard, Modules/Customer,
/// foreign Application/Infrastructure/Domain leakage from customer-account presentation,
/// or duplicate customer.session.required registration reappear.
/// </summary>
public sealed class HostCustomerFullClosureGuardTests
{
    [Fact]
    public void Host_Customer_folder_has_zero_production_cs_files()
    {
        var folder = Path.Combine(FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Customer");
        var files = Directory.Exists(folder)
            ? Directory.GetFiles(folder, "*.cs", SearchOption.AllDirectories)
            : [];
        Assert.Empty(files);
        Assert.False(Directory.Exists(Path.Combine(FindRepoRoot(), "src", "backend", "Modules", "Customer")));
        Assert.Empty(Directory.GetDirectories(
            Path.Combine(FindRepoRoot(), "src", "backend", "Modules"),
            "Tooba.Customer",
            SearchOption.TopDirectoryOnly));
    }

    [Fact]
    public void Host_does_not_map_customer_profile_or_dashboard_routes()
    {
        var program = File.ReadAllText(Path.Combine(
            FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Program.cs"));
        Assert.DoesNotContain("MapCustomerPanelEndpoints", program, StringComparison.Ordinal);
        Assert.Contains("MapCustomerProfileModuleEndpoints", program, StringComparison.Ordinal);
        Assert.DoesNotContain("CustomerPanelComposer", program, StringComparison.Ordinal);
        Assert.DoesNotContain("MapGet(\"/dashboard\"", program, StringComparison.Ordinal);
        Assert.DoesNotContain("MapGet(\"/profile\"", program, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPut(\"/profile\"", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Customer_account_presentation_is_contracts_only_across_modules()
    {
        var endpointsRoot = Path.Combine(
            FindRepoRoot(),
            "src",
            "backend",
            "Modules",
            "CustomerProfile",
            "Tooba.CustomerProfile.Endpoints");
        foreach (var file in Directory.EnumerateFiles(endpointsRoot, "*.cs", SearchOption.AllDirectories)
                     .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                         && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("Tooba.Order.Application", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Wishlist.Application", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.AddressBook.Application", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Identity.Application", text, StringComparison.Ordinal);
            Assert.DoesNotContain("using Tooba.Order.Infrastructure", text, StringComparison.Ordinal);
            Assert.DoesNotContain("using Tooba.Wishlist.Infrastructure", text, StringComparison.Ordinal);
            Assert.DoesNotContain("using Tooba.AddressBook.Infrastructure", text, StringComparison.Ordinal);
            Assert.DoesNotContain("using Tooba.Identity.Infrastructure", text, StringComparison.Ordinal);
            Assert.DoesNotContain("using Tooba.Order.Domain", text, StringComparison.Ordinal);
            Assert.DoesNotContain("DbContextOptions", text, StringComparison.Ordinal);
            Assert.DoesNotContain("DbSet<", text, StringComparison.Ordinal);
        }

        var appRoot = Path.Combine(
            FindRepoRoot(),
            "src",
            "backend",
            "Modules",
            "CustomerProfile",
            "Tooba.CustomerProfile.Application");
        foreach (var file in Directory.EnumerateFiles(appRoot, "*.cs", SearchOption.AllDirectories)
                     .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                         && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("using Tooba.Order.Application", text, StringComparison.Ordinal);
            Assert.DoesNotContain("using Tooba.Wishlist.Application", text, StringComparison.Ordinal);
            Assert.DoesNotContain("using Tooba.AddressBook.Application", text, StringComparison.Ordinal);
            Assert.DoesNotContain("DbContextOptions", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Customer_session_required_is_not_re_registered_by_CustomerProfile()
    {
        var endpointsRoot = Path.Combine(
            FindRepoRoot(),
            "src",
            "backend",
            "Modules",
            "CustomerProfile",
            "Tooba.CustomerProfile.Endpoints");
        foreach (var file in Directory.EnumerateFiles(endpointsRoot, "*.cs", SearchOption.AllDirectories)
                     .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                         && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("IErrorCatalogContributor", text, StringComparison.Ordinal);
            Assert.DoesNotMatch(
                new Regex(@"new\s+ErrorDefinition\s*\(\s*""customer\.session\.required""", RegexOptions.CultureInvariant),
                text);
        }
    }

    [Fact]
    public void Path_namespace_and_no_stale_host_panel_copy()
    {
        Assert.True(File.Exists(Path.Combine(
            FindRepoRoot(),
            "src",
            "backend",
            "Modules",
            "CustomerProfile",
            "Tooba.CustomerProfile.Endpoints",
            "Customer",
            "CustomerProfileEndpoints.cs")));
        Assert.Contains(
            "namespace Tooba.CustomerProfile.Endpoints.Customer;",
            File.ReadAllText(Path.Combine(
                FindRepoRoot(),
                "src",
                "backend",
                "Modules",
                "CustomerProfile",
                "Tooba.CustomerProfile.Endpoints",
                "Customer",
                "CustomerProfileEndpoints.cs")),
            StringComparison.Ordinal);
        Assert.Contains(
            "namespace Tooba.CustomerProfile.Endpoints.CustomerDashboard;",
            File.ReadAllText(Path.Combine(
                FindRepoRoot(),
                "src",
                "backend",
                "Modules",
                "CustomerProfile",
                "Tooba.CustomerProfile.Endpoints",
                "CustomerDashboard",
                "CustomerAccountDashboardEndpoints.cs")),
            StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(
            FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Composition", "CustomerPanelEndpoints.cs")));

        var profileEp = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src", "backend", "Modules", "CustomerProfile", "Tooba.CustomerProfile.Endpoints",
            "Customer", "CustomerProfileEndpoints.cs"));
        var dashEp = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src", "backend", "Modules", "CustomerProfile", "Tooba.CustomerProfile.Endpoints",
            "CustomerDashboard", "CustomerAccountDashboardEndpoints.cs"));
        Assert.DoesNotContain("Results.Json(page)", profileEp, StringComparison.Ordinal);
        Assert.DoesNotContain("Results.Json(page)", dashEp, StringComparison.Ordinal);
        Assert.Contains("api.From(result)", profileEp, StringComparison.Ordinal);
        Assert.Contains("api.From(result)", dashEp, StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
