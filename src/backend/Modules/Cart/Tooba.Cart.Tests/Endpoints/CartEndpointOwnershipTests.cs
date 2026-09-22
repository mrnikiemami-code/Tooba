using Xunit;

namespace Tooba.Cart.Tests.Endpoints;

public sealed class CartEndpointOwnershipTests
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
    public void Cart_endpoints_own_exact_storefront_urls()
    {
        var endpoint = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "src", "backend", "Modules", "Cart", "Tooba.Cart.Endpoints", "Storefront", "CartStorefrontEndpoints.cs"));
        var module = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "src", "backend", "Modules", "Cart", "Tooba.Cart.Endpoints", "CartEndpointModule.cs"));
        var host = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "src", "backend", "Host", "Tooba.Host", "Storefront", "StorefrontEndpoints.cs"));
        var program = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "src", "backend", "Host", "Tooba.Host", "Program.cs"));

        Assert.Contains("MapGroup(\"/v1/storefront\")", module, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/cart\"", endpoint, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/cart/current\"", endpoint, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/cart/{cartId:guid}\"", endpoint, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/cart/merge\"", endpoint, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/cart/{cartId:guid}/lines\"", endpoint, StringComparison.Ordinal);
        Assert.Contains("MapPatch(\"/cart/{cartId:guid}/lines/{lineId:guid}\"", endpoint, StringComparison.Ordinal);
        Assert.Contains("MapDelete(\"/cart/{cartId:guid}/lines/{lineId:guid}\"", endpoint, StringComparison.Ordinal);
        Assert.Contains("CreateGuestCartCommand", endpoint, StringComparison.Ordinal);
        Assert.Contains("GetCurrentAuthenticatedCartQuery", endpoint, StringComparison.Ordinal);
        Assert.Contains("GetCartQuery", endpoint, StringComparison.Ordinal);
        Assert.Contains("MergeCartAfterLoginCommand", endpoint, StringComparison.Ordinal);
        Assert.Contains("AddCartLineCommand", endpoint, StringComparison.Ordinal);
        Assert.Contains("ChangeCartLineQuantityCommand", endpoint, StringComparison.Ordinal);
        Assert.Contains("RemoveCartLineCommand", endpoint, StringComparison.Ordinal);
        Assert.Contains("X-Tooba-Cart-Version", endpoint, StringComparison.Ordinal);
        Assert.Contains("X-Tooba-Guest-Secret", endpoint, StringComparison.Ordinal);

        Assert.DoesNotContain("MapPost(\"/cart\"", host, StringComparison.Ordinal);
        Assert.Contains("MapCartEndpoints()", program, StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(
            RepoRoot(), "src", "backend", "Host", "Tooba.Host", "Storefront", "StorefrontCartComposer.cs")));
    }
}
