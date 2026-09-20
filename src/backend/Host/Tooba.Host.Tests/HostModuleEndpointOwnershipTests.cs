using Xunit;
namespace Tooba.Host.Tests;

/// <summary>
/// HOST-MODULE-ENDPOINT-001: module-owned Offer routes must not be remapped in Host seller panel.
/// </summary>
public sealed class HostModuleEndpointOwnershipTests
{
    [Fact]
    public void SellerPanelEndpoints_do_not_map_offer_http_routes()
    {
        var path = Path.Combine(
            FindRepoRoot(),
            "src",
            "backend",
            "Host",
            "Tooba.Host",
            "Seller",
            "SellerPanelEndpoints.cs");
        var text = File.ReadAllText(path);
        Assert.DoesNotContain("MapGet(\"/offers\"", text, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPost(\"/offers\"", text, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPatch(\"/offers/", text, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPost(\"/offers/{offerId:guid}/price\"", text, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPost(\"/offers/{offerId:guid}/inventory\"", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Program_maps_offer_module()
    {
        var path = Path.Combine(FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Program.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("MapOfferModule()", text, StringComparison.Ordinal);
        Assert.Contains("MapTaxModule()", text, StringComparison.Ordinal);
        Assert.Contains("MapPricingModule()", text, StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, ".git"))
                || File.Exists(Path.Combine(dir.FullName, "docs", "ai", "TOOBA-PIPELINE-PROTOCOL.md")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
