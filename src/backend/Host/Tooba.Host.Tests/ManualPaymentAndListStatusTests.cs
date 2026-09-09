using Tooba.Host.Grid;
using Tooba.Order.Domain;
using Tooba.Payment.Infrastructure;
using Tooba.Returns.Domain;
using Xunit;

namespace Tooba.Host.Tests;

public sealed class ManualPaymentAndListStatusTests
{
    [Fact]
    public async Task Manual_gateway_verify_stays_pending_without_admin_confirm()
    {
        var gateway = new ManualPaymentGateway();
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
            AdminOrdersGridQueryEngine.ComposeOperationalStatus(orderStatuses, [ReturnRequestStatus.Requested]));
        Assert.Equal(
            "ReturnApproved",
            AdminOrdersGridQueryEngine.ComposeOperationalStatus(orderStatuses, [ReturnRequestStatus.Approved]));
        Assert.Equal(
            "RefundPending",
            AdminOrdersGridQueryEngine.ComposeOperationalStatus(orderStatuses, [ReturnRequestStatus.RefundProcessing]));
        Assert.Equal(
            "RefundCompleted",
            AdminOrdersGridQueryEngine.ComposeOperationalStatus(orderStatuses, [ReturnRequestStatus.Completed]));
        Assert.Equal(
            "Paid",
            AdminOrdersGridQueryEngine.ComposeOperationalStatus(orderStatuses, Array.Empty<ReturnRequestStatus>()));
    }

    [Fact]
    public void Composer_source_filters_overlay_return_statuses()
    {
        var root = FindRepoRoot();
        var engine = File.ReadAllText(Path.Combine(
            root, "src", "backend", "Host", "Tooba.Host", "Grid", "AdminOrdersGridQueryEngine.cs"));
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
