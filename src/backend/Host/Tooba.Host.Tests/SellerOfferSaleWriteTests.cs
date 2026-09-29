using Xunit;

namespace Tooba.Host.Tests;

public sealed class SellerOfferSaleWriteTests
{
    [Fact]
    public void Host_seller_panel_has_no_offer_read_or_write_surface()
    {
        // R4 removed the zero-consumer SellerPanelComposer.cs from Host/Seller. The durable invariant is
        // that no Host/Seller production file exposes an offer read/write surface; the Offer seller HTTP
        // surface lives in Tooba.Offer.Endpoints (OfferSellerEndpoints).
        var hostSeller = Path.Combine(FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Seller");

        Assert.False(File.Exists(Path.Combine(hostSeller, "SellerPanelComposer.cs")));

        foreach (var file in Directory.EnumerateFiles(hostSeller, "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("GetOfferAsync", text, StringComparison.Ordinal);
            Assert.DoesNotContain("ListOffersAsync", text, StringComparison.Ordinal);
            Assert.DoesNotContain("SetOfferPriceAsync", text, StringComparison.Ordinal);
            Assert.DoesNotContain("SetOfferInventoryAsync", text, StringComparison.Ordinal);
        }
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
