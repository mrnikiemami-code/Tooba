using Xunit;

namespace Tooba.Host.Tests;

public sealed class SellerOfferSaleWriteTests
{
    [Fact]
    public void Host_seller_panel_has_no_offer_read_or_write_surface()
    {
        // R4 removed the zero-consumer SellerPanelComposer.cs from Host/Seller and R5 removed the whole
        // Host/Seller folder. The durable invariant is that no Host production file exposes an offer
        // read/write surface; the Offer seller HTTP surface lives in Tooba.Offer.Endpoints.
        var hostSeller = Path.Combine(FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Seller");
        Assert.False(Directory.Exists(hostSeller), "Host/Seller must be absent after R5");

        var offerEndpoints = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "backend",
            "Modules",
            "Offer",
            "Tooba.Offer.Endpoints",
            "Seller",
            "OfferSellerEndpoints.cs"));
        Assert.Contains("SetOfferPriceCommand", offerEndpoints, StringComparison.Ordinal);
        Assert.Contains("SetOfferInventoryCommand", offerEndpoints, StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "AGENTS.md")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
