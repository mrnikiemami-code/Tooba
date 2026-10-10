using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Notification.Contracts.Commands;
using Tooba.Notification.Contracts.Copy;
using Tooba.Notification.Contracts.Dtos;
using Tooba.Notification.Contracts.Routes;
using Tooba.Wallet.Application.Refunds.Models;
using Tooba.Wallet.Contracts.Errors;
using Tooba.Wallet.Contracts.Refunds;
using Tooba.Wallet.Domain.Aggregates;
using Tooba.Wallet.Domain.ValueObjects;

namespace Tooba.Wallet.Infrastructure.Persistence;

/// <summary>Refund-credit capability: once-per-return credit into the wallet ledger.</summary>
public sealed partial class WalletDirectory
{
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
            throw new ContractOperationException(WalletErrorCodes.IdsRequired);
        if (amount <= 0)
            throw new ContractOperationException(WalletErrorCodes.AmountPositive);
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new ContractOperationException(WalletErrorCodes.IdempotencyRequired);

        var key = idempotencyKey.Trim();
        var expectedKey = $"wallet-refund-credit:{returnRequestId:D}";
        if (!string.Equals(key, expectedKey, StringComparison.Ordinal))
            throw new ContractOperationException(WalletErrorCodes.IdempotencyConflict);

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
                throw new ContractOperationException(WalletErrorCodes.IdempotencyConflict);
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
                throw new ContractOperationException(WalletErrorCodes.AccountNotMutable);
            if (!string.Equals(account.Currency, normalizedCurrency, StringComparison.Ordinal))
                throw new ContractOperationException(WalletErrorCodes.CurrencyMismatch);

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
}
