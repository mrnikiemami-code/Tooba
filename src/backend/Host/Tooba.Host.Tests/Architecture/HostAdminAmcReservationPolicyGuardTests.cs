using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>Reservation-policy Admin/Seller settings owned by Order after Host evacuation.</summary>
public sealed class HostAdminAmcReservationPolicyGuardTests
{
    [Fact]
    public void Reservation_policy_owned_by_Order_and_absent_from_Host_Admin()
    {
        var root = FindRepoRoot();
        Assert.False(File.Exists(Path.Combine(root, "src/backend/Host/Tooba.Host/Admin/ReservationPolicyAdminEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(root, "src/backend/Host/Tooba.Host/Admin/ReservationPolicyAdminComposer.cs")));
        Assert.False(File.Exists(Path.Combine(root, "src/backend/Host/Tooba.Host/Admin/ReservationPolicyAdminModels.cs")));

        var adminEndpoints = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Order/Tooba.Order.Endpoints/Admin/Settings/ReservationPolicyAdminEndpoints.cs"));
        Assert.Contains("/v1/admin/settings/reservation-policy", adminEndpoints, StringComparison.Ordinal);
        Assert.Contains("IOrderAdminAuthorizer", adminEndpoints, StringComparison.Ordinal);
        Assert.Contains("ISender", adminEndpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("CatalogDbContext", adminEndpoints, StringComparison.Ordinal);

        var sellerEndpoints = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Order/Tooba.Order.Endpoints/Seller/Settings/ReservationPolicySellerEndpoints.cs"));
        Assert.Contains("/v1/seller/settings/reservation-policy", sellerEndpoints, StringComparison.Ordinal);
        Assert.Contains("IOrderSellerAuthorizer", sellerEndpoints, StringComparison.Ordinal);

        var module = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Order/Tooba.Order.Endpoints/OrderEndpointModule.cs"));
        Assert.Contains("MapReservationPolicyAdminEndpoints()", module, StringComparison.Ordinal);
        Assert.Contains("MapReservationPolicySellerEndpoints()", module, StringComparison.Ordinal);

        var program = File.ReadAllText(Path.Combine(root, "src/backend/Host/Tooba.Host/Program.cs"));
        Assert.DoesNotContain("MapReservationPolicyAdminEndpoints()", program, StringComparison.Ordinal);

        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Contracts/Reservation/StoreReservationPolicySettingsContracts.cs")));
        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/Reservation/StoreReservationPolicySettingsPort.cs")));
        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/Order/Tooba.Order.Application/Admin/Settings/ReservationPolicy/Queries/GetStoreReservationPolicyQuery.cs")));
    }

    [Fact]
    public void Host_Admin_count_28_StoreAppearance_deferred()
    {
        var root = FindRepoRoot();
        var admin = Path.Combine(root, "src/backend/Host/Tooba.Host/Admin");
        var adminCount = Directory.GetFiles(admin, "*.cs", SearchOption.AllDirectories).Length;
        Assert.Equal(23, adminCount);
        Assert.True(File.Exists(Path.Combine(admin, "StoreAppearanceSettingsEndpoints.cs")));
        Assert.True(File.Exists(Path.Combine(admin, "HoldPolicySettingsEndpoints.cs")));
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
