using Tooba.BuildingBlocks;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tooba.Notification.Contracts.Commands;
using Tooba.Notification.Contracts.Copy;
using Tooba.Notification.Contracts.Dtos;
using Tooba.Notification.Contracts.Ports;
using Tooba.Notification.Contracts.Routes;
using Tooba.Wallet.Application.Models;
using Tooba.Wallet.Application.Ports;
using Tooba.Wallet.Contracts.Dtos;
using Tooba.Wallet.Contracts.Payments;
using Tooba.Wallet.Contracts.Refunds;
using Tooba.Wallet.Domain.Aggregates;
using Tooba.Wallet.Domain.ValueObjects;
using Tooba.Wallet.Infrastructure.Persistence;

namespace Tooba.Wallet.Infrastructure.Directories;

/// <summary>پیاده‌سازی دایرکتوری کیف پول در schema wallet.</summary>
public sealed class WalletDirectory : IWalletDirectory, IWalletOrderPaymentPort, IWalletRefundCreditPort
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
            throw new InvalidOperationException("wallet.rejected.SWRlbXBv");

        var existing = await _db.Redemptions.AsNoTracking()
            .SingleOrDefaultAsync(x => x.IdempotencyKey == command.IdempotencyKey.Trim(), cancellationToken);
        if (existing is not null)
        {
            var accountReplay = await _db.Accounts.AsNoTracking()
                .SingleAsync(x => x.AccountId == existing.AccountId, cancellationToken);
            if (accountReplay.CustomerActorUserId != customerActorUserId)
                throw new InvalidOperationException("wallet.rejected.2KjYp9iy");
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

        var now = _clock.UtcNow;
        var codeHash = GiftCard.HashCode(command.Code);
        var card = await _db.GiftCards.SingleOrDefaultAsync(x => x.CodeHash == codeHash, cancellationToken)
                   ?? throw new InvalidOperationException("wallet.rejected.2qnYryDa");
        card.EnsureRedeemable(now);
        if (!string.Equals(card.Currency, WalletAccount.DefaultCurrency, StringComparison.Ordinal))
            throw new InvalidOperationException("wallet.rejected.2KfYsdiy");

        var account = await EnsureAccountTrackedAsync(customerActorUserId, cancellationToken);
        if (!account.CanMutateLedger)
            throw new InvalidOperationException("wallet.rejected.2K3Ys9in");
        if (!string.Equals(account.Currency, card.Currency, StringComparison.Ordinal))
            throw new InvalidOperationException("wallet.rejected.2KfYsdiy");

        var amount = card.RemainingAmount;
        card.ApplyRedemption(amount, now);
        var redemption = GiftCardRedemption.Create(_ids.NewId(), card.CardId, account.AccountId, amount, command.IdempotencyKey, now);
        var entry = WalletLedgerEntry.PostGiftCardCredit(
            _ids.NewId(),
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
                NotificationSemanticTypes.WalletGiftCardRedeemed,
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
            throw new InvalidOperationException("wallet.rejected.SWRlbXBv");

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
                   ?? throw new InvalidOperationException("wallet.rejected.2qnYp9ix");
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
            ?? throw new InvalidOperationException("wallet.rejected.2K3Ys9in");
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
            throw new InvalidOperationException("wallet.rejected.SWRlbXBv");
        if (string.IsNullOrWhiteSpace(command.Reason) || command.Reason.Trim().Length > 500)
            throw new InvalidOperationException("wallet.rejected.2K_ZhNuM");

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
            throw new InvalidOperationException("wallet.rejected.2K3Ys9in");

        if (direction == LedgerDirection.Debit)
        {
            var balance = await DeriveBalanceAsync(account.AccountId, cancellationToken);
            if (command.Amount > balance)
                throw new InvalidOperationException("wallet.rejected.2YXZiNis");
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

    /// <inheritdoc />
    public async Task<WalletSpendResultDto> SpendForOrderPaymentAsync(
        Guid customerActorId,
        decimal amount,
        string currency,
        Guid paymentId,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (customerActorId == Guid.Empty || paymentId == Guid.Empty)
            throw new InvalidOperationException("wallet.rejected.2YfZiNuM");
        if (amount <= 0)
            throw new InvalidOperationException("wallet.rejected.2YXYqNmE");
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new InvalidOperationException("wallet.rejected.SWRlbXBv");

        var key = idempotencyKey.Trim();
        var normalizedCurrency = WalletAccount.NormalizeCurrency(currency);
        var existing = await _db.LedgerEntries.AsNoTracking()
            .SingleOrDefaultAsync(x => x.IdempotencyKey == key, cancellationToken);
        if (existing is not null)
        {
            if (existing.SourceId != paymentId
                || existing.Type != LedgerEntryType.OrderPaymentDebit
                || existing.Amount != decimal.Round(amount, 0, MidpointRounding.AwayFromZero)
                || !string.Equals(existing.Currency, normalizedCurrency, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("wallet.rejected.2qnZhNuM");
            }

            var balanceReplay = await DeriveBalanceAsync(existing.AccountId, cancellationToken);
            return new WalletSpendResultDto(MapEntry(existing), balanceReplay, IdempotentReplay: true);
        }

        await using var tx = await _db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable,
            cancellationToken);
        try
        {
            existing = await _db.LedgerEntries.AsNoTracking()
                .SingleOrDefaultAsync(x => x.IdempotencyKey == key, cancellationToken);
            if (existing is not null)
            {
                await tx.CommitAsync(cancellationToken);
                var balanceReplay = await DeriveBalanceAsync(existing.AccountId, cancellationToken);
                return new WalletSpendResultDto(MapEntry(existing), balanceReplay, IdempotentReplay: true);
            }

            var account = await EnsureAccountTrackedAsync(customerActorId, cancellationToken);
            if (!account.CanMutateLedger)
                throw new InvalidOperationException("wallet.rejected.2K3Ys9in");
            if (!string.Equals(account.Currency, normalizedCurrency, StringComparison.Ordinal))
                throw new InvalidOperationException("wallet.rejected.2KfYsdiy");

            var balance = await DeriveBalanceAsync(account.AccountId, cancellationToken);
            var rounded = decimal.Round(amount, 0, MidpointRounding.AwayFromZero);
            if (rounded > balance)
                throw new InvalidOperationException("wallet.rejected.2YXZiNis");

            var now = _clock.UtcNow;
            var entry = WalletLedgerEntry.PostOrderPaymentDebit(
                _ids.NewId(),
                account.AccountId,
                paymentId,
                rounded,
                normalizedCurrency,
                key,
                now,
                JsonSerializer.Serialize(new { reason = "order_payment_debit", paymentId }));
            _db.LedgerEntries.Add(entry);
            // لمس ردیف حساب برای قفل خوش‌بینانه/سریال در Serializable.
            _db.Entry(account).Property(x => x.Status).IsModified = true;
            await _db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            await _notifications.CreateIfAbsentAsync(
                new CreateNotificationCommand(
                    NotificationRecipientKind.Customer,
                    customerActorId,
                    customerActorId,
                    NotificationSemanticTypes.WalletPaymentSucceeded,
                    new { amount = entry.Amount, currency = entry.Currency, paymentId },
                    NotificationTargetRoutes.CustomerWallet(),
                    $"wallet.payment-succeeded:{paymentId:D}",
                    "wallet.payment.succeeded"),
                cancellationToken);

            var newBalance = await DeriveBalanceAsync(account.AccountId, cancellationToken);
            return new WalletSpendResultDto(MapEntry(entry), newBalance, IdempotentReplay: false);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    async Task<WalletOrderPaymentDebitResultDto> IWalletOrderPaymentPort.SpendForOrderPaymentAsync(
        Guid customerActorId,
        decimal amount,
        string currency,
        Guid paymentId,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await SpendForOrderPaymentAsync(
            customerActorId, amount, currency, paymentId, idempotencyKey, cancellationToken);
        return new WalletOrderPaymentDebitResultDto(result.Balance, result.IdempotentReplay);
    }

    /// <inheritdoc />
    public async Task<WalletCreditResultDto> CreditRefundAsync(
        Guid customerActorId,
        decimal amount,
        string currency,
        Guid returnRequestId,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (customerActorId == Guid.Empty || returnRequestId == Guid.Empty)
            throw new InvalidOperationException("wallet.rejected.2YfZiNuM");
        if (amount <= 0)
            throw new InvalidOperationException("wallet.rejected.2YXYqNmE");
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new InvalidOperationException("wallet.rejected.SWRlbXBv");

        var key = idempotencyKey.Trim();
        var expectedKey = $"wallet-refund-credit:{returnRequestId:D}";
        if (!string.Equals(key, expectedKey, StringComparison.Ordinal))
            throw new InvalidOperationException("wallet.rejected.2qnZhNuM");

        var normalizedCurrency = WalletAccount.NormalizeCurrency(currency);
        var existing = await _db.LedgerEntries.AsNoTracking()
            .SingleOrDefaultAsync(x => x.IdempotencyKey == key, cancellationToken);
        if (existing is not null)
        {
            if (existing.SourceId != returnRequestId
                || existing.Type != LedgerEntryType.RefundCredit
                || existing.Amount != decimal.Round(amount, 0, MidpointRounding.AwayFromZero)
                || !string.Equals(existing.Currency, normalizedCurrency, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("wallet.rejected.2qnZhNuM");
            }

            var balanceReplay = await DeriveBalanceAsync(existing.AccountId, cancellationToken);
            return new WalletCreditResultDto(MapEntry(existing), balanceReplay, IdempotentReplay: true);
        }

        await using var tx = await _db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable,
            cancellationToken);
        try
        {
            existing = await _db.LedgerEntries.AsNoTracking()
                .SingleOrDefaultAsync(x => x.IdempotencyKey == key, cancellationToken);
            if (existing is not null)
            {
                await tx.CommitAsync(cancellationToken);
                var balanceReplay = await DeriveBalanceAsync(existing.AccountId, cancellationToken);
                return new WalletCreditResultDto(MapEntry(existing), balanceReplay, IdempotentReplay: true);
            }

            var account = await EnsureAccountTrackedAsync(customerActorId, cancellationToken);
            if (!account.CanMutateLedger)
                throw new InvalidOperationException("wallet.rejected.2K3Ys9in");
            if (!string.Equals(account.Currency, normalizedCurrency, StringComparison.Ordinal))
                throw new InvalidOperationException("wallet.rejected.2KfYsdiy");

            var now = _clock.UtcNow;
            var rounded = decimal.Round(amount, 0, MidpointRounding.AwayFromZero);
            var entry = WalletLedgerEntry.PostRefundCredit(
                _ids.NewId(),
                account.AccountId,
                returnRequestId,
                rounded,
                normalizedCurrency,
                key,
                now,
                JsonSerializer.Serialize(new { reason = "refund_credit", returnRequestId }));
            _db.LedgerEntries.Add(entry);
            _db.Entry(account).Property(x => x.Status).IsModified = true;
            await _db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            await _notifications.CreateIfAbsentAsync(
                new CreateNotificationCommand(
                    NotificationRecipientKind.Customer,
                    customerActorId,
                    customerActorId,
                    NotificationSemanticTypes.WalletRefundCredited,
                    new { amount = entry.Amount, currency = entry.Currency, returnRequestId },
                    NotificationTargetRoutes.CustomerWallet(),
                    $"wallet.refund-credited:{returnRequestId:D}",
                    "wallet.refund.credited"),
                cancellationToken);

            var newBalance = await DeriveBalanceAsync(account.AccountId, cancellationToken);
            return new WalletCreditResultDto(MapEntry(entry), newBalance, IdempotentReplay: false);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    async Task<WalletRefundCreditResultDto> IWalletRefundCreditPort.CreditRefundAsync(
        Guid customerActorId,
        decimal amount,
        string currency,
        Guid returnRequestId,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await CreditRefundAsync(
            customerActorId, amount, currency, returnRequestId, idempotencyKey, cancellationToken);
        return new WalletRefundCreditResultDto(result.Balance, result.IdempotentReplay);
    }

    /// <inheritdoc />
    public async Task<WalletCheckoutQuoteDto> QuoteForPayableAsync(
        Guid customerActorId,
        decimal payableAmount,
        string currency,
        CancellationToken cancellationToken)
    {
        if (payableAmount < 0)
            throw new InvalidOperationException("wallet.rejected.2YXYqNmE");
        var normalizedCurrency = WalletAccount.NormalizeCurrency(currency);
        var summary = await GetOrCreateSummaryForCustomerAsync(customerActorId, cancellationToken);
        if (!string.Equals(summary.Currency, normalizedCurrency, StringComparison.Ordinal))
        {
            return new WalletCheckoutQuoteDto(
                summary.Balance,
                0m,
                payableAmount,
                false,
                normalizedCurrency);
        }

        var maxUsable = Math.Min(summary.Balance, payableAmount);
        if (summary.Status != nameof(WalletAccountStatus.Active))
            maxUsable = 0m;
        var remaining = payableAmount - maxUsable;
        return new WalletCheckoutQuoteDto(
            summary.Balance,
            maxUsable,
            remaining,
            payableAmount > 0 && remaining == 0 && maxUsable == payableAmount,
            normalizedCurrency);
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
