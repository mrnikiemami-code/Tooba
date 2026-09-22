using Xunit;

namespace Tooba.Settlement.Tests.Endpoints;

public sealed class SettlementEndpointOwnershipTests
{
    private static string RepoRoot()
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

    [Fact]
    public void Settlement_endpoints_own_exact_seller_and_admin_urls()
    {
        var seller = File.ReadAllText(Path.Combine(
            RepoRoot(), "src", "backend", "Modules", "Settlement", "Tooba.Settlement.Endpoints", "Seller", "SettlementSellerEndpoints.cs"));
        var admin = File.ReadAllText(Path.Combine(
            RepoRoot(), "src", "backend", "Modules", "Settlement", "Tooba.Settlement.Endpoints", "Admin", "SettlementAdminEndpoints.cs"));
        var module = File.ReadAllText(Path.Combine(
            RepoRoot(), "src", "backend", "Modules", "Settlement", "Tooba.Settlement.Endpoints", "SettlementEndpointModule.cs"));
        var program = File.ReadAllText(Path.Combine(
            RepoRoot(), "src", "backend", "Host", "Tooba.Host", "Program.cs"));

        Assert.Contains("MapGroup(\"/v1/seller\")", module, StringComparison.Ordinal);
        Assert.Contains("MapGroup(\"/v1/admin\")", module, StringComparison.Ordinal);
        Assert.Contains("ISettlementSellerAuthorizer", seller, StringComparison.Ordinal);
        Assert.Contains("ISettlementAdminAuthorizer", admin, StringComparison.Ordinal);
        Assert.Contains("RequestSellerPayoutCommand", seller, StringComparison.Ordinal);
        Assert.Contains("ProcessAdminPayoutCommand", admin, StringComparison.Ordinal);
        Assert.Contains("RetryAdminPayoutCommand", admin, StringComparison.Ordinal);
        Assert.Contains("QueryAdminPayoutGridQuery", admin, StringComparison.Ordinal);
        Assert.Contains("MapSettlementEndpoints()", program, StringComparison.Ordinal);
        Assert.False(Directory.Exists(Path.Combine(
            RepoRoot(), "src", "backend", "Host", "Tooba.Host", "Settlement")));
    }
}
