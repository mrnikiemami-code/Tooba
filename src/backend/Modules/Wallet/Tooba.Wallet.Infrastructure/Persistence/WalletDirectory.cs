using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Notification.Contracts.Ports;
using Tooba.Wallet.Application.Admin.Models;
using Tooba.Wallet.Application.Customer.Models;
using Tooba.Wallet.Application.Ports;
using Tooba.Wallet.Contracts.Payments;
using Tooba.Wallet.Contracts.Refunds;
using Tooba.Wallet.Domain.Aggregates;
using Tooba.Wallet.Domain.ValueObjects;

namespace Tooba.Wallet.Infrastructure.Persistence;

/// <summary>
/// پیاده‌سازی دایرکتوری کیف پول در schema wallet.
/// <para>
/// This type is deliberately split by capability across cohesive partial files so no single file
/// mixes customer, admin, order-payment and refund responsibilities (ARCH-MODULE-FILE-001): this
/// file holds the shared account/ledger core plus the mapping helpers, while
/// <c>WalletDirectory.Customer.cs</c>, <c>WalletDirectory.Admin.cs</c>,
/// <c>WalletDirectory.Payments.cs</c> and <c>WalletDirectory.Refunds.cs</c> hold the capability
/// use cases. Identity, DI registration and wire behavior are unchanged.
/// </para>
/// </summary>
public sealed partial class WalletDirectory : IWalletDirectory, IWalletOrderPaymentPort, IWalletRefundCreditPort
{
    private readonly WalletDbContext _db;
    private readonly INotificationCreationPort _notifications;
    private readonly IClock _clock;
    private readonly IIdGenerator _ids;

    /// <summary>دایرکتوری را می‌سازد.</summary>
    public WalletDirectory(WalletDbContext db, INotificationCreationPort notifications, IClock clock, IIdGenerator ids)
    {
        _db = db;
        _notifications = notifications;
        _clock = clock;
        _ids = ids;
    }

    private async Task<WalletAccount> EnsureAccountAsync(Guid customerActorUserId, CancellationToken cancellationToken)
    {
        var existing = await _db.Accounts.AsNoTracking()
            .SingleOrDefaultAsync(x => x.CustomerActorUserId == customerActorUserId, cancellationToken);
        if (existing is not null) return existing;
        return await EnsureAccountTrackedAsync(customerActorUserId, cancellationToken);
    }

    private async Task<WalletAccount> EnsureAccountTrackedAsync(Guid customerActorUserId, CancellationToken cancellationToken)
    {
        var existing = await _db.Accounts
            .SingleOrDefaultAsync(x => x.CustomerActorUserId == customerActorUserId, cancellationToken);
        if (existing is not null) return existing;

        var account = WalletAccount.Create(_ids.NewId(), customerActorUserId, WalletAccount.DefaultCurrency, _clock.UtcNow);
        _db.Accounts.Add(account);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            return account;
        }
        catch (DbUpdateException)
        {
            _db.Entry(account).State = EntityState.Detached;
            return await _db.Accounts.SingleAsync(x => x.CustomerActorUserId == customerActorUserId, cancellationToken);
        }
    }

    private async Task<WalletLedgerPageDto> ListLedgerAsync(
        WalletAccount account,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var p = Math.Max(1, page);
        var size = Math.Clamp(pageSize, 1, 100);
        var q = _db.LedgerEntries.AsNoTracking().Where(x => x.AccountId == account.AccountId);
        var total = await q.CountAsync(cancellationToken);
        var items = await q.OrderByDescending(x => x.CreatedAt)
            .Skip((p - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken);
        var balance = await DeriveBalanceAsync(account.AccountId, cancellationToken);
        return new WalletLedgerPageDto(items.Select(MapEntry).ToArray(), total, p, size, balance);
    }

    private async Task<WalletSummaryDto> BuildSummaryAsync(WalletAccount account, CancellationToken cancellationToken)
    {
        var entries = await _db.LedgerEntries.AsNoTracking()
            .Where(x => x.AccountId == account.AccountId)
            .Select(x => new { x.Direction, x.Amount })
            .ToListAsync(cancellationToken);
        var credits = entries.Where(x => x.Direction == LedgerDirection.Credit).Sum(x => x.Amount);
        var debits = entries.Where(x => x.Direction == LedgerDirection.Debit).Sum(x => x.Amount);
        return new WalletSummaryDto(
            account.AccountId,
            account.CustomerActorUserId,
            account.Currency,
            account.Status.ToString(),
            credits - debits,
            credits,
            debits,
            entries.Count,
            account.CreatedAt);
    }

    private async Task<decimal> DeriveBalanceAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var entries = await _db.LedgerEntries.AsNoTracking()
            .Where(x => x.AccountId == accountId)
            .Select(x => new { x.Direction, x.Amount })
            .ToListAsync(cancellationToken);
        return entries.Sum(x => x.Direction == LedgerDirection.Credit ? x.Amount : -x.Amount);
    }

    private static GiftCardSummaryDto MapSummary(GiftCard card, int redemptionCount) =>
        new(
            card.CardId,
            card.Currency,
            card.InitialAmount,
            card.RemainingAmount,
            card.Status.ToString(),
            card.IssuedAt,
            card.ExpiresAt,
            card.RecipientActorUserId,
            card.CreatedByActorUserId,
            redemptionCount);

    private static GiftCardDetailDto MapDetail(GiftCard card, IReadOnlyList<GiftCardRedemption> redemptions) =>
        new(
            card.CardId,
            card.Currency,
            card.InitialAmount,
            card.RemainingAmount,
            card.Status.ToString(),
            card.IssuedAt,
            card.ExpiresAt,
            card.RecipientActorUserId,
            card.CreatedByActorUserId,
            redemptions.Select(r => new GiftCardRedemptionDto(
                r.RedemptionId, r.CardId, r.AccountId, r.Amount, r.CreatedAt)).ToArray());

    private static WalletLedgerEntryDto MapEntry(WalletLedgerEntry entry) =>
        new(
            entry.EntryId,
            entry.AccountId,
            entry.Type.ToString(),
            entry.Amount,
            entry.Currency,
            entry.Direction.ToString(),
            entry.SourceType,
            entry.SourceId,
            entry.CreatedAt,
            entry.Metadata);
}
