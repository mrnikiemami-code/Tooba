using Tooba.Order.Application.Admin.Operations.Policies;
using System.IO;
using Xunit;

namespace Tooba.Host.Tests;

public sealed class OrderSupplyUxTests
{
    private static string Host(string relative) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Tooba.Host", relative));

    [Fact]
    public void List_items_carry_supply_status()
    {
        var models = Host(Path.Combine("Admin", "AdminPanelModels.cs"));
        Assert.Contains("SupplyStatus", models, StringComparison.Ordinal);
        var orders = Host(Path.Combine(
            "..", "..", "Modules", "Order", "Tooba.Order.Infrastructure",
            "Admin", "OrdersGrid", "AdminOrdersGridReader.cs"));
        Assert.Contains("GetStatusesAsync", orders, StringComparison.Ordinal);
        Assert.DoesNotContain("GetStatusAsync(r.CheckoutId", orders, StringComparison.Ordinal);
        var payments = Host(Path.Combine("..", "..", "Modules", "Payment", "Tooba.Payment.Application", "Queries", "QueryAdminPaymentsGrid", "QueryAdminPaymentsGridQuery.cs"));
        Assert.Contains("IPaymentAdminOrderEnrichmentReader", payments, StringComparison.Ordinal);
        Assert.Contains("GetProjectionsAsync", orders, StringComparison.Ordinal);
        Assert.Contains("EnrichAsync", payments, StringComparison.Ordinal);
        Assert.DoesNotContain("GetProjectionAsync(", orders, StringComparison.Ordinal);
        Assert.DoesNotContain("GetProjectionAsync(", payments, StringComparison.Ordinal);
    }

    [Fact]
    public void Confirm_messages_and_recovery_capability()
    {
        var ops = File.ReadAllText(Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..",
            "Modules", "Order", "Tooba.Order.Application", "Admin", "Operations", "Services",
            "AdminOrderOperationsOrchestrator.cs")));
        Assert.Contains("موجودی قابل تأمین است و هنگام تأیید واریز به‌صورت خودکار رزرو می‌شود.", ops, StringComparison.Ordinal);
        Assert.Contains("این سفارش در حال حاضر قابل تأمین نیست.", ops, StringComparison.Ordinal);
        Assert.Contains("canRecover", ops, StringComparison.Ordinal);
        Assert.Contains("AvailableForReacquire", ops, StringComparison.Ordinal);
    }

    [Fact]
    public void Batch_status_loads_fulfillment_items_once()
    {
        var supply = Host(Path.Combine("Admin", "OrderSupplyComposer.cs"));
        Assert.Contains("GetStatusesAsync", supply, StringComparison.Ordinal);
        Assert.Contains("LoadShippedByLineAsync", supply, StringComparison.Ordinal);
    }
}
