using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tooba.Notification.Application;
using Tooba.Notification.Domain;
using Tooba.Wallet.Application;
using Tooba.Wallet.Domain;
using Tooba.Wallet.Infrastructure.Persistence;

namespace Tooba.Wallet.Infrastructure;

/// <summary>پیاده‌سازی دایرکتوری کیف پول در schema wallet.</summary>
public sealed class WalletDirectory : IWalletDirectory
{
    private readonly WalletDbContext _db;
    private readonly INotificationDirectory _notifications;

    /// <summary>دایرکتوری را می‌سازد.</summary>
    public WalletDirectory(WalletDbContext db, INotificationDirectory notifications)
    {
        _db = db;
        _notifications = notifications;
    }

    /// <inheritdoc />
    public async Task<WalletSummaryDto> GetOrCreateSummaryForCustomerAsync(
        Guid customerActorUserId,
        CancellationToken cancellationToken)
    {
        var account = await EnsureAccountAsync(customerActorUserId, cancellationToken);
        return await BuildSummaryAsync(account, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WalletLedgerPageDto> ListLedgerForCustomerAsync(
        Guid customerActorUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var account = await EnsureAccountAsync(customerActorUserId, cancellationToken);
        return await ListLedgerAsync(account, page, pageSize, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<GiftCardRedeemResultDto> RedeemGiftCardForCustomerAsync(
        Guid customerActorUserId,
        RedeemGiftCardCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
            throw new InvalidOperationException("IdempotencyKey الزامی است.");

        var existing = await _db.Redemptions.AsNoTracking()
            .SingleOrDefaultAsync(x => x.IdempotencyKey == command.IdempotencyKey.Trim(), cancellationToken);
        if (existing is not null)
        {
            var accountReplay = await _db.Accounts.AsNoTracking()
                .SingleAsync(x => x.AccountId == existing.AccountId, cancellationToken);
            if (accountReplay.CustomerActorUserId != customerActorUserId)
                throw new InvalidOperationException("بازخرید متعلق به مشتری دیگری است.");
            var cardReplay = await _db.GiftCards.AsNoTracking()
                .SingleAsync(x => x.CardId == existing.CardId, cancellationToken);
            var balanceReplay = await DeriveBalanceAsync(accountReplay.AccountId, cancellationToken);
            return new GiftCardRedeemResultDto(
                existing.RedemptionId,
                existing.CardId,
                existing.AccountId,
                existing.Amount,
                balanceReplay,
                cardReplay.Status.ToString(),
                cardReplay.RemainingAmount,
                IdempotentReplay: true);
        }

        var now = DateTimeOffset.UtcNow;
        var codeHash = GiftCard.HashCode(command.Code);
        var card = await _db.GiftCards.SingleOrDefaultAsync(x => x.CodeHash == codeHash, cancellationToken)
                   ?? throw new InvalidOperationException("کد کارت هدیه نامعتبر است.");
        card.EnsureRedeemable(now);
        if (!string.Equals(card.Currency, WalletAccount.DefaultCurrency, StringComparison.Ordinal))
            throw new InvalidOperationException("ارز کارت با کیف پول سازگار نیست.");

        var account = await EnsureAccountTrackedAsync(customerActorUserId, cancellationToken);
        if (!account.CanMutateLedger)
            throw new InvalidOperationException("حساب کیف پول مسدود است.");
        if (!string.Equals(account.Currency, card.Currency, StringComparison.Ordinal))
            throw new InvalidOperationException("ارز حساب با کارت سازگار نیست.");

        var amount = card.RemainingAmount;
        card.ApplyRedemption(amount, now);
        var redemption = GiftCardRedemption.Create(card.CardId, account.AccountId, amount, command.IdempotencyKey, now);
        var entry = WalletLedgerEntry.PostGiftCardCredit(
            account.AccountId,
            card.CardId,
            amount,
            card.Currency,
            $"gift-redeem:{redemption.RedemptionId:D}",
            now,
            JsonSerializer.Serialize(new { reason = "gift_card_redeem", cardId = card.CardId }));

        _db.Redemptions.Add(redemption);
        _db.LedgerEntries.Add(entry);
        await _db.SaveChangesAsync(cancellationToken);

        await _notifications.CreateIfAbsentAsync(
            new CreateNotificationCommand(
                NotificationRecipientKind.Customer,
                customerActorUserId,
                customerActorUserId,
                NotificationCopy.WalletGiftCardRedeemed,
                new { amount, currency = card.Currency, cardId = card.CardId },
                NotificationTargetRoutes.CustomerWallet(),
                $"wallet.gift-redeem:{redemption.RedemptionId:D}",
                "wallet.gift_card.redeemed"),
            cancellationToken);

        var balance = await DeriveBalanceAsync(account.AccountId, cancellationToken);
        return new GiftCardRedeemResultDto(
            redemption.RedemptionId,
            card.CardId,
            account.AccountId,
            amount,
            balance,
            card.Status.ToString(),
            card.RemainingAmount,
            IdempotentReplay: false);
    }

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
            throw new InvalidOperationException("IdempotencyKey الزامی است.");

        var existing = await _db.GiftCards.AsNoTracking()
            .SingleOrDefaultAsync(x => x.IdempotencyKey == command.IdempotencyKey.Trim(), cancellationToken);
        if (existing is not null)
        {
            var count = await _db.Redemptions.AsNoTracking().CountAsync(x => x.CardId == existing.CardId, cancellationToken);
            return new GiftCardIssueResultDto(MapSummary(existing, count), DisplayCode: string.Empty, IdempotentReplay: true);
        }

        var now = DateTimeOffset.UtcNow;
        var (card, display) = GiftCard.Issue(
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
                   ?? throw new InvalidOperationException("کارت پیدا نشد.");
        card.Revoke(DateTimeOffset.UtcNow);
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
            ?? throw new InvalidOperationException("حساب کیف پول پیدا نشد.");
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
            throw new InvalidOperationException("IdempotencyKey الزامی است.");
        if (string.IsNullOrWhiteSpace(command.Reason) || command.Reason.Trim().Length > 500)
            throw new InvalidOperationException("دلیل تعدیل الزامی است.");

        var existing = await _db.LedgerEntries.AsNoTracking()
            .SingleOrDefaultAsync(x => x.IdempotencyKey == command.IdempotencyKey.Trim(), cancellationToken);
        if (existing is not null)
        {
            var balanceReplay = await DeriveBalanceAsync(existing.AccountId, cancellationToken);
            return new AdminWalletAdjustmentResultDto(MapEntry(existing), balanceReplay, IdempotentReplay: true);
        }

        var direction = WalletEnumParsing.ParseDirection(command.Direction);
        var now = DateTimeOffset.UtcNow;
        var account = await EnsureAccountTrackedAsync(customerActorUserId, cancellationToken);
        if (!account.CanMutateLedger)
            throw new InvalidOperationException("حساب کیف پول مسدود است.");

        if (direction == LedgerDirection.Debit)
        {
            var balance = await DeriveBalanceAsync(account.AccountId, cancellationToken);
            if (command.Amount > balance)
                throw new InvalidOperationException("موجودی برای تعدیل بدهکار کافی نیست.");
        }

        var adjustmentId = Guid.NewGuid();
        var entry = WalletLedgerEntry.PostAdminAdjustment(
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
                NotificationCopy.WalletAdminAdjustment,
                new { amount = entry.Amount, direction = entry.Direction.ToString(), currency = entry.Currency },
                NotificationTargetRoutes.CustomerWallet(),
                $"wallet.admin-adjust:{entry.EntryId:D}",
                "wallet.admin_adjustment"),
            cancellationToken);

        var newBalance = await DeriveBalanceAsync(account.AccountId, cancellationToken);
        return new AdminWalletAdjustmentResultDto(MapEntry(entry), newBalance, IdempotentReplay: false);
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

        var account = WalletAccount.Create(customerActorUserId, WalletAccount.DefaultCurrency, DateTimeOffset.UtcNow);
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
