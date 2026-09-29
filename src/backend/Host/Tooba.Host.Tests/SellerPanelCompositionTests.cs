using System.Text.Json;
using Tooba.Host.Seller;
using Tooba.Order.Application.Seller.Models;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// قفل قرارداد و ایزولهٔ پنل فروشنده: فیلتر در سرور است و Product.Price وجود ندارد.
/// </summary>
public sealed class SellerPanelCompositionTests
{
    [Fact]
    public void Order_seller_dashboard_view_keeps_dashboard_field_names_and_active_offers_zero()
    {
        // The dashboard response shape is preserved by moving the SellerDashboardSummary into the
        // Order-owned SellerDashboardView (display name enriched from Party.Contracts, ActiveOffers = 0).
        var names = typeof(SellerDashboardView).GetProperties().Select(p => p.Name).ToArray();
        Assert.Equal(
            ["SellerPartyId", "SellerDisplayName", "ActiveOffers", "OpenOrders", "PaidOrders"],
            names);

        var json = JsonSerializer.Serialize(
            new SellerDashboardView(Guid.Empty, "فروشگاه آرمان", 0, 3, 7),
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        Assert.Contains("\"sellerPartyId\"", json, StringComparison.Ordinal);
        Assert.Contains("\"sellerDisplayName\"", json, StringComparison.Ordinal);
        Assert.Contains("\"activeOffers\":0", json, StringComparison.Ordinal);
        Assert.Contains("\"openOrders\":3", json, StringComparison.Ordinal);
        Assert.Contains("\"paidOrders\":7", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Order_list_and_detail_keep_seller_slice_identity()
    {
        var list = typeof(SellerOrderListItem).GetProperties().Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
        Assert.Contains("SellerOrderId", list);
        Assert.Contains("PayableAmount", list);
        Assert.DoesNotContain("BuyerUserId", list);

        var detail = typeof(SellerOrderDetailPage).GetProperties().Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
        Assert.Contains("SellerPartyId", detail);
        Assert.Contains("Lines", detail);
    }

    [Fact]
    public void Endpoints_require_seller_party_header_and_own_no_catalog_order_settings_or_dashboard_routes()
    {
        Assert.Equal("X-Tooba-Seller-Party-Id", SellerPanelEndpoints.SellerPartyHeader);
        Assert.Equal("X-Tooba-Dev-Actor-User-Id", SellerPanelEndpoints.DevActorHeader);

        // R4 deleted the zero-consumer composer/models residue from Host/Seller.
        Assert.False(File.Exists(Path.Combine(
            FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Seller", "SellerPanelComposer.cs")));
        Assert.False(File.Exists(Path.Combine(
            FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Seller", "SellerPanelModels.cs")));

        var endpoints = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "backend",
            "Host",
            "Tooba.Host",
            "Seller",
            "SellerPanelEndpoints.cs"));
        Assert.Contains("group.MapGet(\"/dev-contexts\"", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("MapGet(\"/orders\"", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("group.MapGet(\"/dashboard\"", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("SellerPanelComposer", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("SellerDashboardSummary", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("GetSellerOrderDashboardSummaryQuery", endpoints, StringComparison.Ordinal);
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
