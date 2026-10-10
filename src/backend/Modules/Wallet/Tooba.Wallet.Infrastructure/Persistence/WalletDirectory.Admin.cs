using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Notification.Contracts.Commands;
using Tooba.Notification.Contracts.Copy;
using Tooba.Notification.Contracts.Dtos;
using Tooba.Notification.Contracts.Routes;
using Tooba.Wallet.Application.Admin.Models;
using Tooba.Wallet.Application.Customer.Models;
using Tooba.Wallet.Application.Models;
using Tooba.Wallet.Contracts.Errors;
using Tooba.Wallet.Domain.Aggregates;
using Tooba.Wallet.Domain.ValueObjects;

namespace Tooba.Wallet.Infrastructure.Persistence;

/// <summary>Admin wallet capability: gift-card administration and immutable wallet adjustments.</summary>
public sealed partial class WalletDirectory
{
    /// <inheritdoc />
    public async Task<GiftCardListPageDto> ListGiftCardsForAdminAsync(
        AdminGiftCardListQuery query,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var status = WalletEnumParsing.TryParseGiftCardStatus(query.Status);
        IQueryable<GiftCard> q = _db.GiftCards.AsNoTracking();
        if (status is { } st)
            q = q.Where(x => x.Status == st);
        if (!string.IsNullOrWhiteSpace(query.Q) && Guid.TryParse(query.Q.Trim(), out var cardId))
            q = q.Where(x => x.CardId == cardId);

        var total = await q.CountAsync(cancellationToken);
        var cards = await q.OrderByDescending(x => x.IssuedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        var ids = cards.Select(c => c.CardId).ToArray();
        var counts = await _db.Redemptions.AsNoTracking()
            .Where(r => ids.Contains(r.CardId))
            .GroupBy(r => r.CardId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);

        var items = cards.Select(c => MapSummary(c, counts.GetValueOrDefault(c.CardId))).ToArray();
        return new GiftCardListPageDto(items, total, page, pageSize);
    }

    /// <inheritdoc />
    public async Task<GiftCardDetailDto?> GetGiftCardForAdminAsync(Guid cardId, CancellationToken cancellationToken)
    {
        var card = await _db.GiftCards.AsNoTracking()
            .SingleOrDefaultAsync(x => x.CardId == cardId, cancellationToken);
        if (card is null) return null;
        var redemptions = await _db.Redemptions.AsNoTracking()
            .Where(x => x.CardId == cardId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        return MapDetail(card, redemptions);
    }

    /// <inheritdoc />
    public async Task<GiftCardIssueResultDto> IssueGiftCardForAdminAsync(
        Guid adminActorUserId,
        IssueGiftCardCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
            throw new ContractOperationException(WalletErrorCodes.IdempotencyRequired);

        var existing = await _db.GiftCards.AsNoTracking()
            .SingleOrDefaultAsync(x => x.IdempotencyKey == command.IdempotencyKey.Trim(), cancellationToken);
        if (existing is not null)
        {
            var count = await _db.Redemptions.AsNoTracking().CountAsync(x => x.CardId == existing.CardId, cancellationToken);
            return new GiftCardIssueResultDto(MapSummary(existing, count), DisplayCode: string.Empty, IdempotentReplay: true);
        }

        var now = _clock.UtcNow;
        var (card, display) = GiftCard.Issue(
            _ids.NewId(),
            command.InitialAmount,
            string.IsNullOrWhiteSpace(command.Currency) ? WalletAccount.DefaultCurrency : command.Currency!,
            adminActorUserId,
            command.IdempotencyKey,
            now,
            command.ExpiresAt,
            command.RecipientActorUserId);
        _db.GiftCards.Add(card);
        await _db.SaveChangesAsync(cancellationToken);
        return new GiftCardIssueResultDto(MapSummary(card, 0), display, IdempotentReplay: false);
    }

    /// <inheritdoc />
    public async Task<GiftCardDetailDto> RevokeGiftCardForAdminAsync(Guid cardId, CancellationToken cancellationToken)
    {
        var card = await _db.GiftCards.SingleOrDefaultAsync(x => x.CardId == cardId, cancellationToken)
                   ?? throw new ContractOperationException(WalletErrorCodes.GiftCardNotFound);
        card.Revoke(_clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        var redemptions = await _db.Redemptions.AsNoTracking()
            .Where(x => x.CardId == cardId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        return MapDetail(card, redemptions);
    }

    /// <inheritdoc />
    public async Task<WalletSummaryDto?> GetWalletForAdminAsync(Guid customerActorUserId, CancellationToken cancellationToken)
    {
        var account = await _db.Accounts.AsNoTracking()
            .SingleOrDefaultAsync(x => x.CustomerActorUserId == customerActorUserId, cancellationToken);
        return account is null ? null : await BuildSummaryAsync(account, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WalletLedgerPageDto> ListLedgerForAdminAsync(
        Guid customerActorUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var account = await _db.Accounts.AsNoTracking()
            .SingleOrDefaultAsync(x => x.CustomerActorUserId == customerActorUserId, cancellationToken)
            ?? throw new ContractOperationException(WalletErrorCodes.AccountNotFound);
        return await ListLedgerAsync(account, page, pageSize, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AdminWalletAdjustmentResultDto> AdjustWalletForAdminAsync(
        Guid customerActorUserId,
        Guid adminActorUserId,
        AdminWalletAdjustmentCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
            throw new ContractOperationException(WalletErrorCodes.IdempotencyRequired);
        if (string.IsNullOrWhiteSpace(command.Reason) || command.Reason.Trim().Length > 500)
            throw new ContractOperationException(WalletErrorCodes.AdjustmentReasonInvalid);

        var existing = await _db.LedgerEntries.AsNoTracking()
            .SingleOrDefaultAsync(x => x.IdempotencyKey == command.IdempotencyKey.Trim(), cancellationToken);
        if (existing is not null)
        {
            var balanceReplay = await DeriveBalanceAsync(existing.AccountId, cancellationToken);
            return new AdminWalletAdjustmentResultDto(MapEntry(existing), balanceReplay, IdempotentReplay: true);
        }

        var direction = WalletEnumParsing.ParseDirection(command.Direction);
        var now = _clock.UtcNow;
        var account = await EnsureAccountTrackedAsync(customerActorUserId, cancellationToken);
        if (!account.CanMutateLedger)
            throw new ContractOperationException(WalletErrorCodes.AccountNotMutable);

        if (direction == LedgerDirection.Debit)
        {
            var balance = await DeriveBalanceAsync(account.AccountId, cancellationToken);
            if (command.Amount > balance)
                throw new ContractOperationException(WalletErrorCodes.BalanceInsufficient);
        }

        var adjustmentId = _ids.NewId();
        var entry = WalletLedgerEntry.PostAdminAdjustment(
            _ids.NewId(),
            account.AccountId,
            adjustmentId,
            command.Amount,
            account.Currency,
            direction,
            command.IdempotencyKey,
            now,
            JsonSerializer.Serialize(new { reason = command.Reason.Trim(), adminActorUserId }));
        _db.LedgerEntries.Add(entry);
        await _db.SaveChangesAsync(cancellationToken);

        await _notifications.CreateIfAbsentAsync(
            new CreateNotificationCommand(
                NotificationRecipientKind.Customer,
                customerActorUserId,
                customerActorUserId,
                NotificationSemanticTypes.WalletAdminAdjustment,
                new { amount = entry.Amount, direction = entry.Direction.ToString(), currency = entry.Currency },
                NotificationTargetRoutes.CustomerWallet(),
                $"wallet.admin-adjust:{entry.EntryId:D}",
                "wallet.admin_adjustment"),
            cancellationToken);

        var newBalance = await DeriveBalanceAsync(account.AccountId, cancellationToken);
        return new AdminWalletAdjustmentResultDto(MapEntry(entry), newBalance, IdempotentReplay: false);
    }
}
