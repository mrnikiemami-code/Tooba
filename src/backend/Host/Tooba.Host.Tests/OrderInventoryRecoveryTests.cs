using System.IO;
using Xunit;

namespace Tooba.Host.Tests;

public sealed class OrderInventoryRecoveryTests
{
    private static string Read(string relative)
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Tooba.Host"));
        return File.ReadAllText(Path.Combine(root, relative));
    }

    [Fact]
    public void Recovery_composer_never_resurrects_and_rolls_back_partial_acquire()
    {
        var src = Read(Path.Combine("Admin", "OrderInventoryRecoveryComposer.cs"));
        Assert.Contains("RecoverAsync", src, StringComparison.Ordinal);
        Assert.Contains("ReleaseAsync", src, StringComparison.Ordinal);
        Assert.Contains("ReplaceReservation", src, StringComparison.Ordinal);
        Assert.Contains("RebindActiveReservationsFromOrderAsync", src, StringComparison.Ordinal);
        Assert.Contains("inventory.recovery.insufficient", src, StringComparison.Ordinal);
        Assert.Contains("AlreadyHealthy", src, StringComparison.Ordinal);
        Assert.Contains("RequiresManualReview", src, StringComparison.Ordinal);
        Assert.Contains("NotEligible", src, StringComparison.Ordinal);
        Assert.DoesNotMatch(new System.Text.RegularExpressions.Regex(@"Status\s*=\s*StockReservationStatus\.Held"), src);
    }

    [Fact]
    public void Admin_projects_recover_action_and_warning()
    {
        var ops = Read(Path.Combine("Admin", "AdminOrderOperationsComposer.cs"));
        Assert.Contains("recover_inventory_reservation", ops, StringComparison.Ordinal);
        Assert.Contains("ProjectInventoryRecovery", ops, StringComparison.Ordinal);
        Assert.Contains("رزرو موجودی این سفارش از چرخه قبلی معتبر نیست", ops, StringComparison.Ordinal);
        var models = Read(Path.Combine("Admin", "AdminOrderOperationsModels.cs"));
        Assert.Contains("InventoryRecoveryWarningFa", models, StringComparison.Ordinal);
    }

    [Fact]
    public void Fulfillment_exposes_rebind_without_reactivate()
    {
        var contracts = File.ReadAllText(Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Modules", "Fulfillment",
            "Tooba.Fulfillment.Application", "FulfillmentContracts.cs")));
        Assert.Contains("RebindActiveReservationsFromOrderAsync", contracts, StringComparison.Ordinal);
        var dir = File.ReadAllText(Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Modules", "Fulfillment",
            "Tooba.Fulfillment.Infrastructure", "FulfillmentDirectory.cs")));
        var idx = dir.IndexOf("RebindActiveReservationsFromOrderAsync", StringComparison.Ordinal);
        Assert.True(idx >= 0);
        var slice = dir.Substring(idx, Math.Min(800, dir.Length - idx));
        Assert.Contains("RebindActiveReservations", slice, StringComparison.Ordinal);
        Assert.DoesNotContain("ReactivateAfterOrderRestore(now)", slice, StringComparison.Ordinal);
    }

    [Fact]
    public void History_maps_inventory_recovery_note_kinds()
    {
        var src = Read(Path.Combine("Admin", "AdminOrderCompletenessComposer.cs"));
        Assert.Contains("inventory_recovery_requested", src, StringComparison.Ordinal);
        Assert.Contains("inventory_recovery_succeeded", src, StringComparison.Ordinal);
        Assert.Contains("inventory_recovery_failed_insufficient", src, StringComparison.Ordinal);
        Assert.Contains("inventory_recovery_manual_review", src, StringComparison.Ordinal);
    }
}
