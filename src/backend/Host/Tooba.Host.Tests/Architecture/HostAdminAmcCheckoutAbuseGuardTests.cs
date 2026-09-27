using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>Checkout-abuse Admin settings owned by Catalog after Host evacuation.</summary>
public sealed class HostAdminAmcCheckoutAbuseGuardTests
{
    private static readonly Regex MapRouteRegex = new(@"Map(Get|Post|Put|Patch|Delete)\(", RegexOptions.Compiled);

    [Fact]
    public void Checkout_abuse_settings_owned_by_Catalog_and_absent_from_Host_Admin()
    {
        var root = FindRepoRoot();
        var hostFile = Path.Combine(root, "src/backend/Host/Tooba.Host/Admin/CheckoutAbuseSettingsEndpoints.cs");
        Assert.False(File.Exists(hostFile));

        var catalogEndpoints = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Admin/Settings/CheckoutAbuseSettingsEndpoints.cs"));
        Assert.Contains("MapGet(\"/\", GetAsync)", catalogEndpoints, StringComparison.Ordinal);
        Assert.Contains("MapPut(\"/\", PutAsync)", catalogEndpoints, StringComparison.Ordinal);
        Assert.Contains("/v1/admin/settings/checkout-abuse", catalogEndpoints, StringComparison.Ordinal);

        var module = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/CatalogEndpointModule.cs"));
        Assert.Contains("MapCheckoutAbuseSettingsEndpoints()", module, StringComparison.Ordinal);

        var program = File.ReadAllText(Path.Combine(root, "src/backend/Host/Tooba.Host/Program.cs"));
        Assert.DoesNotContain("MapCheckoutAbuseSettingsEndpoints()", program, StringComparison.Ordinal);

        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/StoreCheckoutAbuseSettingsDirectory.cs")));
        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Application/Settings/CheckoutAbuse/Queries/GetCheckoutAbuseSettingsQuery.cs")));
    }

    [Fact]
    public void Host_Admin_count_15_StoreAppearance_evacuated()
    {
        var root = FindRepoRoot();
        var admin = Path.Combine(root, "src/backend/Host/Tooba.Host/Admin");
        var adminCount = Directory.GetFiles(admin, "*.cs", SearchOption.AllDirectories).Length;
        Assert.Equal(15, adminCount);
        Assert.False(File.Exists(Path.Combine(admin, "StoreAppearanceSettingsEndpoints.cs")));
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Tooba.sln"))
                || File.Exists(Path.Combine(dir.FullName, "AGENTS.md")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
    }
}
