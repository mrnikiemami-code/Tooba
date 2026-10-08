using Tooba.Order.Application.Admin.Operations.Policies;
using Tooba.BuildingBlocks;
using Tooba.Returns.Application.Composition;
using Tooba.Returns.Application.ReturnRequests.Models;
using Tooba.Returns.Application.ReturnRequests.Ports;
using Tooba.Returns.Contracts.Errors;
using Tooba.Returns.Domain.Aggregates;
using Tooba.Returns.Domain.ValueObjects;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>TB-P09-T019 — صف کار مرجوعی/بازگشت وجه Admin.</summary>
public sealed class AdminReturnWorkQueueTests
{
    [Theory]
    [InlineData(ReturnRequestStatus.Requested, AdminReturnQueueFilters.PendingReview, true)]
    [InlineData(ReturnRequestStatus.Approved, AdminReturnQueueFilters.Approved, true)]
    [InlineData(ReturnRequestStatus.RefundProcessing, AdminReturnQueueFilters.Approved, true)]
    [InlineData(ReturnRequestStatus.Completed, AdminReturnQueueFilters.Approved, false)]
    [InlineData(ReturnRequestStatus.Approved, AdminReturnQueueFilters.ReceivedAwaitingRefund, true)]
    [InlineData(ReturnRequestStatus.RefundProcessing, AdminReturnQueueFilters.RefundPending, true)]
    [InlineData(ReturnRequestStatus.RefundFailed, AdminReturnQueueFilters.RefundFailed, true)]
    [InlineData(ReturnRequestStatus.Completed, AdminReturnQueueFilters.Completed, true)]
    [InlineData(ReturnRequestStatus.Rejected, AdminReturnQueueFilters.Rejected, true)]
    [InlineData(ReturnRequestStatus.Requested, AdminReturnQueueFilters.Completed, false)]
    public void Queue_filters_map_to_real_return_statuses(
        ReturnRequestStatus status,
        string filter,
        bool expected)
    {
        Assert.Equal(expected, AdminReturnQueueFilters.Matches(status, filter));
    }

    [Fact]
    public void Compose_keeps_return_and_refund_statuses_separate()
    {
        Assert.Equal("Requested", AdminReturnQueueFilters.ComposeReturnStatus(ReturnRequestStatus.Requested));
        Assert.Equal("Approved", AdminReturnQueueFilters.ComposeReturnStatus(ReturnRequestStatus.RefundProcessing));
        Assert.Equal("Approved", AdminReturnQueueFilters.ComposeReturnStatus(ReturnRequestStatus.RefundFailed));
        Assert.Equal("none", AdminReturnQueueFilters.ComposeRefundStatus(ReturnRequestStatus.Requested, []));
        Assert.Equal("pending", AdminReturnQueueFilters.ComposeRefundStatus(ReturnRequestStatus.Approved, []));
        Assert.Equal("failed", AdminReturnQueueFilters.ComposeRefundStatus(ReturnRequestStatus.RefundFailed, [RefundAttemptStatus.Failed]));
        Assert.Equal("completed", AdminReturnQueueFilters.ComposeRefundStatus(ReturnRequestStatus.Completed, [RefundAttemptStatus.Succeeded]));
    }

    [Fact]
    public void Project_action_codes_match_existing_ops()
    {
        Assert.Equal(["approve_return", "reject_return"], AdminReturnQueueFilters.ProjectActionCodes(ReturnRequestStatus.Requested));
        Assert.Equal(["retry_refund"], AdminReturnQueueFilters.ProjectActionCodes(ReturnRequestStatus.RefundFailed));
        Assert.Empty(AdminReturnQueueFilters.ProjectActionCodes(ReturnRequestStatus.Completed));
        Assert.Empty(AdminReturnQueueFilters.ProjectActionCodes(ReturnRequestStatus.Rejected));
    }

    [Fact]
    public void Eligibility_summary_uses_snapshot_not_current_offer()
    {
        var now = DateTimeOffset.Parse("2026-09-09T12:00:00Z");
        Assert.Equal(
            "۷ روز پس از تحویل",
            AdminReturnQueueFilters.ComposeEligibilitySummary(true, 7, "۷ روز پس از تحویل", null, now));
        Assert.Equal(
            "غیرقابل مرجوعی",
            AdminReturnQueueFilters.ComposeEligibilitySummary(false, 0, "غیرقابل مرجوعی", now, now));
        Assert.Equal(
            "منقضی",
            AdminReturnQueueFilters.ComposeEligibilitySummary(true, 7, "۷ روز پس از تحویل", now.AddDays(-10), now));
        var remaining = AdminReturnQueueFilters.ComposeEligibilitySummary(
            true, 7, "۷ روز پس از تحویل", now.AddDays(-1), now);
        Assert.Contains("باقی‌مانده", remaining);
        Assert.DoesNotContain("01a0", remaining);
    }

    [Fact]
    public void Work_queue_engine_is_guid_free_and_pages_natively()
    {
        var root = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Modules", "Returns",
            "Tooba.Returns.Infrastructure", "Queries"));
        var engine = File.ReadAllText(Path.Combine(root, "AdminReturnGridQueryEngine.cs"));
        Assert.Contains("EfGridQuery.PageAsync", engine, StringComparison.Ordinal);
        Assert.Contains("case \"orderReference\"", engine, StringComparison.Ordinal);
        Assert.Contains("IOrderGridEnrichmentReader", engine, StringComparison.Ordinal);
        Assert.Contains("ComposeEligibilitySummary", engine, StringComparison.Ordinal);
        Assert.Contains("ReturnRequestStatus.Requested", engine, StringComparison.Ordinal);
        Assert.Contains("ReturnsDbContext", engine, StringComparison.Ordinal);
        Assert.DoesNotContain("OrderDbContext", engine, StringComparison.Ordinal);

        var hostRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Tooba.Host"));
        Assert.False(Directory.Exists(Path.Combine(hostRoot, "Returns")));
        Assert.False(File.Exists(Path.Combine(hostRoot, "Returns", "ReturnPanelComposer.cs")));
    }

    [Fact]
    public void Endpoints_map_stale_and_expired_without_english_bad_request()
    {
        var endpoints = File.ReadAllText(Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Modules", "Returns",
            "Tooba.Returns.Endpoints", "Seller", "ReturnSellerEndpoints.cs")));
        Assert.DoesNotContain("title = \"Bad Request\"", endpoints, StringComparison.Ordinal);
        Assert.Contains("ApiResponseFactory", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("ReturnErrorMapper", endpoints, StringComparison.Ordinal);

        var reasonCatalog = File.ReadAllText(Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Modules", "Returns",
            "Tooba.Returns.Contracts", "Operations", "ReturnAdminOperationsContracts.cs")));
        Assert.Contains("ToErrorCode", reasonCatalog, StringComparison.Ordinal);
        Assert.Contains("return.expired", reasonCatalog, StringComparison.Ordinal);

        var expired = ReturnsOperation.ToSemanticError(new ContractOperationException(ReturnsErrorCodes.Expired));
        Assert.Equal("return.expired", expired.Code);

        var stale = ReturnsOperation.ToSemanticError(new ContractOperationException(ReturnsErrorCodes.Stale));
        Assert.Equal("return.stale", stale.Code);

        var qty = ReturnsOperation.ToSemanticError(new ContractOperationException(ReturnsErrorCodes.QuantityExceeded));
        Assert.Equal("return.quantity_exceeded", qty.Code);
    }
}
