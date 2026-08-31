using Tooba.BuildingBlocks.Grid;
using Tooba.Host.Grid;
using Tooba.Settlement.Application;

namespace Tooba.Host.Settlement;

/// <summary>
/// درخواست payout از HTTP.
/// </summary>
public sealed record RequestPayoutBody(decimal Amount, string IdempotencyKey);

/// <summary>
/// ترکیب HTTP تسویه برای seller/admin.
/// </summary>
public sealed class SettlementPanelComposer
{
    private readonly ISettlementDirectory _settlement;

    /// <summary>سازندهٔ ترکیب تسویه.</summary>
    public SettlementPanelComposer(ISettlementDirectory settlement) => _settlement = settlement;

    /// <summary>مانده فروشنده را برمی‌گرداند.</summary>
    public Task<SettlementBalanceSnapshot?> GetBalanceAsync(Guid sellerPartyId, CancellationToken cancellationToken) =>
        _settlement.GetBalanceAsync(sellerPartyId, cancellationToken);

    /// <summary>سطرهای posted فروشنده.</summary>
    public Task<IReadOnlyList<SettlementEntrySnapshot>> ListEntriesAsync(Guid sellerPartyId, CancellationToken cancellationToken) =>
        _settlement.ListEntriesAsync(sellerPartyId, cancellationToken);

    /// <summary>صورت‌حساب‌های فروشنده.</summary>
    public Task<IReadOnlyList<SettlementStatementSnapshot>> ListStatementsAsync(Guid sellerPartyId, CancellationToken cancellationToken) =>
        _settlement.ListStatementsAsync(sellerPartyId, cancellationToken);

    /// <summary>درخواست payout می‌سازد.</summary>
    public Task<PayoutRequestSnapshot> RequestPayoutAsync(
        Guid sellerPartyId,
        Guid actorUserId,
        RequestPayoutBody body,
        CancellationToken cancellationToken) =>
        _settlement.RequestPayoutAsync(
            new RequestPayoutCommand(sellerPartyId, body.Amount, body.IdempotencyKey, actorUserId),
            cancellationToken);

    /// <summary>فهرست payoutهای فروشنده.</summary>
    public Task<IReadOnlyList<PayoutRequestSnapshot>> ListPayoutRequestsAsync(
        Guid sellerPartyId,
        CancellationToken cancellationToken) =>
        _settlement.ListPayoutRequestsForSellerAsync(sellerPartyId, cancellationToken);

    /// <summary>درخواست payout را می‌خواند.</summary>
    public async Task<PayoutRequestSnapshot?> GetPayoutForSellerAsync(
        Guid sellerPartyId,
        Guid payoutRequestId,
        CancellationToken cancellationToken)
    {
        var snapshot = await _settlement.GetPayoutRequestAsync(payoutRequestId, cancellationToken);
        return snapshot is null || snapshot.SellerPartyId != sellerPartyId ? null : snapshot;
    }

    /// <summary>مانده همه فروشندگان (admin).</summary>
    public Task<IReadOnlyList<SettlementBalanceSnapshot>> ListAllBalancesAsync(CancellationToken cancellationToken) =>
        _settlement.ListAllBalancesAsync(cancellationToken);

    /// <summary>صف payout (admin).</summary>
    public Task<IReadOnlyList<PayoutRequestSnapshot>> ListPayoutQueueAsync(CancellationToken cancellationToken) =>
        _settlement.ListPayoutQueueAsync(cancellationToken);

    /// <summary>صفحه‌بندی server-side گرید payout Admin.</summary>
    public async Task<GridPageResponse<PayoutRequestSnapshot>> QueryPayoutGridAsync(
        GridQueryRequest request,
        CancellationToken cancellationToken)
    {
        var rows = await ListPayoutQueueAsync(cancellationToken);
        return AdminListGridPolicies.Payouts.Execute(rows, request);
    }

    /// <summary>payout را پردازش می‌کند (admin/dev).</summary>
    public Task<PayoutRequestSnapshot> ProcessPayoutAsync(
        Guid payoutRequestId,
        Guid actorUserId,
        CancellationToken cancellationToken) =>
        _settlement.ProcessPayoutAsync(new ProcessPayoutCommand(payoutRequestId, actorUserId), cancellationToken);

    /// <summary>payout را retry می‌کند (admin/dev).</summary>
    public Task<PayoutRequestSnapshot> RetryPayoutAsync(
        Guid payoutRequestId,
        Guid actorUserId,
        CancellationToken cancellationToken) =>
        _settlement.RetryPayoutAsync(new RetryPayoutCommand(payoutRequestId, actorUserId), cancellationToken);
}
