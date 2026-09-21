using Tooba.BuildingBlocks.Grid;
using Tooba.Host.Admin;
using Tooba.Host.Grid;
using Tooba.Returns.Application.Models;
using Tooba.Returns.Application.Ports;
using Tooba.Returns.Domain.ValueObjects;

namespace Tooba.Host.Returns;

/// <summary>
/// ترکیب HTTP مرجوعی — بدون Host returns DbContext؛ گرید از module query port.
/// </summary>
public sealed class ReturnPanelComposer
{
    private readonly IReturnDirectory _returns;
    private readonly IAdminReturnGridQuery _grid;

    /// <summary>سازندهٔ ترکیب مرجوعی.</summary>
    public ReturnPanelComposer(IReturnDirectory returns, IAdminReturnGridQuery grid)
    {
        _returns = returns;
        _grid = grid;
    }

    /// <summary>درخواست را می‌خواند.</summary>
    public Task<ReturnSnapshot?> GetAsync(Guid returnRequestId, CancellationToken cancellationToken) =>
        _returns.GetAsync(returnRequestId, cancellationToken);

    /// <summary>فهرست درخواست‌های یک مشتری.</summary>
    public Task<IReadOnlyList<ReturnSnapshot>> ListForCustomerAsync(Guid customerUserId, CancellationToken cancellationToken) =>
        _returns.ListForCustomerAsync(customerUserId, cancellationToken);

    /// <summary>درخواست را برای همان فروشنده می‌خواند.</summary>
    public async Task<ReturnSnapshot?> GetForSellerAsync(Guid sellerPartyId, Guid returnRequestId, CancellationToken cancellationToken)
    {
        var snapshot = await _returns.GetAsync(returnRequestId, cancellationToken);
        return snapshot is null || snapshot.SellerPartyId != sellerPartyId ? null : snapshot;
    }

    /// <summary>فهرست درخواست‌های یک فروشنده.</summary>
    public Task<IReadOnlyList<ReturnSnapshot>> ListForSellerAsync(Guid sellerPartyId, CancellationToken cancellationToken) =>
        _returns.ListForSellerAsync(sellerPartyId, cancellationToken);

    /// <summary>فهرست همه درخواست‌ها برای admin.</summary>
    public Task<IReadOnlyList<ReturnSnapshot>> ListAllAsync(CancellationToken cancellationToken) =>
        _returns.ListAllAsync(cancellationToken);

    /// <summary>صفحه‌بندی server-side گرید مرجوعی Admin.</summary>
    public Task<GridPageResponse<AdminReturnWorkQueueRow>> QueryGridAsync(
        GridQueryRequest request,
        CancellationToken cancellationToken)
    {
        var q = AdminListGridPolicies.Returns.Normalize(request);
        return _grid.QueryAsync(q, cancellationToken);
    }

    /// <summary>درخواست مرجوعی می‌سازد.</summary>
    public Task<ReturnSnapshot> CreateAsync(
        Guid actorUserId,
        CreateReturnRequest request,
        RefundDestination destination,
        CancellationToken cancellationToken) =>
        _returns.CreateAsync(
            new CreateReturnCommand(
                request.SellerOrderId,
                actorUserId,
                request.IdempotencyKey,
                request.Reason,
                request.Items.Select(x => new ReturnLineCommand(x.OrderLineId, x.Quantity)).ToArray(),
                destination),
            cancellationToken);

    /// <summary>درخواست را تأیید می‌کند.</summary>
    public Task<ReturnSnapshot> ApproveAsync(
        Guid returnRequestId,
        Guid actorUserId,
        RefundDestination? refundDestination,
        CancellationToken cancellationToken) =>
        _returns.ApproveAsync(
            new ApproveReturnCommand(returnRequestId, actorUserId, refundDestination),
            cancellationToken);

    /// <summary>درخواست را رد می‌کند.</summary>
    public Task<ReturnSnapshot> RejectAsync(
        Guid returnRequestId,
        Guid actorUserId,
        string? reason,
        CancellationToken cancellationToken) =>
        _returns.RejectAsync(new RejectReturnCommand(returnRequestId, actorUserId, reason), cancellationToken);

    /// <summary>refund را دوباره تلاش می‌کند (admin).</summary>
    public Task<ReturnSnapshot> RetryRefundAsync(Guid returnRequestId, Guid actorUserId, CancellationToken cancellationToken) =>
        _returns.RetryRefundAsync(new RetryRefundCommand(returnRequestId, actorUserId), cancellationToken);
}
