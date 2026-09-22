using Tooba.Order.Application.Admin.Operations.Policies;
using Tooba.Promotion.Application.Ports;
using Tooba.Promotion.Infrastructure.Queries;
using Tooba.Promotion.Infrastructure.Messaging;
using Tooba.Promotion.Infrastructure.Adapters;
using Tooba.Promotion.Infrastructure.Directories;
using Tooba.Inventory.Infrastructure.Messaging;
using Tooba.Inventory.Infrastructure.Adapters;
using Tooba.Inventory.Infrastructure.Directories;
﻿using System.IO;
using Xunit;

namespace Tooba.Host.Tests;

public sealed class OrderSupplyFoundationTests
{
    private static string Host(string relative)
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Tooba.Host"));
        return File.ReadAllText(Path.Combine(root, relative));
    }

    private static string InvInfra()
    {
        return File.ReadAllText(Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Modules", "Inventory",
            "Tooba.Inventory.Infrastructure", "Directories", "InventoryDirectory.cs")));
    }

    private static string Contracts()
    {
        return File.ReadAllText(Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Modules", "Inventory",
            "Tooba.Inventory.Application", "Orders", "OrderSupplyContracts.cs")));
    }

    [Fact]
    public void Canonical_capability_and_modes_exist()
    {
        var contracts = Contracts();
        Assert.Contains("EnsureOrderSupplyRequest", contracts, StringComparison.Ordinal);
        Assert.Contains("CheckOnly", contracts, StringComparison.Ordinal);
        Assert.Contains("EnsureReviewHold", contracts, StringComparison.Ordinal);
        Assert.Contains("EnsurePaidDurable", contracts, StringComparison.Ordinal);
        Assert.Contains("EnsureFulfillmentSupply", contracts, StringComparison.Ordinal);
        Assert.Contains("AvailableForReacquire", contracts, StringComparison.Ordinal);
        var dir = InvInfra();
        Assert.Contains("EnsureOrderSupplyAsync", dir, StringComparison.Ordinal);
        Assert.Contains("GetOrderSupplyStatusAsync", dir, StringComparison.Ordinal);
        Assert.DoesNotContain("Status = StockReservationStatus.Held", dir, StringComparison.Ordinal);
    }

    [Fact]
    public void CheckOnly_does_not_call_Reserve()
    {
        var dir = InvInfra();
        var idx = dir.IndexOf("if (request.Mode == OrderSupplyMode.CheckOnly)", StringComparison.Ordinal);
        Assert.True(idx >= 0);
        var slice = dir.Substring(idx, 500);
        Assert.DoesNotContain("ReserveAsync", slice, StringComparison.Ordinal);
    }

    private static string OrderOps() =>
        File.ReadAllText(Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..",
            "Modules", "Order", "Tooba.Order.Application", "Admin", "Operations", "Services",
            "AdminOrderOperationsOrchestrator.cs")));

    [Fact]
    public void Confirm_uses_EnsurePaidDurable_and_blocks_unavailable()
    {
        var ops = OrderOps();
        Assert.Contains("EnsurePaidDurableAsync", ops, StringComparison.Ordinal);
        Assert.Contains("inventory.supply.unavailable", ops, StringComparison.Ordinal);
        Assert.Contains("این سفارش در حال حاضر قابل تأمین نیست.", ops, StringComparison.Ordinal);
    }

    [Fact]
    public void Settings_have_hold_policy_keys()
    {
        var app = Host("appsettings.json");
        Assert.Contains("ManualPaymentReviewHoldHours", app, StringComparison.Ordinal);
        Assert.Contains("ManualPaymentInitialHoldHours", app, StringComparison.Ordinal);
        Assert.Contains("OnlinePaymentHoldHours", app, StringComparison.Ordinal);
        Assert.Contains("CartHoldMinutes", app, StringComparison.Ordinal);
        var opts = File.ReadAllText(Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Modules", "Payment",
            "Tooba.Payment.Infrastructure", "Providers", "PaymentGatewayOptions.cs")));
        Assert.Contains("OrderSupplyHoldOverrides", opts, StringComparison.Ordinal);
    }

    [Fact]
    public void Ensure_rolls_back_acquired_reservations()
    {
        var dir = InvInfra();
        Assert.Contains("ReleaseAsync", dir, StringComparison.Ordinal);
        Assert.Contains("order-supply-", dir, StringComparison.Ordinal);
        Assert.Contains("os-{(int)request.Mode}", dir, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "order-supply-{request.Mode}-{input.OrderLineId:N}-{request.CorrelationId",
            dir,
            StringComparison.Ordinal);
    }
}
