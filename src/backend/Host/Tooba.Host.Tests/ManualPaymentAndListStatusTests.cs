using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Observability.Tracing;
using Tooba.Host.Grid;
using Tooba.Order.Domain;
using Tooba.Payment.Infrastructure.Adapters;
using Tooba.Payment.Infrastructure.DependencyInjection;
using Tooba.Payment.Infrastructure.Directories;
using Tooba.Payment.Infrastructure.Messaging;
using Tooba.Payment.Infrastructure.Providers;
using Tooba.Order.Application.Admin.OrdersGrid;
using Tooba.Returns.Contracts.Operations;
using Tooba.Returns.Domain.Aggregates;
using Tooba.Returns.Domain.ValueObjects;
using Xunit;

namespace Tooba.Host.Tests;

public sealed class ManualPaymentAndListStatusTests
{
    [Fact]
    public async Task Manual_gateway_verify_stays_pending_without_admin_confirm()
    {
        var gateway = new ManualPaymentGateway(new SystemUtcClock());
        var pending = await gateway.VerifyAsync($"manual-{Guid.NewGuid():N}", true, CancellationToken.None);
        Assert.False(pending.VerifiedSuccess);
        Assert.Equal("MANUAL_DEPOSIT_PENDING", pending.FailureCode);
    }

    [Fact]
    public void Manual_provider_code_is_stable()
    {
        Assert.Equal("manual", ManualPaymentGateway.ProviderCodeValue);
        Assert.True(ManualPaymentGateway.IsManual("manual"));
        Assert.False(ManualPaymentGateway.IsManual("fake"));
    }

    [Fact]
    public void Compose_operational_status_prefers_return_refund_states()
    {
        var orderStatuses = new[] { SellerOrderStatus.Paid };
        Assert.Equal(
            "ReturnRequested",
            AdminOrdersGridProjection.ComposeOperationalStatus(orderStatuses, [ReturnRequestOperationStatus.Requested]));
        Assert.Equal(
            "ReturnApproved",
            AdminOrdersGridProjection.ComposeOperationalStatus(orderStatuses, [ReturnRequestOperationStatus.Approved]));
        Assert.Equal(
            "RefundPending",
            AdminOrdersGridProjection.ComposeOperationalStatus(orderStatuses, [ReturnRequestOperationStatus.RefundProcessing]));
        Assert.Equal(
            "RefundCompleted",
            AdminOrdersGridProjection.ComposeOperationalStatus(orderStatuses, [ReturnRequestOperationStatus.Completed]));
        Assert.Equal(
            "Paid",
            AdminOrdersGridProjection.ComposeOperationalStatus(orderStatuses, Array.Empty<ReturnRequestOperationStatus>()));
    }

    [Fact]
    public void Composer_source_filters_overlay_return_statuses()
    {
        var root = FindRepoRoot();
        var engine = File.ReadAllText(Path.Combine(
            root, "src", "backend", "Modules", "Order", "Tooba.Order.Infrastructure",
            "Admin", "OrdersGrid", "AdminOrdersGridReader.cs"));
        Assert.Contains("ApplyStatusFilterAsync", engine, StringComparison.Ordinal);
        Assert.Contains("ReturnRequested", engine, StringComparison.Ordinal);
        Assert.Contains("RefundFailed", engine, StringComparison.Ordinal);
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
