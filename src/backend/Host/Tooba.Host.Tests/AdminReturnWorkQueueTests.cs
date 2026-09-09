using Tooba.Host.Admin;
using Tooba.Host.Returns;
using Tooba.Returns.Application;
using Tooba.Returns.Domain;
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
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Tooba.Host"));
        var engine = File.ReadAllText(Path.Combine(root, "Grid", "AdminReturnGridQueryEngine.cs"));
        Assert.Contains("AdminEfGridQuery.PageAsync", engine, StringComparison.Ordinal);
        Assert.Contains("case \"orderReference\"", engine, StringComparison.Ordinal);
        Assert.Contains("x => x.OrderNumber", engine, StringComparison.Ordinal);
        Assert.Contains("ComposeEligibilitySummary", engine, StringComparison.Ordinal);
        Assert.Contains("ReturnRequestStatus.Requested", engine, StringComparison.Ordinal);
        Assert.DoesNotContain("AdminReturnQueueFilters.Matches(x.Status", engine, StringComparison.Ordinal);
        Assert.DoesNotContain("ReturnRequestId.ToString(\"N\")", engine, StringComparison.Ordinal);
        Assert.DoesNotContain("CheckoutId.ToString(\"N\")", engine, StringComparison.Ordinal);
    }

    [Fact]
    public void Endpoints_map_stale_and_expired_without_english_bad_request()
    {
        var endpoints = File.ReadAllText(Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "Tooba.Host", "Returns", "ReturnEndpoints.cs")));
        Assert.DoesNotContain("title = \"Bad Request\"", endpoints, StringComparison.Ordinal);
        Assert.Contains("ReturnErrorMapper.Map", endpoints, StringComparison.Ordinal);

        var composer = File.ReadAllText(Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "Tooba.Host", "Admin", "AdminOrderOperationsComposer.cs")));
        Assert.Contains("isReturnLifecycleOp", composer, StringComparison.Ordinal);
        Assert.Contains("ToErrorCode(eligibility.ReasonCode)", composer, StringComparison.Ordinal);

        var expired = ReturnErrorMapper.Map(ReturnEligibilityReasonCodes.ToFaMessage(ReturnEligibilityReasonCodes.WindowExpired));
        Assert.Equal("return.expired", expired.Code);
        Assert.Equal("مهلت مرجوعی تمام شده است.", expired.Fa);

        var stale = ReturnErrorMapper.Map("انتقال وضعیت از این حالت مجاز نیست.");
        Assert.Equal("return.stale", stale.Code);
        Assert.DoesNotContain("Bad Request", stale.Fa, StringComparison.OrdinalIgnoreCase);

        var qty = ReturnErrorMapper.Map("تعداد مرجوعی از باقیماندهٔ تحویل‌شده بیشتر است.");
        Assert.Equal("return.quantity_exceeded", qty.Code);
    }
}
