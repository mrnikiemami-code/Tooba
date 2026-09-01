using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks.Grid;
using Tooba.Host.Admin;
using Tooba.Host.Grid;
using Tooba.Party.Infrastructure.Persistence;
using Tooba.Settlement.Application;
using Tooba.Settlement.Infrastructure.Persistence;

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
    private readonly PartyDbContext _parties;
    private readonly AdminPayoutGridQueryEngine _payoutGrid;

    /// <summary>سازندهٔ ترکیب تسویه.</summary>
    public SettlementPanelComposer(
        ISettlementDirectory settlement,
        SettlementDbContext db,
        PartyDbContext parties)
    {
        _settlement = settlement;
        _parties = parties;
        _payoutGrid = new AdminPayoutGridQueryEngine(db, parties);
    }

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
    public async Task<IReadOnlyList<AdminSettlementBalanceListItem>> ListAllBalancesAsync(
        CancellationToken cancellationToken)
    {
        var balances = await _settlement.ListAllBalancesAsync(cancellationToken);
        if (balances.Count == 0)
        {
            return [];
        }

        var sellerIds = balances.Select(x => x.SellerPartyId).Distinct().ToList();
        var sellerNames = await LoadSellerDisplayNamesAsync(sellerIds, cancellationToken);
        return balances.Select(balance =>
        {
            sellerNames.TryGetValue(balance.SellerPartyId, out var displayName);
            return new AdminSettlementBalanceListItem(
                balance.SettlementAccountId,
                balance.SellerPartyId,
                displayName ?? "فروشنده",
                balance.Currency,
                balance.PostedCredits,
                balance.PostedDebits,
                balance.ReservedPayouts,
                balance.AvailableBalance);
        }).ToList();
    }

    /// <summary>صف payout (admin).</summary>
    public Task<IReadOnlyList<PayoutRequestSnapshot>> ListPayoutQueueAsync(CancellationToken cancellationToken) =>
        _settlement.ListPayoutQueueAsync(cancellationToken);

    /// <summary>صفحه‌بندی server-side گرید payout Admin (DB-native).</summary>
    public Task<GridPageResponse<AdminPayoutListItem>> QueryPayoutGridAsync(
        GridQueryRequest request,
        CancellationToken cancellationToken)
    {
        var q = AdminListGridPolicies.Payouts.Normalize(request);
        return _payoutGrid.QueryAsync(q, cancellationToken);
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

    private async Task<IReadOnlyDictionary<Guid, string>> LoadSellerDisplayNamesAsync(
        IReadOnlyCollection<Guid> sellerIds,
        CancellationToken cancellationToken)
    {
        if (sellerIds.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var sellerRows = await _parties.Parties.AsNoTracking()
            .Where(x => sellerIds.Contains(x.PartyId))
            .Select(x => new { x.PartyId, x.DisplayName })
            .ToListAsync(cancellationToken);
        return sellerRows.ToDictionary(x => x.PartyId, x => x.DisplayName);
    }
}
