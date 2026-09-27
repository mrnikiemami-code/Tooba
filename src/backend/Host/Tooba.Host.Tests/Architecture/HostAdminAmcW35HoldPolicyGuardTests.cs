using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-ADMIN-AMC-001-W35-HOLD-POLICY — evacuate Host Admin hold-policy aggregate to Catalog.
/// </summary>
public sealed class HostAdminAmcW35HoldPolicyGuardTests
{
    private static readonly Regex MapRouteRegex = new(@"Map(Get|Post|Put|Patch|Delete)\(", RegexOptions.Compiled);

    [Fact]
    public void Hold_policy_settings_owned_by_Catalog_and_absent_from_Host_Admin()
    {
        var root = FindRepoRoot();
        var hostFile = Path.Combine(root, "src/backend/Host/Tooba.Host/Admin/HoldPolicySettingsEndpoints.cs");
        Assert.False(File.Exists(hostFile));

        var catalogEndpoints = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Admin/Settings/HoldPolicySettingsEndpoints.cs"));
        Assert.Contains("MapGet(\"/\", GetAsync)", catalogEndpoints, StringComparison.Ordinal);
        Assert.Contains("MapPut(\"/\", PutAsync)", catalogEndpoints, StringComparison.Ordinal);
        Assert.Contains("/v1/admin/settings/hold-policy", catalogEndpoints, StringComparison.Ordinal);
        Assert.Contains("ICatalogAdminAuthorizer", catalogEndpoints, StringComparison.Ordinal);
        Assert.Contains("ApiResponseFactory", catalogEndpoints, StringComparison.Ordinal);

        var module = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/CatalogEndpointModule.cs"));
        Assert.Contains("MapHoldPolicySettingsEndpoints()", module, StringComparison.Ordinal);

        var program = File.ReadAllText(Path.Combine(root, "src/backend/Host/Tooba.Host/Program.cs"));
        Assert.DoesNotContain("MapHoldPolicySettingsEndpoints()", program, StringComparison.Ordinal);

        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Application/Settings/HoldPolicy/Queries/GetHoldPolicySettingsQuery.cs")));
        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Application/Settings/HoldPolicy/Commands/SaveHoldPolicySettingsCommand.cs")));
        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Contracts/Reservation/StoreHoldPolicySettingsContracts.cs")));
        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/Order/Tooba.Order.Contracts/Reservation/ReservationCyclePolicyPreviewContracts.cs")));
        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/Reservation/StoreHoldPolicySettingsPort.cs")));
        _ = MapRouteRegex;
    }

    [Fact]
    public void Host_Admin_count_17_StoreAppearance_still_deferred()
    {
        var root = FindRepoRoot();
        var admin = Path.Combine(root, "src/backend/Host/Tooba.Host/Admin");
        var adminCount = Directory.GetFiles(admin, "*.cs", SearchOption.AllDirectories).Length;
        Assert.Equal(17, adminCount);
        Assert.True(File.Exists(Path.Combine(admin, "StoreAppearanceSettingsEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(admin, "HoldPolicySettingsEndpoints.cs")));
    }

    [Fact]
    public void SoT_and_evidence_hostAdminAmcHoldPolicy_present()
    {
        var root = FindRepoRoot();
        var sot = File.ReadAllText(Path.Combine(root, "docs/architecture/tmar-current-state.json"));
        Assert.Contains("\"hostAdminAmcHoldPolicy\"", sot, StringComparison.Ordinal);
        Assert.True(Directory.Exists(Path.Combine(
            root, "docs/evidence/TB-TMAR-HOST-ADMIN-AMC-001-W35-HOLD-POLICY")));
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
