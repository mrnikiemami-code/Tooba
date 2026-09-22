using Xunit;

namespace Tooba.Returns.Tests.Endpoints;

public sealed class ReturnsEndpointOwnershipTests
{
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "docs", "ai", "TOOBA-PIPELINE-PROTOCOL.md"))
                || Directory.Exists(Path.Combine(dir.FullName, ".git")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo.root.not_found");
    }

    [Fact]
    public void Exact_endpoint_urls_and_verbs_are_preserved()
    {
        var root = Path.Combine(RepoRoot(), "src", "backend", "Modules", "Returns", "Tooba.Returns.Endpoints");
        var customer = File.ReadAllText(Path.Combine(root, "Customer", "ReturnCustomerEndpoints.cs"));
        var seller = File.ReadAllText(Path.Combine(root, "Seller", "ReturnSellerEndpoints.cs"));
        var admin = File.ReadAllText(Path.Combine(root, "Admin", "ReturnAdminEndpoints.cs"));
        var module = File.ReadAllText(Path.Combine(root, "ReturnEndpointModule.cs"));
        var program = File.ReadAllText(Path.Combine(
            RepoRoot(), "src", "backend", "Host", "Tooba.Host", "Program.cs"));

        Assert.Contains("MapGet(\"/returns\"", customer, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/returns/{returnRequestId:guid}\"", customer, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/returns\"", customer, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/returns\"", seller, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/returns/{returnRequestId:guid}/approve\"", seller, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/returns/{returnRequestId:guid}/reject\"", seller, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/returns/query\"", admin, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/returns/{returnRequestId:guid}/retry-refund\"", admin, StringComparison.Ordinal);
        Assert.Contains("IReturnCustomerAuthorizer", customer, StringComparison.Ordinal);
        Assert.Contains("IReturnSellerAuthorizer", seller, StringComparison.Ordinal);
        Assert.Contains("IReturnAdminAuthorizer", admin, StringComparison.Ordinal);
        Assert.Contains("MapReturnEndpoints()", program, StringComparison.Ordinal);
        Assert.Contains("MapGroup(\"/v1/customer\")", module, StringComparison.Ordinal);
        Assert.DoesNotContain("ReturnPanelComposer", program, StringComparison.Ordinal);
        Assert.False(Directory.Exists(Path.Combine(RepoRoot(), "src", "backend", "Host", "Tooba.Host", "Returns")));
    }
}
