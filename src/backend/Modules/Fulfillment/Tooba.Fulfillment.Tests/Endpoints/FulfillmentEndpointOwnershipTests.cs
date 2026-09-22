using Xunit;

namespace Tooba.Fulfillment.Tests.Endpoints;

public sealed class FulfillmentEndpointOwnershipTests
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
    public void Fulfillment_endpoints_own_exact_seller_admin_shipping_and_customer_urls()
    {
        var root = RepoRoot();
        var seller = File.ReadAllText(Path.Combine(
            root, "src", "backend", "Modules", "Fulfillment", "Tooba.Fulfillment.Endpoints", "Seller", "FulfillmentSellerEndpoints.cs"));
        var admin = File.ReadAllText(Path.Combine(
            root, "src", "backend", "Modules", "Fulfillment", "Tooba.Fulfillment.Endpoints", "Admin", "FulfillmentAdminEndpoints.cs"));
        var shipping = File.ReadAllText(Path.Combine(
            root, "src", "backend", "Modules", "Fulfillment", "Tooba.Fulfillment.Endpoints", "Shipping", "ShippingServiceEndpoints.cs"));
        var methods = File.ReadAllText(Path.Combine(
            root, "src", "backend", "Modules", "Fulfillment", "Tooba.Fulfillment.Endpoints", "Shipping", "ShippingMethodsEndpoints.cs"));
        var module = File.ReadAllText(Path.Combine(
            root, "src", "backend", "Modules", "Fulfillment", "Tooba.Fulfillment.Endpoints", "FulfillmentEndpointModule.cs"));
        var program = File.ReadAllText(Path.Combine(
            root, "src", "backend", "Host", "Tooba.Host", "Program.cs"));

        Assert.Contains("MapGroup(\"/v1/seller\")", module, StringComparison.Ordinal);
        Assert.Contains("MapGroup(\"/v1/admin\")", module, StringComparison.Ordinal);
        Assert.Contains("IFulfillmentSellerAuthorizer", seller, StringComparison.Ordinal);
        Assert.Contains("IFulfillmentAdminAuthorizer", admin, StringComparison.Ordinal);
        Assert.Contains("SellerMutateFulfillmentCommand", seller, StringComparison.Ordinal);
        Assert.Contains("ExecuteAdminFulfillmentBulkCommand", admin, StringComparison.Ordinal);
        Assert.Contains("QueryAdminFulfillmentWorkQueueQuery", admin, StringComparison.Ordinal);
        Assert.Contains("CreateShippingServiceCommand", shipping, StringComparison.Ordinal);
        Assert.Contains("/v1/admin/shipping-methods", methods, StringComparison.Ordinal);
        Assert.Contains("MapFulfillmentEndpoints()", program, StringComparison.Ordinal);
        Assert.DoesNotContain("MapShippingServiceEndpoints()", program, StringComparison.Ordinal);
        Assert.False(Directory.Exists(Path.Combine(root, "src", "backend", "Host", "Tooba.Host", "Fulfillment")));
        Assert.False(File.Exists(Path.Combine(root, "src", "backend", "Host", "Tooba.Host", "Admin", "ShippingServiceEndpoints.cs")));
    }
}
