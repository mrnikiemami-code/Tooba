using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Notification.Contracts.Commands;
using Tooba.Notification.Contracts.Copy;
using Tooba.Notification.Contracts.Dtos;
using Tooba.Notification.Contracts.Routes;
using Tooba.Wallet.Application.Customer.Models;
using Tooba.Wallet.Contracts.Errors;
using Tooba.Wallet.Domain.Aggregates;
using Tooba.Wallet.Domain.ValueObjects;

namespace Tooba.Wallet.Infrastructure.Persistence;

/// <summary>Customer wallet capability: owner summary, owner ledger and gift-card redemption.</summary>
public sealed partial class WalletDirectory
{
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
            throw new ContractOperationException(WalletErrorCodes.IdempotencyRequired);

        var existing = await _db.Redemptions.AsNoTracking()
            .SingleOrDefaultAsync(x => x.IdempotencyKey == command.IdempotencyKey.Trim(), cancellationToken);
        if (existing is not null)
        {
            var accountReplay = await _db.Accounts.AsNoTracking()
                .SingleAsync(x => x.AccountId == existing.AccountId, cancellationToken);
            if (accountReplay.CustomerActorUserId != customerActorUserId)
                throw new ContractOperationException(WalletErrorCodes.RedemptionOwnerMismatch);
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
                   ?? throw new ContractOperationException(WalletErrorCodes.GiftCardCodeNotFound);
        card.EnsureRedeemable(now);
        if (!string.Equals(card.Currency, WalletAccount.DefaultCurrency, StringComparison.Ordinal))
            throw new ContractOperationException(WalletErrorCodes.CurrencyMismatch);

        var account = await EnsureAccountTrackedAsync(customerActorUserId, cancellationToken);
        if (!account.CanMutateLedger)
            throw new ContractOperationException(WalletErrorCodes.AccountNotMutable);
        if (!string.Equals(account.Currency, card.Currency, StringComparison.Ordinal))
            throw new ContractOperationException(WalletErrorCodes.CurrencyMismatch);

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
}
