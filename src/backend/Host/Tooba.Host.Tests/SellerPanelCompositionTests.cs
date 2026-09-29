using System.Text.Json;
using Tooba.Order.Application.Seller.Models;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// قفل قرارداد و ایزولهٔ پنل فروشنده: فیلتر در سرور است و Product.Price وجود ندارد.
/// R5: پنل Host/Seller کاملاً حذف شده و مسیر dev-contexts در مالکیت AccessControl است؛ هدرهای
/// قراردادی از درز امنیتی Host/Security/Seller می‌آیند.
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
    public void Endpoints_require_seller_party_header_and_host_owns_no_seller_panel_folder()
    {
        // R5 evacuated the final Host/Seller surface; the shared header contract now lives on the
        // canonical Host security adapter boundary.
        Assert.Equal("X-Tooba-Seller-Party-Id", Tooba.Host.Security.Seller.SellerPanelAccess.SellerPartyHeader);
        Assert.Equal("X-Tooba-Dev-Actor-User-Id", Tooba.Host.Security.Seller.SellerPanelAccess.DevActorHeader);

        // R4 deleted the zero-consumer composer/models residue from Host/Seller; R5 removed the folder.
        var hostSeller = Path.Combine(
            FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Seller");
        Assert.False(Directory.Exists(hostSeller), "Host/Seller must be absent after R5");
        Assert.False(File.Exists(Path.Combine(hostSeller, "SellerPanelComposer.cs")));
        Assert.False(File.Exists(Path.Combine(hostSeller, "SellerPanelModels.cs")));

        var endpoints = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "backend",
            "Modules",
            "AccessControl",
            "Tooba.AccessControl.Endpoints",
            "Seller",
            "Development",
            "SellerDevContextEndpoints.cs"));
        Assert.Contains("MapGet(\"/dev-contexts\"", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("MapGet(\"/orders\"", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("MapGet(\"/dashboard\"", endpoints, StringComparison.Ordinal);
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
